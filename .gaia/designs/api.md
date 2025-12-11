# API Design

## API Standards

### Base URL
- Development: `http://localhost:5001/api`
- Frontend Proxy: `http://localhost:5173/api` (proxied via Vite)

### Authentication
- Bearer token: `Authorization: Bearer <jwt-token>`
- API Key: `X-API-Key: <key>` (for external integrations)

## v1.1 New Endpoints Summary
- **Identity Profile**: `/api/identity-profile` - CRUD for user identity/persona
- **Primary Stats**: `/api/primary-stats` - Get/history of primary stats
- **Nested Metrics Ingestion**: `POST /api/metrics/record` - Enhanced with nested structure
- **Reviews**: `/api/reviews` - Weekly/monthly review generation
- **Onboarding**: `/api/onboarding` - Goal-first onboarding flow
- **Longevity Models**: `/api/longevity/models` - Risk-based models
- **Scenario Comparison**: `/api/simulations/compare` - Compare baseline vs scenarios

## REST Conventions

### HTTP Methods
- `GET` - Read resources
- `POST` - Create resources
- `PATCH` - Partial update
- `DELETE` - Remove resources

### Status Codes
- `200` - Success
- `201` - Created
- `204` - No content
- `400` - Bad request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not found
- `422` - Validation error
- `429` - Rate limited
- `500` - Server error

### Response Format (JSON:API)

#### Success Response
```json
{
  "data": {
    "id": "uuid",
    "type": "account",
    "attributes": {
      "name": "Savings Account",
      "accountType": "Bank",
      "currentBalance": 50000.00,
      "currency": "ZAR"
    }
  },
  "meta": {
    "timestamp": "2024-01-01T00:00:00Z"
  }
}
```

#### Collection Response
```json
{
  "data": [
    { "id": "...", "type": "account", "attributes": {...} }
  ],
  "meta": {
    "page": 1,
    "perPage": 20,
    "total": 100,
    "totalPages": 5
  }
}
```

#### Error Response
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input",
    "details": [
      { "field": "email", "message": "Invalid email format" }
    ]
  }
}
```

## Core Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Invalidate tokens |

### Dashboard
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard` | Dashboard summary (life score, net worth, dimensions, streaks, tasks) |
| GET | `/api/dashboard/net-worth/history` | Historical net worth data |

### Dimensions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dimensions` | List all dimensions |
| GET | `/api/dimensions/{id}` | Get dimension with metrics |
| POST | `/api/dimensions` | Create dimension |
| PATCH | `/api/dimensions/{id}` | Update dimension |

### Accounts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | List accounts with net worth meta |
| GET | `/api/accounts/{id}` | Get account details |
| POST | `/api/accounts` | Create account |
| PATCH | `/api/accounts/{id}` | Update account |
| DELETE | `/api/accounts/{id}` | Delete account |

### Income Sources
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/income-sources` | List income sources |
| POST | `/api/income-sources` | Create income source |
| PATCH | `/api/income-sources/{id}` | Update income source |
| DELETE | `/api/income-sources/{id}` | Delete income source |

### Expense Definitions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/expense-definitions` | List expenses |
| POST | `/api/expense-definitions` | Create expense |
| PATCH | `/api/expense-definitions/{id}` | Update expense |
| DELETE | `/api/expense-definitions/{id}` | Delete expense |

### Investment Contributions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/investment-contributions` | List investments |
| POST | `/api/investment-contributions` | Create investment |
| PATCH | `/api/investment-contributions/{id}` | Update investment |
| DELETE | `/api/investment-contributions/{id}` | Delete investment |

### Tax Profiles
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tax-profiles` | List tax profiles |
| POST | `/api/tax-profiles` | Create tax profile |
| PATCH | `/api/tax-profiles/{id}` | Update tax profile |
| DELETE | `/api/tax-profiles/{id}` | Delete tax profile |

### Financial Goals
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/financial-goals` | List financial goals |
| POST | `/api/financial-goals` | Create financial goal |
| PATCH | `/api/financial-goals/{id}` | Update financial goal |
| DELETE | `/api/financial-goals/{id}` | Delete financial goal |

