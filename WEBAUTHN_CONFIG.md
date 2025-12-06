# WebAuthn Configuration Guide

## Overview

LifeOS uses WebAuthn (FIDO2) for passwordless authentication via biometrics (Face ID, Touch ID, Windows Hello). The WebAuthn configuration must match the domain where the application is deployed.

## Problem

The error "The RP ID 'localhost' is invalid for this domain" occurs when the WebAuthn configuration is set to `localhost` but the application is accessed from a different domain (e.g., `lifeos.frostaura.net`).

## Solution

### Environment Variables

Configure the following environment variables based on your deployment:

#### For Production (lifeos.frostaura.net)

```bash
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net
```

#### For Local Development

```bash
export FIDO2_SERVER_DOMAIN=localhost
export FIDO2_ORIGIN=http://localhost:5173
export CORS_ORIGIN_0=http://localhost:5173
```

### Using .env File

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your production values:
   ```bash
   FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
   FIDO2_ORIGIN=https://lifeos.frostaura.net
   CORS_ORIGIN_0=https://lifeos.frostaura.net
   ```

3. Start the services:
   ```bash
   docker compose up -d
   ```

### Configuration Details

The Fido2 configuration includes:

- **ServerDomain**: The domain (RP ID) that WebAuthn credentials are scoped to. Must match the domain in the browser URL (without protocol).
  - Local: `localhost`
  - Production: `lifeos.frostaura.net`

- **Origins**: List of allowed origins that can make WebAuthn requests. Must include the protocol.
  - Local: `http://localhost:5173`
  - Production: `https://lifeos.frostaura.net`

- **ServerName**: Human-readable name shown during authentication (e.g., "LifeOS")

- **TimestampDriftTolerance**: Allowed time drift in milliseconds (default: 300000 = 5 minutes)

## Testing

### Test Authentication Endpoint

```bash
# Check if the backend is configured correctly
curl http://localhost:5001/health

# Test the passkey login begin endpoint
curl -X POST http://localhost:5001/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" \
  -d '{}'
```

### Test in Browser

1. Navigate to your LifeOS URL
2. Click "Sign in with Biometrics"
3. If configured correctly, you should see your device's biometric prompt
4. If you see the "RP ID invalid" error, verify your environment variables match your domain

## WebAuthn Requirements

- **HTTPS**: Production deployments MUST use HTTPS (except localhost)
- **Valid Domain**: The domain must be publicly resolvable or `localhost`
- **Browser Support**: Modern browsers with WebAuthn support (Chrome, Firefox, Safari, Edge)
- **Authenticator**: Device with biometric capability (Face ID, Touch ID, Windows Hello, etc.)

## Troubleshooting

### Error: "The RP ID 'X' is invalid for this domain"

**Cause**: Mismatch between configured ServerDomain and the actual domain in the browser URL.

**Solution**: 
1. Check your current domain in the browser
2. Set `FIDO2_SERVER_DOMAIN` to match (without protocol)
3. Restart the backend container

### Error: "Origin 'X' is not allowed"

**Cause**: The origin (protocol + domain + port) is not in the allowed Origins list.

**Solution**:
1. Set `FIDO2_ORIGIN` to match your frontend URL (with protocol)
2. Set `CORS_ORIGIN_0` to match as well
3. Restart the backend container

### Error: "This device does not support biometric authentication"

**Cause**: Browser or device doesn't support WebAuthn or biometrics.

**Solution**:
- Use a supported browser (Chrome, Firefox, Safari, Edge)
- Ensure your device has biometric hardware (fingerprint, face recognition, etc.)
- Check browser settings to ensure biometric access is allowed

## Production Deployment Checklist

- [ ] Set `FIDO2_SERVER_DOMAIN` to production domain (e.g., `lifeos.frostaura.net`)
- [ ] Set `FIDO2_ORIGIN` to production URL with HTTPS (e.g., `https://lifeos.frostaura.net`)
- [ ] Set `CORS_ORIGIN_0` to production URL with HTTPS
- [ ] Ensure SSL/TLS certificate is valid and active
- [ ] Test biometric login on production domain
- [ ] Verify no CORS errors in browser console
- [ ] Test on multiple devices and browsers
