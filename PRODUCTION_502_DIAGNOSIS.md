# Production 502 Error - Diagnostic Steps

## Issue
Getting 502 (Bad Gateway) error when trying to access `/api/auth/passkey/login/begin` in production.

## What a 502 Error Means
- The reverse proxy (nginx/traefik/etc.) can reach the backend container
- BUT the backend is either:
  - Not responding
  - Crashed/not running
  - Responding with an error that the proxy doesn't understand

## Immediate Diagnostic Steps

### 1. Check if Backend Container is Running
```bash
docker ps | grep lifeos-backend
```

**Expected**: Container should be "Up" and healthy
**If not running**: Container crashed during startup

### 2. Check Backend Logs
```bash
# Get recent logs
docker logs lifeos-backend --tail 100

# Follow logs in real-time
docker logs lifeos-backend -f
```

**Look for**:
- ✅ "FIDO2 Configuration validation passed"
- ❌ Any exceptions or stack traces
- ❌ "Application startup exception"
- ❌ Database connection errors

### 3. Check Backend Health Directly
```bash
# From host machine
curl http://localhost:5001/health

# From inside container
docker exec lifeos-backend curl http://localhost:5000/health
```

**Expected**: `{"status": "Healthy"}` or similar
**If fails**: Backend is not responding at all

### 4. Check Container Status
```bash
docker inspect lifeos-backend | grep -A 10 "State"
```

Look for:
- `"Status": "running"`
- `"Health": {"Status": "healthy"}`

## Common Causes of 502 Error

### A. Backend Not Deployed with Production Config

**Symptom**: Using default `docker-compose.yml` without production overrides

**Check**:
```bash
docker exec lifeos-backend env | grep ASPNETCORE_ENVIRONMENT
```

**Should show**: `ASPNETCORE_ENVIRONMENT=Production`
**If shows**: `ASPNETCORE_ENVIRONMENT=Development` → Need to redeploy with prod config

**Fix**:
```bash
docker compose down
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### B. Backend Crashed During Startup

**Symptom**: Container starts but immediately exits or restarts

**Check logs for**:
- Database connection errors
- Missing environment variables
- Configuration parsing errors
- Unhandled exceptions in startup code

**Common fixes**:
1. Ensure database is accessible
2. Check all required env vars are set
3. Verify JWT secret key is configured

### C. FIDO2 Configuration Error Blocking Startup

**Symptom**: Backend fails to start due to FIDO2 config issues

**Check logs for**:
```
FIDO2 Configuration Error: ...
```

**Note**: The validator I added ONLY LOGS errors, it doesn't prevent startup. If you see FIDO2 errors in logs but container is still running, the issue is elsewhere.

### D. Port Binding Issues

**Symptom**: Container starts but can't bind to port

**Check**:
```bash
docker logs lifeos-backend | grep -i "address already in use"
netstat -tuln | grep 5001  # Check if port is already in use
```

**Fix**: Stop conflicting service or change port mapping

### E. Missing Health Check Endpoint

**Symptom**: Health checks fail causing container to be marked unhealthy

**Check**:
```bash
docker logs lifeos-backend | grep -i health
```

**Verify endpoint exists**:
```bash
docker exec lifeos-backend curl http://localhost:5000/health
```

## Production Deployment Checklist

To properly deploy with my fixes:

```bash
# 1. Ensure environment variables are set or using docker-compose.prod.yml
export FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
export FIDO2_ORIGIN=https://lifeos.frostaura.net
export CORS_ORIGIN_0=https://lifeos.frostaura.net

# 2. Deploy with production config
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# 3. Wait for services to be healthy
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# 4. Check backend logs
docker logs lifeos-backend | tail -50

# 5. Verify FIDO2 config
curl https://lifeos.frostaura.net/api/auth/passkey/config

# 6. Test auth endpoint
curl -X POST https://lifeos.frostaura.net/api/auth/passkey/login/begin \
  -H "Content-Type: application/json" -d '{}'
```

## What to Send Me for Further Help

If the issue persists, please provide:

1. **Backend logs**:
   ```bash
   docker logs lifeos-backend --tail 200 > backend.log
   ```

2. **Container status**:
   ```bash
   docker ps -a | grep lifeos
   docker inspect lifeos-backend > backend-inspect.json
   ```

3. **Environment variables** (redact secrets):
   ```bash
   docker exec lifeos-backend env | grep -E "ASPNETCORE|FIDO2|CORS" > backend-env.txt
   ```

4. **Health check result**:
   ```bash
   curl -v http://localhost:5001/health > health-check.txt 2>&1
   ```

5. **Deployment method used**:
   - Which docker-compose command was used?
   - Were environment variables set?
   - Was the backend rebuilt?

## Next Steps

Based on the 502 error, the most likely scenario is:

1. **Backend not deployed with production configuration** → Use `docker-compose.prod.yml`
2. **Backend crashed during startup** → Check logs for exceptions
3. **Database connection issue** → Verify postgres container is healthy

Please run the diagnostic steps above and share the results so I can identify the specific issue.