### Simulations
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/simulations/scenarios` | List scenarios |
| GET | `/api/simulations/scenarios/{id}` | Get scenario details |
| POST | `/api/simulations/scenarios` | Create scenario |
| PATCH | `/api/simulations/scenarios/{id}` | Update scenario |
| DELETE | `/api/simulations/scenarios/{id}` | Delete scenario |
| POST | `/api/simulations/scenarios/{id}/run` | Run simulation |
| GET | `/api/simulations/scenarios/{id}/projections` | Get projections |

### Metrics
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/metrics/definitions` | List metric definitions |
| POST | `/api/metrics/definitions` | Create definition |
| PATCH | `/api/metrics/definitions/{code}` | Update definition |
| DELETE | `/api/metrics/definitions/{code}` | Delete definition |
| POST | `/api/metrics/record` | Record metric values (bulk) |
| GET | `/api/metrics/{code}/records` | Get metric records |
| GET | `/api/metrics/history` | Get history with aggregation |

### Health & Longevity
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/longevity` | Get longevity estimate |
| GET | `/api/longevity/models` | List longevity models |
| PATCH | `/api/longevity/models/{id}` | Update model parameters |
| GET | `/api/longevity/years-added` | Get calculated years added (v1.1) |

### Identity Profile (v1.1)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/identity-profile` | Get user identity profile |
| PUT | `/api/identity-profile` | Create/update identity profile |
| GET | `/api/primary-stats` | Get current primary stats |
| GET | `/api/primary-stats/history` | Get primary stats history |

### Reviews (v1.1)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reviews/weekly` | Get current week review |
| GET | `/api/reviews/monthly` | Get current month review |
| GET | `/api/reviews/history` | Get past reviews |
| POST | `/api/reviews/generate` | Force generate review |

### Onboarding (v1.1)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/onboarding/status` | Get onboarding completion status |
| POST | `/api/onboarding/health-baselines` | Submit health baselines |
| POST | `/api/onboarding/major-goals` | Submit major goals |
| POST | `/api/onboarding/identity` | Submit identity traits |
| POST | `/api/onboarding/complete` | Mark onboarding complete |

### Scenario Comparison (v1.1)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/simulations/compare` | Compare multiple scenarios |
| GET | `/api/simulations/what-if` | Quick what-if calculation |

### Tasks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | List tasks with filters |
| GET | `/api/tasks/{id}` | Get task details |
| POST | `/api/tasks` | Create task |
| PATCH | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |
| POST | `/api/tasks/{id}/complete` | Complete task |

### Milestones
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/milestones` | List milestones |
| POST | `/api/milestones` | Create milestone |
| PATCH | `/api/milestones/{id}` | Update milestone |
| DELETE | `/api/milestones/{id}` | Delete milestone |

### Achievements & Gamification
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/achievements` | List all achievements |
| GET | `/api/achievements/user` | Get user achievements |
| GET | `/api/xp` | Get user XP and level |
| GET | `/api/streaks` | List user streaks |

### Settings
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/settings/profile` | Get user profile |
| PATCH | `/api/settings/profile` | Update user profile |
| GET | `/api/settings/api-keys` | List API keys |
| POST | `/api/settings/api-keys` | Create API key |
| DELETE | `/api/settings/api-keys/{id}` | Revoke API key |

### Data Portability
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/data/export` | Export all user data |
| POST | `/api/data/import` | Import user data |

## Query Parameters

### Filtering
```
GET /api/accounts?accountType=Bank&isLiability=false
GET /api/tasks?dimensionId={uuid}&taskType=habit&isCompleted=false
```

### Sorting
```
GET /api/accounts?sort=-currentBalance
GET /api/tasks?sort=scheduledDate
```

### Pagination
```
GET /api/accounts?page=2&perPage=20
```

## Rate Limiting

### Headers
- `X-RateLimit-Limit`: Request limit per window
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Reset timestamp

### Limits
- Authenticated: 1000 requests/15 minutes
- API Key: 5000 requests/hour

## WebSocket/SignalR

### Connection
```
wss://localhost:5001/hubs/notifications
```

### Events
| Event | Description |
|-------|-------------|
| `ProjectionsUpdated` | Simulation projections recalculated |
| `CalculationProgress` | Progress updates during simulation |
| `AchievementUnlocked` | User unlocked new achievement |

