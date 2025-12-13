# System Architecture

## Overview
LifeOS is a comprehensive life management platform built with Clean Architecture principles, providing data-driven insights across multiple life dimensions including finances, health, productivity, and personal growth.

**Version**: 3.0.0 - Metrics-First Architecture with AI Integration

## v3.0 New Features Summary
- **Metrics-First Architecture**: All tracked facts become MetricRecords with enhanced MetricDefinition (isDerived, aggregationType, targetValue)
- **Advanced Health Index Scoring**: Per-metric normalization with at_or_above/at_or_below/range logic, composite dimension scoring
- **Behavioral Adherence Calculation**: Task completion % × streak penalty factor (forgiving first miss, escalating penalties)
- **Wealth Health Score**: 5-component formula (savings rate, debt-to-income, emergency fund, diversification, net worth growth)
- **Longevity Model**: Risk reduction factors → years added (evidence-based, capped at 20 years)
- **LifeOS Score Composite**: Weighted combination (wH=0.4, wA=0.3, wW=0.3) with Primary Stats computation
- **Task Auto-Evaluation**: Background job auto-completes tasks when linked metrics meet target conditions (metricCode + targetValue + targetComparison)
- **Enhanced Streak Logic**: Forgiving first miss (no penalty), riskPenaltyScore = 5 (2nd miss) or 10×(consecutiveMisses-1) (further misses), -2 decay per success
- **MCP Tools for AI**: 15+ JSON-in/JSON-out endpoints (getDashboardSnapshot, recordMetrics, listTasks, completeTask, getWeeklyReview, runSimulation, createWhatIfScenario)
- **Responsive UI Specification**: 12-col grid (desktop), 6-col (tablet), 12-col stacked (mobile); breakpoints <768px/768-1199px/>=1200px
- **User Flow Enhancements**: Onboarding wizard (7 steps), daily dashboard (LifeOS Score rings), weekly/monthly review flows, what-if simulation UI

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

#### Authentication Flow & Session Recovery

**Problem**: When a user's session expires (401 response from API), the redirect to login loses the current route context. Users are always redirected to the dashboard after re-authentication, even if they were deep in the application (e.g., /settings).

**Solution**: Store the current application state in `sessionStorage` before redirecting to login, then restore the route after successful authentication.

**Why sessionStorage**: 
- Survives page redirects (unlike React state)
- Clears on tab close (perfect for temporary redirect state)
- More secure than localStorage for sensitive routing context
- Browser-native, no external dependencies

**Implementation**:
1. **Pre-redirect storage** (`apiSlice.ts`, line ~36):
   ```typescript
   // On 401 response, before redirect
   const currentPath = window.location.pathname;
   const currentSearch = window.location.search;
   const currentHash = window.location.hash;
   sessionStorage.setItem('redirectAfterLogin', 
     JSON.stringify({ path: currentPath, search: currentSearch, hash: currentHash })
   );
   window.location.href = '/#/login';
   ```

2. **Post-login restoration** (`Login.tsx`, after successful authentication):
   ```typescript
   // After login success
   const redirectData = sessionStorage.getItem('redirectAfterLogin');
   if (redirectData) {
     const { path, search, hash } = JSON.parse(redirectData);
     sessionStorage.removeItem('redirectAfterLogin');
     navigate(`${path}${search}${hash}`);
   } else {
     navigate('/dashboard'); // Default fallback
   }
   ```

**Edge Cases Handled**:
- No redirect data present → Default to `/dashboard`
- Invalid JSON in sessionStorage → Caught by try/catch, default to `/dashboard`
- User manually navigates to login → No redirect data, proceeds to `/dashboard`
- Multiple tabs → Each tab maintains its own sessionStorage context

#### Developer Login (Development Bypass)

**Purpose**: Bypass biometric (passkey) authentication during development to speed up testing workflows.

**Environment Detection**:
- **Frontend**: Checks `VITE_ENV` environment variable (not `import.meta.env.DEV`)
  - Why: Frontend uses production build served via nginx in Docker, so `import.meta.env.DEV` is always `false`
  - Developer login visible when `window.VITE_ENV === 'development'`
- **Backend**: Checks `ASPNETCORE_ENVIRONMENT` configuration
  - Developer login endpoint (`/api/auth/dev-login`) only active in `Development` environment
  - Returns 404 in production/staging environments

