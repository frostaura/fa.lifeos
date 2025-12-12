# E2E Testing Architecture

## Overview
Comprehensive Playwright-based E2E testing for LifeOS v3.0, designed to test the fully Dockerized stack (postgres:5432, backend:5001, frontend:5173) with minimal data mutation and extensive visual regression coverage.

**Design Principles**:
- **Non-Destructive**: Read-only tests where possible, use dedicated test user accounts
- **Docker-First**: Tests run against `docker-compose.yml` stack
- **Visual Excellence**: Screenshot baselines at all breakpoints for glassmorphic UI
- **Realistic Data**: Tests use actual dev data to validate real-world scenarios
- **Fast Feedback**: Parallel execution, smart retries, isolated test contexts

## Test Architecture

### Physical Structure
```
fa.lifeos/
├── playwright.config.ts           # Configuration (browsers, baseURL, retries)
├── e2e/
│   ├── auth/
│   │   ├── login.spec.ts          # JWT login/logout flows
│   │   ├── webauthn.spec.ts       # Passkey registration/auth
│   │   └── refresh-token.spec.ts  # Token refresh scenarios
│   ├── dashboard/
│   │   ├── overview.spec.ts       # LifeOS Score rings, Primary Stats
│   │   ├── dimensions.spec.ts     # 8 dimensions display
│   │   └── responsive.spec.ts     # Mobile/tablet/desktop layouts
│   ├── metrics/
│   │   ├── record-ui.spec.ts      # UI-based metric recording
│   │   ├── record-api.spec.ts     # Direct API metric ingestion
│   │   └── history.spec.ts        # Metric history browsing
│   ├── tasks/
│   │   ├── lifecycle.spec.ts      # Create, complete, delete
│   │   ├── streaks.spec.ts        # Streak calculation, forgiving logic
│   │   └── auto-eval.spec.ts      # Task auto-evaluation via metrics
│   ├── finances/
│   │   ├── accounts.spec.ts       # Account CRUD
│   │   ├── transactions.spec.ts   # Transaction recording
│   │   ├── simulation.spec.ts     # What-if scenarios
│   │   └── wealth-score.spec.ts   # Wealth health score calculation
│   ├── reviews/
│   │   ├── weekly.spec.ts         # Weekly review generation
│   │   └── monthly.spec.ts        # Monthly review insights
│   ├── onboarding/
│   │   └── wizard.spec.ts         # 7-step onboarding flow
│   ├── mcp/
│   │   └── tools.spec.ts          # MCP tool endpoints validation
│   └── visual-regression/
│       ├── baseline.spec.ts       # Generate screenshot baselines
│       ├── dashboard.visual.ts    # Dashboard visual tests
│       ├── forms.visual.ts        # Form component visual tests
│       └── mobile.visual.ts       # Mobile breakpoint visuals
├── e2e/fixtures/
│   ├── auth.fixture.ts            # Authenticated page fixture
│   ├── test-data.ts               # Shared test data constants
│   └── api-helpers.ts             # Direct API call utilities
└── e2e/utils/
    ├── wait-for-docker.ts         # Docker health check utility
    ├── visual-compare.ts          # Screenshot diff utilities
    └── metric-factory.ts          # Metric test data generators
```

### Playwright Configuration

**Target Environment**: Docker Compose stack
- Frontend: `http://localhost:5173`
- Backend: `http://localhost:5001/api`
- Database: `postgres:5432` (indirect via backend)

**Browser Matrix**:
- Chromium (primary)
- Firefox (compatibility)
- WebKit (Safari compatibility)

**Viewport Breakpoints**:
- Mobile: 375×667 (iPhone SE)
- Tablet: 768×1024 (iPad)
- Desktop: 1920×1080 (Full HD)

**Execution Settings**:
- Parallel workers: 4
- Retries: 2 (flaky network tolerance)
- Timeout: 30s per test
- Video: on-first-retry
- Screenshot: only-on-failure

