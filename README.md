<p align="center">
  <img src="README.icon.gif" alt="LifeOS Logo" width="300" />
</p>

<h1 align="center">LifeOS</h1>

<p align="center">
  <strong>🌱 Comprehensive Life Management Platform</strong>
</p>

<p align="center">
  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet" alt=".NET 9"></a>
  <a href="https://react.dev/"><img src="https://img.shields.io/badge/React-19-61DAFB?logo=react" alt="React 19"></a>
  <a href="https://www.typescriptlang.org/"><img src="https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript" alt="TypeScript"></a>
  <a href="https://www.postgresql.org/"><img src="https://img.shields.io/badge/PostgreSQL-17-336791?logo=postgresql" alt="PostgreSQL"></a>
  <a href="https://vitejs.dev/"><img src="https://img.shields.io/badge/Vite-7.2-646CFF?logo=vite" alt="Vite"></a>
  <a href="https://tailwindcss.com/"><img src="https://img.shields.io/badge/Tailwind-4.1-06B6D4?logo=tailwindcss" alt="Tailwind CSS"></a>
</p>

<p align="center">
  Track, analyze, and optimize all aspects of your life through data-driven insights, financial simulations, and health metrics.
</p>

---

## ✨ Features

| Module | Description |
|--------|-------------|
| **📊 Life Score** | Aggregated score (0-100) across all life dimensions |
| **💰 Financial Management** | Multi-currency accounts, net worth tracking, tax profiles |
| **🔮 Simulation Engine** | Project your financial future with scenarios and events |
| **🏃 Health & Longevity** | Evidence-based life expectancy calculations |
| **📈 Custom Metrics** | Track any metric with API integration |
| **🎮 Gamification** | XP, achievements, streaks, milestones |

## 🚀 Quick Start

### Docker (Recommended)
```bash
git clone https://github.com/frostaura/fa.lifeos.git
cd fa.lifeos
docker compose up -d
```
- **Frontend**: http://localhost:5173
- **Backend**: http://localhost:5001
- **Default Login**: admin@system.local / Admin123!

### Local Development
```bash
# Database
docker compose up -d postgres

# Backend
cd src/backend && dotnet run --project LifeOS.Api

# Frontend
cd src/frontend && npm install && npm run dev
```

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Frontend (React 19 + Vite + RTK Query + Tailwind)              │
├─────────────────────────────────────────────────────────────────┤
│  Backend (ASP.NET Core 9 + EF Core + Clean Architecture)        │
├─────────────────────────────────────────────────────────────────┤
│  Database (PostgreSQL 17)                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Key Services
| Service | Purpose |
|---------|---------|
| SimulationEngine | Month-by-month projections with taxes, interest, inflation |
| LongevityEstimator | Life expectancy from health metrics |
| ScoreCalculator | Life Score computation across dimensions |
| AchievementService | XP, levels, achievement unlocks |

## 📡 API Overview

| Endpoint | Description |
|----------|-------------|
| `GET /api/dashboard` | Dashboard summary |
| `GET/POST /api/accounts` | Financial accounts |
| `POST /api/simulations/scenarios/{id}/run` | Run simulation |
| `GET /api/longevity` | Longevity estimate |
| `POST /api/metrics/record` | Record metrics |

Full API docs at `/swagger` when running.

## 🧪 Testing

```bash
# E2E Tests (Playwright)
npm test

# Backend Tests
cd src/backend && dotnet test
```

## 📁 Project Structure

```
fa.lifeos/
├── src/backend/          # .NET 9 API (Clean Architecture)
│   ├── LifeOS.Api/       # Controllers, Middleware
│   ├── LifeOS.Application/ # Services, DTOs
│   ├── LifeOS.Domain/    # Entities, Enums
│   └── LifeOS.Infrastructure/ # DbContext, Repos
├── src/frontend/         # React 19 SPA
│   └── src/
│       ├── components/   # Atomic design (atoms/molecules/organisms)
│       ├── pages/        # Route pages
│       └── services/     # RTK Query endpoints
└── docker-compose.yml    # Development environment
```

## 🔧 Configuration

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection |
| `Jwt__SecretKey` | JWT signing key (32+ chars) |
| `Cors__AllowedOrigins__0` | Allowed CORS origin |

## 📄 License

MIT License - see [LICENSE](./LICENSE)

---

<p align="center"><i>"Optimize your life, one dimension at a time."</i></p>
