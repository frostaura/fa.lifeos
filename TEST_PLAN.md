# WebAuthn Configuration Test Plan

## Test Scenarios

### Scenario 1: Local Development (localhost)
**Configuration:**
```bash
FIDO2_SERVER_DOMAIN=localhost
FIDO2_ORIGIN=http://localhost:5173
CORS_ORIGIN_0=http://localhost:5173
```

**Expected Behavior:**
- WebAuthn RP ID should be "localhost"
- Origins should include http://localhost:5173
- Browser accessing http://localhost:5173 should be able to use biometric auth

**Test Commands:**
```bash
# Set environment variables
export FIDO2_SERVER_DOMAIN=localhost
export FIDO2_ORIGIN=http://localhost:5173
export CORS_ORIGIN_0=http://localhost:5173

# Start services
docker compose up -d

# Test health endpoint
curl http://localhost:5001/health

# Test WebAuthn configuration
curl -X POST http://localhost:5001/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}'

# Expected response should contain "rpId":"localhost"
```

### Scenario 2: Production (lifeos.frostaura.net)
**Configuration:**
```bash
FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
FIDO2_ORIGIN=https://lifeos.frostaura.net
CORS_ORIGIN_0=https://lifeos.frostaura.net
```

**Expected Behavior:**
- WebAuthn RP ID should be "lifeos.frostaura.net"
- Origins should include https://lifeos.frostaura.net
- Browser accessing https://lifeos.frostaura.net should be able to use biometric auth
- NO "RP ID invalid" error should occur

**Test Commands:**
```bash
# Set environment variables
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# Start services (in production environment)
docker compose up -d

# Test health endpoint
curl https://lifeos.frostaura.net/api/health

# Test WebAuthn configuration
curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}'

# Expected response should contain "rpId":"lifeos.frostaura.net"
```

## Manual Browser Testing

### Steps to Test Biometric Auth:
1. Navigate to the application URL (localhost:5173 or lifeos.frostaura.net)
2. Click "Sign in with Biometrics" button
3. Verify NO error appears (no "RP ID invalid" message)
4. Device biometric prompt should appear (Face ID, Touch ID, Windows Hello)
5. Complete biometric authentication
6. Should be successfully logged in

### Steps to Test Registration:
1. Navigate to the application URL
2. Click "Register with Passkey"
3. Enter email address
4. Click register button
5. Verify NO error appears
6. Device biometric prompt should appear for registration
7. Complete biometric enrollment
8. Should be successfully registered and logged in

## Expected API Responses

### Health Check Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T12:00:00Z"
}
```

### WebAuthn Begin Login Response (localhost):
```json
{
  "challenge": "...",
  "rpId": "localhost",
  "timeout": 60000,
  "userVerification": "required",
  ...
}
```

### WebAuthn Begin Login Response (production):
```json
{
  "challenge": "...",
  "rpId": "lifeos.frostaura.net",
  "timeout": 60000,
  "userVerification": "required",
  ...
}
```

## Validation Checklist

- [ ] Configuration files updated (appsettings.json, docker-compose.yml)
- [ ] Fido2Settings class created with proper properties
- [ ] Program.cs updated to read from configuration
- [ ] Environment variables supported for production override
- [ ] .env.example file created with documentation
- [ ] WEBAUTHN_CONFIG.md documentation created
- [ ] Verification script created and tested
- [ ] Local development test passes
- [ ] Production configuration documented
- [ ] No hardcoded localhost references remain in Fido2 config

## Common Issues and Solutions

### Issue 1: "The RP ID 'localhost' is invalid for this domain"
**Cause:** FIDO2_SERVER_DOMAIN environment variable not set for production
**Solution:** Set FIDO2_SERVER_DOMAIN=lifeos.frostaura.net

### Issue 2: "Origin 'https://lifeos.frostaura.net' is not allowed"
**Cause:** FIDO2_ORIGIN not set for production
**Solution:** Set FIDO2_ORIGIN=https://lifeos.frostaura.net

### Issue 3: CORS errors in browser console
**Cause:** CORS_ORIGIN_0 not set for production
**Solution:** Set CORS_ORIGIN_0=https://lifeos.frostaura.net

### Issue 4: Configuration not taking effect
**Cause:** Services not restarted after environment variable changes
**Solution:** Run `docker compose restart backend` or `docker compose up -d --force-recreate`
