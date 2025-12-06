# WebAuthn Troubleshooting Guide

This guide helps diagnose and fix WebAuthn (biometric authentication) issues in LifeOS.

## Quick Diagnostics

### 1. Check Backend Configuration

```bash
# Check if backend is running
curl http://localhost:5001/health

# Check FIDO2 configuration status
curl http://localhost:5001/api/auth/passkey/config | jq .
```

Expected response for production:
```json
{
  "status": "healthy",
  "configuration": {
    "serverDomain": "lifeos.frostaura.net",
    "origins": ["https://lifeos.frostaura.net", ...]
  },
  "request": {
    "origin": "https://lifeos.frostaura.net",
    "isProduction": true
  },
  "warnings": []
}
```

### 2. Check Backend Logs

```bash
# View backend logs
docker logs lifeos-backend

# Look for FIDO2 configuration validation on startup
docker logs lifeos-backend 2>&1 | grep FIDO2
```

Expected log output:
```
info: LifeOS.Api.Services.Fido2ConfigurationValidator[0]
      Validating FIDO2 configuration...
info: LifeOS.Api.Services.Fido2ConfigurationValidator[0]
      FIDO2 Configuration:
info: LifeOS.Api.Services.Fido2ConfigurationValidator[0]
        ServerDomain: lifeos.frostaura.net
info: LifeOS.Api.Services.Fido2ConfigurationValidator[0]
        Origins: https://lifeos.frostaura.net, ...
info: LifeOS.Api.Services.Fido2ConfigurationValidator[0]
      FIDO2 Configuration validation passed ✓
```

## Common Issues and Solutions

### Issue 1: "Failed to start authentication"

**Symptoms:**
- Clicking "Sign in with Biometrics" shows error immediately
- No biometric prompt appears
- Browser console shows 400 Bad Request from `/api/auth/passkey/login/begin`

**Diagnosis:**
```bash
# Test the endpoint directly
curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}' | jq .
```

**Common Causes:**

#### A. ServerDomain set to 'localhost' in production

**Error response:**
```json
{
  "error": {
    "code": "LOGIN_BEGIN_ERROR",
    "message": "WebAuthn configuration error: The server domain doesn't match your browser URL...",
    "origin": "https://lifeos.frostaura.net"
  }
}
```

**Fix:**
```bash
# Set FIDO2_SERVER_DOMAIN environment variable
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net

# Or use docker-compose.prod.yml
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Or update docker-compose.yml environment section
# Fido2__ServerDomain=lifeos.frostaura.net
```

**Verification:**
```bash
# Check config endpoint
curl https://lifeos.frostaura.net/api/auth/passkey/config | jq .configuration.serverDomain
# Should output: "lifeos.frostaura.net"
```

#### B. Origin not in allowed list

**Error response:**
```json
{
  "error": {
    "code": "LOGIN_BEGIN_ERROR",
    "message": "WebAuthn origin mismatch: The request origin is not in the allowed origins list...",
    "origin": "https://lifeos.frostaura.net"
  }
}
```

**Fix:**
```bash
# Set FIDO2_ORIGIN environment variable
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# Restart backend
docker compose restart backend
```

### Issue 2: "The RP ID 'localhost' is invalid for this domain"

**Symptoms:**
- Browser shows this error in console
- Error appears after biometric prompt
- In the browser WebAuthn API

**This means the backend is STILL configured with localhost.**

**Fix:**
```bash
# Stop containers
docker compose down

# Set environment variables
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net

# Restart with new configuration
docker compose up -d

# Verify
docker logs lifeos-backend 2>&1 | grep "ServerDomain"
```

### Issue 3: Configuration Changes Not Taking Effect

**Symptoms:**
- Environment variables set but old configuration still active
- Backend logs show old values

**Causes:**
- Docker containers not recreated after env var changes
- Environment variables only read on container startup

**Fix:**
```bash
# Stop and remove containers
docker compose down

# Verify environment variables are set
echo $FIDO2_SERVER_DOMAIN
echo $FIDO2_ORIGIN

# Start with fresh containers
docker compose up -d

# Check logs to verify new configuration
docker logs lifeos-backend 2>&1 | grep "FIDO2 Configuration:"
```

### Issue 4: HTTP vs HTTPS Mismatch

