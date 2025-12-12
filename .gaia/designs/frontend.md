# Frontend Design Guide

## Technology Stack

### Core Framework
- **Framework**: React 19.2
- **Language**: TypeScript 5.9
- **Build Tool**: Vite 7.2
- **Package Manager**: npm

### State Management
- **Global State**: Redux Toolkit 2.11
- **Server State**: RTK Query
- **URL State**: React Router 7.9

### UI & Styling
- **CSS Framework**: Tailwind CSS 4.1
- **Design System**: Custom glassmorphic dark theme
- **Responsive Grid**: 12-column (desktop), 6-column (tablet), 12-column stacked (mobile) (v3.0)
- **Breakpoints**: <768px (mobile), 768-1199px (tablet), ≥1200px (desktop) (v3.0)
- **Charts**: Recharts 3.5
- **Icons**: Lucide React

### Testing
- **E2E**: Playwright

## Project Structure

```
src/
├── components/
│   ├── atoms/           # Button, Badge, Input, Select, Spinner, GlassCard
│   ├── molecules/       # AccountRow, MetricSparkline, CurrencySelector
│   └── organisms/       # LifeScoreCard, NetWorthChart, DimensionGrid
├── pages/               # Route components (Dashboard, Finances, Health, etc.)
├── services/            # RTK Query API endpoints
├── store/               # Redux store configuration
├── hooks/               # Custom React hooks
├── types/               # TypeScript definitions
├── utils/               # Helper functions (cn, formatCurrency, etc.)
└── router.tsx           # React Router configuration
```

## Design System

### Theme: Glassmorphic Dark Mode

#### Color Palette
```css
:root {
  /* Backgrounds */
  --bg-primary: #0f0f1a;
  --bg-secondary: #1a1a2e;
  --bg-tertiary: #25253a;
  --bg-hover: rgba(255, 255, 255, 0.05);

  /* Accents */
  --accent-purple: #8b5cf6;
  --accent-cyan: #22d3ee;
  --accent-pink: #ec4899;

  /* Semantic */
  --semantic-success: #22c55e;
  --semantic-error: #ef4444;
  --semantic-warning: #eab308;

  /* Text */
  --text-primary: #f1f5f9;
  --text-secondary: #94a3b8;
  --text-tertiary: #64748b;

  /* Glass */
  --glass-bg: rgba(255, 255, 255, 0.05);
  --glass-border: rgba(255, 255, 255, 0.1);
}
```

### GlassCard Component
```tsx
interface GlassCardProps {
  variant: 'default' | 'elevated';
  glow?: 'accent' | 'success' | 'error';
  className?: string;
  children: React.ReactNode;
}

// Usage
<GlassCard variant="elevated" glow="accent">
  <h2>Net Worth</h2>
  <p>R1,234,567</p>
</GlassCard>
```

### Button Variants
```tsx
<Button variant="primary">Save</Button>
<Button variant="secondary">Cancel</Button>
<Button variant="ghost">More</Button>
<Button variant="danger">Delete</Button>
```

## Page Structure

### Dashboard (`/`) - v3.0 Enhanced
- **LifeOS Score Rings** (v3.0) - Overlapping rings: Health Index (outer), Adherence (middle), Wealth Health (inner) with composite score in center
- **Today's Priority Actions** (v3.0) - Tasks sorted by dimension score deficits × task impact
- **Identity Radar** - Primary stats visualization with current vs targets (v1.1)
- Net Worth card with trend + projection preview
- Dimensions grid (8 life areas) with score, top metric, active streak
- **Quick Metric Recording** (v3.0) - Floating action button for common metrics

### Onboarding (`/onboarding`) - v3.0 Enhanced 7-Step Wizard
- **Step 1**: Welcome & account creation
- **Step 2**: Profile details (DOB, home currency, timezone)
- **Step 3**: Health baselines (weight, target weight, body fat %, height)
- **Step 4**: Financial baselines (accounts, monthly income, major expenses)
- **Step 5**: Major goals (financial targets with dates, life milestones)
- **Step 6**: Identity traits (archetype selection or custom, core values, primary stat priorities)
- **Step 7**: Seed structure (auto-generate dimensions, baseline scenario, initial habit suggestions)

### Finances (`/finances`)
- Tab navigation: Overview | Tax Profiles | Income/Expenses | Investments | Goals
- Net worth banner
- Accounts list with CRUD
- Projection chart
- Financial goals widget
- **"What if I buy this?" wizard** (v3.0) - Modal-based scenario creation with immediate impact preview

### Finance Simulator (`/simulation`) - v1.1 Enhanced
- Scenarios list with baseline indicator
- Scenario builder
- Event modeling with source/target accounts
- Projection visualization
- **Baseline vs scenario comparison charts** (v1.1)
- **Scenario comparison table** (v1.1)

