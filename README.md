# HefestAI (Loremaster)

AI-powered worldbuilding and lore management platform for tabletop RPG game masters and creative writers.

## Architecture Overview

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│    Frontend     │────▶│   .NET Backend   │────▶│  Genkit Service │
│   (React/Vite)  │     │   (ASP.NET Core) │     │   (Node/Express)│
│                 │     │                  │     │                 │
│   Vercel        │     │    Fly.io        │     │    Fly.io       │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌──────────────────┐
                        │   PostgreSQL     │
                        │   + pgvector     │
                        │                  │
                        │ Dev: Docker      │
                        │ Prod: Supabase   │
                        └──────────────────┘
```

### Components

| Component | Technology | Port (Dev) | Deployment |
|-----------|------------|------------|------------|
| Frontend | React 19 + Vite 6 + TypeScript | 5173 | Vercel |
| Backend API | .NET 8 + EF Core 8 | 5000 | Fly.io |
| AI Service | Node.js 20 + Genkit 1.0 + Gemini | 3000 | Fly.io |
| Database | PostgreSQL 16 + pgvector | 5432 | Supabase |

## Features

### Core Features
- **AI-Powered Content Generation**: Characters, NPCs, enemies, locations, items, missions, encounters using Google Gemini 2.0 Flash
- **Image Generation**: AI-generated images using Gemini 2.5 Flash with style options (realistic, artistic, anime, fantasy, sketch)
- **RAG (Retrieval-Augmented Generation)**: Semantic search with pgvector embeddings for context-aware generation
- **Campaign Management**: Create and manage campaigns with join codes for players
- **Multi-Role Support**: Player, Master, Admin roles with role-based access control

### Generators (9 Types)
| Generator | Description | Role Required |
|-----------|-------------|---------------|
| Character | Player character creation with 3D visualization | Player+ |
| NPC | Non-player character generation | Master+ |
| Enemy | Antagonist and monster generation | Master+ |
| Vehicle | Starships, ground vehicles, etc. | Master+ |
| Solar System | Planetary systems with 3D visualization | Master+ |
| Mission | Quest and mission generation | Master+ |
| Encounter | Combat and social encounter design | Master+ |
| Campaign | Full campaign outline generation | Master+ |
| Campaign Settings | Campaign configuration and management | Master+ |

### Additional Features
- **3D Visualizations**: Three.js powered character, solar system, and vehicle viewers
- **PDF Export**: Character sheet generation and export via jsPDF
- **Entity Gallery**: Browse, edit, and manage all created lore entities
- **Document Import**: Import PDFs/manuals for RAG context
- **Dice Roller**: Built-in dice rolling utility
- **Rule Query**: AI-powered rules lookup

## Quick Start

### Prerequisites

- Node.js 20+
- .NET 8 SDK
- Docker Desktop
- Google AI API Key ([Get one here](https://aistudio.google.com/apikey))

### 1. Clone and Setup

```bash
git clone <repository-url>
cd hefestai

# Copy environment file
cp .env.example .env
# Edit .env and add your GOOGLE_GENAI_API_KEY
```

### 2. Start Database (Docker)

```bash
docker-compose -f docker-compose.dev.yml up -d
```

### 3. Run Backend

```bash
cd src/backend

# Restore packages
dotnet restore

# Run migrations
dotnet ef database update --project Loremaster.Infrastructure --startup-project Loremaster.Api

# Start the API
dotnet run --project Loremaster.Api
```

### 4. Run Genkit Service

```bash
cd src/genkit-service

# Install dependencies
npm install

# Start in development mode
npm run dev
```

### 5. Run Frontend

```bash
cd src/frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

Open http://localhost:5173 in your browser.

## Project Structure