**Base Configuration**:
```typescript
// playwright.config.ts
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 4,
  reporter: [
    ['html', { outputFolder: 'test-results/html' }],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/junit.xml' }]
  ],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10000,
  },
  projects: [
    {
      name: 'chromium-desktop',
      use: { 
        ...devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 }
      },
    },
    {
      name: 'chromium-tablet',
      use: { 
        ...devices['iPad Pro'],
        viewport: { width: 1024, height: 1366 }
      },
    },
    {
      name: 'chromium-mobile',
      use: { 
        ...devices['iPhone 13'],
        viewport: { width: 390, height: 844 }
      },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],
  webServer: {
    command: 'docker-compose up',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000, // 2 minutes for container startup
  },
});
```

## Critical User Journeys

### 1. Authentication Flow (Priority: P0)

**Test Cases**:
- JWT login with email/password
- Login validation (wrong password, non-existent user)
- Token refresh on expiry
- Logout clears session
- WebAuthn passkey registration
- WebAuthn login
- Token persistence across page reload

**Data Strategy**: 
- Test users: `test-user@lifeos.local`, `admin@lifeos.local`
- Pre-seeded in dev database, never mutated

**Visual Checkpoints**:
- Login form (all states: empty, filled, error, loading)
- WebAuthn prompts
- Dashboard after successful login

**Example Test Structure**:
```typescript
// e2e/auth/login.spec.ts
test('successful login redirects to dashboard', async ({ page }) => {
  await page.goto('/login');
  await page.fill('[data-testid="email-input"]', 'test-user@lifeos.local');
  await page.fill('[data-testid="password-input"]', 'Test123!');
  await page.click('[data-testid="login-button"]');
  
  // Verify JWT stored
  const token = await page.evaluate(() => localStorage.getItem('accessToken'));
  expect(token).toBeTruthy();
  
  // Verify navigation
  await page.waitForURL('/dashboard');
  await expect(page.locator('[data-testid="lifeos-score"]')).toBeVisible();
});
```

### 2. Dashboard Viewing (Priority: P0)

**Test Cases**:
- LifeOS Score rings display correctly
- Primary Stats (Health Index, Adherence, Wealth Health, Longevity) shown
- 8 Dimension cards render with latest scores
- Responsive layout adapts to breakpoints
- Empty state when no data
- Loading states during data fetch
- Real-time updates via SignalR

**Data Strategy**:
- Use existing dev data for read-only verification
- No mutations

**Visual Checkpoints**:
- Full dashboard at 3 breakpoints
- Each ring component isolated
- Dimension cards (all 8)
- Glassmorphic UI elements

**Key Assertions**:
```typescript
// e2e/dashboard/overview.spec.ts
test('dashboard displays LifeOS score and components', async ({ page }) => {
  await authenticatedPage(page); // Fixture handles login
  await page.goto('/dashboard');
  
  // Verify score rings present
  await expect(page.locator('[data-testid="lifeos-score-ring"]')).toBeVisible();
  await expect(page.locator('[data-testid="health-index-ring"]')).toBeVisible();
  await expect(page.locator('[data-testid="adherence-ring"]')).toBeVisible();
  await expect(page.locator('[data-testid="wealth-health-ring"]')).toBeVisible();
  
  // Verify scores are numeric
  const lifeosScore = await page.locator('[data-testid="lifeos-score-value"]').textContent();
  expect(parseFloat(lifeosScore)).toBeGreaterThanOrEqual(0);
  expect(parseFloat(lifeosScore)).toBeLessThanOrEqual(100);
  
  // Visual regression
  await page.screenshot({ path: 'e2e/screenshots/dashboard-overview.png', fullPage: true });
});
```

### 3. Metrics Recording (Priority: P0)

**Test Cases - UI Recording**:
- Open metric recording modal
- Select dimension and metric
- Enter value and timestamp
- Submit and verify success toast
- Verify metric appears in history

