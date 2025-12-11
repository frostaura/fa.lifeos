# Database Design

## Database Selection
- **Primary DB**: PostgreSQL 17
- **ORM**: Entity Framework Core 9.0
- **Provider**: Npgsql
- **Justification**: ACID compliance, JSONB support for flexible data, proven scalability

## v1.1 New Tables Summary
- **identity_profiles** - User target persona and primary stat targets
- **primary_stat_records** - Historical primary stat values
- **fx_rates** - Multi-currency exchange rates
- **longevity_models** - Risk-based longevity calculation models
- **review_snapshots** - Weekly/monthly review data
- **onboarding_responses** - Goal-first onboarding data

## Entity Relationship Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              CORE ENTITIES                                    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────┐     1:N    ┌───────────┐     1:N    ┌────────────────┐         │
│  │  User   │───────────▶│  Account  │───────────▶│  Transaction   │         │
│  └────┬────┘            └─────┬─────┘            └────────────────┘         │
│       │                       │                                              │
│       │ 1:N                   │ 1:N                                          │
│       ▼                       ▼                                              │
│  ┌─────────────┐        ┌─────────────────────┐                             │
│  │ IncomeSource│        │ AccountProjection   │                             │
│  └─────────────┘        └─────────────────────┘                             │
│       │                                                                      │
│       │ N:1                                                                  │
│       ▼                                                                      │
│  ┌────────────┐                                                              │
│  │ TaxProfile │                                                              │
│  └────────────┘                                                              │
│                                                                              │
│  ┌─────────┐     1:N    ┌──────────────────┐                                │
│  │  User   │───────────▶│SimulationScenario│                                │
│  └─────────┘            └────────┬─────────┘                                │
│                                  │ 1:N                                       │
│                                  ▼                                           │
│                         ┌─────────────────┐                                  │
│                         │SimulationEvent  │                                  │
│                         └─────────────────┘                                  │
│                                                                              │
│  ┌───────────┐    1:N   ┌─────────────────┐   1:N   ┌──────────────┐       │
│  │ Dimension │─────────▶│MetricDefinition │────────▶│ MetricRecord │       │
│  └───────────┘          └─────────────────┘         └──────────────┘       │
│       │                                                                      │
│       │ 1:N                                                                  │
│       ▼                                                                      │
│  ┌───────────┐    1:N   ┌─────────────┐                                     │
│  │ Milestone │─────────▶│  LifeTask   │─────────▶ Streak                    │
│  └───────────┘          └─────────────┘                                     │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Core Tables

### Users
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    username VARCHAR(50),
    password_hash VARCHAR(255),              -- Nullable for biometric-only
    home_currency VARCHAR(3) DEFAULT 'ZAR',
    date_of_birth DATE,
    life_expectancy_baseline DECIMAL(5,2) DEFAULT 80,
    default_assumptions JSONB DEFAULT '{"inflationRateAnnual": 0.05, "defaultGrowthRate": 0.07, "retirementAge": 65}',
    role VARCHAR(20) DEFAULT 'User',         -- User | Admin
    status VARCHAR(20) DEFAULT 'Active',     -- Active | Inactive | Suspended
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Accounts
```sql
CREATE TABLE accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    account_type VARCHAR(20) NOT NULL,       -- Bank | Investment | Loan | Credit | Crypto | Property | Other
    currency VARCHAR(3) DEFAULT 'ZAR',
    initial_balance DECIMAL(19,4) DEFAULT 0,
    current_balance DECIMAL(19,4) DEFAULT 0,
    balance_updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    institution VARCHAR(100),
    is_liability BOOLEAN DEFAULT FALSE,
    interest_rate_annual DECIMAL(8,4),       -- Annual rate as percentage (e.g., 10.5)
    interest_compounding VARCHAR(20),        -- None | Daily | Monthly | Quarterly | Annually | Continuous
    monthly_fee DECIMAL(19,4) DEFAULT 0,
    metadata JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Simulation Scenarios
```sql
CREATE TABLE simulation_scenarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    start_date DATE NOT NULL,
    end_date DATE,
    end_condition TEXT,                      -- Expression like "netWorth >= 10000000"
    base_assumptions JSONB DEFAULT '{}',
    is_baseline BOOLEAN DEFAULT FALSE,
    last_run_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Income Sources
