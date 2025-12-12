# API Design

## API Standards

### Base URL
- Development: `http://localhost:5001/api`
- Frontend Proxy: `http://localhost:5173/api` (proxied via Vite)

### Authentication
- Bearer token: `Authorization: Bearer <jwt-token>`
- API Key: `X-API-Key: <key>` (for external integrations)

## v3.0 New Endpoints Summary
- **MCP Tools**: `/api/mcp/tools/{toolName}` - 15+ AI-friendly JSON tools (getDashboardSnapshot, recordMetrics, listTasks, etc.)
- **Metrics Recording**: `POST /api/metrics/record` - Enhanced nested ingestion with validation
- **Task Auto-Evaluation**: Background job auto-completes tasks when metric conditions met
- **Score Snapshots**: `/api/scores/history` - Historical LifeOS Score components
- **Dimension Scores**: `/api/dimensions/{id}/scores` - Per-dimension scoring history with metric contributions
- **Enhanced Reviews**: Weekly/monthly reviews with updated formulas (Health Index, Adherence, Wealth Health)

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

### Scores & History (v3.0)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/scores/current` | Current LifeOS Score with components |
| GET | `/api/scores/history` | Historical score snapshots |
| GET | `/api/dimensions/{id}/scores` | Per-dimension score history |

### Task Completions (v3.0)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks/{id}/completions` | Task completion history |
| GET | `/api/tasks/auto-evaluated` | Tasks auto-completed by metric conditions |

### MCP Tools (v3.0)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/mcp/tools/{toolName}` | Execute MCP tool (15+ available tools) |
| GET | `/api/mcp/tools` | List available MCP tools with schemas |

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

## v3.0 Score & History Endpoints

### GET /api/scores/current

**Purpose**: Get current LifeOS Score with all components

**Response**:
```json
{
  "data": {
    "lifeosScore": 78.5,
    "components": {
      "healthIndex": 82.3,
      "adherenceIndex": 75.2,
      "wealthHealthScore": 78.1
    },
    "weights": {
      "health": 0.4,
      "adherence": 0.3,
      "wealth": 0.3
    },
    "calculatedAt": "2024-12-11T18:00:00Z",
    "breakdown": {
      "healthIndex": {
        "dimensionScores": {
          "health_recovery": 85,
          "play_adventure": 78
        }
      },
      "adherenceIndex": {
        "completionRate": 0.82,
        "streakPenaltyFactor": 7.0,
        "rawScore": 82,
        "adjustedScore": 75.2
      },
      "wealthHealthScore": {
        "savingsRate": 85,
        "debtToIncome": 90,
        "emergencyFund": 100,
        "diversification": 65,
        "netWorthGrowth": 50,
        "average": 78.1
      }
    }
  }
}
```

### GET /api/scores/history

**Purpose**: Get historical score snapshots

**Query Parameters**:
- `dateFrom` (optional): Start date (ISO 8601)
- `dateTo` (optional): End date (ISO 8601)
- `interval` (optional): daily | weekly | monthly (default: daily)

**Response**:
```json
{
  "data": [
    {
      "recordedAt": "2024-12-11T00:00:00Z",
      "lifeosScore": 78.5,
      "healthIndex": 82.3,
      "adherenceIndex": 75.2,
      "wealthHealthScore": 78.1,
      "longevityYearsAdded": 8.5
    }
  ],
  "meta": {
    "dateFrom": "2024-12-01",
    "dateTo": "2024-12-11",
    "count": 11
  }
}
```

### GET /api/dimensions/{id}/scores

**Purpose**: Get per-dimension score history with metric contributions

**Query Parameters**:
- `dateFrom` (optional): Start date
- `dateTo` (optional): End date
- `includeMetrics` (optional): Boolean, include per-metric breakdown (default: false)