### Health (`/health`)
- **Longevity Years Added** card with v3.0 calculation (risk reduction factors → years added, capped at 20)
- Contributing factors breakdown with risk models and evidence sources
- Recommendations list (prioritized by impact)
- Health metrics grid with sparklines
- Longevity rules editor

### Reviews (`/reviews`) - v3.0 Enhanced
- **Weekly Review Dashboard**:
  - Score deltas (Health Index, Adherence, Wealth Health) with v3.0 formulas
  - Top streaks with forgiving first miss visualization
  - Recommended focus actions (top 3, prioritized by impact)
  - Primary stats delta (7 stats: beginning vs end of week)
- **Monthly Review Dashboard**:
  - Net worth trajectory (12-month history + 12-month projection)
  - Scenario comparison summary (baseline vs active scenarios)
  - Identity radar evolution (beginning vs end of month)
  - Milestone progress grid (all active milestones with % completion)
- Historical reviews list

### Metrics (`/metrics`)
- Metric definitions table
- API playground with Monaco editor
- **Nested ingestion tester** (v1.1)
- Event log

### Settings (`/settings`)
- Profile settings
- **Identity Profile editor** (v1.1)
- API keys management
- Dimension configuration
- Data portability (export/import)

## RTK Query Setup

### API Slice
```typescript
// services/api.ts
export const api = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: '/api',
    prepareHeaders: (headers) => {
      const token = localStorage.getItem('accessToken');
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  tagTypes: [
    'Dashboard',
    'Accounts',
    'Dimensions',
    'Tasks',
    'Milestones',
    'Metrics',
    'Scenarios',
    'Projections',
    'Achievements',
    'Streaks',
  ],
  endpoints: () => ({}),
});
```

### Example Endpoints
```typescript
// services/endpoints/finances.ts
export const financeApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getAccounts: builder.query<AccountsResponse, void>({
      query: () => '/accounts',
      providesTags: ['Accounts'],
    }),
    createAccount: builder.mutation<AccountResponse, CreateAccountRequest>({
      query: (body) => ({ url: '/accounts', method: 'POST', body }),
      invalidatesTags: ['Accounts', 'Dashboard'],
    }),
  }),
});
```

## Routing

### Route Configuration
```typescript
// router.tsx
export const router = createHashRouter([
  { path: '/login', element: <Login /> },
  {
    path: '/',
    element: <AuthGuard><AppLayout /></AuthGuard>,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'dimensions', element: <Dimensions /> },
      { path: 'dimensions/:dimensionId', element: <DimensionDetail /> },
      {
        path: 'finances',
        element: <FinancesLayout />,
        children: [
          { index: true, element: <FinancesOverview /> },
          { path: 'tax-profiles', element: <TaxProfileSettings /> },
          { path: 'income-expenses', element: <IncomeExpenseSettings /> },
          { path: 'investments', element: <InvestmentSettings /> },
          { path: 'goals', element: <GoalsSettings /> },
        ],
      },
      { path: 'simulation', element: <Simulation /> },
      { path: 'simulation/:scenarioId', element: <SimulationDetail /> },
      { path: 'health', element: <Health /> },
      { path: 'metrics', element: <Metrics /> },
      { path: 'settings/*', element: <Settings /> },
    ],
  },
  { path: '*', element: <NotFound /> },
]);
```

### Auth Guard
```typescript
export function AuthGuard({ children }: { children: React.ReactNode }) {
  const token = localStorage.getItem('accessToken');
  
  if (!token) {
    return <Navigate to="/login" replace />;
  }
  
  return <>{children}</>;
}
```

## Responsive Breakpoints (v3.0 Specification)

```css
/* Mobile first with explicit breakpoints */
/* Mobile: < 768px */
/* Tablet: 768px - 1199px */
/* Desktop: >= 1200px */

/* Tailwind equivalents */
sm: 640px   /* Mobile landscape (not primary breakpoint) */
md: 768px   /* Tablet portrait (PRIMARY: mobile → tablet) */
lg: 1024px  /* Tablet landscape */
xl: 1280px  /* Desktop (PRIMARY: tablet → desktop at 1200px) */
2xl: 1536px /* Large desktop */
```

### Layout Guidelines (v3.0)
- **Mobile (< 768px)**: 
  - Single column, stacked cards
  - Compact typography (14px base)
  - Hamburger menu
  - Full-width modals
  - Touch-optimized buttons (min 44px tap target)
  - Simplified charts (fewer data points)
  
- **Tablet (768-1199px)**: 
  - 6-column grid (2-card layouts)
  - Collapsible sidebar
  - Medium typography (16px base)
  - Centered modals (10 columns)
  - Enhanced charts
  
- **Desktop (≥1200px)**: 
  - 12-column grid (3-4 card layouts)
  - Fixed sidebar
  - Full typography (16px base)
  - Multi-panel layouts
  - Full-featured charts

