#!/bin/bash

# WebAuthn Configuration Verification Script
# This script helps verify that the WebAuthn configuration is correct for your environment

echo "=================================="
echo "WebAuthn Configuration Verifier"
echo "=================================="
echo ""

# Check if backend is running
BACKEND_URL="${BACKEND_URL:-http://localhost:5001}"
echo "Testing backend at: $BACKEND_URL"
echo ""

# Test health endpoint
echo "1. Testing health endpoint..."
HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" "$BACKEND_URL/health" 2>&1)
HEALTH_STATUS=$(echo "$HEALTH_RESPONSE" | tail -n1)

if [ "$HEALTH_STATUS" = "200" ]; then
    echo "   ✓ Backend is healthy"
    echo "   Response: $(echo "$HEALTH_RESPONSE" | head -n1)"
else
    echo "   ✗ Backend health check failed (Status: $HEALTH_STATUS)"
    echo "   Make sure the backend is running at $BACKEND_URL"
    exit 1
fi

echo ""

# Test passkey login begin endpoint
echo "2. Testing WebAuthn configuration..."
PASSKEY_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$BACKEND_URL/api/auth/passkey/login/begin" 2>&1)
PASSKEY_STATUS=$(echo "$PASSKEY_RESPONSE" | tail -n1)
PASSKEY_BODY=$(echo "$PASSKEY_RESPONSE" | head -n -1)

if [ "$PASSKEY_STATUS" = "200" ]; then
    echo "   ✓ WebAuthn endpoint is responding"
    
    # Parse the rpId from the response
    RP_ID=$(echo "$PASSKEY_BODY" | grep -o '"rpId":"[^"]*"' | cut -d'"' -f4)
    
    if [ -n "$RP_ID" ]; then
        echo "   ✓ RP ID (Server Domain): $RP_ID"
    else
        echo "   ⚠ Could not extract RP ID from response"
    fi
else
    echo "   ✗ WebAuthn endpoint failed (Status: $PASSKEY_STATUS)"
    echo "   Response: $PASSKEY_BODY"
fi

echo ""

# Show environment variables if set
echo "3. Environment Configuration:"
if [ -n "$FIDO2_SERVER_DOMAIN" ]; then
    echo "   FIDO2_SERVER_DOMAIN = $FIDO2_SERVER_DOMAIN"
else
    echo "   FIDO2_SERVER_DOMAIN = (not set, using default: localhost)"
fi

if [ -n "$FIDO2_ORIGIN" ]; then
    echo "   FIDO2_ORIGIN = $FIDO2_ORIGIN"
else
    echo "   FIDO2_ORIGIN = (not set, using default: http://localhost:5173)"
fi

echo ""

# Provide recommendations
echo "4. Deployment Checklist:"
echo ""

# Detect the current domain
CURRENT_DOMAIN=$(echo "$BACKEND_URL" | sed -E 's|^https?://([^/:]+).*|\1|')

if [ "$CURRENT_DOMAIN" = "localhost" ] || [ "$CURRENT_DOMAIN" = "127.0.0.1" ]; then
    echo "   ℹ You are testing locally"
    echo "   ✓ For local development:"
    echo "     export FIDO2_SERVER_DOMAIN=localhost"
    echo "     export FIDO2_ORIGIN=http://localhost:5173"
else
    echo "   ℹ You appear to be testing a remote deployment"
    echo "   ✓ For production ($CURRENT_DOMAIN):"
    echo "     export FIDO2_SERVER_DOMAIN=$CURRENT_DOMAIN"
    
    # Determine protocol
    if [[ "$BACKEND_URL" == https* ]]; then
        echo "     export FIDO2_ORIGIN=https://$CURRENT_DOMAIN"
    else
        echo "     ⚠ WARNING: Production should use HTTPS!"
        echo "     export FIDO2_ORIGIN=https://$CURRENT_DOMAIN"
    fi
fi

echo ""
echo "5. Common Issues:"
echo "   - If you see 'RP ID invalid for this domain':"
echo "     → Set FIDO2_SERVER_DOMAIN to match your domain (without protocol)"
echo "   - If you see 'Origin not allowed':"
echo "     → Set FIDO2_ORIGIN to match your frontend URL (with protocol)"
echo "   - Production MUST use HTTPS (except localhost)"
echo ""

echo "=================================="
echo "Verification Complete"
echo "=================================="
