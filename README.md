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
| Frontend | React 19 + Vite + TypeScript | 5173 | Vercel |
| Backend API | .NET 8 + EF Core | 5000 | Fly.io |
| AI Service | Node.js + Genkit + Gemini | 3000 | Fly.io |
| Database | PostgreSQL 16 + pgvector | 5432 | Supabase |

## Features

- **AI-Powered Content Generation**: Characters, locations, items, lore using Google Gemini
- **RAG (Retrieval-Augmented Generation)**: Semantic search with pgvector embeddings
- **Project Management**: Organize worlds, campaigns, and lore
- **Multi-Role Support**: Player, Master, Admin roles
- **3D Visualizations**: Three.js powered generators (characters, solar systems, vehicles)

## Quick Start

### Prerequisites

- Node.js 20+
- .NET 8 SDK
- Docker Desktop
- Google AI API Key ([Get one here](https://aistudio.google.com/apikey))

### 1. Clone and Setup

```bash
git clone <repository-url>
cd loremaster

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
loremaster/
├── src/
│   ├── frontend/                 # React + Vite frontend
│   │   ├── src/
│   │   │   ├── core/             # Hooks, context, services, types
│   │   │   ├── features/         # Feature modules (auth, generators, gallery)
│   │   │   └── shared/           # Shared UI components
│   │   ├── package.json
│   │   └── vercel.json           # Vercel deployment config
│   │
│   ├── backend/                  # .NET 8 Clean Architecture
│   │   ├── Loremaster.Api/       # ASP.NET Core Web API
│   │   ├── Loremaster.Application/  # CQRS commands/queries (MediatR)
│   │   ├── Loremaster.Domain/    # Entities, enums, exceptions
│   │   ├── Loremaster.Infrastructure/  # EF Core, repositories, services
│   │   ├── Loremaster.Shared/    # Helpers, extensions
│   │   ├── Loremaster.Tests.Unit/
│   │   ├── Loremaster.Tests.Integration/
│   │   └── Dockerfile
│   │
│   └── genkit-service/           # Node.js AI microservice
│       ├── src/
│       │   ├── index.ts          # Express server
│       │   ├── flows.ts          # Genkit AI flows
│       │   ├── schemas.ts        # Zod validation
│       │   └── middleware/       # JWT auth
│       └── Dockerfile
│
├── docker/
│   └── init.sql                  # PostgreSQL initialization
├── docker-compose.yml            # Full stack (production-like)
├── docker-compose.dev.yml        # Development (DB only)
└── .env.example                  # Environment template
```

## API Endpoints

### Backend API (Port 5000)

| Endpoint | Method | Description | Auth |
|----------|--------|-------------|------|
| `/api/auth/register` | POST | Register new user | No |
| `/api/auth/login` | POST | Login, get tokens | No |
| `/api/auth/refresh-token` | POST | Refresh access token | No |
| `/api/auth/me` | GET | Get current user | Yes |
| `/api/auth/logout` | POST | Logout, revoke refresh token | Yes |
| `/api/projects` | GET/POST | List/create projects | Yes |
| `/api/projects/{id}` | GET/PUT/DELETE | Project CRUD | Yes |
| `/api/projects/{id}/archive` | POST | Archive project | Yes |
| `/api/projects/{id}/restore` | POST | Restore project | Yes |
| `/api/entities` | GET/POST | List/create world entities | Yes |
| `/api/entities/{id}` | GET/PUT/DELETE | Entity CRUD | Yes |
| `/api/documents/ingest` | POST | Ingest document for RAG | Yes |
| `/api/documents/search` | POST | Semantic search | Yes |
| `/api/ai/generate` | POST | Generate text content | Yes |
| `/api/ai/chat` | POST | Multi-turn chat | Yes |
| `/health` | GET | Health check | No |

### Genkit Service (Port 3000)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/generate` | POST | Text generation |
| `/api/chat` | POST | Multi-turn conversation |
| `/api/summarize` | POST | Text summarization |
| `/api/embeddings` | POST | Generate vector embeddings |
| `/api/rag/generate` | POST | RAG-based generation |
| `/api/generate-image` | POST | Image generation (placeholder) |
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

# Unit tests
dotnet test Loremaster.Tests.Unit

# Integration tests (requires Docker)
dotnet test Loremaster.Tests.Integration
```

### Frontend Build

```bash
cd src/frontend
npm run build
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
     │  GET /api/projects           │                              │
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
     │  Projects + AI Content       │                              │
     │◀─────────────────────────────│                              │
```

## Key Technologies

### Frontend
- **React 19** - UI framework
- **Vite** - Build tool
- **TypeScript** - Type safety
- **Three.js** - 3D visualizations
- **TanStack Query** - Server state management

### Backend
- **.NET 8** - Runtime
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **MediatR** - CQRS pattern
- **FluentValidation** - Request validation
- **Serilog** - Structured logging
- **Polly** - Resilience policies

### AI Service
- **Genkit** - AI orchestration framework
- **Google Gemini 1.5 Flash** - LLM
- **text-embedding-004** - Embeddings model
- **Express** - HTTP server
- **Zod** - Schema validation

### Database
- **PostgreSQL 16** - Relational database
- **pgvector** - Vector similarity search
- **Supabase** - Managed PostgreSQL (production)

## License

MIT

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests
5. Submit a pull request
