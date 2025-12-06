# WebAuthn Configuration Fix - Visual Guide

## The Problem

### Before the Fix (Broken Production)

```
┌──────────────────────────────────────────────────────────────┐
│  Browser: https://lifeos.frostaura.net                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Login Page                                            │  │
│  │                                                        │  │
│  │  [Sign in with Biometrics] ← User clicks              │  │
│  │                                                        │  │
│  │  ❌ Error: "The RP ID 'localhost' is invalid          │  │
│  │           for this domain"                            │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            ↓
                      API Request
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Backend API (LifeOS.Api)                                     │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Program.cs - FIDO2 Configuration                     │  │
│  │                                                        │  │
│  │  ❌ HARDCODED:                                         │  │
│  │     ServerDomain = "localhost"                        │  │
│  │     Origins = ["http://localhost:5173"]               │  │
│  │                                                        │  │
│  │  Problem: Backend tells browser to use "localhost"    │  │
│  │  but browser is at "lifeos.frostaura.net"             │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘

Domain Mismatch:
  Browser Domain: lifeos.frostaura.net
  FIDO2 RP ID:    localhost
  Result:         ❌ AUTHENTICATION FAILS
```

### After the Fix (Working Production)

```
┌──────────────────────────────────────────────────────────────┐
│  Browser: https://lifeos.frostaura.net                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Login Page                                            │  │
│  │                                                        │  │
│  │  [Sign in with Biometrics] ← User clicks              │  │
│  │                                                        │  │
│  │  ✅ Face ID / Touch ID prompt appears                 │  │
│  │  ✅ User authenticated successfully!                  │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            ↓
                      API Request
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Backend API (LifeOS.Api)                                     │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Program.cs - FIDO2 Configuration                     │  │
│  │                                                        │  │
│  │  ✅ READS FROM ENVIRONMENT:                            │  │
│  │     ServerDomain = env.FIDO2_SERVER_DOMAIN            │  │
│  │                  = "lifeos.frostaura.net"             │  │
│  │     Origins = [env.FIDO2_ORIGIN]                      │  │
│  │             = ["https://lifeos.frostaura.net"]        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  docker-compose.yml:                                          │
│    FIDO2_SERVER_DOMAIN=lifeos.frostaura.net                  │
│    FIDO2_ORIGIN=https://lifeos.frostaura.net                 │
└──────────────────────────────────────────────────────────────┘

Domain Match:
  Browser Domain: lifeos.frostaura.net
  FIDO2 RP ID:    lifeos.frostaura.net
  Result:         ✅ AUTHENTICATION SUCCEEDS
```

## Configuration Flow

```
┌─────────────────────────────────────────────────────────────┐
│  1. Environment Variables (.env or system)                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  FIDO2_SERVER_DOMAIN=lifeos.frostaura.net             │  │
│  │  FIDO2_ORIGIN=https://lifeos.frostaura.net            │  │
│  │  CORS_ORIGIN_0=https://lifeos.frostaura.net           │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  2. Docker Compose (docker-compose.yml)                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  backend:                                             │  │
│  │    environment:                                       │  │
│  │      - Fido2__ServerDomain=${FIDO2_SERVER_DOMAIN}    │  │
│  │      - Fido2__Origins__0=${FIDO2_ORIGIN}              │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  3. Backend Configuration (appsettings.json + env vars)     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  "Fido2": {                                           │  │
│  │    "ServerDomain": "localhost",  ← Default            │  │
│  │    "Origins": ["http://localhost:5173"]               │  │
│  │  }                                                    │  │
│  │                                                       │  │
│  │  Environment variables override defaults!             │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  4. Program.cs - FIDO2 Service Registration                 │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  var fido2Settings = new Fido2Settings();            │  │
│  │  configuration.GetSection("Fido2").Bind(settings);    │  │
│  │                                                       │  │
│  │  builder.Services.AddFido2(options => {               │  │
│  │    options.ServerDomain = settings.ServerDomain;      │  │
│  │    options.Origins = settings.Origins;                │  │
│  │  });                                                  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  5. Runtime - WebAuthn API Responses                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  POST /api/auth/passkey/login/begin                   │  │
│  │  Response: {                                          │  │
│  │    "rpId": "lifeos.frostaura.net",  ← Matches domain │  │
│  │    "challenge": "...",                                │  │
│  │    ...                                                │  │
│  │  }                                                    │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## File Changes Summary

```
Files Modified:
  ✅ src/backend/LifeOS.Api/Program.cs
     - Changed from hardcoded "localhost" to configuration-based
     - Now reads from Fido2Settings configuration object

  ✅ src/backend/LifeOS.Api/appsettings.json
     - Added "Fido2" configuration section with defaults
     - Provides base configuration for local development

  ✅ docker-compose.yml
     - Added Fido2__ServerDomain environment variable
     - Added Fido2__Origins__0 environment variable
     - Uses ${VARIABLE:-default} syntax for flexibility

Files Created:
  ✅ src/backend/LifeOS.Api/Configuration/Fido2Settings.cs
     - New configuration class for FIDO2 settings
     - Includes ServerDomain, ServerName, Origins, TimestampDriftTolerance

  ✅ .env.example
     - Template for production environment variables
     - Shows how to configure for both dev and prod

  ✅ WEBAUTHN_CONFIG.md
     - Comprehensive guide for WebAuthn configuration
     - Troubleshooting section for common issues

  ✅ scripts/verify-webauthn-config.sh
     - Automated verification script
     - Tests backend configuration and provides recommendations

  ✅ TEST_PLAN.md
     - Complete test scenarios for local and production
     - Manual testing procedures
```

## Deployment Checklist

### Local Development
```bash
# ✅ No changes needed - uses defaults
docker compose up -d
```

### Production Deployment
```bash
# 1. Set environment variables
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# 2. Deploy with updated configuration
docker compose up -d

# 3. Verify configuration
./scripts/verify-webauthn-config.sh

# 4. Test in browser
# Navigate to https://lifeos.frostaura.net
# Click "Sign in with Biometrics"
# Should work without "RP ID invalid" error
```

## Expected Outcomes

### ✅ Success Indicators
- No "RP ID invalid" error in browser
- Biometric prompt appears when clicking "Sign in with Biometrics"
- Users can register and login with Face ID / Touch ID / Windows Hello
- WebAuthn API responses contain correct domain in "rpId" field

### ❌ Failure Indicators (if not configured correctly)
- Error: "The RP ID 'localhost' is invalid for this domain"
- Error: "Origin 'X' is not allowed"
- CORS errors in browser console
- Biometric prompt never appears

## Testing the Fix

### Quick Test (curl)
```bash
# Test the WebAuthn endpoint
curl -X POST http://localhost:5001/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}'

# Check the response for "rpId" field
# Should match your domain (localhost or lifeos.frostaura.net)
```

### Full Browser Test
1. Open browser to your LifeOS URL
2. Open Developer Console (F12)
3. Click "Sign in with Biometrics"
4. Check console for errors
5. Verify biometric prompt appears
6. Complete authentication
7. Verify successful login

## Support

For detailed configuration instructions, see:
- [WEBAUTHN_CONFIG.md](./WEBAUTHN_CONFIG.md) - Complete configuration guide
- [TEST_PLAN.md](./TEST_PLAN.md) - Testing procedures
- [.env.example](./.env.example) - Environment variable template
