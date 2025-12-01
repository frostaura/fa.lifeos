using Fido2NetLib;
using Fido2NetLib.Objects;
using LifeOS.Api.Configuration;
using LifeOS.Domain.Entities;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/auth/passkey")]
public class PasskeyController : ControllerBase
{
    private readonly LifeOSDbContext _context;
    private readonly IFido2 _fido2;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        LifeOSDbContext context,
        IFido2 fido2,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        IDistributedCache cache,
        ILogger<PasskeyController> logger)
    {
        _context = context;
        _fido2 = fido2;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Begin passkey registration for a new user
    /// </summary>
    [HttpPost("register/begin")]
    [AllowAnonymous]
    public async Task<IActionResult> BeginRegistration([FromBody] BeginRegistrationRequest request)
    {
        // Check if user exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            // Check if they already have a passkey
            var hasPasskey = await _context.WebAuthnCredentials.AnyAsync(c => c.UserId == existingUser.Id);
            if (hasPasskey)
            {
                return BadRequest(new { error = new { code = "ALREADY_REGISTERED", message = "This email already has a passkey registered" } });
            }
        }

        // Create user handle
        var userHandle = Guid.NewGuid().ToByteArray();
        
        var user = new Fido2User
        {
            Id = userHandle,
            Name = request.Email,
            DisplayName = request.DisplayName ?? request.Email.Split('@')[0]
        };

        // Get existing credentials for this user (if upgrading)
        var existingCredentials = existingUser != null
            ? await _context.WebAuthnCredentials
                .Where(c => c.UserId == existingUser.Id)
                .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
                .ToListAsync()
            : new List<PublicKeyCredentialDescriptor>();

        var authenticatorSelection = new AuthenticatorSelection
        {
            AuthenticatorAttachment = AuthenticatorAttachment.Platform,  // Prefer platform (biometric)
            UserVerification = UserVerificationRequirement.Required
        };

        var options = _fido2.RequestNewCredential(
            user, 
            existingCredentials, 
            authenticatorSelection, 
            AttestationConveyancePreference.None);

        // Store in cache using challenge as key (challenge is unique per request)
        // Use base64url encoding to match what the client will send back
        var challengeKey = Base64UrlEncode(options.Challenge);
        _logger.LogInformation("Storing registration with challenge key: {ChallengeKey}", challengeKey);
        
        var cacheData = new RegistrationCacheData
        {
            OptionsJson = options.ToJson(),
            Email = request.Email,
            DisplayName = request.DisplayName ?? request.Email.Split('@')[0]
        };
        
        await _cache.SetStringAsync(
            $"fido2:register:{challengeKey}", 
            JsonSerializer.Serialize(cacheData),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        // Return Fido2's properly serialized JSON
        return Content(options.ToJson(), "application/json");
    }

    /// <summary>
    /// Complete passkey registration
    /// </summary>
    [HttpPost("register/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRegistration([FromBody] JsonDocument requestBody)
    {
        try
        {
            // Parse the SimpleWebAuthn response format
            var root = requestBody.RootElement;
            
            var id = root.GetProperty("id").GetString()!;
            var rawId = root.GetProperty("rawId").GetString()!;
            var responseObj = root.GetProperty("response");
            var clientDataJSON = responseObj.GetProperty("clientDataJSON").GetString()!;
            var attestationObject = responseObj.GetProperty("attestationObject").GetString()!;
            
            _logger.LogInformation("Received registration complete - id: {Id}", id);
            
            // Convert base64url to bytes
            var rawIdBytes = Base64UrlDecode(rawId);
            var clientDataJSONBytes = Base64UrlDecode(clientDataJSON);
            var attestationObjectBytes = Base64UrlDecode(attestationObject);
            
            // Build the Fido2 attestation response
            var attestationResponse = new AuthenticatorAttestationRawResponse
            {
                Id = rawIdBytes,
                RawId = rawIdBytes,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    ClientDataJson = clientDataJSONBytes,
                    AttestationObject = attestationObjectBytes
                },
                Type = PublicKeyCredentialType.PublicKey
            };
            
            // Extract challenge from clientDataJSON
            var clientDataJsonStr = Encoding.UTF8.GetString(clientDataJSONBytes);
            _logger.LogInformation("ClientDataJson: {ClientDataJson}", clientDataJsonStr);
            
            var clientData = JsonSerializer.Deserialize<ClientDataJson>(clientDataJsonStr);
            
            if (clientData?.Challenge == null)
            {
                _logger.LogError("Challenge is null in clientData");
                return BadRequest(new { error = new { code = "INVALID_RESPONSE", message = "Invalid attestation response - no challenge" } });
            }

            _logger.LogInformation("Challenge from client: {Challenge}", clientData.Challenge);
            
            var cacheKey = $"fido2:register:{clientData.Challenge}";
            var cachedDataJson = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(cachedDataJson))
            {
                _logger.LogError("Cache miss for key: {CacheKey}", cacheKey);
                return BadRequest(new { error = new { code = "SESSION_EXPIRED", message = "Registration session expired. Please start again." } });
            }

        var cacheData = JsonSerializer.Deserialize<RegistrationCacheData>(cachedDataJson);
        if (cacheData == null)
        {
            return BadRequest(new { error = new { code = "SESSION_EXPIRED", message = "Registration session expired. Please start again." } });
        }

        var options = CredentialCreateOptions.FromJson(cacheData.OptionsJson);

        // Verify the attestation
        var result = await _fido2.MakeNewCredentialAsync(attestationResponse, options, async (args, cancellation) =>
        {
            // Check if credential ID already exists
            var exists = await _context.WebAuthnCredentials.AnyAsync(c => c.CredentialId == args.CredentialId, cancellation);
            return !exists;
        });

        if (result.Result == null)
        {
            return BadRequest(new { error = new { code = "VERIFICATION_FAILED", message = "Failed to verify passkey" } });
        }

        // Create or get user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == cacheData.Email);
        var isNewUser = user == null;
        if (user == null)
        {
            user = new User
            {
                Email = cacheData.Email,
                Username = cacheData.DisplayName,
                PasswordHash = string.Empty  // No password - biometric only
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Seed default SA tax profile for new user
            await SeedDefaultTaxProfileForUserAsync(user.Id);
        }

        // Store the credential
        var credential = new WebAuthnCredential
        {
            UserId = user.Id,
            CredentialId = result.Result.CredentialId,
            PublicKey = result.Result.PublicKey,
            UserHandle = result.Result.User.Id,
            SignatureCounter = result.Result.Counter,
            CredType = result.Result.CredType,
            AaGuid = result.Result.Aaguid,
            DeviceName = GetDeviceName(Request),
            RegisteredAt = DateTime.UtcNow
        };

        _context.WebAuthnCredentials.Add(credential);
        await _context.SaveChangesAsync();

        // Remove from cache
        await _cache.RemoveAsync(cacheKey);

        _logger.LogInformation("Passkey registered for user: {Email}", cacheData.Email);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

        Response.Cookies.Append("lifeos_refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(_jwtSettings.RefreshTokenDays)
        });

        return Ok(new
        {
            data = new
            {
                accessToken,
                expiresIn = _jwtSettings.AccessTokenMinutes * 60,
                tokenType = "Bearer",
                user = new { id = user.Id, email = user.Email }
            }
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration");
            return BadRequest(new { error = new { code = "REGISTRATION_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Begin passkey authentication
    /// </summary>
    [HttpPost("login/begin")]
    [AllowAnonymous]
    public async Task<IActionResult> BeginLogin([FromBody] BeginLoginRequest? request)
    {
        // For discoverable credentials, we don't need to specify allowed credentials
        var options = _fido2.GetAssertionOptions(
            new List<PublicKeyCredentialDescriptor>(),  // Empty for discoverable credentials
            UserVerificationRequirement.Required);

        // Store in cache using challenge as key
        var challengeKey = Base64UrlEncode(options.Challenge);
        await _cache.SetStringAsync(
            $"fido2:login:{challengeKey}", 
            options.ToJson(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        // Return Fido2's properly serialized JSON
        return Content(options.ToJson(), "application/json");
    }

    /// <summary>
    /// Complete passkey authentication
    /// </summary>
    [HttpPost("login/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteLogin([FromBody] JsonDocument requestBody)
    {
        try
        {
            // Parse the SimpleWebAuthn response format
            var root = requestBody.RootElement;
            
            var id = root.GetProperty("id").GetString()!;
            var rawId = root.GetProperty("rawId").GetString()!;
            var responseObj = root.GetProperty("response");
            var clientDataJSON = responseObj.GetProperty("clientDataJSON").GetString()!;
            var authenticatorData = responseObj.GetProperty("authenticatorData").GetString()!;
            var signature = responseObj.GetProperty("signature").GetString()!;
            var userHandle = responseObj.TryGetProperty("userHandle", out var uh) ? uh.GetString() : null;
            
            // Convert base64url to bytes
            var rawIdBytes = Base64UrlDecode(rawId);
            var clientDataJSONBytes = Base64UrlDecode(clientDataJSON);
            var authenticatorDataBytes = Base64UrlDecode(authenticatorData);
            var signatureBytes = Base64UrlDecode(signature);
            var userHandleBytes = userHandle != null ? Base64UrlDecode(userHandle) : null;
            
            // Build the Fido2 assertion response
            var assertionResponse = new AuthenticatorAssertionRawResponse
            {
                Id = rawIdBytes,
                RawId = rawIdBytes,
                Response = new AuthenticatorAssertionRawResponse.AssertionResponse
                {
                    ClientDataJson = clientDataJSONBytes,
                    AuthenticatorData = authenticatorDataBytes,
                    Signature = signatureBytes,
                    UserHandle = userHandleBytes
                },
                Type = PublicKeyCredentialType.PublicKey
            };
            
            // Extract challenge from clientDataJSON
            var clientDataJsonStr = Encoding.UTF8.GetString(clientDataJSONBytes);
            var clientData = JsonSerializer.Deserialize<ClientDataJson>(clientDataJsonStr);
            
            if (clientData?.Challenge == null)
            {
                return BadRequest(new { error = new { code = "INVALID_RESPONSE", message = "Invalid assertion response" } });
            }

            var cacheKey = $"fido2:login:{clientData.Challenge}";
            var optionsJson = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(optionsJson))
            {
                return BadRequest(new { error = new { code = "SESSION_EXPIRED", message = "Login session expired. Please start again." } });
            }

            var options = AssertionOptions.FromJson(optionsJson);

            // Find the credential
            var credential = await _context.WebAuthnCredentials
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CredentialId == rawIdBytes);

            if (credential == null)
            {
                return Unauthorized(new { error = new { code = "CREDENTIAL_NOT_FOUND", message = "Passkey not found" } });
            }

            // Verify the assertion
            var result = await _fido2.MakeAssertionAsync(
                assertionResponse,
                options,
                credential.PublicKey,
                credential.SignatureCounter,
                async (args, cancellation) =>
                {
                    // Verify user handle matches
                    return credential.UserHandle.SequenceEqual(args.UserHandle);
                });

            if (result.Status != "ok")
            {
                return Unauthorized(new { error = new { code = "VERIFICATION_FAILED", message = "Failed to verify passkey" } });
            }

            // Update counter
            credential.SignatureCounter = result.Counter;
            credential.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

        // Remove from cache
        await _cache.RemoveAsync(cacheKey);

        var user = credential.User;
        _logger.LogInformation("User logged in with passkey: {Email}", user.Email);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

        Response.Cookies.Append("lifeos_refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(_jwtSettings.RefreshTokenDays)
        });

        return Ok(new
        {
            data = new
            {
                accessToken,
                expiresIn = _jwtSettings.AccessTokenMinutes * 60,
                tokenType = "Bearer",
                user = new { id = user.Id, email = user.Email }
            }
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey login");
            return BadRequest(new { error = new { code = "LOGIN_ERROR", message = ex.Message } });
        }
    }

    private string GetDeviceName(HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString();
        if (userAgent.Contains("Mac")) return "Mac";
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("iPhone")) return "iPhone";
        if (userAgent.Contains("iPad")) return "iPad";
        if (userAgent.Contains("Android")) return "Android";
        return "Unknown Device";
    }
    
    private async Task SeedDefaultTaxProfileForUserAsync(Guid userId)
    {
        // South African 2024/2025 tax brackets
        var saTaxBrackets = @"[
            {""min"": 0, ""max"": 237100, ""rate"": 0.18, ""baseTax"": 0},
            {""min"": 237101, ""max"": 370500, ""rate"": 0.26, ""baseTax"": 42678},
            {""min"": 370501, ""max"": 512800, ""rate"": 0.31, ""baseTax"": 77362},
            {""min"": 512801, ""max"": 673000, ""rate"": 0.36, ""baseTax"": 121475},
            {""min"": 673001, ""max"": 857900, ""rate"": 0.39, ""baseTax"": 179147},
            {""min"": 857901, ""max"": 1817000, ""rate"": 0.41, ""baseTax"": 251258},
            {""min"": 1817001, ""max"": null, ""rate"": 0.45, ""baseTax"": 644489}
        ]";

        var taxRebates = @"{
            ""primary"": 17235,
            ""secondary"": 9444,
            ""tertiary"": 3145
        }";

        var taxProfile = new TaxProfile
        {
            UserId = userId,
            Name = "SA Tax 2024/2025",
            TaxYear = 2024,
            CountryCode = "ZA",
            Brackets = saTaxBrackets,
            UifRate = 0.01m,
            UifCap = 177.12m, // Monthly contribution cap (R17,712 income ceiling × 1%)
            VatRate = 0.15m,
            IsVatRegistered = false,
            TaxRebates = taxRebates,
            IsActive = true
        };

        _context.TaxProfiles.Add(taxProfile);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created default SA tax profile for user {UserId}", userId);
    }
    
    private static byte[] Base64UrlDecode(string base64Url)
    {
        string padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');
        
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        
        return Convert.FromBase64String(padded);
    }
    
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

public class BeginRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class BeginLoginRequest
{
    // Optional - for non-discoverable credentials
    public string? Email { get; set; }
}

public class RegistrationCacheData
{
    public string OptionsJson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class ClientDataJson
{
    [System.Text.Json.Serialization.JsonPropertyName("challenge")]
    public string? Challenge { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("origin")]
    public string? Origin { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }
}