```sql
CREATE TABLE income_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZAR',
    base_amount DECIMAL(19,4) NOT NULL,
    is_pre_tax BOOLEAN DEFAULT TRUE,
    tax_profile_id UUID REFERENCES tax_profiles(id),
    payment_frequency VARCHAR(20) NOT NULL,  -- Weekly | Biweekly | Monthly | Quarterly | Annually | Once
    next_payment_date DATE,
    annual_increase_rate DECIMAL(8,4) DEFAULT 0.05,
    employer_name VARCHAR(100),
    target_account_id UUID REFERENCES accounts(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Expense Definitions
```sql
CREATE TABLE expense_definitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZAR',
    amount_type VARCHAR(20) DEFAULT 'Fixed', -- Fixed | Percentage | Formula
    amount_value DECIMAL(19,4),
    amount_formula TEXT,
    frequency VARCHAR(20) NOT NULL,
    start_date DATE,
    category VARCHAR(50),
    is_tax_deductible BOOLEAN DEFAULT FALSE,
    linked_account_id UUID REFERENCES accounts(id),
    inflation_adjusted BOOLEAN DEFAULT TRUE,
    end_condition_type VARCHAR(30) DEFAULT 'None', -- None | UntilAccountSettled | UntilDate | UntilAmount
    end_condition_account_id UUID REFERENCES accounts(id),
    end_date DATE,
    end_amount_threshold DECIMAL(19,4),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Tax Profiles
```sql
CREATE TABLE tax_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) DEFAULT 'Default',
    tax_year INT NOT NULL,
    country_code VARCHAR(2) DEFAULT 'ZA',
    brackets JSONB NOT NULL,                 -- Array of {min, max, rate, baseTax}
    uif_rate DECIMAL(8,4) DEFAULT 0.01,
    uif_cap DECIMAL(19,4) DEFAULT 177.12,
    vat_rate DECIMAL(8,4) DEFAULT 0.15,
    is_vat_registered BOOLEAN DEFAULT FALSE,
    tax_rebates JSONB,                       -- {primary: 17235, secondary: 9444, ...}
    medical_credits JSONB,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Metric Definitions
```sql
CREATE TABLE metric_definitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    dimension_id UUID REFERENCES dimensions(id),
    unit VARCHAR(20),
    value_type VARCHAR(20) DEFAULT 'Number', -- Number | Integer | Decimal | Boolean | Percentage
    aggregation_type VARCHAR(20) DEFAULT 'Last', -- Last | Sum | Average | Max | Min
    enum_values TEXT[],
    min_value DECIMAL(19,4),
    max_value DECIMAL(19,4),
    target_value DECIMAL(19,4),
    target_direction VARCHAR(20) DEFAULT 'AtOrAbove', -- AtOrAbove | AtOrBelow
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Achievements
```sql
CREATE TABLE achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT NOT NULL,
    icon VARCHAR(50),
    xp_value INT DEFAULT 0,
    category VARCHAR(30),                    -- financial | health | streak | milestone
    tier VARCHAR(20) DEFAULT 'bronze',       -- bronze | silver | gold | platinum | diamond
    unlock_condition TEXT NOT NULL,          -- Expression like "netWorth >= 1000000"
    is_active BOOLEAN DEFAULT TRUE,
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## v1.1 New Tables

### Identity Profiles (v1.1)
```sql
CREATE TABLE identity_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    archetype VARCHAR(100) NOT NULL,         -- "God of Mind-Power"
    archetype_description TEXT,
    values JSONB NOT NULL,                   -- ["discipline", "growth", "impact"]
    primary_stat_targets JSONB NOT NULL,     -- {"strength": 80, "wisdom": 95, ...}
    linked_milestone_ids JSONB,              -- [uuid, uuid, ...]
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Primary Stat Records (v1.1)
```sql
CREATE TABLE primary_stat_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    strength INT NOT NULL CHECK (strength BETWEEN 0 AND 100),
    wisdom INT NOT NULL CHECK (wisdom BETWEEN 0 AND 100),
    charisma INT NOT NULL CHECK (charisma BETWEEN 0 AND 100),
    composure INT NOT NULL CHECK (composure BETWEEN 0 AND 100),
    energy INT NOT NULL CHECK (energy BETWEEN 0 AND 100),
    influence INT NOT NULL CHECK (influence BETWEEN 0 AND 100),
    vitality INT NOT NULL CHECK (vitality BETWEEN 0 AND 100),
    calculation_details JSONB                -- breakdown by dimension
);

CREATE INDEX idx_primary_stat_records_user_date ON primary_stat_records(user_id, recorded_at DESC);
```