### v3.0 Grid System Specification

```css
.grid-container {
  display: grid;
  gap: 1rem;
}

/* Mobile: 12 columns stacked */
@media (max-width: 767px) {
  .grid-container {
    grid-template-columns: repeat(12, 1fr);
  }
  .card { grid-column: span 12; }
  .sidebar { display: none; } /* Hidden, accessible via drawer */
}

/* Tablet: 6 columns */
@media (min-width: 768px) and (max-width: 1199px) {
  .grid-container {
    grid-template-columns: repeat(6, 1fr);
  }
  .card-small { grid-column: span 3; }   /* 2-col */
  .card-medium { grid-column: span 6; }  /* 1-col full width */
  .sidebar { 
    grid-column: span 2; 
    position: sticky;
  }
  .main-content { grid-column: span 4; }
}

/* Desktop: 12 columns */
@media (min-width: 1200px) {
  .grid-container {
    grid-template-columns: repeat(12, 1fr);
  }
  .card-small { grid-column: span 3; }    /* 4-col */
  .card-medium { grid-column: span 4; }   /* 3-col */
  .card-large { grid-column: span 6; }    /* 2-col */
  .card-full { grid-column: span 12; }    /* 1-col */
  .sidebar { 
    grid-column: span 2; 
    position: fixed;
    height: 100vh;
  }
  .main-content { grid-column: span 10; }
}
```

### Component Sizing Rules (v3.0)
| Component | Mobile (<768px) | Tablet (768-1199px) | Desktop (≥1200px) |
|-----------|----------------|---------------------|-------------------|
| Dashboard Cards | 12 cols (full) | 3 cols (2-card) | 3-4 cols (3-4 card) |
| LifeOS Score Rings | 12 cols, compact | 6 cols, full size | 4 cols, full size |
| Charts | 12 cols, simplified | 6 cols, enhanced | 8-12 cols, full |
| Forms | 12 cols | 6 cols (centered) | 6-8 cols (centered) |
| Sidebar | Hidden (drawer) | 2 cols (collapsible) | 2 cols (fixed) |
| Modals | Full screen | 5 cols (centered) | 6-8 cols (centered) |
| Metric Cards | 12 cols | 3 cols | 3 cols |
| Task Lists | 12 cols | 6 cols | 8 cols |
| What-If Modal | Full screen | 5 cols | 8 cols |

### Typography Scaling (v3.0)
```css
:root {
  /* Desktop & Tablet base */
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
    --text-4xl: 1.875rem;   /* 30px */
  }
}
```

### Card Padding (v3.0)
```css
.card {
  padding: 1rem;  /* Mobile */
}

@media (min-width: 768px) {
  .card {
    padding: 1.5rem;  /* Tablet */
  }
}

@media (min-width: 1200px) {
  .card {
    padding: 2rem;  /* Desktop */
  }
}
```

## Responsive Breakpoints

```css
/* Tailwind defaults */
sm: 640px   /* Mobile landscape */
md: 768px   /* Tablet portrait */
lg: 1024px  /* Tablet landscape / Small desktop */
xl: 1280px  /* Desktop */
2xl: 1536px /* Large desktop */
```

### Layout Guidelines
- **Mobile (< 768px)**: Single column, stacked cards, compact typography
- **Tablet (768-1023px)**: Two-column grids, collapsible sidebar
- **Desktop (1024px+)**: Fixed sidebar, three-column grids, full charts

## v3.0 New Components

### LifeOS Score Rings (v3.0 - Overlapping Circles)
```tsx
interface LifeOSScoreRingsProps {
  healthIndex: number;       // 0-100
  adherenceIndex: number;    // 0-100
  wealthHealthScore: number; // 0-100
  lifeosScore: number;       // Composite
  size?: 'sm' | 'md' | 'lg';
}

// Renders 3 overlapping rings (Health outer, Adherence middle, Wealth inner)
// Composite score displayed in center
// On hover: Show component breakdown
<LifeOSScoreRings 
  healthIndex={82.3}
  adherenceIndex={75.2}
  wealthHealthScore={78.1}
  lifeosScore={78.5}
  size="lg"
/>
```

### Health Index Breakdown Modal (v3.0)
```tsx
interface HealthIndexBreakdownProps {
  dimensionScores: Array<{
    dimension: string;
    score: number;
    metricContributions: Array<{
      metricCode: string;
      metricName: string;
      score: number;
      currentValue: number;
      targetValue: number;
      weight: number;
    }>;
  }>;
}

// Shows per-dimension scores with drilldown to per-metric scores
// Visualizes normalization formula (at_or_above/at_or_below/range)
```

