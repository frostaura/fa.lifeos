# Production Deployment Quick Start

## Problem Fixed
The WebAuthn (biometric authentication) was broken in production with error:
**"The RP ID 'localhost' is invalid for this domain"**

## Solution
WebAuthn configuration is now environment-aware and can be configured via environment variables.

## For Production Deployment (lifeos.frostaura.net)

### Option 1: Using .env file
```bash
# Create .env file in project root
cat > .env << 'EOF'
FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
FIDO2_ORIGIN=https://lifeos.frostaura.net
CORS_ORIGIN_0=https://lifeos.frostaura.net
CORS_ORIGIN_1=https://lifeos.frostaura.net
EOF

# Deploy
docker compose up -d
```

### Option 2: Using environment variables
```bash
# Set environment variables
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# Deploy
docker compose up -d
```

### Option 3: Docker compose with inline env vars
```bash
FIDO2_SERVER_DOMAIN=lifeos.frostaura.net \
FIDO2_ORIGIN=https://lifeos.frostaura.net \
CORS_ORIGIN_0=https://lifeos.frostaura.net \
docker compose up -d
```

## Verification

### Quick Check
```bash
# Run the verification script
./scripts/verify-webauthn-config.sh

# Or manually test
curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}'

# Response should contain: "rpId":"lifeos.frostaura.net"
```

### Browser Test
1. Navigate to https://lifeos.frostaura.net
2. Click "Sign in with Biometrics"
3. **Should NOT see** "RP ID invalid" error
4. **Should see** biometric prompt (Face ID / Touch ID / Windows Hello)

## Important Notes

- ⚠️ **HTTPS Required**: Production deployments MUST use HTTPS (except localhost)
- ⚠️ **Domain Match**: `FIDO2_SERVER_DOMAIN` must match the domain in browser URL (without protocol)
- ⚠️ **Origin Match**: `FIDO2_ORIGIN` must match the full URL (with protocol)
- ⚠️ **Restart Required**: After changing environment variables, restart the backend:
  ```bash
  docker compose restart backend
  ```

## For Other Domains

If deploying to a different domain (e.g., `mylifeos.com`):
```bash
export FIDO2_SERVER_DOMAIN=mylifeos.com
export FIDO2_ORIGIN=https://mylifeos.com
export CORS_ORIGIN_0=https://mylifeos.com
```

## Local Development (Default)

No changes needed for local development - defaults to localhost:
```bash
# Just start normally
docker compose up -d
```

Defaults:
- `FIDO2_SERVER_DOMAIN=localhost`
- `FIDO2_ORIGIN=http://localhost:5173`
- `CORS_ORIGIN_0=http://localhost:5173`

## Troubleshooting

### Error: "RP ID 'localhost' is invalid for this domain"
**Fix:** Set `FIDO2_SERVER_DOMAIN` to your actual domain
```bash
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
docker compose restart backend
```

### Error: "Origin not allowed"
**Fix:** Set `FIDO2_ORIGIN` and `CORS_ORIGIN_0` to your frontend URL
```bash
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net
docker compose restart backend
```

### Changes not taking effect
**Fix:** Ensure containers are recreated after env var changes
```bash
docker compose down
docker compose up -d
```

## Resources

- [WEBAUTHN_CONFIG.md](./WEBAUTHN_CONFIG.md) - Detailed configuration guide
- [WEBAUTHN_FIX_GUIDE.md](./WEBAUTHN_FIX_GUIDE.md) - Visual guide showing the fix
- [TEST_PLAN.md](./TEST_PLAN.md) - Complete testing procedures
- [.env.example](./.env.example) - Environment variable template