**Symptoms:**
- Mixed content warnings in browser console
- CORS errors
- WebAuthn fails silently

**Fix:**
Production MUST use HTTPS for all origins:
```bash
# Wrong
export FIDO2_ORIGIN=http://lifeos.frostaura.net

# Correct
export FIDO2_ORIGIN=https://lifeos.frostaura.net
```

### Issue 5: Session/Cookie Issues

**Symptoms:**
- "Session expired" errors
- Registration works but login fails

**Causes:**
- Session cookies not being set correctly
- CORS misconfiguration
- Cookie SameSite settings

**Fix:**
```bash
# Ensure CORS is configured for your origin
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# Check browser cookies (F12 > Application > Cookies)
# Should see cookies with domain matching your URL
```

## Step-by-Step Deployment Checklist

### For lifeos.frostaura.net Production

1. **Set Environment Variables**
   ```bash
   export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
   export FIDO2_ORIGIN=https://lifeos.frostaura.net
   export FIDO2_ORIGIN_1=https://lifeos.frostaura.net
   export FIDO2_ORIGIN_2=https://lifeos.frostaura.net
   export CORS_ORIGIN_0=https://lifeos.frostaura.net
   export CORS_ORIGIN_1=https://lifeos.frostaura.net
   ```

2. **Deploy**
   ```bash
   # Option A: Using docker-compose.prod.yml
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   
   # Option B: Using environment variables
   docker compose up -d
   ```

3. **Verify Deployment**
   ```bash
   # Check health
   curl https://lifeos.frostaura.net/health
   
   # Check FIDO2 config
   curl https://lifeos.frostaura.net/api/auth/passkey/config | jq .
   
   # Check logs
   docker logs lifeos-backend 2>&1 | grep "FIDO2 Configuration:"
   ```

4. **Test WebAuthn**
   - Open https://lifeos.frostaura.net in browser
   - Open Developer Console (F12)
   - Click "Sign in with Biometrics"
   - Should see biometric prompt
   - No errors in console

## Advanced Diagnostics

### Inspect WebAuthn API Response

```bash
# Get assertion options
curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}' | jq .

# Expected response should contain:
# - "rpId": "lifeos.frostaura.net"  # Must match browser domain
# - "challenge": "..."              # Base64 encoded challenge
# - "timeout": 60000                # 60 second timeout
```

### Check CORS Headers

```bash
curl -X OPTIONS https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Origin: https://lifeos.frostaura.net" \
  -H "Access-Control-Request-Method: POST" \
  -v 2>&1 | grep "Access-Control"

# Should see:
# Access-Control-Allow-Origin: https://lifeos.frostaura.net
# Access-Control-Allow-Credentials: true
```

### Verify Docker Environment

```bash
# Check environment variables in running container
docker exec lifeos-backend env | grep FIDO2
docker exec lifeos-backend env | grep CORS

# Expected output:
# Fido2__ServerDomain=lifeos.frostaura.net
# Fido2__Origins__0=https://lifeos.frostaura.net
# Cors__AllowedOrigins__0=https://lifeos.frostaura.net
```

## Browser-Specific Issues

### Safari

- Requires HTTPS in production
- May cache WebAuthn errors - try clearing browser cache
- Check "Develop" > "Show Web Inspector" > Console for errors

### Chrome

- Open DevTools > Application > Cookies to verify cookies are set
- Check Console for WebAuthn errors
- May require "Secure context" (HTTPS) warning acknowledgment

### Firefox

- Check "Privacy & Security" settings
- May block third-party cookies affecting session
- Console errors usually very descriptive

## Getting Help

If you're still experiencing issues:

1. **Collect diagnostic information:**
   ```bash
   # Get backend logs
   docker logs lifeos-backend > backend.log
   
   # Get configuration
   curl https://lifeos.frostaura.net/api/auth/passkey/config > config.json
   
   # Get test endpoint response
   curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
     -H "Content-Type: application/json" \
     -d '{}' > login-begin.json
   ```

2. **Check browser console** for errors (F12)

3. **Review documentation:**
   - [WEBAUTHN_CONFIG.md](./WEBAUTHN_CONFIG.md)
   - [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)

4. **Create GitHub issue** with:
   - Error messages
   - Browser console logs
   - Backend logs
   - Configuration (from `/api/auth/passkey/config`)
   - Environment (production domain, browser, OS)
