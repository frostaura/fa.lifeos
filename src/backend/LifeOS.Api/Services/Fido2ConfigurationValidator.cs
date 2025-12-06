using LifeOS.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LifeOS.Api.Services;

/// <summary>
/// Validates FIDO2 configuration at application startup
/// </summary>
public class Fido2ConfigurationValidator : IHostedService
{
    private readonly ILogger<Fido2ConfigurationValidator> _logger;
    private readonly Fido2Settings _fido2Settings;
    private readonly IWebHostEnvironment _environment;

    public Fido2ConfigurationValidator(
        ILogger<Fido2ConfigurationValidator> logger,
        IOptions<Fido2Settings> fido2Settings,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _fido2Settings = fido2Settings.Value;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating FIDO2 configuration...");
        
        var warnings = new List<string>();
        var errors = new List<string>();

        // Validate ServerDomain
        if (string.IsNullOrWhiteSpace(_fido2Settings.ServerDomain))
        {
            errors.Add("FIDO2 ServerDomain is not configured");
        }
        else if (_fido2Settings.ServerDomain.Contains("http://") || _fido2Settings.ServerDomain.Contains("https://"))
        {
            errors.Add("FIDO2 ServerDomain should not include protocol (http:// or https://)");
        }
        
        // Validate Origins
        if (_fido2Settings.Origins == null || _fido2Settings.Origins.Length == 0)
        {
            errors.Add("FIDO2 Origins list is empty");
        }
        else
        {
            foreach (var origin in _fido2Settings.Origins)
            {
                if (!origin.StartsWith("http://") && !origin.StartsWith("https://"))
                {
                    warnings.Add($"FIDO2 Origin '{origin}' should include protocol (http:// or https://)");
                }
            }
        }

        // Production-specific checks
        if (_environment.IsProduction())
        {
            if (_fido2Settings.ServerDomain == "localhost")
            {
                errors.Add("CRITICAL: FIDO2 ServerDomain is 'localhost' in production environment. " +
                          "Set FIDO2_SERVER_DOMAIN environment variable to your production domain (e.g., lifeos.frostaura.net)");
            }

            if (_fido2Settings.Origins?.Any(o => o.Contains("localhost")) == true)
            {
                warnings.Add("FIDO2 Origins contain 'localhost' in production environment");
            }

            if (_fido2Settings.Origins?.Any(o => o.StartsWith("http://") && !o.Contains("localhost")) == true)
            {
                warnings.Add("FIDO2 Origins contain HTTP (non-secure) URLs in production. Use HTTPS for security.");
            }
        }

        // Log results
        _logger.LogInformation("FIDO2 Configuration:");
        _logger.LogInformation("  ServerDomain: {ServerDomain}", _fido2Settings.ServerDomain);
        _logger.LogInformation("  ServerName: {ServerName}", _fido2Settings.ServerName);
        _logger.LogInformation("  Origins: {Origins}", _fido2Settings.Origins != null ? string.Join(", ", _fido2Settings.Origins) : "none");
        _logger.LogInformation("  Environment: {Environment}", _environment.EnvironmentName);

        foreach (var warning in warnings)
        {
            _logger.LogWarning("FIDO2 Configuration Warning: {Warning}", warning);
        }

        foreach (var error in errors)
        {
            _logger.LogError("FIDO2 Configuration Error: {Error}", error);
        }

        if (errors.Any())
        {
            _logger.LogError("FIDO2 Configuration has {Count} error(s). WebAuthn authentication may not work correctly.", errors.Count);
            _logger.LogError("See WEBAUTHN_CONFIG.md for configuration instructions: https://github.com/frostaura/fa.lifeos/blob/main/WEBAUTHN_CONFIG.md");
        }
        else if (warnings.Any())
        {
            _logger.LogWarning("FIDO2 Configuration has {Count} warning(s).", warnings.Count);
        }
        else
        {
            _logger.LogInformation("FIDO2 Configuration validation passed ✓");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