**Security Implications**:
- Frontend check prevents UI from showing dev login button in production
- Backend check prevents endpoint from functioning even if called directly
- Double-gated: both frontend and backend must be in development mode
- No risk to production deployments (both checks fail in prod environment)

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
| **HealthIndexCalculator** | **Per-metric normalization and composite dimension scoring (v3.0)** |
| **BehavioralAdherenceCalculator** | **Task completion % × streak penalty factor (v3.0)** |
| WealthHealthScoreService | 5-factor financial health scoring (savings rate, debt-to-income, emergency fund, diversification, net worth growth) |
| **LifeOSScoreAggregator** | **Weighted composite: wH×HealthIndex + wA×Adherence + wW×WealthHealth (v3.0)** |
| AchievementService | XP, levels, and achievement unlock evaluation |
| **StreakService** | **Forgiving first miss streak tracking with escalating penalties (v3.0)** |
| IdentityProfileService | Primary stats calculation (7 stats from dimension scores × dimension-stat weights) |
| LongevityModelService | Risk-based longevity years added calculation (risk factors → years added, capped at 20) |
| ReviewService | Weekly/monthly review generation with score deltas and recommendations |
| MetricsIngestionService | Nested metrics parsing and recording with validation |
| **TaskEvaluationService** | **Auto-completion when metric conditions met (metricCode + targetValue + targetComparison) (v3.0)** |
| ScenarioComparisonService | Compare baseline vs scenarios |
| **MCPToolsService** | **AI integration endpoints proxying to API (v3.0)** |

## v3.0 Domain Models

### Metrics-First Architecture (v3.0)
All tracked facts (health metrics, financial snapshots, task completions) are stored as MetricRecords. MetricDefinitions enhanced with:
- **code**: Unique identifier (e.g., `weight_kg`, `net_worth_homeccy`)
- **dimension**: One of 8 life dimensions
- **unit**: Measurement unit (kg, USD, steps, etc.)
- **valueType**: Number | Integer | Decimal | Boolean | Percentage
- **aggregationType**: Last | Sum | Average | Max | Min
- **targetValue**: Goal value for scoring (optional)
- **targetDirection**: AtOrAbove | AtOrBelow | Range
- **isDerived**: Boolean (computed from other metrics vs. directly recorded)

### Scoring System (v3.0)

#### 1. Health Index (Per-Metric Normalization)
```
For each metric in health-related dimensions:
  - If targetDirection = AtOrAbove: score = min(100, (currentValue / targetValue) × 100)
  - If targetDirection = AtOrBelow: score = min(100, (targetValue / currentValue) × 100) if currentValue > 0
  - If targetDirection = Range: score = 100 if within range, else distance-based penalty

Dimension Health Score = average of all metric scores in that dimension
Overall Health Index = weighted average of health-related dimension scores
```

#### 2. Behavioral Adherence Index
```
completedTasksCount = count of completed tasks in period
totalExpectedCompletions = sum of expected completions based on frequency
completionRate = completedTasksCount / totalExpectedCompletions

streakPenaltyFactor = sum of all riskPenaltyScores from active streaks
Adherence Index = (completionRate × 100) - streakPenaltyFactor
Capped: max(0, min(100, Adherence Index))
```

#### 3. Wealth Health Score (5 Components)
```
1. Savings Rate: (monthly_savings / monthly_income) × 100
2. Debt-to-Income Ratio: 100 - ((total_debt_payments / monthly_income) × 100)
3. Emergency Fund Coverage: min(100, (emergency_fund / monthly_expenses) × (100/6))
4. Diversification: percentage of assets in different classes (stocks, bonds, real estate, etc.)
5. Net Worth Growth: ((current_net_worth - previous_year_net_worth) / previous_year_net_worth) × 100

Wealth Health Score = average of 5 components
```

#### 4. Longevity Years Added
```
For each risk factor (smoking, exercise, BMI, sleep, etc.):
  riskReductionFactor = apply formula from LongevityModel (based on metric value)
  yearsAdded += factor_weight × riskReductionFactor

Total Longevity Years Added = min(20, sum of all yearsAdded)
```

#### 5. LifeOS Score (Composite)
```
LifeOS Score = (wH × HealthIndex) + (wA × AdherenceIndex) + (wW × WealthHealthScore)
Default weights: wH = 0.4, wA = 0.3, wW = 0.3
```