### FX Rates (v1.1 Enhancement)
```sql
CREATE TABLE fx_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    base_currency VARCHAR(3) NOT NULL,
    target_currency VARCHAR(3) NOT NULL,
    rate DECIMAL(19,8) NOT NULL,
    rate_date DATE NOT NULL,
    source VARCHAR(50) DEFAULT 'manual',     -- manual | coingecko | openexchange
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(base_currency, target_currency, rate_date)
);

CREATE INDEX idx_fx_rates_currencies ON fx_rates(base_currency, target_currency, rate_date DESC);
```

### Longevity Models (v1.1)
```sql
CREATE TABLE longevity_models (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    factor_type VARCHAR(30) NOT NULL,        -- smoking | exercise | bmi | sleep | etc.
    baseline_risk DECIMAL(8,4) DEFAULT 1.0,
    risk_reduction_formula TEXT,             -- Expression like "1 - (value * 0.02)"
    input_metric_code VARCHAR(50),           -- Linked metric
    evidence_source TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Review Snapshots (v1.1)
```sql
CREATE TABLE review_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    review_type VARCHAR(20) NOT NULL,        -- weekly | monthly
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    health_index_delta DECIMAL(8,4),
    adherence_index_delta DECIMAL(8,4),
    wealth_health_delta DECIMAL(8,4),
    longevity_delta DECIMAL(8,4),
    top_streaks JSONB,                       -- [{taskId, streakDays, name}]
    recommended_actions JSONB,               -- [{action, priority, dimension}]
    primary_stats_delta JSONB,               -- {strength: +2, wisdom: -1, ...}
    scenario_comparison JSONB,               -- baseline vs scenarios summary
    generated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_review_snapshots_user_type ON review_snapshots(user_id, review_type, period_end DESC);
```

### Onboarding Responses (v1.1)
```sql
CREATE TABLE onboarding_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    step_code VARCHAR(50) NOT NULL,          -- health_baselines | major_goals | identity
    response_data JSONB NOT NULL,
    completed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Enhanced Streaks (v1.1 Enhancement)
```sql
-- Existing streaks table enhanced with penalty fields
ALTER TABLE streaks ADD COLUMN IF NOT EXISTS consecutive_misses INT DEFAULT 0;
ALTER TABLE streaks ADD COLUMN IF NOT EXISTS risk_penalty_score DECIMAL(8,4) DEFAULT 0;
ALTER TABLE streaks ADD COLUMN IF NOT EXISTS last_penalty_calculated_at TIMESTAMP;
```

### Enhanced Simulation Events (v1.1)
```sql
-- Existing simulation_events table enhanced
ALTER TABLE simulation_events ADD COLUMN IF NOT EXISTS source_account_id UUID REFERENCES accounts(id);
ALTER TABLE simulation_events ADD COLUMN IF NOT EXISTS target_account_id UUID REFERENCES accounts(id);
```

## Relationships Summary