### Task Auto-Evaluation Indicator (v3.0)
```tsx
interface TaskAutoEvalProps {
  task: {
    metricCode?: string;
    targetValue?: number;
    targetComparison?: string;
  };
  currentMetricValue?: number;
}

// Visual indicator showing:
// - Metric being tracked
// - Current value vs target
// - Progress bar
// - "Auto-completes when..." tooltip
<TaskAutoEvalIndicator task={task} currentMetricValue={74.5} />
```

### Streak Penalty Visualization (v3.0 - Forgiving First Miss)
```tsx
interface StreakPenaltyVisualizationProps {
  consecutiveMisses: number;
  riskPenaltyScore: number;
  streakDays: number;
}

// Color-coded visualization:
// - Green: 0 misses, healthy streak
// - Yellow: 1 miss (forgiving, 0 penalty)
// - Orange: 2 misses (penalty = 5)
// - Red: 3+ misses (penalty = 10 × (misses - 1))
// Shows decay on success (-2 per day)
```

### Quick Metric Recording FAB (v3.0)
```tsx
interface QuickMetricRecordingProps {
  commonMetrics: string[];  // User's most-used metrics
  onRecord: (metricCode: string, value: number) => void;
}

// Floating Action Button in bottom-right
// Opens quick-record modal with:
// - Dropdown of common metrics (last 5 used + dimension defaults)
// - Value input
// - Timestamp (defaults to now, editable)
// - Submit button
// Closes automatically after recording
```

### Onboarding Step Indicator (v3.0)
```tsx
interface OnboardingStepIndicatorProps {
  currentStep: number;  // 1-7
  totalSteps: number;   // 7
  stepLabels: string[];
}

// Progress bar with step dots
// Shows: Step X of 7: [Label]
// Visual progress: filled dots for completed, hollow for pending
```

### What-If Impact Diff View (v3.0)
```tsx
interface WhatIfImpactDiffProps {
  withoutPurchase: {
    netWorthAt5Years: number;
    netWorthAt10Years: number;
    firstMillionDate?: string;
  };
  withPurchase: {
    netWorthAt5Years: number;
    netWorthAt10Years: number;
    firstMillionDate?: string;
    netWorthReduction: number;
    milestoneDelays: Record<string, string>;
  };
}

// Side-by-side or stacked comparison
// Highlights:
// - Net worth deltas (with percentages)
// - Milestone delays (in months/years)
// - Cashflow impact chart
// Color-coded: Green (positive), Red (negative), Gray (neutral)
```

### Monthly Review Net Worth Trajectory (v3.0)
```tsx
interface NetWorthTrajectoryProps {
  historicalData: Array<{ date: string; netWorth: number }>;  // 12 months
  projectionData: Array<{ date: string; netWorth: number }>;  // 12 months
  milestoneMarkers: Array<{ amount: number; label: string }>;
}

// Line chart with:
// - Solid line: Historical (past 12 months)
// - Dashed line: Projection (next 12 months)
// - Milestone markers (horizontal lines with labels)
// - Tooltip: Date, amount, trend
```

## Charts

### NetWorthChart
```tsx
<NetWorthChart 
  data={netWorthHistory}  // Array<{ date: string, value: number, accounts?: [...] }>
  currency="ZAR"
  height={300}
/>
```

### MetricSparkline
```tsx
<MetricSparkline
  data={points}           // Array<{ date: string, value: number }>
  targetValue={10000}
  targetDirection="AtOrAbove"
  currentValue={8500}
  height={50}
/>
```

## Accessibility

- All interactive elements keyboard accessible
- ARIA labels on icon-only buttons
- Focus visible outlines
- Color contrast meets WCAG 2.1 AA
- Screen reader announcements for dynamic content

## Feature: Enhanced Dimensions Pages

### Overview
The Dimensions feature provides a comprehensive view of life dimensions with goal tracking, task management, and metric visualization. This enhancement addresses user feedback about limited functionality.

### User Requirements Addressed
1. **More functionality on Dimensions page** - Add summary stats, quick actions, metric previews
2. **Task management** - Full CRUD for tasks within dimension context
3. **Info/help text** - Collapsible info sections explaining each dimension
4. **Goals linked to metrics** - Visual metric progress for milestones with targets

### Page: Dimensions List (`/dimensions`)

