# System Architecture

## Overview
LifeOS is a comprehensive life management platform built with Clean Architecture principles, providing data-driven insights across multiple life dimensions including finances, health, productivity, and personal growth.

**Version**: 1.1.0 - Identity-Aligned Revision

## v1.1 New Features Summary
- **8 Custom Life Dimensions**: health_recovery, relationships, work_contribution, play_adventure, asset_care, create_craft, growth_mind, community_meaning
- **Primary Stats Model**: strength, wisdom, charisma, composure, energy, influence, vitality (0-100)
- **Identity Profile**: Target Persona archetype with values and stat targets
- **Scientific Scoring**: Health Index, Behavioral Adherence, Wealth Health, Longevity Years Added
- **Multi-Currency Financial Engine**: FX rates table, deterministic simulation
- **Extended Simulation Events**: Source/target accounts, formula events, scenario comparison
- **Nested Metrics Ingestion**: Hierarchical API payload structure
- **Enhanced Streaks**: Penalty formula with consecutive misses tracking
- **Goal-First Onboarding**: Collect baselines, goals, identity traits
- **Weekly/Monthly Reviews**: Scheduled reports with deltas

## Core Components

### Frontend
- **Framework**: React 19.2 with TypeScript 5.9
- **Build Tool**: Vite 7.2
- **State Management**: Redux Toolkit 2.11 + RTK Query
- **Routing**: React Router 7.9
- **Styling**: Tailwind CSS 4.1 (Glassmorphic dark theme)
- **Charts**: Recharts 3.5
- **Testing**: Playwright for E2E

### Backend
- **Framework**: ASP.NET Core (.NET 9.0)
- **ORM**: Entity Framework Core 9.0
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → API)
- **Authentication**: JWT with refresh tokens + WebAuthn passkey support
- **Real-time**: SignalR for live updates
- **API Style**: RESTful with JSON:API format

### Database
- **Primary**: PostgreSQL 17
- **Provider**: Npgsql
- **Migrations**: EF Core Code-First

### Infrastructure
- **Containerization**: Docker Compose
- **Development**: docker-compose.yml (PostgreSQL, Backend, Frontend)
- **CI/CD**: GitHub Actions

## Project Structure

```
fa.lifeos/
├── src/
│   ├── backend/
│   │   ├── LifeOS.Api/           # Controllers, Middleware, Program.cs
│   │   ├── LifeOS.Application/   # Services, Commands, Queries, DTOs
│   │   ├── LifeOS.Domain/        # Entities, Enums, ValueObjects
│   │   ├── LifeOS.Infrastructure/ # DbContext, Repositories, External Services
│   │   └── LifeOS.Tests/         # Unit & Integration Tests
│   └── frontend/
│       └── src/
│           ├── components/       # Atomic Design (atoms/molecules/organisms)
│           ├── pages/            # Route Pages
│           ├── services/         # RTK Query API Endpoints
│           ├── store/            # Redux Store & Slices
│           ├── hooks/            # Custom React Hooks
│           ├── types/            # TypeScript Definitions
│           └── utils/            # Helper Functions
├── .gaia/                        # GAIA AI Development System
├── docker-compose.yml            # Development Environment
└── playwright.config.ts          # E2E Test Configuration
```

## Data Flow Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────────┐    ┌────────────┐
│   Browser   │───▶│  Frontend   │───▶│   Backend API   │───▶│ PostgreSQL │
│   (React)   │◀───│  (Vite)     │◀───│   (ASP.NET)     │◀───│  Database  │
└─────────────┘    └─────────────┘    └─────────────────┘    └────────────┘
       │                  │                    │
       │                  │                    ▼
       │                  │           ┌─────────────────┐
       │                  │           │  SignalR Hub    │
       │                  │           │  (Real-time)    │
       │                  │           └─────────────────┘
       │                  ▼
       │          ┌─────────────────┐
       │          │   RTK Query     │
       │          │   State Cache   │
       │          └─────────────────┘
       ▼