**Test Cases - API Recording**:
- Direct POST to `/api/metrics/record`
- Nested metric ingestion
- Validation errors for unknown metric codes
- Metric with derived flag handling

**Data Strategy**:
- Create temporary metrics with unique identifiers
- Delete after test (or use transaction rollback if possible)
- Alternatively: dedicated test user with isolated data

**Visual Checkpoints**:
- Metric recording modal (empty, filled, validation errors)
- Success notification
- Metric history table

**Example Test**:
```typescript
// e2e/metrics/record-ui.spec.ts
test('record metric via UI', async ({ page }) => {
  await authenticatedPage(page);
  await page.goto('/dashboard');
  
  // Open recording modal
  await page.click('[data-testid="record-metric-button"]');
  await expect(page.locator('[data-testid="metric-modal"]')).toBeVisible();
  
  // Fill form
  await page.selectOption('[data-testid="dimension-select"]', 'Physical Health');
  await page.selectOption('[data-testid="metric-select"]', 'Weight (kg)');
  await page.fill('[data-testid="value-input"]', '75.5');
  
  // Submit
  await page.click('[data-testid="submit-metric"]');
  
  // Verify success
  await expect(page.locator('[data-testid="toast-success"]')).toContainText('Metric recorded');
  
  // Verify in history
  await page.goto('/metrics/history');
  await expect(page.locator('text=Weight (kg)')).toBeVisible();
  await expect(page.locator('text=75.5')).toBeVisible();
});
```

### 4. Task Management (Priority: P0)

**Test Cases**:
- Create task with title, description, due date
- Complete task (verify completion timestamp)
- Streak calculation after consecutive completions
- Forgiving logic: first miss has no penalty
- Escalating penalty: 2nd miss = 5 points, 3rd+ = 10×(n-1)
- Task auto-evaluation: task completes when linked metric meets target

**Data Strategy**:
- Create test tasks with unique prefixes (`[E2E-Test]`)
- Clean up after test completion

**Visual Checkpoints**:
- Task list (empty, populated)
- Task form (create, edit modes)
- Streak badge animations
- Completion confirmation

**Example Test**:
```typescript
// e2e/tasks/lifecycle.spec.ts
test('complete task updates streak', async ({ page }) => {
  await authenticatedPage(page);
  await page.goto('/tasks');
  
  // Create task
  await page.click('[data-testid="create-task"]');
  await page.fill('[data-testid="task-title"]', '[E2E-Test] Daily Workout');
  await page.click('[data-testid="save-task"]');
  
  // Complete task
  const taskRow = page.locator('text=[E2E-Test] Daily Workout').locator('..');
  await taskRow.locator('[data-testid="complete-task"]').click();
  
  // Verify streak
  await expect(taskRow.locator('[data-testid="streak-badge"]')).toContainText('1');
  
  // Cleanup
  await taskRow.locator('[data-testid="delete-task"]').click();
  await page.click('[data-testid="confirm-delete"]');
});
```

### 5. Finance Simulation (Priority: P1)

**Test Cases**:
- Navigate to Finance section
- View accounts and current balances
- Create what-if scenario
- Adjust scenario parameters (income, expenses, savings rate)
- Run simulation
- Compare baseline vs scenario projections
- View wealth health score impact

**Data Strategy**:
- Use existing dev accounts (read-only)
- Scenarios are ephemeral (stored in memory/session)
- No persistent data mutations

**Visual Checkpoints**:
- Account summary cards
- Simulation controls
- Projection chart (baseline vs scenario)
- Wealth health score comparison