**Response**:
```json
{
  "data": {
    "dimensionId": "uuid",
    "dimensionCode": "health_recovery",
    "dimensionName": "Health",
    "scores": [
      {
        "recordedAt": "2024-12-11T00:00:00Z",
        "dimensionScore": 85,
        "metricContributions": [
          {
            "metricCode": "weight_kg",
            "metricName": "Body Weight",
            "metricScore": 78,
            "currentValue": 74.5,
            "targetValue": 70,
            "targetDirection": "AtOrBelow",
            "weight": 0.3
          },
          {
            "metricCode": "sleep_hours",
            "metricName": "Sleep Hours",
            "metricScore": 90,
            "currentValue": 7.5,
            "targetValue": 8.0,
            "targetDirection": "AtOrAbove",
            "weight": 0.4
          }
        ]
      }
    ]
  }
}
```

### GET /api/tasks/{id}/completions

**Purpose**: Get completion history for a task

**Query Parameters**:
- `dateFrom` (optional): Start date
- `dateTo` (optional): End date
- `page` (optional): Page number
- `perPage` (optional): Items per page (default: 50)

**Response**:
```json
{
  "data": [
    {
      "id": "uuid",
      "taskId": "uuid",
      "completedAt": "2024-12-11T07:30:00Z",
      "completionType": "Manual",
      "metricValue": 5.2,
      "metricCode": null,
      "notes": null
    },
    {
      "id": "uuid2",
      "taskId": "uuid",
      "completedAt": "2024-12-10T07:25:00Z",
      "completionType": "AutoMetric",
      "metricValue": 5.0,
      "metricCode": "running_distance_km",
      "notes": "Auto-completed when running_distance_km >= 5"
    }
  ],
  "meta": {
    "page": 1,
    "perPage": 50,
    "total": 28,
    "totalPages": 1
  }
}
```

### GET /api/tasks/auto-evaluated

**Purpose**: List tasks that have auto-evaluation configured