#### Layout Structure
```
┌─────────────────────────────────────────────────────────────┐
│ Dimensions                                                   │
│ Track and optimize all areas of your life                   │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│ │ [Icon]      │ │ [Icon]      │ │ [Icon]      │ │ [Icon]  │ │
│ │ Health   >  │ │ Mind     >  │ │ Work     >  │ │ Money > │ │
│ │             │ │             │ │             │ │         │ │
│ │ Score: 78   │ │ Score: 85   │ │ Score: 62   │ │ Scr: 71 │ │
│ │ ████████░░  │ │ █████████░  │ │ ██████░░░░  │ │ ███████░│ │
│ │             │ │             │ │             │ │         │ │
│ │ 3 tasks     │ │ 2 tasks     │ │ 5 tasks     │ │ 1 task  │ │
│ │ 2 milestones│ │ 1 milestone │ │ 3 milestones│ │ 2 miles │ │
│ │             │ │             │ │             │ │         │ │
│ │ [⚡ Quick]  │ │ [⚡ Quick]  │ │ [⚡ Quick]  │ │[⚡Quick]│ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│ │ Relation... │ │ Play        │ │ Growth      │ │Community│ │
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Enhanced Card Information
Each dimension card displays:
- **Icon & Name** - Visual identifier
- **Score with progress bar** - Current dimension score (0-100)
- **Active counts** - Number of tasks and milestones
- **Quick Action button** - Opens quick-add modal (task or milestone)
- **Trend indicator** - Optional score trend (up/down/stable)

#### Quick Actions Dropdown
```tsx
interface QuickAction {
  label: string;
  icon: React.ComponentType;
  action: () => void;
}

// Actions per dimension card
const quickActions: QuickAction[] = [
  { label: 'Add Task', icon: PlusCircle, action: openAddTaskModal },
  { label: 'Add Milestone', icon: Target, action: openAddMilestoneModal },
  { label: 'View Metrics', icon: BarChart2, action: navigateToMetrics },
];
```

### Page: Dimension Detail (`/dimensions/:dimensionId`)

#### Layout Structure
```
┌─────────────────────────────────────────────────────────────┐
│ ← Back to Dimensions                                        │
├─────────────────────────────────────────────────────────────┤
│ [Icon]  Health                                              │
│         Physical well-being and fitness                     │
├─────────────────────────────────────────────────────────────┤
│ ┌─ ⓘ What is Health? ─────────────────────────────────────┐ │
│ │ The Health dimension tracks your physical well-being...  │ │
│ │ This includes exercise habits, nutrition metrics,        │ │
│ │ sleep quality, and health-related goals.                 │ │
│ │                                                          │ │
│ │ Key areas: Exercise • Nutrition • Sleep • Medical        │ │
│ └─────────────────────────────────────────────[Collapse]──┘ │
├─────────────────────────────────────────────────────────────┤
│ ┌────────────┐ ┌────────────┐ ┌────────────┐               │
│ │ Score      │ │ Weight     │ │ Active     │               │
│ │   78/100   │ │   12.5%    │ │  5 items   │               │
│ │ ████████░░ │ │            │ │ 2M + 3T    │               │
│ └────────────┘ └────────────┘ └────────────┘               │
├─────────────────────────────────────────────────────────────┤
│ 📊 METRICS                                    [View All →] │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Weight        68kg → 65kg target    ████████░░ 85%      │ │
│ │ Steps/Day     8,500 → 10,000 target ████████░░ 85%      │ │
│ │ Sleep Hours   7.2 → 8.0 target      █████████░ 90%      │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ 🎯 MILESTONES                               [+ Add Milestone]│
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ ○ Run a marathon         Target: Dec 2025              │ │
│ │   └─ Linked: Running distance ≥ 42km  [███░░░░ 28%]    │ │
│ │                                        [Edit] [Delete] │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ ● Reach target weight    Completed: Nov 2024           │ │
│ │   └─ Linked: Weight ≤ 70kg            [██████████ ✓]   │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ ✅ TASKS                                        [+ Add Task]│
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Filter: [All▾] [Active▾] [Type▾]           Search: [__] │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ □ Morning run              Habit  Daily    [✓][✎][🗑]   │ │
│ │   └─ Streak: 12 days 🔥                                │ │
│ │ □ Gym session              Habit  3x/week  [✓][✎][🗑]   │ │
│ │ □ Book doctor appointment  One-off         [✓][✎][🗑]   │ │
│ │ ☑ Meal prep Sunday         Scheduled Dec 15 [↩][✎][🗑]  │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Section: Dimension Info (Collapsible)

```tsx
interface DimensionInfo {
  dimensionCode: string;
  title: string;           // "What is {dimension}?"
  description: string;     // Multi-paragraph explanation
  keyAreas: string[];      // Bullet points of focus areas
  tips?: string[];         // Optional usage tips
}

// Info content per dimension (stored in frontend constants or fetched)
const DIMENSION_INFO: Record<string, DimensionInfo> = {
  health: {
    dimensionCode: 'health',
    title: 'What is Health?',
    description: 'The Health dimension tracks your physical well-being...',
    keyAreas: ['Exercise', 'Nutrition', 'Sleep', 'Medical Checkups'],
    tips: ['Set realistic daily step goals', 'Track water intake']
  },
  // ... other dimensions
};
```

#### Section: Linked Metrics Display