**Example Test**:
```typescript
// e2e/finances/simulation.spec.ts
test('run what-if scenario shows projection', async ({ page }) => {
  await authenticatedPage(page);
  await page.goto('/finances/simulation');
  
  // Create scenario
  await page.click('[data-testid="new-scenario"]');
  await page.fill('[data-testid="scenario-name"]', 'Increase Savings');
  await page.fill('[data-testid="savings-rate"]', '25'); // 25% savings rate
  
  // Run simulation
  await page.click('[data-testid="run-simulation"]');
  
  // Wait for chart render
  await expect(page.locator('[data-testid="projection-chart"]')).toBeVisible();
  
  // Verify two lines present (baseline + scenario)
  const lines = page.locator('[data-testid="projection-chart"] path[stroke]');
  await expect(lines).toHaveCount(2);
  
  // Visual regression
  await page.screenshot({ path: 'e2e/screenshots/simulation-result.png' });
});
```

### 6. Weekly/Monthly Reviews (Priority: P1)

**Test Cases**:
- Navigate to Reviews section
- Trigger weekly review generation
- Verify review components: Health Index, Adherence, Wealth Health, Longevity
- View metric contributions per dimension
- Monthly review aggregates 4 weeks
- Export review as PDF/JSON

**Data Strategy**:
- Use existing historical data
- Read-only verification

**Visual Checkpoints**:
- Review summary page
- Score breakdown charts
- Metric contribution tables

**Example Test**:
```typescript
// e2e/reviews/weekly.spec.ts
test('weekly review displays score components', async ({ page }) => {
  await authenticatedPage(page);
  await page.goto('/reviews/weekly');
  
  // Select current week
  await page.click('[data-testid="current-week"]');
  
  // Verify review sections
  await expect(page.locator('[data-testid="health-index-section"]')).toBeVisible();
  await expect(page.locator('[data-testid="adherence-section"]')).toBeVisible();
  await expect(page.locator('[data-testid="wealth-health-section"]')).toBeVisible();
  await expect(page.locator('[data-testid="longevity-section"]')).toBeVisible();
  
  // Verify scores are numeric
  const healthScore = await page.locator('[data-testid="health-index-value"]').textContent();
  expect(parseFloat(healthScore)).toBeGreaterThanOrEqual(0);
});
```

### 7. Onboarding Flow (Priority: P1)

**Test Cases**:
- New user sees onboarding wizard
- Step 1: Welcome screen
- Step 2: Identity profile (name, age, goals)
- Step 3: Dimension priorities
- Step 4: Initial metrics setup
- Step 5: Task templates selection
- Step 6: Financial accounts setup
- Step 7: Completion and redirect to dashboard
- Skip onboarding redirects to dashboard immediately

**Data Strategy**:
- Create ephemeral test user
- Delete user after test completion

**Visual Checkpoints**:
- Each onboarding step
- Progress indicator
- Completion celebration screen

**Example Test**:
```typescript
// e2e/onboarding/wizard.spec.ts
test('complete onboarding wizard', async ({ page }) => {
  // Create new user via API
  const testUser = await createTestUser({ email: 'onboarding-test@lifeos.local' });
  
  // Login as new user
  await page.goto('/login');
  await page.fill('[data-testid="email-input"]', testUser.email);
  await page.fill('[data-testid="password-input"]', testUser.password);
  await page.click('[data-testid="login-button"]');
  
  // Should auto-redirect to onboarding
  await page.waitForURL('/onboarding');
  
  // Step 1: Welcome
  await expect(page.locator('text=Welcome to LifeOS')).toBeVisible();
  await page.click('[data-testid="next-step"]');
  
  // Step 2: Identity
  await page.fill('[data-testid="name-input"]', 'Test User');
  await page.fill('[data-testid="age-input"]', '30');
  await page.click('[data-testid="next-step"]');
  
  // ... (continue through all steps)
  
  // Final step
  await page.click('[data-testid="complete-onboarding"]');
  
  // Should redirect to dashboard
  await page.waitForURL('/dashboard');
  await expect(page.locator('[data-testid="lifeos-score"]')).toBeVisible();
  
  // Cleanup
  await deleteTestUser(testUser.id);
});
```

## Visual Regression Strategy

### Screenshot Baseline Management