```
hefestai/
├── src/
│   ├── frontend/                    # React 19 + Vite + TypeScript
│   │   ├── core/                    # Core services & hooks
│   │   │   ├── context/             # AuthContext, CampaignContext
│   │   │   ├── hooks/               # useApi, useTransition, useCharacterSheetPdf
│   │   │   ├── services/            # API clients, PDF generation, storage
│   │   │   ├── types/               # TypeScript type definitions
│   │   │   └── utils/               # Security, validation utilities
│   │   ├── features/                # Feature modules
│   │   │   ├── auth/                # Login, Signup pages
│   │   │   ├── gallery/             # Entity gallery & management
│   │   │   └── generators/          # 9 generator pages
│   │   ├── shared/                  # Shared components
│   │   │   ├── components/          # UI, layout, feedback, modals
│   │   │   └── guards/              # AuthGuard, RoleGuard
│   │   └── components/              # DiceRoller, RuleQuery
│   │
│   ├── backend/                     # .NET 8 Clean Architecture
│   │   ├── Loremaster.Api/          # ASP.NET Core Web API
│   │   │   └── Controllers/         # REST endpoints
│   │   ├── Loremaster.Application/  # CQRS commands/queries (MediatR)
│   │   │   └── Features/            # Feature-based organization
│   │   ├── Loremaster.Domain/       # Domain layer
│   │   │   ├── Entities/            # Campaign, User, LoreEntity, etc.
│   │   │   ├── Enums/               # UserRole, CampaignRole, etc.
│   │   │   └── ValueObjects/        # FieldDefinition, EntityGenerationConfig
│   │   ├── Loremaster.Infrastructure/  # Data access & external services
│   │   │   ├── Persistence/         # EF Core, repositories, migrations
│   │   │   ├── Identity/            # JWT, password hashing
│   │   │   └── Services/            # GenkitAiService, TextChunkingService
│   │   ├── Loremaster.Shared/       # Helpers, extensions
│   │   ├── Loremaster.Tests.Unit/   # xUnit + FluentAssertions + Moq
│   │   └── Loremaster.Tests.Integration/  # Testcontainers integration tests
│   │
│   └── genkit-service/              # Node.js AI microservice
│       └── src/
│           ├── index.ts             # Express server
│           ├── flows.ts             # Genkit AI flows (6 flows)
│           ├── schemas.ts           # Zod validation
│           └── middleware/          # JWT auth
│
├── docker/
│   └── init.sql                     # PostgreSQL initialization
├── docker-compose.yml               # Full stack (production-like)
├── docker-compose.dev.yml           # Development (DB only)
└── .env.example                     # Environment template
```

## Domain Model

### Core Entities

| Entity | Description |
|--------|-------------|
| **User** | Users with roles (Player, Master, Admin) and JWT auth |
| **Campaign** | Game campaigns with join codes and game system |
| **CampaignMember** | User participation with campaign-specific roles |
| **GameSystem** | Supported RPG systems |
| **LoreEntity** | Polymorphic entity for all lore content |
| **EntityTemplate** | Reusable templates with field definitions |
| **Document** | Ingested documents for RAG |
| **RagSource** | RAG document sources (rulebooks, supplements) |
| **GenerationRequest** | AI generation request tracking |
| **GenerationResult** | AI generation outputs with source tracing |

### Entity Categories

- **Characters** - Player characters, NPCs, enemies
- **Planets** - Locations, solar systems
- **Vehicles** - Ships, vehicles, mounts

### User Roles

| Role | Permissions |
|------|-------------|
| **Player** | Create characters, view allowed entities, join campaigns |
| **Master** | All player permissions + create campaigns, all generators, manage entities |
| **Admin** | Full system access, user management |

## API Endpoints

### Backend API (Port 5000)