## API Documentation
- Swagger UI: `/swagger`
- OpenAPI spec: `/swagger/v1/swagger.json`

## Feature: Enhanced Dimensions API

### Overview
API endpoints supporting the enhanced Dimensions feature with task management, metric-linked milestones, and dimension information.

### Tasks API (`/api/tasks`)

The Tasks API already exists with full CRUD support. Below documents the existing endpoints and any enhancements needed.

#### List Tasks
```http
GET /api/tasks
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `taskType` | string | Filter by type: `habit`, `one_off`, `scheduled_event` |
| `dimensionId` | uuid | Filter by dimension |
| `milestoneId` | uuid | Filter by linked milestone |
| `isCompleted` | boolean | Filter by completion status |
| `isActive` | boolean | Filter by active status |
| `scheduledFrom` | date | Filter scheduled tasks from date |
| `scheduledTo` | date | Filter scheduled tasks to date |
| `tags` | string | Comma-separated tag filter |
| `page` | int | Page number (default: 1) |
| `perPage` | int | Items per page (default: 20, max: 100) |

**Response:**
```json
{
  "data": [
    {
      "id": "uuid",
      "type": "task",
      "attributes": {
        "title": "Morning run",
        "description": "30 minute jog",
        "taskType": "habit",
        "frequency": "daily",
        "dimensionId": "uuid",
        "dimensionCode": "health",
        "milestoneId": "uuid",
        "linkedMetricCode": "running_distance",
        "scheduledDate": null,
        "scheduledTime": null,
        "startDate": "2024-01-01",
        "endDate": null,
        "isCompleted": false,
        "completedAt": null,
        "isActive": true,
        "tags": ["cardio", "outdoor"],
        "streakDays": 12
      }
    }
  ],
  "meta": {
    "page": 1,
    "perPage": 20,
    "total": 45,
    "totalPages": 3
  }
}
```

#### Get Task by ID
```http
GET /api/tasks/{id}
```

**Response:**
```json
{
  "data": {
    "id": "uuid",
    "type": "task",
    "attributes": {
      "title": "Morning run",
      "description": "30 minute jog",
      "taskType": "habit",
      "frequency": "daily",
      "dimensionId": "uuid",
      "dimensionCode": "health",
      "milestoneId": "uuid",
      "linkedMetricCode": "running_distance",
      "scheduledDate": null,
      "scheduledTime": null,
      "startDate": "2024-01-01",
      "endDate": null,
      "isCompleted": false,
      "completedAt": null,
      "isActive": true,
      "tags": ["cardio"],
      "streakDays": 12,
      "streak": {
        "currentLength": 12,
        "longestLength": 28,
        "lastCompletedAt": "2024-12-09T07:30:00Z"
      }
    }
  }
}
```

#### Create Task
```http
POST /api/tasks
```

**Request Body:**
```json
{
  "title": "Morning run",
  "description": "30 minute jog around the park",
  "taskType": "habit",
  "frequency": "daily",
  "dimensionId": "uuid",
  "milestoneId": "uuid",
  "linkedMetricCode": "running_distance",
  "scheduledDate": null,
  "scheduledTime": null,
  "startDate": "2024-01-01",
  "endDate": null,
  "tags": ["cardio", "outdoor"]
}
```

**Validation Rules:**
- `title`: Required, max 200 characters
- `taskType`: Required, one of `habit`, `one_off`, `scheduled_event`
- `frequency`: Required for habits, one of `daily`, `weekly`, `monthly`, `ad_hoc`
- `scheduledDate`: Required for `scheduled_event` type
- `dimensionId`: Optional, must exist if provided
- `milestoneId`: Optional, must exist and belong to same dimension if provided
- `linkedMetricCode`: Optional, must exist if provided

**Response:** `201 Created` with task object

#### Update Task
```http
PATCH /api/tasks/{id}
```

**Request Body:** (all fields optional)
```json
{
  "title": "Evening run",
  "description": "Updated description",
  "frequency": "weekly",
  "scheduledDate": "2024-12-20",
  "scheduledTime": "18:00",
  "endDate": "2024-12-31",
  "isActive": true,
  "tags": ["cardio"]
}
```

**Note:** `taskType` cannot be changed after creation.

**Response:** `200 OK` with updated task object

#### Delete Task
```http
DELETE /api/tasks/{id}
```

**Response:** `204 No Content`

#### Complete Task
```http
POST /api/tasks/{id}/complete
```

**Request Body:** (optional)
```json
{
  "completedAt": "2024-12-10T07:30:00Z",
  "metricValue": 5.2
}
```

**Behavior:**
- For `habit` tasks: Updates streak, optionally records metric value
- For `one_off` tasks: Marks as completed
- For `scheduled_event`: Marks as completed

**Response:**
```json
{
  "data": {
    "id": "uuid",
    "type": "task",
    "attributes": {
      "title": "Morning run",
      "isCompleted": true,
      "completedAt": "2024-12-10T07:30:00Z"
    }
  },
  "meta": {
    "streakUpdated": true,
    "newStreakLength": 13,
    "metricRecorded": true
  }
}
```

#### Uncomplete Task (NEW)
```http
POST /api/tasks/{id}/uncomplete
```

**Purpose:** Allow users to undo accidental completions within a time window.

**Business Rules:**
- Only allowed within 24 hours of completion
- Decrements streak if task was a habit
- Deletes metric record if one was created

**Response:** `200 OK` with task object

### Enhanced Milestones API (`/api/milestones`)

#### Get Milestones with Progress
```http
GET /api/milestones?dimensionId={uuid}&includeProgress=true
```

**Response:**
```json
{
  "data": [
    {
      "id": "uuid",
      "type": "milestone",
      "attributes": {
        "title": "Run a marathon",
        "description": "Complete a full 42km marathon",
        "dimensionId": "uuid",
        "dimensionCode": "health",
        "targetDate": "2025-12-31",
        "targetMetricCode": "running_distance_total",
        "targetMetricValue": 42,
        "status": "active",
        "completedAt": null,
        "progress": {
          "metricName": "Running Distance (Total)",
          "metricUnit": "km",
          "currentValue": 12.5,
          "targetValue": 42,
          "progressPercent": 29.76,
          "targetDirection": "AtOrAbove",
          "onTrack": true
        }
      }
    }
  ]
}
```

**Progress Calculation:**
- For `AtOrAbove`: `(currentValue / targetValue) * 100`
- For `AtOrBelow`: `((targetValue - currentValue) / targetValue) * 100` (inverted)
- `onTrack`: Based on linear projection to target date

#### Create Milestone with Metric Target
```http
POST /api/milestones
```

**Request Body:**
```json
{
  "title": "Run a marathon",
  "description": "Complete a full 42km marathon",
  "dimensionId": "uuid",
  "targetDate": "2025-12-31",
  "targetMetricCode": "running_distance_total",
  "targetMetricValue": 42
}
```

### Enhanced Dimensions API (`/api/dimensions`)

#### Get Dimension with Metrics
```http
GET /api/dimensions/{id}?include=metrics
```

**Response:**
```json
{
  "data": {
    "id": "uuid",
    "type": "dimension",
    "attributes": {
      "code": "health",
      "name": "Health",
      "description": "Physical well-being and fitness",
      "icon": "heart",
      "weight": 0.125,
      "defaultWeight": 0.125,
      "sortOrder": 1,
      "isActive": true,
      "currentScore": 78
    },
    "relationships": {
      "milestones": [...],
      "activeTasks": [...],
      "linkedMetrics": [
        {
          "code": "weight",
          "name": "Body Weight",
          "unit": "kg",
          "currentValue": 68.5,
          "targetValue": 65,
          "targetDirection": "AtOrBelow",
          "progressPercent": 78.6,
          "latestRecordedAt": "2024-12-09T08:00:00Z"
        },
        {
          "code": "daily_steps",
          "name": "Daily Steps",
          "unit": "steps",
          "currentValue": 8500,
          "targetValue": 10000,
          "targetDirection": "AtOrAbove",
          "progressPercent": 85,
          "latestRecordedAt": "2024-12-09T23:59:00Z"
        }
      ]
    }
  }
}
```

#### Get Dimension Info (NEW)
```http
GET /api/dimensions/{id}/info
```

**Purpose:** Returns detailed informational content about a dimension for help/onboarding.

**Response:**
```json
{
  "data": {
    "dimensionCode": "health",
    "title": "What is Health?",
    "description": "The Health dimension tracks your physical well-being and fitness levels. This encompasses all aspects of your physical body including exercise, nutrition, sleep, and medical health.\n\nBy tracking habits and metrics in this dimension, you gain visibility into patterns that affect your energy levels, longevity, and quality of life.",
    "keyAreas": [
      "Exercise & Physical Activity",
      "Nutrition & Diet",
      "Sleep Quality",
      "Medical & Preventive Care",
      "Mental Health"
    ],
    "tips": [
      "Start with one simple daily habit like walking",
      "Track metrics that matter most to your goals",
      "Connect habits to your milestones for better motivation"
    ],
    "relatedMetrics": ["weight", "daily_steps", "sleep_hours", "water_intake"]
  }
}
```

**Note:** Dimension info can be stored in the database or served from static configuration. For MVP, use static configuration in the frontend; add API endpoint later if dynamic content is needed.

### Summary Statistics Endpoint (Enhancement)

#### Get Dimension Summary
```http
GET /api/dimensions/{id}/summary
```

**Response:**
```json
{
  "data": {
    "dimensionId": "uuid",
    "dimensionCode": "health",
    "score": 78,
    "scoreTrend": 2.5,
    "activeTaskCount": 5,
    "completedTaskCount": 12,
    "activeMilestoneCount": 2,
    "completedMilestoneCount": 1,
    "linkedMetricCount": 4,
    "streaksSummary": {
      "activeStreaks": 3,
      "longestCurrentStreak": 28,
      "totalStreakDays": 45
    }
  }
}
```

### Error Responses

All endpoints follow standard error format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input",
    "details": [
      {
        "field": "taskType",
        "message": "TaskType must be one of: habit, one_off, scheduled_event"
      }
    ]
  }
}
```