**Baseline Generation**:
```bash
# Generate initial baselines (run once)
npm run test:visual -- --update-snapshots

# Baselines stored in:
# e2e/__screenshots__/
#   ├── chromium-desktop/
#   ├── chromium-tablet/
#   ├── chromium-mobile/
#   ├── firefox/
#   └── webkit/
```

**Comparison Thresholds**:
- Pixel difference threshold: 0.2% (allow minor anti-aliasing)
- Max diff pixels: 100 (ignore tiny glitches)
- Fail on layout shifts > 5px

**Visual Test Coverage**:

| Component/Page | Breakpoints | States |
|----------------|-------------|--------|
| Dashboard | Desktop, Tablet, Mobile | Empty, Populated, Loading |
| LifeOS Score Rings | Desktop, Tablet, Mobile | 0%, 50%, 100% |
| Metric Form | Desktop, Tablet, Mobile | Empty, Filled, Error |
| Task List | Desktop, Tablet, Mobile | Empty, Populated, Completed |
| Finance Charts | Desktop, Tablet | Baseline, Simulation |
| Review Summary | Desktop, Tablet | Weekly, Monthly |
| Onboarding Steps | Desktop, Tablet, Mobile | All 7 steps |

**Visual Test Pattern**:
```typescript
// e2e/visual-regression/dashboard.visual.ts
test.describe('Dashboard Visual Regression', () => {
  test.use({ viewport: { width: 1920, height: 1080 } }); // Desktop

  test('dashboard with data', async ({ page }) => {
    await authenticatedPage(page);
    await page.goto('/dashboard');
    
    // Wait for data load
    await page.waitForSelector('[data-testid="lifeos-score-ring"]');
    
    // Hide dynamic timestamps
    await page.evaluate(() => {
      document.querySelectorAll('[data-testid="timestamp"]').forEach(el => {
        el.textContent = '2024-01-01 12:00:00';
      });
    });
    
    // Screenshot comparison
    await expect(page).toHaveScreenshot('dashboard-overview.png', {
      maxDiffPixels: 100,
      threshold: 0.2,
    });
  });

  test('dashboard mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await authenticatedPage(page);
    await page.goto('/dashboard');
    
    await page.waitForSelector('[data-testid="lifeos-score-ring"]');
    await expect(page).toHaveScreenshot('dashboard-mobile.png');
  });
});
```

### Glassmorphic UI Testing

**Challenges**:
- Blur effects may vary across browsers
- Transparency requires consistent background
- Animation timing affects screenshots

**Mitigation**:
```typescript
// Disable animations for consistent screenshots
await page.addStyleTag({
  content: `
    *, *::before, *::after {
      animation-duration: 0s !important;
      transition-duration: 0s !important;
    }
  `
});

// Ensure blur effects rendered
await page.waitForTimeout(500); // Allow GPU to finish rendering
```

## Test Data Management

### Seed Data Strategy

**Development Database** (`fa_lifeos`):
- Pre-seeded test users with stable credentials
- Representative historical data for read-only tests
- Preserved across test runs

**Test Users** (pre-seeded in dev DB):
```sql
-- Test user with full data
INSERT INTO users (id, email, password_hash, created_at)
VALUES ('test-user-uuid', 'test-user@lifeos.local', '<hash>', NOW());

-- Admin user
INSERT INTO users (id, email, password_hash, role, created_at)
VALUES ('admin-user-uuid', 'admin@lifeos.local', '<hash>', 'Admin', NOW());

-- Empty user (for onboarding tests)
INSERT INTO users (id, email, password_hash, created_at)
VALUES ('empty-user-uuid', 'empty@lifeos.local', '<hash>', NOW());
```

**Mutation Strategies**:

1. **Read-Only Tests** (preferred): No data changes, validate existing data
2. **Isolated Mutations**: Use unique identifiers (`[E2E-Test-{timestamp}]`), clean up after test
3. **Test User Isolation**: Mutations only affect dedicated test user accounts
4. **Transaction Rollback** (future): Wrap tests in DB transactions, rollback on completion