```tsx
interface LinkedMetricDisplay {
  code: string;
  name: string;
  currentValue: number | null;
  targetValue: number | null;
  targetDirection: 'AtOrAbove' | 'AtOrBelow';
  unit: string;
  progressPercent: number;    // Calculated: (current/target) * 100
  trend: 'up' | 'down' | 'stable';
}

// Metric card component
<MetricProgressCard
  metric={metric}
  showTarget={true}
  onClick={() => navigate(`/metrics/${metric.code}`)}
/>
```

#### Section: Milestones with Metric Linkage

Enhanced milestone display showing:
- Title and target date
- Status indicator (active/completed/abandoned)
- **Linked metric progress** (if `targetMetricCode` is set):
  - Metric name and current value
  - Target value with direction indicator
  - Progress bar with percentage
- Edit/Delete actions

```tsx
interface MilestoneWithProgress {
  id: string;
  title: string;
  description?: string;
  targetDate?: string;
  status: 'active' | 'completed' | 'abandoned';
  // Metric linkage
  targetMetricCode?: string;
  targetMetricValue?: number;
  // Computed from metric data
  currentMetricValue?: number;
  metricName?: string;
  metricUnit?: string;
  progressPercent?: number;
}
```

#### Section: Task Management (Full CRUD)

##### Task List Features
- **Filtering**: By type (habit/one_off/scheduled), status (active/completed), search
- **Sorting**: By due date, title, creation date
- **Inline actions**: Complete, Edit, Delete, Uncomplete (for mistakes)

##### Task Item Display
```tsx
interface TaskDisplay {
  id: string;
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency?: string;
  scheduledDate?: string;
  isCompleted: boolean;
  isActive: boolean;
  // For habits
  streakDays?: number;
  linkedMetricCode?: string;
  // UI state
  isExpanded: boolean;
}

// Task row with actions
<TaskRow
  task={task}
  onComplete={() => completeTask(task.id)}
  onEdit={() => openEditModal(task)}
  onDelete={() => confirmDelete(task.id)}
  onUncomplete={() => uncompleteTask(task.id)}
/>
```

##### Add/Edit Task Modal

```tsx
interface TaskFormData {
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency?: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId: string;        // Pre-filled from context
  milestoneId?: string;       // Optional: link to milestone
  linkedMetricCode?: string;  // Optional: for habits
  scheduledDate?: string;     // For scheduled_event
  scheduledTime?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

// Form sections
1. Basic Info (title, description)
2. Task Type selector with contextual fields
3. Scheduling (frequency, dates)
4. Linking (milestone dropdown, metric dropdown)
5. Tags (multi-select or free-form)
```

### Component Hierarchy

```
src/pages/
├── Dimensions.tsx (enhanced)
│   └── components/
│       ├── DimensionCard.tsx (enhanced with counts, quick actions)
│       └── QuickActionDropdown.tsx
│
└── DimensionDetail.tsx (enhanced)
    └── components/
        ├── DimensionInfoSection.tsx (collapsible info)
        ├── DimensionStatsRow.tsx (score, weight, counts)
        ├── LinkedMetricsSection.tsx (metric progress cards)
        ├── MilestonesSection.tsx (with metric progress)
        │   ├── MilestoneCard.tsx
        │   └── AddMilestoneModal.tsx (enhanced)
        └── TasksSection.tsx (full CRUD)
            ├── TaskFilters.tsx
            ├── TaskList.tsx
            ├── TaskRow.tsx
            └── AddEditTaskModal.tsx
```

### State Management (RTK Query)

```typescript
// New/enhanced endpoints needed
const tasksApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    // Get tasks for dimension with full details
    getDimensionTasks: builder.query<TaskListResponse, {
      dimensionId: string;
      taskType?: string;
      isCompleted?: boolean;
      isActive?: boolean;
      page?: number;
      perPage?: number;
    }>({
      query: (params) => ({
        url: '/api/tasks',
        params: {
          dimensionId: params.dimensionId,
          taskType: params.taskType,
          isCompleted: params.isCompleted,
          isActive: params.isActive,
          page: params.page || 1,
          perPage: params.perPage || 50,
        },
      }),
      providesTags: ['Tasks', 'Dimensions'],
    }),

    // Create task
    createTask: builder.mutation<TaskDetailResponse, CreateTaskRequest>({
      query: (body) => ({
        url: '/api/tasks',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard'],
    }),

    // Update task
    updateTask: builder.mutation<TaskDetailResponse, { id: string } & UpdateTaskRequest>({
      query: ({ id, ...body }) => ({
        url: `/api/tasks/${id}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: ['Tasks', 'Dimensions'],
    }),

    // Complete task
    completeTask: builder.mutation<TaskCompleteResponse, { id: string; metricValue?: number }>({
      query: ({ id, metricValue }) => ({
        url: `/api/tasks/${id}/complete`,
        method: 'POST',
        body: { metricValue },
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard', 'Streaks'],
    }),

    // Delete task
    deleteTask: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/tasks/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Tasks', 'Dimensions', 'Dashboard'],
    }),
  }),
});

