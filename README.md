<p align="center">
  <img src="README.icon.gif" alt="LifeOS Logo" width="300" />
</p>

<h1 align="center">
  <b>LifeOS</b>
</h1>
<h3 align="center">🌱 Comprehensive Life Management Platform</h3>

**A full-stack life management application that tracks and optimizes multiple life dimensions including finances, health, productivity, and personal growth through data-driven insights and financial simulations.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB.svg)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6.svg)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791.svg)](https://www.postgresql.org/)
[![Vite](https://img.shields.io/badge/Vite-7.2-646CFF.svg)](https://vitejs.dev/)

## 🎯 What is LifeOS?

LifeOS is a comprehensive life management platform designed to help you track, analyze, and optimize all aspects of your life. It provides a unified dashboard for monitoring your overall "Life Score" across multiple dimensions, with deep capabilities for:

- **📊 Life Dimensions** - Track progress across customizable life areas (Health, Wealth, Career, Relationships, etc.)
- **💰 Financial Management** - Track accounts, net worth, transactions, and financial goals
- **🏃 Health & Longevity** - Monitor health metrics and longevity estimates
- **📈 Custom Metrics** - Define and track any personal metrics with API integration
- **🎮 Gamification** - Achievements, streaks, XP, and milestones to stay motivated
- **🔮 Financial Simulations** - Build scenarios and project your financial future
- **📱 Modern UI** - Beautiful glassmorphic design with dark mode

## ✨ Key Features

### Dashboard
- **Life Score** - Aggregated score across all life dimensions
- **Net Worth Tracking** - Real-time financial overview with trend charts
- **Dimension Grid** - Quick view of all life areas with scores and trends
- **Active Streaks** - Track habit consistency
- **Daily Tasks** - Today's priorities at a glance

### Financial Module
- **Multi-Currency Accounts** - Track bank accounts, investments, crypto, and liabilities
- **Net Worth History** - Visualize wealth growth over time
- **Financial Goals** - Set and track savings/investment targets
- **Loan Calculator** - Payoff projections and strategies
- **Exchange Rates** - Live FX rates for multi-currency portfolios

### Health & Longevity
- **Health Metrics** - Weight, body fat, steps, sleep, heart rate, and custom metrics
- **Longevity Estimates** - AI-powered life expectancy projections based on lifestyle factors
- **Habit Streaks** - Visual habit tracking with streak calendars
- **Trend Sparklines** - Quick metric trend visualization

### Metrics System
- **Custom Definitions** - Create any metric you want to track
- **API Playground** - Test metric collection with live JSON editor
- **Event Logging** - Full audit trail of all metric submissions
- **Integration Ready** - Connect with n8n, Apple Shortcuts, Zapier, etc.

### Simulations
- **Scenario Builder** - Create multiple financial scenarios
- **Event Modeling** - Model income changes, expenses, investments, inflation
- **Timeline Projections** - Visualize financial futures up to decades ahead
- **Baseline Comparison** - Compare scenarios against your baseline

## 🏗️ Technology Stack

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 9.0 | Web API framework |
| **Entity Framework Core** | 9.0 | ORM & database migrations |
| **PostgreSQL** | 15+ | Primary database |
| **JWT** | - | Authentication |
| **Clean Architecture** | - | Code organization |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| **React** | 19.2 | UI framework |
| **TypeScript** | 5.9 | Type safety |
| **Vite** | 7.2 | Build tool |
| **Redux Toolkit** | 2.11 | State management |
| **Tailwind CSS** | 4.1 | Styling |
| **React Router** | 7.9 | Routing |
| **Recharts** | 3.5 | Charts & visualizations |

### Testing & DevOps
| Technology | Purpose |
|------------|---------|
| **Playwright** | E2E testing |
| **Docker Compose** | Container orchestration |
| **GitHub Actions** | CI/CD |

## 📁 Repository Structure

```
fa.lifeos/
├── src/
│   ├── backend/                   # .NET 9 Web API
│   │   ├── LifeOS.Api/           # API controllers & configuration
│   │   ├── LifeOS.Application/   # Business logic & services
│   │   ├── LifeOS.Domain/        # Entities & domain models
│   │   ├── LifeOS.Infrastructure/ # Data access & external services
│   │   └── LifeOS.Tests/         # Unit & integration tests
│   └── frontend/                  # React 19 SPA
│       ├── src/
│       │   ├── components/       # Reusable UI components
│       │   ├── pages/            # Route pages
│       │   ├── services/         # API client (RTK Query)
│       │   ├── store/            # Redux store
│       │   ├── hooks/            # Custom React hooks
│       │   ├── types/            # TypeScript definitions
│       │   └── utils/            # Helper functions
│       └── public/               # Static assets
├── .gaia/                        # GAIA development system
│   ├── designs/                  # Architecture & design docs
│   ├── agents/                   # AI agent specifications
│   └── mcps/                     # MCP server for AI tooling
├── docker-compose.yml            # Development environment
├── docker-compose.prod.yml       # Production environment
└── playwright.config.ts          # E2E test configuration
```

## 🚀 Quick Start

### Prerequisites

- **Node.js** 20+ LTS
- **.NET SDK** 9.0+
- **Docker** 24+ & Docker Compose
- **PostgreSQL** 15+ (or use Docker)

### Option 1: Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/frostaura/fa.lifeos.git
cd fa.lifeos

# Start all services
docker compose up -d

# Access the application
# Frontend: http://localhost:5173
# Backend:  http://localhost:5001
# Database: localhost:5432
```

### Option 2: Local Development

```bash
# Clone the repository
git clone https://github.com/frostaura/fa.lifeos.git
cd fa.lifeos

# Start PostgreSQL (Docker)
docker compose up -d postgres

# Backend
cd src/backend
dotnet restore
dotnet run --project LifeOS.Api

# Frontend (new terminal)
cd src/frontend
npm install
npm run dev
```

### Default Credentials

| User | Email | Password |
|------|-------|----------|
| Admin | admin@system.local | Admin123! |

> ⚠️ **Note**: Default credentials are for development only. Change them in production!

## 🔧 Configuration

### Backend Configuration

The API is configured via `appsettings.json` and environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lifeos;Username=lifeos;Password=lifeos_dev_password"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-at-least-32-characters",
    "Issuer": "lifeos-api",
    "Audience": "lifeos-client"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Development |
| `ConnectionStrings__DefaultConnection` | Database connection string | - |
| `Jwt__SecretKey` | JWT signing key | - |
| `Cors__AllowedOrigins__0` | Allowed CORS origin | http://localhost:5173 |
| `Fido2__ServerDomain` | WebAuthn RP ID (domain for biometric auth) | localhost |
| `Fido2__Origins__0` | Allowed WebAuthn origin | http://localhost:5173 |

### WebAuthn/Biometric Authentication

LifeOS uses WebAuthn (FIDO2) for passwordless biometric authentication. The configuration must match your deployment domain:

**For local development:**
```bash
FIDO2_SERVER_DOMAIN=localhost
FIDO2_ORIGIN=http://localhost:5173
```

**For production (e.g., lifeos.frostaura.net):**
```bash
FIDO2_SERVER_DOMAIN=lifeos.frostaura.net
FIDO2_ORIGIN=https://lifeos.frostaura.net
CORS_ORIGIN_0=https://lifeos.frostaura.net
```

> **Important:** If you see the error "The RP ID 'localhost' is invalid for this domain", you need to set the `FIDO2_SERVER_DOMAIN` environment variable to match your deployment domain.
>
> For detailed configuration instructions, see [WEBAUTHN_CONFIG.md](./WEBAUTHN_CONFIG.md)

## 📡 API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/refresh` | Refresh token |
| POST | `/api/auth/logout` | User logout |

### Dashboard
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard` | Dashboard summary |
| GET | `/api/dashboard/net-worth/history` | Net worth history |

### Dimensions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dimensions` | List all dimensions |
| GET | `/api/dimensions/{id}` | Get dimension details |
| POST | `/api/dimensions` | Create dimension |
| PATCH | `/api/dimensions/{id}` | Update dimension |

### Finances
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | List accounts |
| POST | `/api/accounts` | Create account |
| GET | `/api/transactions` | List transactions |
| POST | `/api/transactions` | Create transaction |
| GET | `/api/financial-goals` | List financial goals |

### Metrics
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/metrics/definitions` | List metric definitions |
| POST | `/api/metrics/definitions` | Create metric definition |
| POST | `/api/metrics/record` | Record metric values |
| GET | `/api/metrics/{code}/records` | Get metric records |

### Simulations
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/simulations/scenarios` | List scenarios |
| POST | `/api/simulations/scenarios` | Create scenario |
| POST | `/api/simulations/{id}/run` | Run simulation |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/longevity` | Get longevity estimate |
| GET | `/api/health` | Health check |

## 🧪 Testing

### E2E Tests (Playwright)

```bash
# Install dependencies
npm install

# Run all tests
npm test

# Run specific test suites
npm run test:login
npm run test:dashboard
npm run test:finances
npm run test:achievements

# Run with UI
npm run test:ui

# Run with browser visible
npm run test:headed
```

### Backend Tests

```bash
cd src/backend
dotnet test
```

## 🎨 UI Components

LifeOS uses a custom glassmorphic design system with these key components:

- **GlassCard** - Frosted glass containers with glow effects
- **Button** - Primary, secondary, and ghost variants
- **Badge** - Status indicators
- **Input/Select** - Form controls
- **Spinner** - Loading indicators
- **Charts** - Net worth, sparklines, and progress charts

## 🔒 Security Features

- **JWT Authentication** - Secure token-based auth
- **Password Hashing** - Argon2id hashing
- **CORS Protection** - Configurable origins
- **Rate Limiting** - API throttling
- **Input Validation** - Request validation
- **HTTPS** - TLS in production

## 🛠️ Development

### Code Style

- **Backend**: .NET analyzers + StyleCop
- **Frontend**: ESLint + TypeScript strict mode

### Building

```bash
# Frontend
cd src/frontend
npm run build    # Production build
npm run lint     # Type checking

# Backend
cd src/backend
dotnet build     # Build all projects
dotnet publish   # Production build
```

### Database Migrations

```bash
cd src/backend/LifeOS.Api

# Add migration
dotnet ef migrations add MigrationName --project ../LifeOS.Infrastructure

# Apply migrations
dotnet ef database update --project ../LifeOS.Infrastructure
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

---

<p align="center">
  <i>"Optimize your life, one dimension at a time."</i>
</p>