### Fixture Architecture

**Auth Fixture** (`e2e/fixtures/auth.fixture.ts`):
```typescript
export const authenticatedPage = async (page: Page, user: TestUser = 'test-user') => {
  const credentials = TEST_USERS[user];
  
  // Direct API login (faster than UI)
  const response = await page.request.post('http://localhost:5001/api/auth/login', {
    data: {
      email: credentials.email,
      password: credentials.password,
    },
  });
  
  const { accessToken, refreshToken } = await response.json();
  
  // Inject tokens into browser storage
  await page.addInitScript((tokens) => {
    localStorage.setItem('accessToken', tokens.accessToken);
    localStorage.setItem('refreshToken', tokens.refreshToken);
  }, { accessToken, refreshToken });
  
  return page;
};
```

**API Helper** (`e2e/fixtures/api-helpers.ts`):
```typescript
export const recordMetric = async (
  request: APIRequestContext,
  token: string,
  metric: MetricRecord
) => {
  return await request.post('http://localhost:5001/api/metrics/record', {
    headers: { Authorization: `Bearer ${token}` },
    data: metric,
  });
};

export const createTask = async (
  request: APIRequestContext,
  token: string,
  task: TaskCreate
) => {
  return await request.post('http://localhost:5001/api/tasks', {
    headers: { Authorization: `Bearer ${token}` },
    data: task,
  });
};
```

## Test Execution Workflow

### Pre-Test Setup

1. **Docker Stack Health Check**:
```typescript
// e2e/utils/wait-for-docker.ts
export const waitForServices = async () => {
  // Check frontend
  await waitForURL('http://localhost:5173', 60000);
  
  // Check backend health
  await waitForURL('http://localhost:5001/health', 60000);
  
  // Check database connectivity (via backend)
  const response = await fetch('http://localhost:5001/api/health/database');
  if (!response.ok) throw new Error('Database not ready');
};
```

2. **Baseline Verification**:
```bash
# Verify dev data present
curl http://localhost:5001/api/users/me -H "Authorization: Bearer <token>"
```

### Test Execution Commands

```bash
# Run all tests
npm run test

# Run specific suite
npm run test:login
npm run test:dashboard
npm run test:finances

# Visual regression only
npm run test:visual

# Update visual baselines
npm run test:visual -- --update-snapshots

# Headed mode (see browser)
npm run test:headed

# Debug mode (step through)
npm run test:debug

# UI mode (interactive)
npm run test:ui

# CI mode (no parallelism, video on failure)
CI=true npm run test
```

### Post-Test Cleanup

```typescript
// Global teardown (playwright.config.ts)
export default defineConfig({
  globalTeardown: './e2e/global-teardown.ts',
});

// e2e/global-teardown.ts
export default async () => {
  // Clean up test artifacts
  await cleanupTestUsers();
  await cleanupTestTasks();
  await cleanupTestMetrics();
};

const cleanupTestUsers = async () => {
  // Delete users created during tests (except pre-seeded ones)
  const response = await fetch('http://localhost:5001/api/test/cleanup/users', {
    method: 'DELETE',
    headers: { 'X-API-Key': process.env.TEST_API_KEY },
  });
};
```

## Continuous Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/e2e-tests.yml
name: E2E Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  e2e:
    runs-on: ubuntu-latest
    timeout-minutes: 30

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        run: npm ci

      - name: Install Playwright browsers
        run: npx playwright install --with-deps

      - name: Start Docker stack
        run: docker-compose up -d

      - name: Wait for services
        run: |
          npm run wait-for-docker
          sleep 10 # Extra buffer for DB migrations

      - name: Run E2E tests
        run: npm run test
        env:
          CI: true

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: test-results/

      - name: Upload visual diffs
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: visual-diffs
          path: e2e/__screenshots__/__diff_output__/

      - name: Stop Docker stack
        if: always()
        run: docker-compose down -v
