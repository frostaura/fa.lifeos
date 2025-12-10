# API Design

## API Standards

### Base URL
- Development: `http://localhost:3000/api`
- Staging: `https://staging.example.com/api`
- Production: `https://api.example.com`

### Versioning
- URL versioning: `/api/v1/`
- Header versioning: `Accept: application/vnd.api+json;version=1`

### Authentication
- Bearer token: `Authorization: Bearer <token>`
- API Key: `X-API-Key: <key>`
- Session cookie: `sessionId`

## REST Conventions

### HTTP Methods
- `GET` - Read resources
- `POST` - Create resources
- `PUT` - Full update
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

### Response Format

#### Success Response
```json
{
  "data": {
    "id": "123",
    "type": "user",
    "attributes": {}
  },
  "meta": {
    "timestamp": "2024-01-01T00:00:00Z"
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
      {
        "field": "email",
        "message": "Invalid email format"
      }
    ]
  }
}
```

#### Pagination
```json
{
  "data": [],
  "meta": {
    "page": 1,
    "perPage": 20,
    "total": 100,
    "totalPages": 5
  },
  "links": {
    "first": "/api/v1/users?page=1",
    "prev": null,
    "next": "/api/v1/users?page=2",
    "last": "/api/v1/users?page=5"
  }
}
```

## Core Endpoints

### Authentication
- `POST /auth/login` - User login
- `POST /auth/logout` - User logout
- `POST /auth/refresh` - Refresh token
- `POST /auth/register` - User registration
- `POST /auth/forgot-password` - Password reset

### Users
- `GET /users` - List users
- `GET /users/:id` - Get user
- `POST /users` - Create user
- `PATCH /users/:id` - Update user
- `DELETE /users/:id` - Delete user

### Resources (Example)
- `GET /resources` - List with filters
- `GET /resources/:id` - Get single
- `POST /resources` - Create new
- `PATCH /resources/:id` - Update
- `DELETE /resources/:id` - Delete

## Query Parameters

### Filtering
```
GET /api/v1/users?status=active&role=admin
```

### Sorting
```
GET /api/v1/users?sort=-created_at,name
```

### Pagination
```
GET /api/v1/users?page=2&limit=20
```

### Field Selection
```
GET /api/v1/users?fields=id,name,email
```

### Relationships
```
GET /api/v1/users?include=posts,comments
```

## Rate Limiting

### Headers
- `X-RateLimit-Limit`: Request limit
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Reset timestamp

### Limits
- Anonymous: 100 requests/hour
- Authenticated: 1000 requests/hour
- Premium: 10000 requests/hour

## WebSocket Events

### Connection
```javascript
ws://localhost:3000/socket
```

### Events
- `connection` - Client connected
- `disconnect` - Client disconnected
- `message` - New message
- `notification` - Push notification
- `update` - Real-time update

## API Documentation

### OpenAPI/Swagger
Available at `/api/docs`

### Postman Collection
Export available at `/api/postman`

## Testing

### Example CURL
```bash
curl -X GET https://api.example.com/v1/users \
  -H "Authorization: Bearer token" \
  -H "Accept: application/json"
```

### Example Response
```json
{
  "data": [...],
  "meta": {...}
}
```

---

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