┌─────────────────┐
│  External APIs  │
│  (n8n, Apple    │
│  Shortcuts)     │
└─────────────────┘
```

## Key Domain Services

| Service | Purpose |
|---------|---------|
| SimulationEngine | Month-by-month financial projection with compound interest, taxes, inflation |
| LongevityEstimator | Evidence-based life expectancy calculation from health metrics |
| ScoreCalculator | Dimension and Life Score computation (0-100) |
| WealthHealthScoreService | 5-factor financial health scoring |
| AchievementService | XP, levels, and achievement unlock evaluation |
| StreakService | Habit streak tracking and management |
| IdentityProfileService | Primary stats calculation and identity alignment (v1.1) |
| LongevityModelService | Risk-based longevity years added calculation (v1.1) |
| ReviewService | Weekly/monthly review generation (v1.1) |
| MetricsIngestionService | Nested metrics parsing and recording (v1.1) |
| ScenarioComparisonService | Compare baseline vs scenarios (v1.1) |

## v1.1 Domain Models

### Primary Stats (New)
Each dimension contributes to 7 primary stats (0-100 scale):
- **Strength**: Physical capability, endurance
- **Wisdom**: Knowledge, decision quality
- **Charisma**: Social influence, communication
- **Composure**: Emotional regulation, stress handling
- **Energy**: Vitality, motivation levels
- **Influence**: Impact on others, leadership
- **Vitality**: Overall health, longevity

### Identity Profile (New)
```csharp
public class IdentityProfile
{
    Guid UserId;
    string Archetype;                    // "God of Mind-Power"
    string ArchetypeDescription;
    string[] Values;                     // Core values
    Dictionary<string, int> PrimaryStatTargets; // stat -> target (0-100)
    Guid[] LinkedMilestones;
}
```

### Dimension-to-Stat Mapping (New)
```
health_recovery    → vitality, energy, composure
relationships      → charisma, influence, composure
work_contribution  → wisdom, influence, energy
play_adventure     → energy, vitality, charisma
asset_care         → wisdom, composure
create_craft       → wisdom, energy, influence
growth_mind        → wisdom, strength, composure
community_meaning  → charisma, influence, vitality
```

### Scientific Scores (v1.1)
1. **Health Index** - Metric-based health score (BMI, BP, etc.)
2. **Behavioral Adherence Index** - Task/habit compliance rate
3. **Wealth Health Score** - 5-factor financial health
4. **Longevity Years Added** - Risk model output (capped)
5. **Composite LifeOS Score** - Weighted combination

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                     LifeOS.Api                              │
│  Controllers, Middleware, Contracts, DI Configuration       │
├─────────────────────────────────────────────────────────────┤
│                   LifeOS.Application                        │
│  Commands, Queries, DTOs, Services, Validators              │
├─────────────────────────────────────────────────────────────┤
│                    LifeOS.Domain                            │
│  Entities, Enums, ValueObjects, Interfaces                  │
├─────────────────────────────────────────────────────────────┤
│                  LifeOS.Infrastructure                      │
│  DbContext, Repositories, External Service Implementations  │
└─────────────────────────────────────────────────────────────┘
```

## Scalability Considerations

### Horizontal Scaling
- Stateless API services (JWT-based auth)
- Database connection pooling via Npgsql
- SignalR with Redis backplane (production)

### Performance Optimizations
- RTK Query caching with tag invalidation
- Lazy loading of React components
- Code splitting by route
- Database indexing on frequently queried columns
- Projection queries (AsNoTracking) for read operations

## Development Workflow

1. **Local Development**: `docker compose up -d` for full stack
2. **Hot Reload**: Vite for frontend, dotnet watch for backend
3. **Testing**: Playwright E2E tests against running services
4. **Migrations**: EF Core migrations for schema changes
5. **Deployment**: Docker images for production