```

## Performance & Reliability

### Test Execution Speed

**Target Metrics**:
- Full suite: < 10 minutes (parallel execution)
- Critical path (P0 tests): < 3 minutes
- Single test: < 30 seconds

**Optimization Strategies**:
- **API Authentication**: Use API login instead of UI for faster setup
- **Parallel Execution**: 4 workers on local, 1 in CI
- **Smart Retries**: Retry only on network/timing issues, not assertion failures
- **Selective Testing**: Run only affected tests on PR

### Flakiness Mitigation

**Common Causes**:
- Network latency to backend
- Slow chart rendering
- SignalR connection timing
- Animation timing

**Solutions**:
```typescript
// Wait for network idle before assertions
await page.waitForLoadState('networkidle');

// Explicit waits for dynamic content
await page.waitForSelector('[data-testid="chart-loaded"]');

// Retry assertion with timeout
await expect(page.locator('[data-testid="score"]')).toHaveText(/\d+/, { timeout: 10000 });

// Disable animations in tests
await page.addInitScript(() => {
  document.documentElement.classList.add('no-animations');
});
```

### Error Handling

**Test Failure Artifacts**:
- Screenshot on failure
- Video recording (last 30s before failure)
- Trace file (step-by-step debugging)
- Network logs (API calls)
- Console logs (browser errors)

**Example Test with Full Debugging**:
```typescript
test('dashboard loads', async ({ page }, testInfo) => {
  try {
    await authenticatedPage(page);
    await page.goto('/dashboard');
    await expect(page.locator('[data-testid="lifeos-score"]')).toBeVisible();
  } catch (error) {
    // Capture extra context on failure
    await testInfo.attach('console-logs', {
      body: JSON.stringify(page.context().logs),
      contentType: 'application/json',
    });
    
    await testInfo.attach('network-logs', {
      body: JSON.stringify(await page.context().requests()),
      contentType: 'application/json',
    });
    
    throw error;
  }
});
```

## MCP Tools E2E Testing

### Test Coverage for AI Integration

**MCP Endpoints** (15+ tools):
- `getDashboardSnapshot` - Verify JSON structure, score accuracy
- `recordMetrics` - Validate nested ingestion, error handling
- `listTasks` - Check filtering, pagination
- `completeTask` - Verify streak update
- `getWeeklyReview` - Validate aggregation logic
- `runSimulation` - Test scenario projections
- `createWhatIfScenario` - Verify scenario creation

**Test Pattern**:
```typescript
// e2e/mcp/tools.spec.ts
test('getDashboardSnapshot returns valid JSON', async ({ request }) => {
  const token = await getTestUserToken();
  
  const response = await request.post('http://localhost:5001/api/mcp/tools/getDashboardSnapshot', {
    headers: { Authorization: `Bearer ${token}` },
  });
  
  expect(response.ok()).toBeTruthy();
  
  const data = await response.json();
  
  // Validate structure
  expect(data).toHaveProperty('lifeosScore');
  expect(data).toHaveProperty('healthIndex');
  expect(data).toHaveProperty('adherence');
  expect(data).toHaveProperty('wealthHealth');
  expect(data).toHaveProperty('longevity');
  expect(data).toHaveProperty('dimensions');
  
  // Validate types
  expect(typeof data.lifeosScore).toBe('number');
  expect(data.lifeosScore).toBeGreaterThanOrEqual(0);
  expect(data.lifeosScore).toBeLessThanOrEqual(100);
});
```

## Accessibility Testing

### WCAG 2.1 AA Compliance

**Automated Checks** (via axe-core):
```typescript
import { injectAxe, checkA11y } from 'axe-playwright';