**Response**:
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Run 5km",
      "taskType": "habit",
      "metricCode": "running_distance_km",
      "targetValue": 5,
      "targetComparison": "GreaterThanOrEqual",
      "lastAutoCompletedAt": "2024-12-11T07:30:00Z",
      "autoCompletionCount": 28
    }
  ]
}
```

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

## v3.0 MCP Tools API

### Overview
MCP (Model Context Protocol) Tools provide simplified, AI-friendly endpoints that proxy to existing REST APIs. All tools use a unified request/response format.

### Base Endpoint
```
POST /api/mcp/tools/{toolName}
Authorization: Bearer {jwt_token}
```

### Unified Request Format
```json
{
  "params": {
    // tool-specific parameters
  }
}
```

### Unified Response Format
```json
{
  "success": true,
  "data": {
    // tool-specific response data
  },
  "meta": {
    "toolName": "getDashboardSnapshot",
    "executedAt": "2024-12-11T18:00:00Z",
    "executionTimeMs": 45
  }
}
```

### Available Tools

#### getDashboardSnapshot
**Purpose**: Get current dashboard overview
**Params**: None
**Response**:
```json
{
  "data": {
    "lifeosScore": 78.5,
    "healthIndex": 82.3,
    "adherenceIndex": 75.2,
    "wealthHealthScore": 78.1,
    "netWorth": 1250000.50,
    "currency": "ZAR",
    "dimensionScores": [
      {"code": "health_recovery", "name": "Health", "score": 82, "trend": "up"},
      {"code": "work_contribution", "name": "Work", "score": 75, "trend": "stable"}
    ],
    "todaysTasks": [
      {"id": "uuid", "title": "Morning run", "type": "habit", "streakDays": 12}
    ],
    "activeStreaks": [
      {"taskTitle": "Morning run", "streakDays": 12, "riskPenalty": 0}
    ]
  }
}
```

#### recordMetrics
**Purpose**: Record metric values (nested structure)
**Params**:
```json
{
  "timestamp": "2024-12-11T08:00:00Z",
  "source": "apple_health",
  "metrics": {
    "health_recovery": {
      "weight_kg": 74.5,
      "sleep_hours": 7.5
    },
    "work_contribution": {
      "deep_work_hours": 4.2
    }
  }
}
```
**Response**:
```json
{
  "data": {
    "recordedCount": 3,
    "skippedCount": 0,
    "errors": []
  }
}
```

#### listTasks
**Purpose**: List tasks with filters
**Params**:
```json
{
  "dimensionCode": "health_recovery",  // optional
  "taskType": "habit",                 // optional: habit | one_off | scheduled_event
  "isCompleted": false,                // optional
  "dateFrom": "2024-12-01",           // optional
  "dateTo": "2024-12-31"              // optional
}
```
**Response**:
```json
{
  "data": {
    "tasks": [
      {
        "id": "uuid",
        "title": "Morning run",
        "taskType": "habit",
        "frequency": "daily",
        "isCompleted": false,
        "streakDays": 12,
        "dimensionCode": "health_recovery"
      }
    ],
    "count": 1
  }
}
```

#### completeTask
**Purpose**: Mark task as complete
**Params**:
```json
{
  "taskId": "uuid",
  "completedAt": "2024-12-11T07:30:00Z",  // optional, defaults to now
  "metricValue": 5.2                      // optional, if task is metric-linked
}
```
**Response**:
```json
{
  "data": {
    "taskId": "uuid",
    "streakUpdated": true,
    "newStreakLength": 13,
    "metricRecorded": true
  }
}
```

#### getWeeklyReview
**Purpose**: Get weekly review data
**Params**:
```json
{
  "weekOffset": 0  // 0 = current week, -1 = last week, etc.
}
```
**Response**:
```json
{
  "data": {
    "periodStart": "2024-12-02",
    "periodEnd": "2024-12-08",
    "healthIndexDelta": 2.5,
    "adherenceIndexDelta": -1.2,
    "wealthHealthDelta": 0.8,
    "topStreaks": [
      {"taskTitle": "Morning run", "streakDays": 28}
    ],
    "recommendedActions": [
      {"action": "Increase sleep to 8 hours", "priority": "high", "dimension": "health_recovery"}
    ]
  }
}
```

#### getMonthlyReview
**Purpose**: Get monthly review data
**Params**:
```json
{
  "monthOffset": 0  // 0 = current month, -1 = last month, etc.
}
```
**Response**: Similar to weekly but with net worth trajectory and scenario comparison

#### runSimulation
**Purpose**: Execute simulation scenario
**Params**:
```json
{
  "scenarioId": "uuid",        // optional, uses baseline if not provided
  "horizonYears": 10           // optional, defaults to scenario end date
}
```
**Response**:
```json
{
  "data": {
    "scenarioId": "uuid",
    "scenarioName": "Current Path",
    "projections": [
      {"date": "2025-01-01", "netWorth": 1300000, "cashflow": 5000}
    ],
    "endNetWorth": 2500000,
    "milestones": {
      "1000000": {"achieved": true, "date": "2027-06-15"}
    }
  }
}
```

#### createWhatIfScenario
**Purpose**: Model one-off purchase impact
**Params**:
```json
{
  "purchaseAmount": 50000,
  "purchaseDate": "2025-03-15",
  "baseScenarioId": "uuid"  // optional, uses baseline if not provided
}
```
**Response**:
```json
{
  "data": {
    "withoutPurchase": {
      "netWorthAt10Years": 2500000
    },
    "withPurchase": {
      "netWorthAt10Years": 2320000,
      "netWorthReduction": 180000,
      "milestoneDelays": {
        "1000000": "3 months"
      }
    }
  }
}
```

#### compareScenarios
**Purpose**: Compare multiple scenarios
**Params**:
```json
{
  "baselineScenarioId": "uuid",
  "compareScenarioIds": ["uuid1", "uuid2"],
  "horizonYears": 10
}
```
**Response**: Baseline vs scenario comparison with deltas

#### listMilestones
**Purpose**: List milestones with progress
**Params**:
```json
{
  "dimensionCode": "health_recovery",  // optional
  "status": "active"                    // optional: active | completed | abandoned
}
```
**Response**:
```json
{
  "data": {
    "milestones": [
      {
        "id": "uuid",
        "title": "Run a marathon",
        "dimensionCode": "health_recovery",
        "targetDate": "2025-12-31",
        "status": "active",
        "progress": {
          "currentValue": 12.5,
          "targetValue": 42,
          "progressPercent": 29.76,
          "metricCode": "running_distance_total"
        }
      }
    ]
  }
}
```

#### getIdentityProfile
**Purpose**: Get user identity profile and stats
**Params**: None
**Response**:
```json
{
  "data": {
    "archetype": "God of Mind-Power",
    "values": ["discipline", "growth", "impact"],
    "currentStats": {
      "strength": 62,
      "wisdom": 78,
      "charisma": 71,
      "composure": 65,
      "energy": 70,
      "influence": 58,
      "vitality": 74
    },
    "targetStats": {
      "strength": 75,
      "wisdom": 95,
      "charisma": 80,
      "composure": 90,
      "energy": 85,
      "influence": 80,
      "vitality": 85
    }
  }
}
```

#### getLongevityEstimate
**Purpose**: Get longevity years added breakdown
**Params**: None
**Response**:
```json
{
  "data": {
    "totalYearsAdded": 8.5,
    "baselineLifeExpectancy": 80,
    "adjustedLifeExpectancy": 88.5,
    "factors": [
      {
        "factorType": "exercise",
        "yearsAdded": 3.2,
        "evidenceSource": "WHO study 2018",
        "currentMetricValue": 150,
        "metricUnit": "minutes/week"
      }
    ]
  }
}
```

#### getHealthIndex
**Purpose**: Get current health index with breakdown
**Params**: None
**Response**:
```json
{
  "data": {
    "healthIndex": 82.3,
    "dimensionScores": [
      {
        "dimension": "health_recovery",
        "dimensionScore": 85,
        "metricContributions": [
          {"metricCode": "weight_kg", "score": 78, "weight": 0.3},
          {"metricCode": "sleep_hours", "score": 90, "weight": 0.4}
        ]
      }
    ]
  }
}
```

#### getNetWorthSummary
**Purpose**: Get net worth summary and trend
**Params**: None
**Response**:
```json
{
  "data": {
    "currentNetWorth": 1250000.50,
    "currency": "ZAR",
    "breakdown": {
      "assets": 1500000,
      "liabilities": 250000
    },
    "byAccountType": {
      "Bank": 50000,
      "Investment": 1000000,
      "Loan": -250000
    },
    "trend": {
      "7day": 2.5,
      "30day": 8.3,
      "90day": 12.1
    }
  }
}
```

#### getMetricHistory
**Purpose**: Get historical values for metric
**Params**:
```json
{
  "metricCode": "weight_kg",
  "dateFrom": "2024-01-01",     // optional
  "dateTo": "2024-12-31",       // optional
  "aggregation": "daily"        // optional: daily | weekly | monthly
}
```
**Response**:
```json
{
  "data": {
    "metricCode": "weight_kg",
    "metricName": "Body Weight",
    "unit": "kg",
    "records": [
      {"date": "2024-12-01", "value": 75.2},
      {"date": "2024-12-08", "value": 74.5}
    ]
  }
}
```

#### listMetricDefinitions
**Purpose**: List available metrics
**Params**:
```json
{
  "dimensionCode": "health_recovery"  // optional
}
```
**Response**:
```json
{
  "data": {
    "metrics": [
      {
        "code": "weight_kg",
        "name": "Body Weight",
        "unit": "kg",
        "valueType": "Decimal",
        "targetValue": 70,
        "targetDirection": "AtOrBelow",
        "isDerived": false
      }
    ]
  }
}
```

### MCP Tool Error Responses
```json
{
  "success": false,
  "error": {
    "code": "TOOL_ERROR",
    "message": "Task not found",
    "details": {
      "taskId": "uuid"
    }
  }
}
```

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