| Endpoint | Method | Description | Auth |
|----------|--------|-------------|------|
| `/api/auth/register` | POST | Register new user | No |
| `/api/auth/login` | POST | Login, get tokens | No |
| `/api/auth/refresh-token` | POST | Refresh access token | No |
| `/api/auth/me` | GET | Get current user | Yes |
| `/api/auth/logout` | POST | Logout, revoke refresh token | Yes |
| `/api/campaigns` | GET/POST | List/create campaigns | Yes |
| `/api/campaigns/{id}` | GET/PUT/DELETE | Campaign CRUD | Yes |
| `/api/campaigns/{id}/join` | POST | Join campaign with code | Yes |
| `/api/entities` | GET/POST | List/create lore entities | Yes |
| `/api/entities/{id}` | GET/PUT/DELETE | Entity CRUD | Yes |
| `/api/entity-templates` | GET/POST | List/create templates | Yes |
| `/api/entity-templates/{id}` | GET/PUT/DELETE | Template CRUD | Yes |
| `/api/game-systems` | GET/POST | List/create game systems | Yes |
| `/api/documents/ingest` | POST | Ingest document for RAG | Yes |
| `/api/documents/search` | POST | Semantic search | Yes |
| `/api/ai/generate` | POST | Generate text content | Yes |
| `/api/ai/chat` | POST | Multi-turn chat | Yes |
| `/health` | GET | Health check | No |
| `/health/ready` | GET | Readiness probe | No |
| `/health/live` | GET | Liveness probe | No |

### Genkit Service (Port 3000)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/generate` | POST | Text generation (Gemini 2.0 Flash) |
| `/api/chat` | POST | Multi-turn conversation |
| `/api/summarize` | POST | Text summarization (concise/detailed/bullet-points) |
| `/api/embeddings` | POST | Vector embeddings (text-embedding-004) |
| `/api/rag/generate` | POST | RAG-based generation |
| `/api/generate-image` | POST | Image generation (Gemini 2.5 Flash) |
| `/health` | GET | Health check |

## Environment Configuration

### Development (.env)

```bash
# Database (Local Docker)
DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=loremaster;Username=postgres;Password=postgres

# JWT (Development keys - DO NOT use in production)
JWT_SECRET=DEVELOPMENT_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32CHARS
SERVICE_JWT_SECRET=DEV_SERVICE_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32

# Google AI
GOOGLE_GENAI_API_KEY=your_api_key_here

# Services
GENKIT_SERVICE_URL=http://localhost:3000
VITE_API_URL=http://localhost:5000/api
```

### Production (Fly.io Secrets)

```bash
# Backend (loremaster-api)
fly secrets set ConnectionStrings__SupabaseConnection="Host=db.xxx.supabase.co;Port=6543;..."
fly secrets set Jwt__Secret="your-secure-jwt-secret"
fly secrets set ServiceJwt__Secret="your-service-secret"
fly secrets set Cors__Origins__0="https://your-app.vercel.app"

# Genkit (loremaster-genkit)
fly secrets set GOOGLE_GENAI_API_KEY="your-api-key"
fly secrets set SERVICE_JWT_SECRET="your-service-secret"
```

## Deployment

### 1. Supabase Database Setup