test('dashboard is accessible', async ({ page }) => {
  await authenticatedPage(page);
  await page.goto('/dashboard');
  
  await injectAxe(page);
  await checkA11y(page, null, {
    detailedReport: true,
    detailedReportOptions: { html: true },
  });
});
```

**Manual Checks**:
- Keyboard navigation (Tab, Enter, Escape)
- Screen reader compatibility (aria-labels, roles)
- Color contrast ratios
- Focus indicators

## Test Coverage Goals

**By Priority**:
- **P0** (Critical): 100% coverage (auth, dashboard, metrics, tasks)
- **P1** (High): 90% coverage (finances, reviews, onboarding)
- **P2** (Medium): 70% coverage (settings, exports, integrations)

**By Type**:
- E2E User Journeys: 100% of critical paths
- Visual Regression: All pages at 3 breakpoints
- API Validation: 80% of endpoints
- Accessibility: 100% of pages

**Quality Gates**:
- All P0 tests must pass before merge
- No new visual regressions allowed
- No accessibility violations (level A, AA)
- Performance budget maintained (no regressions > 10%)

## Future Enhancements

### Phase 2 (Post-MVP)
- **Component Testing**: Isolated tests for React components
- **API Mocking**: Use MSW for faster tests, offline development
- **Load Testing**: Simulate 10K concurrent users via k6
- **Cross-Browser Cloud**: BrowserStack integration for real devices
- **Visual AI**: Percy or Applitools for smarter screenshot comparison

### Phase 3 (Advanced)
- **Chaos Engineering**: Random failure injection (network, DB, backend)
- **Security Testing**: OWASP ZAP integration for vulnerability scanning
- **Contract Testing**: Pact for frontend/backend API contracts
- **Synthetic Monitoring**: Production E2E tests on schedule (Checkly)

## Implementation Checklist

### Phase 1: Foundation (Week 1)
- [ ] Create `playwright.config.ts` with Docker config
- [ ] Set up `e2e/` directory structure
- [ ] Create auth fixture for test user login
- [ ] Write Docker health check utility
- [ ] Seed test users in dev database

### Phase 2: Critical Path (Week 2)
- [ ] Implement auth tests (login, logout, WebAuthn)
- [ ] Implement dashboard tests (score rings, dimensions)
- [ ] Implement metric recording tests (UI + API)
- [ ] Implement task lifecycle tests
- [ ] Set up visual regression baselines

### Phase 3: Extended Coverage (Week 3)
- [ ] Implement finance simulation tests
- [ ] Implement review tests (weekly, monthly)
- [ ] Implement onboarding wizard tests
- [ ] Implement MCP tools tests
- [ ] Add accessibility checks

### Phase 4: CI/CD Integration (Week 4)
- [ ] Create GitHub Actions workflow
- [ ] Configure artifact uploads
- [ ] Set up test result reporting
- [ ] Document test execution procedures
- [ ] Train team on debugging failures

## Success Metrics

**Test Health**:
- Flaky test rate < 5%
- Avg test execution time < 30s
- Full suite completion < 10 minutes
- Visual regression false positive rate < 2%

**Code Quality**:
- Zero critical bugs in production
- 100% critical path coverage
- < 1 hour from commit to test feedback

**Team Efficiency**:
- Developers can run tests locally in < 5 minutes
- Clear test failure debugging with artifacts
- Documentation enables self-service test authoring

## Conclusion

This E2E test architecture provides comprehensive coverage of LifeOS v3.0's critical functionality while preserving dev data integrity. The Docker-first approach ensures tests run against the production-like environment, and the visual regression strategy guarantees UI quality across all breakpoints.

**Key Differentiators**:
- ✅ Non-destructive testing (read-only where possible)
- ✅ Docker stack validation (full integration)
- ✅ Visual excellence enforcement (glassmorphic UI)
- ✅ Realistic data usage (dev database)
- ✅ Fast feedback (parallel execution, smart retries)
- ✅ AI-ready (MCP tools validation)

**Next Steps**:
- @Builder: Implement `playwright.config.ts` and core test infrastructure
- @Builder: Create auth fixture and test user seeding script
- @Builder: Implement P0 tests (auth, dashboard, metrics, tasks)
- @Tester: Execute tests and refine visual regression thresholds
