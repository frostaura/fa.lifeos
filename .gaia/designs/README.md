# LifeOS Design Documentation

**Spec-Driven Development**: All features MUST have updated design specs BEFORE implementation.

## Design Documents

| Document | Description |
|----------|-------------|
| `architecture.md` | System architecture, data flow, project structure |
| `api.md` | REST API endpoints, request/response formats |
| `database.md` | Entity schemas, relationships, PostgreSQL tables |
| `security.md` | JWT auth, RBAC, data protection, validation |
| `frontend.md` | React components, RTK Query, routing, styling |

## Current Project Stage: **Production**

LifeOS is a comprehensive life management platform with:
- Full financial simulation engine
- Health & longevity tracking
- Gamification system (XP, achievements, streaks)
- Multi-tenant user support

## Before ANY Implementation

1. **Update relevant design docs** - Which docs does this feature touch?
2. **Review specs** - Are they detailed enough to code from?
3. **Implement** - Build according to specs
4. **Keep docs current** - Update as implementation evolves

## Key Design Decisions

### Backend
- **Clean Architecture** with Domain → Application → Infrastructure → API layers
- **EF Core 9** for database access with PostgreSQL 17
- **JWT + WebAuthn** for authentication

### Frontend
- **Atomic Design** (atoms/molecules/organisms)
- **RTK Query** for server state with cache invalidation
- **Glassmorphic dark theme** with Tailwind CSS

### Simulation Engine
- Month-by-month iteration with compound interest calculations
- Tax bracket application for income sources
- End conditions for expenses/investments (date, amount, account settled)

### Health & Longevity
- Evidence-based longevity models (threshold, range, linear, boolean)
- Life expectancy calculation from baseline + metric adjustments