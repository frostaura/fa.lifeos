# Security Design

## v3.0 Security Considerations
- **MCP Tools**: All tools require JWT authentication, same rate limits as standard API
- **Task Auto-Evaluation**: Background job respects user data isolation, no cross-user data access
- **Score Snapshots**: Historical scoring data is user-scoped, immutable after creation
- **Metric Recording**: Nested ingestion validates all metric codes before recording, rejects unknown metrics unless `allowDynamicCreation=true`
- **Identity Profile**: User persona and values data is sensitive - ensure user-only access (unchanged from v1.1)
- **Onboarding Data**: Should not be exposed after completion (unchanged from v1.1)

## v1.1 Security Considerations
- Identity Profile data is sensitive (user persona, values) - ensure user-only access
- Onboarding data should not be exposed after completion
- Review snapshots contain aggregated sensitive data - user-only access
- Nested metrics ingestion must validate all metric codes before recording

## Authentication

### Methods
- **Primary**: JWT with refresh tokens (HS256)
- **Biometric**: WebAuthn/Passkey support
- **API Keys**: For external integrations (n8n, Apple Shortcuts)

### JWT Implementation
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-uuid",
    "email": "user@example.com",
    "role": "User",
    "iat": 1234567890,
    "exp": 1234567890,
    "iss": "lifeos-api",
    "aud": "lifeos-client"
  }
}
```

### Token Management
- **Access token**: 15 minutes expiry
- **Refresh token**: 7 days expiry
- **Storage**: httpOnly cookies (recommended) or localStorage
- **Rotation**: Refresh tokens rotated on use

## Authorization

### Access Control
- **RBAC** (Role-Based Access Control): User, Admin roles
- **Resource Ownership**: Users can only access their own data
- **API Key Scoping**: Limited to specific endpoints

### Roles
| Role | Permissions |
|------|-------------|
| User | Full CRUD on own data, read system dimensions/achievements, access MCP tools (v3.0) |
| Admin | All User permissions + system configuration + view background job logs (v3.0) |

### Middleware Authorization
```csharp
[Authorize]                      // Requires authenticated user
[Authorize(Roles = "Admin")]     // Requires Admin role
[RequireOwnership("userId")]     // Validates resource ownership
```

## Data Protection

### Encryption
- **In Transit**: TLS 1.3+ (HTTPS required in production)
- **At Rest**: Database-level encryption via PostgreSQL
- **Passwords**: Argon2id hashing

### Sensitive Data Handling
```csharp
// Fields that are never returned in API responses
private static readonly string[] RedactedFields = {
    "passwordHash",
    "refreshToken",
    "apiKeyHash"
};

// Fields that are hashed, not encrypted
private static readonly string[] HashedFields = {
    "password",
    "apiKey"
};
```

### Password Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- Hashed with Argon2id

## Input Validation

### Request Validation
All API requests are validated using FluentValidation:
```csharp
public class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CurrentBalance).NotNull();
    }
}
```

### SQL Injection Prevention
- All queries use Entity Framework Core parameterized queries
- No raw SQL without parameterization

## Security Headers

### HTTP Security Headers
```javascript
{
  'Content-Security-Policy': "default-src 'self'",
  'X-Frame-Options': 'DENY',
  'X-Content-Type-Options': 'nosniff',
  'X-XSS-Protection': '1; mode=block',
  'Strict-Transport-Security': 'max-age=31536000; includeSubDomains',
  'Referrer-Policy': 'strict-origin-when-cross-origin'
}
```

### CORS Configuration
```javascript
{
  origin: ['https://trusted-domain.com'],
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization']
}
```

## Rate Limiting

### Strategies
```javascript
{
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 1000, // requests per window (increased for MCP tools - v3.0)
  message: 'Too many requests',
  standardHeaders: true,
  legacyHeaders: false,
}
```

### Endpoints
- Login: 5 attempts per 15 minutes
- API (standard): 1000 requests per 15 minutes (v3.0 - increased for AI integration)
- MCP Tools: 1000 requests per 15 minutes (v3.0 - shared with standard API limit)
- Password reset: 3 per hour
- Registration: 10 per hour per IP

## Session Management

### Session Security
- Secure flag (HTTPS only)
- HttpOnly flag (no JS access)
- SameSite=Strict
- Session rotation on privilege escalation
- Absolute and idle timeouts

### Session Storage
```javascript
{
  secret: process.env.SESSION_SECRET,
  resave: false,
  saveUninitialized: false,
  cookie: {
    secure: true,
    httpOnly: true,
    maxAge: 1000 * 60 * 60 * 2, // 2 hours
    sameSite: 'strict'
  }
}
```

## Vulnerability Prevention

### OWASP Top 10
1. **Injection**: Parameterized queries, input validation
2. **Broken Auth**: MFA, secure session management
3. **Sensitive Data**: Encryption, minimal exposure
4. **XXE**: Disable XML external entities
5. **Broken Access**: Proper authorization checks
6. **Security Misconfig**: Secure defaults, hardening
7. **XSS**: Output encoding, CSP headers
8. **Insecure Deserial**: JSON only, schema validation
9. **Vulnerable Components**: Regular updates, scanning
10. **Insufficient Logging**: Comprehensive audit logs

## Audit & Logging

### Security Events to Log
```javascript
const securityEvents = [
  'login_attempt',
  'login_success',
  'login_failure',
  'logout',
  'password_change',
  'permission_denied',
  'data_access',
  'configuration_change'
];
```

### Log Format
```json
{
  "timestamp": "2024-01-01T00:00:00Z",
  "event": "login_attempt",
  "user_id": "123",
  "ip": "192.168.1.1",
  "user_agent": "Mozilla/5.0...",
  "result": "success",
  "metadata": {}
}
```

## Incident Response

### Response Plan
1. **Detect**: Monitoring and alerts
2. **Contain**: Isolate affected systems
3. **Investigate**: Root cause analysis
4. **Eradicate**: Remove threat
5. **Recover**: Restore services
6. **Learn**: Post-mortem and improvements

### Contact Chain
```yaml
severity_1:
  - Security Team Lead
  - CTO
  - CEO
severity_2:
  - Security Team
  - Engineering Lead
severity_3:
  - On-call Engineer
```

## Compliance

### Standards
- **GDPR**: Privacy by design, data portability
- **CCPA**: User data rights, opt-out
- **PCI DSS**: Payment card security
- **HIPAA**: Healthcare data protection
- **SOC 2**: Security controls audit

### Data Retention
```javascript
{
  user_data: '3 years after account closure',
  logs: '90 days',
  audit_logs: '7 years',
  payment_records: '7 years',
  temp_data: '24 hours'
}
```

## Security Testing

### Testing Types
- **SAST**: Static code analysis
- **DAST**: Dynamic application testing
- **Dependency Scanning**: Vulnerable packages
- **Penetration Testing**: Annual third-party
- **Security Reviews**: Code review checklist

### CI/CD Integration
```yaml
security_checks:
  - dependency_scan
  - sast_scan
  - secret_detection
  - container_scan
  - license_check
```