### Primary Stats (Computed from Dimensions)
Each of 7 primary stats computed from dimension scores using dimension-stat weight matrix:
```
strength = Σ (dimension_score × dimension_to_strength_weight)
wisdom = Σ (dimension_score × dimension_to_wisdom_weight)
charisma = Σ (dimension_score × dimension_to_charisma_weight)
composure = Σ (dimension_score × dimension_to_composure_weight)
energy = Σ (dimension_score × dimension_to_energy_weight)
influence = Σ (dimension_score × dimension_to_influence_weight)
vitality = Σ (dimension_score × dimension_to_vitality_weight)

All stats capped at 0-100 range.
```

### Task Auto-Evaluation (v3.0)
Tasks can be linked to metrics via:
- **metricCode**: The metric to watch
- **targetValue**: Threshold value
- **targetComparison**: GreaterThanOrEqual | LessThanOrEqual | EqualTo

Background job runs periodically:
```
For each active task with metricCode:
  currentMetricValue = latest metric record value
  if condition met (based on targetComparison):
    create TaskCompletion record
    update streak
    mark task as completed (if one-off)
```

Manual completion still supported for tasks without metric linkage.

### Streak Logic (v3.0 - Forgiving First Miss)
```
Initial state: consecutiveMisses = 0, riskPenaltyScore = 0

On miss:
  consecutiveMisses++
  if consecutiveMisses == 1:
    riskPenaltyScore = 0  (forgiving first miss)
  elif consecutiveMisses == 2:
    riskPenaltyScore = 5
  else:
    riskPenaltyScore = 10 × (consecutiveMisses - 1)

On success:
  if consecutiveMisses > 0:
    riskPenaltyScore = max(0, riskPenaltyScore - 2)  (decay)
  consecutiveMisses = 0
  streakDays++
```

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
│  + MCP Tools Endpoints (v3.0)                               │
├─────────────────────────────────────────────────────────────┤
│                   LifeOS.Application                        │
│  Commands, Queries, DTOs, Services, Validators              │
│  + HealthIndexCalculator (v3.0)                             │
│  + BehavioralAdherenceCalculator (v3.0)                     │
│  + TaskEvaluationService (v3.0)                             │
├─────────────────────────────────────────────────────────────┤
│                    LifeOS.Domain                            │
│  Entities, Enums, ValueObjects, Interfaces                  │
│  + Enhanced MetricDefinition (v3.0)                         │
│  + TaskCompletion entity (v3.0)                             │
├─────────────────────────────────────────────────────────────┤
│                  LifeOS.Infrastructure                      │
│  DbContext, Repositories, External Service Implementations  │
│  + Background Jobs (TaskEvaluator, StreakUpdater) (v3.0)    │
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

## v3.0 MCP Tools for AI Integration

### Overview
MCP (Model Context Protocol) Tools provide JSON-in/JSON-out endpoints for AI agent integration. These endpoints proxy to existing REST API calls but with simplified schemas optimized for AI consumption.

### Tool Categories

#### Dashboard & Overview
- **getDashboardSnapshot**: Current LifeOS Score, net worth, dimension scores, today's tasks, active streaks
- **getWeeklyReview**: Review data for current/recent week with score deltas and recommendations
- **getMonthlyReview**: Monthly review with net worth trajectory, simulation summary, identity radar evolution

#### Metrics & Recording
- **recordMetrics**: Simplified nested metric ingestion (dimension → category → metric structure)
- **getMetricHistory**: Historical values for specific metric with aggregation options
- **listMetricDefinitions**: Available metrics with units, targets, and value types

#### Tasks & Habits
- **listTasks**: Filter tasks by dimension, type, completion status, date range
- **createTask**: Quick task creation with sensible defaults
- **completeTask**: Mark task complete (with optional metric value recording)
- **getStreaks**: Current streak data for all active habits

#### Financial Planning
- **getNetWorthSummary**: Current net worth, breakdown by account type, trend data
- **runSimulation**: Execute existing scenario or quick what-if projection
- **createWhatIfScenario**: Model one-off purchase/expense impact on projections
- **compareScenarios**: Compare baseline vs alternative scenarios

#### Goals & Milestones
- **listMilestones**: Active/completed milestones with progress calculations
- **getMilestoneProgress**: Detailed progress for specific milestone (metric-linked)
- **getIdentityProfile**: User archetype, values, primary stat targets, and current stats

#### Health & Longevity
- **getLongevityEstimate**: Years added breakdown by risk factor with evidence sources
- **getHealthIndex**: Current health index with per-metric contribution details