| Parent | Child | Relationship | FK Column |
|--------|-------|--------------|-----------|
| User | Account | 1:N | user_id |
| User | IncomeSource | 1:N | user_id |
| User | ExpenseDefinition | 1:N | user_id |
| User | InvestmentContribution | 1:N | user_id |
| User | TaxProfile | 1:N | user_id |
| User | SimulationScenario | 1:N | user_id |
| User | MetricRecord | 1:N | user_id |
| User | Streak | 1:N | user_id |
| User | FinancialGoal | 1:N | user_id |
| User | IdentityProfile | 1:1 | user_id (v1.1) |
| User | PrimaryStatRecord | 1:N | user_id (v1.1) |
| User | ReviewSnapshot | 1:N | user_id (v1.1) |
| User | OnboardingResponse | 1:N | user_id (v1.1) |
| Dimension | MetricDefinition | 1:N | dimension_id |
| Dimension | Milestone | 1:N | dimension_id |
| Dimension | LifeTask | 1:N | dimension_id |
| SimulationScenario | SimulationEvent | 1:N | scenario_id |
| SimulationScenario | AccountProjection | 1:N | scenario_id |
| SimulationScenario | NetWorthProjection | 1:N | scenario_id |
| IncomeSource | TaxProfile | N:1 | tax_profile_id |
| Account | Transaction | 1:N | source_account_id / target_account_id |
| Account | SimulationEvent | 1:N | source_account_id, target_account_id (v1.1) |

## Data Types & Constraints

### Common Fields
- **IDs**: UUID (distributed systems) or BIGSERIAL (single DB)
- **Timestamps**: TIMESTAMP WITH TIME ZONE
- **Status**: ENUM or VARCHAR with CHECK constraint
- **Money**: DECIMAL(19,4) never FLOAT
- **JSON**: JSONB for flexible data

### Constraints
- Foreign keys with appropriate CASCADE rules
- CHECK constraints for business rules
- UNIQUE constraints for natural keys
- NOT NULL for required fields

## Indexes

### Primary Indexes
- Primary keys (automatic)
- Foreign keys (recommended)
- Unique constraints (automatic)

### Performance Indexes
```sql
-- Frequently queried columns
CREATE INDEX idx_table_column ON table(column);

-- Composite indexes for common queries
CREATE INDEX idx_posts_user_status ON posts(user_id, status);

-- Partial indexes for filtered queries
CREATE INDEX idx_users_active ON users(status) WHERE status = 'active';

-- Full-text search
CREATE INDEX idx_posts_search ON posts USING gin(to_tsvector('english', title || ' ' || content));
```

## Migrations

### Naming Convention
```
YYYYMMDDHHMMSS_description.sql
20240101120000_create_users_table.sql
```

### Migration Template
```sql
-- Up Migration
BEGIN;
ALTER TABLE users ADD COLUMN phone VARCHAR(20);
COMMIT;

-- Down Migration
BEGIN;
ALTER TABLE users DROP COLUMN phone;
COMMIT;
```

## Data Access Patterns

### Read Patterns
- Single record by ID
- Lists with pagination
- Filtered searches
- Aggregations and reports

### Write Patterns
- Insert with returning ID
- Bulk inserts
- Update with optimistic locking
- Soft deletes (status/deleted_at)

## Performance Optimization

### Query Optimization
- Use EXPLAIN ANALYZE
- Avoid N+1 queries
- Use JOIN over multiple queries
- Batch operations when possible

### Connection Pooling
```javascript
{
  min: 2,
  max: 10,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000
}
```

### Caching Strategy
- Query result caching (Redis)
- Row-level caching
- Invalidation on write

## Backup & Recovery

### Backup Strategy
- Daily full backups
- Hourly incremental backups
- Point-in-time recovery (PITR)
- Geo-redundant storage

### Recovery Procedures
1. Stop application
2. Restore from backup
3. Apply WAL/binlog to recovery point
4. Verify data integrity
5. Resume application

## Monitoring

### Key Metrics
- Query execution time
- Connection pool usage
- Lock contention
- Index usage statistics
- Table/index bloat

### Alerts
- Slow queries > 1s
- Connection pool exhaustion
- Replication lag > 10s
- Disk usage > 80%

## Security

### Access Control
- Principle of least privilege
- Separate read/write users
- No direct DB access from frontend
- SSL/TLS connections required

### Data Protection
- Encryption at rest
- Column-level encryption for PII
- Audit logging for sensitive data
- Regular security updates

## Scaling Strategy

### Vertical Scaling
- Increase CPU/RAM as needed
- Monitor resource usage

### Horizontal Scaling
- Read replicas for read-heavy loads
- Sharding for write-heavy loads
- Consider NoSQL for specific use cases

### Partitioning
```sql
-- Time-based partitioning
CREATE TABLE events_2024_01 PARTITION OF events
FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
```