// Enhanced dimension query to include metrics
const dimensionsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getDimensionWithMetrics: builder.query<DimensionDetailWithMetrics, string>({
      query: (id) => `/api/dimensions/${id}?include=metrics`,
      providesTags: (result, error, id) => [{ type: 'Dimensions', id }],
    }),
  }),
});
```

### TypeScript Types

```typescript
// Task types (add to types/index.ts)
export interface LifeTask {
  id: string;
  title: string;
  description?: string;
  taskType: 'habit' | 'one_off' | 'scheduled_event';
  frequency: 'daily' | 'weekly' | 'monthly' | 'ad_hoc';
  dimensionId?: string;
  dimensionCode?: string;
  milestoneId?: string;
  linkedMetricCode?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  startDate: string;
  endDate?: string;
  isCompleted: boolean;
  completedAt?: string;
  isActive: boolean;
  tags?: string[];
  // Computed
  streakDays?: number;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  taskType: string;
  frequency?: string;
  dimensionId?: string;
  milestoneId?: string;
  linkedMetricCode?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  frequency?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  endDate?: string;
  isActive?: boolean;
  tags?: string[];
}

export interface TaskListResponse {
  data: Array<{
    id: string;
    type: 'task';
    attributes: LifeTask;
  }>;
  meta: {
    page: number;
    perPage: number;
    total: number;
    totalPages: number;
  };
}

// Enhanced dimension types
export interface DimensionWithMetrics extends DimensionDetailData {
  linkedMetrics: LinkedMetricDisplay[];
}

export interface LinkedMetricDisplay {
  code: string;
  name: string;
  currentValue: number | null;
  targetValue: number | null;
  targetDirection: 'AtOrAbove' | 'AtOrBelow';
  unit: string;
  progressPercent: number;
}
```

### UI/UX Specifications

#### Info Section Behavior
- **Default state**: Collapsed (shows only title)
- **Expanded state**: Shows full description, key areas, tips
- **Persistence**: Remember collapse state in localStorage per dimension
- **Animation**: Smooth expand/collapse transition (200ms)

#### Task Actions
| Action | Icon | Behavior |
|--------|------|----------|
| Complete | ✓ (CheckCircle) | Mark complete, update streak if habit |
| Edit | ✎ (Pencil) | Open edit modal |
| Delete | 🗑 (Trash) | Confirm dialog, then delete |
| Uncomplete | ↩ (Undo) | Only for recently completed, reactivates task |

#### Milestone Progress Display
- Progress bar color based on percentage:
  - 0-25%: Gray
  - 26-50%: Yellow
  - 51-75%: Blue
  - 76-99%: Green
  - 100%: Green with checkmark
- Show "On track" / "Behind" indicator based on target date proximity

#### Responsive Breakpoints
- **Mobile (< 640px)**: Stack all sections vertically, full-width cards
- **Tablet (640-1024px)**: 2-column grid for tasks/milestones
- **Desktop (> 1024px)**: 3-column stats, side-by-side sections

### Accessibility Requirements
- All interactive elements keyboard accessible
- Proper ARIA labels on action buttons
- Screen reader announcements for task completion
- Focus management when modals open/close
- Color contrast meets WCAG 2.1 AA

## v1.1 New Components

### Identity Radar Component
```tsx
interface IdentityRadarProps {
  currentStats: Record<string, number>;  // strength: 62, wisdom: 78, ...
  targetStats: Record<string, number>;   // strength: 75, wisdom: 95, ...
  size?: 'sm' | 'md' | 'lg';
}

// Renders a radar/spider chart with 7 axes for primary stats
// Current values in solid fill, targets as dashed outline
<IdentityRadar currentStats={stats} targetStats={targets} size="md" />
```

### What-If Wizard Component
```tsx
interface WhatIfWizardProps {
  baselineScenarioId: string;
  onComplete: (result: WhatIfResult) => void;
}

// Step 1: Enter purchase details (amount, date, category)
// Step 2: Select comparison scenario (or use baseline)
// Step 3: View impact analysis (net worth delta, milestone delays)
```

### Weekly Review Dashboard
```tsx
interface WeeklyReviewProps {
  review: WeeklyReviewData;
}

// Layout:
// ┌─────────────────────────────────────────────────┐
// │ Week of Dec 2-8, 2024                           │
// ├─────────────────────────────────────────────────┤
// │ Health Index: 78 (+2.5 ▲)  Adherence: 85 (-1.2 ▼)
// │ Wealth Health: 72 (+0.8 ▲) Longevity: +0.1 yrs  │
// ├─────────────────────────────────────────────────┤
// │ 🔥 Top Streaks                                  │
// │   Morning run: 28 days                          │
// │   Meditation: 14 days                           │
// ├─────────────────────────────────────────────────┤
// │ 📋 Recommended Actions                          │
// │   ⚡ [HIGH] Increase sleep to 8 hours           │
// │   📊 [MED] Review investment allocations        │
// └─────────────────────────────────────────────────┘
```

### Scenario Comparison Chart
```tsx
interface ScenarioComparisonChartProps {
  baseline: ProjectionData;
  scenarios: ProjectionData[];
  horizonYears: number;
}