### Tool Endpoint Pattern
```
POST /api/mcp/tools/{toolName}
Authorization: Bearer {jwt_token}

Request:
{
  "params": {
    // tool-specific parameters
  }
}

Response:
{
  "success": true,
  "data": {
    // tool-specific response
  },
  "meta": {
    "toolName": "getDashboardSnapshot",
    "executedAt": "2024-12-11T18:00:00Z"
  }
}
```

### Security Considerations
- All MCP tools require JWT authentication
- Tools respect user data ownership (can only access own data)
- Rate limiting: Same as standard API (1000 requests/15 minutes)
- Tools are read-heavy with limited write operations (recordMetrics, completeTask, createTask)

## v3.0 Responsive UI Architecture

### Grid System
- **Desktop (≥1200px)**: 12-column grid, fixed sidebar, multi-panel layouts
- **Tablet (768-1199px)**: 6-column grid, collapsible sidebar, two-panel layouts
- **Mobile (<768px)**: 12-column stacked, hamburger menu, single-panel layouts

### Breakpoint Strategy
```css
/* Mobile first approach */
.container {
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  gap: 1rem;
}

/* Tablet: 768-1199px */
@media (min-width: 768px) and (max-width: 1199px) {
  .card {
    grid-column: span 6;
  }
}

/* Desktop: ≥1200px */
@media (min-width: 1200px) {
  .card-small {
    grid-column: span 3;
  }
  .card-medium {
    grid-column: span 4;
  }
  .card-large {
    grid-column: span 6;
  }
}
```

### Component Sizing Rules
| Component | Mobile (<768px) | Tablet (768-1199px) | Desktop (≥1200px) |
|-----------|----------------|---------------------|-------------------|
| Dashboard Cards | 12 cols (full width) | 6 cols (2-col grid) | 4 cols (3-col grid) |
| Charts | 12 cols | 12 cols | 8-12 cols |
| Forms | 12 cols | 8-10 cols (centered) | 6-8 cols (centered) |
| Sidebar | Hidden (drawer) | 4 cols (collapsible) | 3 cols (fixed) |
| Modals | Full screen | 10 cols (centered) | 8 cols (centered) |

### Typography Scaling
```css
:root {
  --text-xs: 0.75rem;    /* 12px */
  --text-sm: 0.875rem;   /* 14px */
  --text-base: 1rem;     /* 16px */
  --text-lg: 1.125rem;   /* 18px */
  --text-xl: 1.25rem;    /* 20px */
  --text-2xl: 1.5rem;    /* 24px */
  --text-3xl: 1.875rem;  /* 30px */
  --text-4xl: 2.25rem;   /* 36px */
}

/* Mobile scale down */
@media (max-width: 767px) {
  :root {
    --text-base: 0.875rem;  /* 14px */
    --text-lg: 1rem;        /* 16px */
    --text-xl: 1.125rem;    /* 18px */
    --text-2xl: 1.25rem;    /* 20px */
    --text-3xl: 1.5rem;     /* 24px */
  }
}
```

### User Flow Highlights (v3.0)

#### Onboarding Flow (7 Steps)
1. Welcome & account setup
2. Profile details (DOB, home currency)
3. Health baselines (weight, target weight, body fat, height)
4. Financial baselines (accounts, income, major expenses)
5. Major goals (financial targets, life milestones)
6. Identity profile (archetype selection, values, stat priorities)
7. Seed structure (auto-generate dimensions, baseline scenario, initial tasks)

#### Daily Dashboard Flow
- LifeOS Score rings (Health, Adherence, Wealth overlapping circles)
- Today's tasks (sorted by priority, with quick-complete buttons)
- Dimension snapshot cards (score + top metric + active streak)
- Net worth banner with 7-day trend sparkline

#### Weekly Review Flow
- Score change visualizations (Health Index, Adherence, Wealth Health deltas)
- Streak status grid (all habits with current/longest, penalties)
- Top 3 recommended focus actions (prioritized by impact)
- Primary stats radar chart (current vs targets)

#### Monthly Review Flow
- Net worth trajectory chart (12-month history + 12-month projection)
- Simulation comparison (baseline vs active scenarios)
- Identity radar evolution (beginning vs end of month)
- Milestone progress grid (all active milestones with % completion)

#### What-If Simulation Flow
1. Click "What if I buy this?" from Finances page
2. Modal: Enter purchase amount, date, category
3. Select comparison scenario (or use baseline)
4. View impact:
   - Net worth delta at 5/10/20 year marks
   - Milestone achievement delays
   - Cashflow impact chart
5. Save as scenario or discard