1. Create project at [supabase.com](https://supabase.com)
2. Enable pgvector extension:
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```
3. Get pooled connection string (Settings > Database > Connection string > URI, port 6543)

### 2. Deploy Genkit to Fly.io

```bash
cd src/genkit-service

# Create app
fly apps create loremaster-genkit

# Set secrets
fly secrets set GOOGLE_GENAI_API_KEY="your-key"
fly secrets set SERVICE_JWT_SECRET="your-service-secret"

# Deploy
fly deploy
```

### 3. Deploy Backend to Fly.io

```bash
cd src/backend

# Create app
fly apps create loremaster-api

# Set secrets
fly secrets set ConnectionStrings__SupabaseConnection="Host=db.xxx.supabase.co;Port=6543;Database=postgres;Username=postgres.xxx;Password=xxx;SSL Mode=Require;Trust Server Certificate=true;Pooling=true"
fly secrets set Jwt__Secret="your-jwt-secret-min-32-chars"
fly secrets set ServiceJwt__Secret="your-service-secret"
fly secrets set Cors__Origins__0="https://your-app.vercel.app"

# Deploy
fly deploy
```

### 4. Deploy Frontend to Vercel

1. Connect GitHub repository to Vercel
2. Set environment variable:
   - `VITE_API_URL` = `https://loremaster-api.fly.dev/api`
3. Deploy

## Testing

### Backend Tests

```bash
cd src/backend

# Unit tests (36+ test files)
dotnet test Loremaster.Tests.Unit

# Integration tests (8+ test files, requires Docker)
dotnet test Loremaster.Tests.Integration
```

### Frontend Tests

```bash
cd src/frontend

# Run tests
npm run test

# Run tests once
npm run test:run

# Run with coverage
npm run test:coverage
```

### Genkit Build

```bash
cd src/genkit-service
npm run build
```

## Authentication Flow

```
┌─────────┐                    ┌─────────┐                    ┌─────────┐
│ Frontend│                    │ Backend │                    │ Genkit  │
└────┬────┘                    └────┬────┘                    └────┬────┘
     │                              │                              │
     │  POST /api/auth/login        │                              │
     │  {email, password}           │                              │
     │─────────────────────────────▶│                              │
     │                              │                              │
     │  {accessToken, refreshToken} │                              │
     │◀─────────────────────────────│                              │
     │                              │                              │
     │  GET /api/entities           │                              │
     │  Authorization: Bearer token │                              │
     │─────────────────────────────▶│                              │
     │                              │                              │
     │                              │  POST /api/generate          │
     │                              │  Service JWT Token           │
     │                              │─────────────────────────────▶│
     │                              │                              │
     │                              │  AI Response                 │
     │                              │◀─────────────────────────────│
     │                              │                              │
     │  Entities + AI Content       │                              │
     │◀─────────────────────────────│                              │
```

## Key Technologies

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI framework |
| Vite | 6.x | Build tool & dev server |
| TypeScript | 5.8.x | Type safety |
| Three.js | 0.160.0 | 3D visualizations |
| cannon-es | 0.20.0 | Physics engine |
| jsPDF | 4.x | PDF generation |
| Vitest | 4.x | Testing framework |
| Testing Library | 16.x | Component testing |

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Runtime |
| ASP.NET Core | 8.0 | Web API framework |
| Entity Framework Core | 8.0.0 | ORM |
| MediatR | 12.2.0 | CQRS pattern |
| FluentValidation | 11.9.0 | Request validation |
| Serilog | 8.0.0 | Structured logging |
| Polly | - | Resilience policies |
| BCrypt.Net-Next | 4.0.3 | Password hashing |
| PdfPig | 0.1.9 | PDF parsing |
| Npgsql + pgvector | 8.0.0 | PostgreSQL + vectors |
| xUnit | 2.6.4 | Unit testing |
| Testcontainers | 3.7.0 | Integration testing |

### AI Service
| Technology | Version | Purpose |
|------------|---------|---------|
| Node.js | 20+ | Runtime |
| Express | 4.21.0 | HTTP server |
| Genkit | 1.0.0 | AI orchestration |
| @genkit-ai/google-genai | 1.28.0 | Gemini integration |
| Zod | 3.23.8 | Schema validation |
| Pino | 9.5.0 | Logging |
| Helmet | 8.0.0 | Security headers |

### AI Models
| Model | Purpose |
|-------|---------|
| Gemini 2.0 Flash | Text generation, chat, summarization |
| Gemini 2.5 Flash | Image generation |
| text-embedding-004 | Vector embeddings for RAG |

### Database
- **PostgreSQL 16** - Relational database
- **pgvector** - Vector similarity search
- **Supabase** - Managed PostgreSQL (production)

## Architecture Patterns

### Clean Architecture (.NET Backend)
```
Domain (no dependencies)
    ↑
Application (depends on Domain)
    ↑
Infrastructure (depends on Application)
    ↑
Api (composes all layers via DI)
```

### CQRS with MediatR
- Commands and Queries are separate concerns
- Each operation has its own handler
- FluentValidation for request validation
- Pipeline behaviors for cross-cutting concerns

### Security
- JWT authentication (user tokens + service-to-service tokens)
- Refresh token rotation
- Rate limiting (per-user, per-endpoint)
- CORS configuration
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- Password hashing with BCrypt
- Input sanitization

## License

MIT

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests
5. Submit a pull request