// Multi-line chart showing net worth projections
// Baseline as solid line, scenarios as dashed lines
// Milestone markers at target amounts
```

### Onboarding Wizard Steps
```tsx
// Step 1: Health Baselines
interface HealthBaselinesStep {
  currentWeight: number;
  targetWeight: number;
  currentBodyFat?: number;
  targetBodyFat?: number;
  height: number;
  birthDate: string;
}

// Step 2: Major Goals
interface MajorGoalsStep {
  financialGoals: Array<{
    description: string;    // "1M by 40"
    targetAmount: number;
    targetAge: number;
  }>;
  lifeMilestones: Array<{
    description: string;
    targetDate?: string;
    dimension: string;
  }>;
}

// Step 3: Identity Profile
interface IdentityStep {
  archetype: string;         // Selected or custom
  values: string[];          // Top 4-5 values
  primaryStatFocus: string[]; // Which stats to prioritize
}
```

### Enhanced Streak Card (v1.1)
```tsx
interface StreakCardProps {
  task: LifeTask;
  streak: StreakData;
  showPenalty?: boolean;
}

// Displays:
// - Current streak length with fire emoji
// - Longest streak record
// - Consecutive misses warning (if > 0)
// - Risk penalty score (if showPenalty)
// - Visual indicator: green (healthy), yellow (at risk), red (broken)
```

## v1.1 New Routes

```typescript
// router.tsx additions
{ path: 'onboarding', element: <Onboarding /> },
{ path: 'onboarding/:step', element: <OnboardingStep /> },
{ path: 'reviews', element: <Reviews /> },
{ path: 'reviews/weekly', element: <WeeklyReview /> },
{ path: 'reviews/monthly', element: <MonthlyReview /> },
{ path: 'settings/identity', element: <IdentityProfileSettings /> },
```

## v3.0 New RTK Query Endpoints

```typescript
// MCP Tools endpoints
getMCPTools: builder.query<MCPToolsListResponse, void>({...}),
executeMCPTool: builder.mutation<MCPToolResponse, { toolName: string; params: unknown }>({...}),

// Score History endpoints
getCurrentScores: builder.query<CurrentScoresResponse, void>({...}),
getScoreHistory: builder.query<ScoreHistoryResponse, { dateFrom?: string; dateTo?: string; interval?: string }>({...}),
getDimensionScores: builder.query<DimensionScoresResponse, { dimensionId: string; dateFrom?: string; dateTo?: string }>({...}),

// Task Completions endpoints
getTaskCompletions: builder.query<TaskCompletionsResponse, { taskId: string; dateFrom?: string; dateTo?: string }>({...}),
getAutoEvaluatedTasks: builder.query<AutoEvaluatedTasksResponse, void>({...}),

// Enhanced Metrics endpoints
recordMetrics: builder.mutation<RecordMetricsResponse, RecordMetricsRequest>({...}),  // v3.0 enhanced
getMetricHistory: builder.query<MetricHistoryResponse, { metricCode: string; dateFrom?: string; dateTo?: string; aggregation?: string }>({...}),
listMetricDefinitions: builder.query<MetricDefinitionsResponse, { dimensionCode?: string }>({...}),
```

## v1.1 New RTK Query Endpoints

```typescript
// Identity Profile endpoints
getIdentityProfile: builder.query<IdentityProfileResponse, void>({...}),
updateIdentityProfile: builder.mutation<void, IdentityProfileRequest>({...}),

// Primary Stats endpoints  
getPrimaryStats: builder.query<PrimaryStatsResponse, void>({...}),
getPrimaryStatsHistory: builder.query<StatsHistoryResponse, { days: number }>({...}),

// Reviews endpoints
getWeeklyReview: builder.query<WeeklyReviewResponse, void>({...}),
getMonthlyReview: builder.query<MonthlyReviewResponse, void>({...}),
getReviewHistory: builder.query<ReviewHistoryResponse, { type: string }>({...}),

// Onboarding endpoints
getOnboardingStatus: builder.query<OnboardingStatusResponse, void>({...}),
submitOnboardingStep: builder.mutation<void, { step: string; data: unknown }>({...}),

// Scenario Comparison endpoints
compareScenarios: builder.mutation<ComparisonResponse, ComparisonRequest>({...}),
getWhatIfAnalysis: builder.query<WhatIfResponse, WhatIfParams>({...}),
```