**Common Error Codes:**
| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 422 | Request body validation failed |
| `NOT_FOUND` | 404 | Resource not found |
| `UNAUTHORIZED` | 401 | Authentication required |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `CONFLICT` | 409 | Resource state conflict (e.g., uncomplete window expired) |

### Cache Invalidation

Task/Milestone operations should invalidate these cache tags:
- `Tasks` - All task queries
- `Dimensions` - Dimension detail (affects counts, scores)
- `Dashboard` - Dashboard data (affects today's tasks, scores)
- `Streaks` - Streak queries (when completing habits)
- `Metrics` - Metric queries (when recording metric values)

## v1.1 Nested Metrics Ingestion API

### POST /api/metrics/record (Enhanced)

**Purpose**: Accept nested payload structure for bulk metric recording.

**Request Body (Nested Structure)**:
```json
{
  "timestamp": "2024-12-10T08:00:00Z",
  "source": "apple_health",
  "metrics": {
    "health_recovery": {
      "weight_kg": 74.5,
      "body_fat_pct": 16.2,
      "resting_hr_bpm": 58,
      "sleep_hours": 7.5,
      "sleep_quality_score": 82
    },
    "play_adventure": {
      "travel_days_count": 2,
      "outdoor_hours": 4.5
    },
    "asset_care": {
      "finance": {
        "net_worth_homeccy": 1250000,
        "savings_rate_pct": 35.5,
        "emergency_fund_months": 6
      }
    },
    "work_contribution": {
      "deep_work_hours": 4.2,
      "meetings_count": 3
    }
  }
}
```

**Nested Path Resolution**:
- `metrics.health_recovery.weight_kg` → metric code `weight_kg` in dimension `health_recovery`
- `metrics.asset_care.finance.net_worth_homeccy` → metric code `finance.net_worth_homeccy` or `net_worth_homeccy`

**Response**:
```json
{
  "data": {
    "recordedCount": 12,
    "skippedCount": 0,
    "errors": []
  },
  "meta": {
    "timestamp": "2024-12-10T08:00:05Z",
    "processingTimeMs": 45
  }
}
```

**Validation Rules**:
- Unknown metrics are rejected unless `allowDynamicCreation` query param is true
- Null values are ignored (not recorded)
- Timestamps default to now if not provided
- Source is optional but recommended for traceability

## v1.1 Identity Profile API

### GET /api/identity-profile

**Response**:
```json
{
  "data": {
    "archetype": "God of Mind-Power",
    "archetypeDescription": "A disciplined achiever focused on mental mastery and financial independence",
    "values": ["discipline", "growth", "impact", "freedom"],
    "primaryStatTargets": {
      "strength": 75,
      "wisdom": 95,
      "charisma": 80,
      "composure": 90,
      "energy": 85,
      "influence": 80,
      "vitality": 85
    },
    "linkedMilestones": [
      {"id": "uuid", "title": "Reach 74kg target weight"},
      {"id": "uuid", "title": "Net worth 1M by 40"}
    ]
  }
}
```

### PUT /api/identity-profile

**Request Body**:
```json
{
  "archetype": "God of Mind-Power",
  "archetypeDescription": "A disciplined achiever...",
  "values": ["discipline", "growth", "impact", "freedom"],
  "primaryStatTargets": {
    "strength": 75,
    "wisdom": 95,
    "charisma": 80,
    "composure": 90,
    "energy": 85,
    "influence": 80,
    "vitality": 85
  },
  "linkedMilestoneIds": ["uuid", "uuid"]
}
```

### GET /api/primary-stats

**Response**:
```json
{
  "data": {
    "currentStats": {
      "strength": 62,
      "wisdom": 78,
      "charisma": 71,
      "composure": 65,
      "energy": 70,
      "influence": 58,
      "vitality": 74
    },
    "targets": {
      "strength": 75,
      "wisdom": 95,
      "charisma": 80,
      "composure": 90,
      "energy": 85,
      "influence": 80,
      "vitality": 85
    },
    "calculatedAt": "2024-12-10T00:00:00Z",
    "breakdown": {
      "strength": {
        "fromDimensions": {
          "health_recovery": 55,
          "growth_mind": 70
        },
        "weighted": 62
      }
    }
  }
}
```

## v1.1 Reviews API

### GET /api/reviews/weekly

**Response**:
```json
{
  "data": {
    "periodStart": "2024-12-02",
    "periodEnd": "2024-12-08",
    "healthIndexDelta": 2.5,
    "adherenceIndexDelta": -1.2,
    "wealthHealthDelta": 0.8,
    "longevityDelta": 0.1,
    "topStreaks": [
      {"taskId": "uuid", "taskTitle": "Morning run", "streakDays": 28},
      {"taskId": "uuid", "taskTitle": "Meditation", "streakDays": 14}
    ],
    "recommendedActions": [
      {"action": "Increase sleep to 8 hours", "priority": "high", "dimension": "health_recovery"},
      {"action": "Review investment allocations", "priority": "medium", "dimension": "asset_care"}
    ],
    "primaryStatsDelta": {
      "strength": 1,
      "wisdom": 2,
      "charisma": 0,
      "composure": -1,
      "energy": 1,
      "influence": 0,
      "vitality": 1
    }
  }
}
```

## v1.1 Scenario Comparison API

### POST /api/simulations/compare

**Request Body**:
```json
{
  "baselineScenarioId": "uuid",
  "compareScenarioIds": ["uuid", "uuid"],
  "horizonYears": 10,
  "milestoneTargets": [1000000, 5000000, 10000000]
}
```

**Response**:
```json
{
  "data": {
    "baseline": {
      "scenarioId": "uuid",
      "scenarioName": "Current Path",
      "endNetWorth": 2500000,
      "milestoneYears": {"1000000": 3.5, "5000000": null}
    },
    "comparisons": [
      {
        "scenarioId": "uuid",
        "scenarioName": "Aggressive Savings",
        "endNetWorth": 4200000,
        "netWorthDelta": 1700000,
        "milestoneYears": {"1000000": 2.1, "5000000": 8.5}
      }
    ]
  }
}
```

### GET /api/simulations/what-if

**Query Parameters**:
- `purchaseAmount`: One-off expense amount
- `purchaseDate`: When to apply
- `scenarioId`: Base scenario (defaults to baseline)

**Response**:
```json
{
  "data": {
    "withoutPurchase": {
      "netWorthAt10Years": 2500000,
      "firstMillionDate": "2027-06-15"
    },
    "withPurchase": {
      "netWorthAt10Years": 2320000,
      "firstMillionDate": "2027-09-20",
      "impact": {
        "netWorthReduction": 180000,
        "milestoneDelay": "3 months"
      }
    }
  }
}
```