# HefestAI

Plataforma de construcción de mundos y gestión de lore impulsada por IA para masters y jugadores de juegos de rol de mesa.

## Resumen de Arquitectura

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│    Frontend     │────▶│   Backend .NET   │────▶│  Servicio Genkit│
│   (React/Vite)  │     │   (ASP.NET Core) │     │   (Node/Express)│
│                 │     │                  │     │                 │
│    Vercel       │     │    Railway       │     │    Railway      │
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

### Componentes

| Componente | Tecnología | Puerto (Dev) | Despliegue |
|------------|------------|--------------|------------|
| Frontend | React 19 + Vite 6 + TypeScript | 5173 | Vercel |
| API Backend | .NET 8 + EF Core 8 | 5000 | Railway |
| Servicio IA | Node.js 20 + Genkit 1.0 + Gemini | 3000 | Railway |
| Base de datos | PostgreSQL 16 + pgvector | 5432 | Supabase |

## Características

### Características Principales

- **Generación de Contenido con IA**: Personajes, PNJs, enemigos, localizaciones, objetos, misiones, encuentros usando Google Gemini 2.0 Flash
- **Generación de Imágenes**: Imágenes generadas por IA con Gemini 2.5 Flash con opciones de estilo (realista, artístico, anime, fantasía, boceto)
- **RAG (Generación Aumentada por Recuperación)**: Búsqueda semántica con embeddings pgvector para generación consciente del contexto
- **Gestión de Campañas**: Crea y gestiona campañas con códigos de unión para jugadores
- **Soporte Multi-rol**: Roles Jugador, Master, Admin con control de acceso basado en roles
- **Sesión Persistente**: Refresh token proactivo para evitar pérdidas de sesión

### Generadores (7 Tipos dependientes de plantillas)

| Generador | Descripción | Rol Requerido |
|-----------|-------------|---------------|
| Personaje | Generación de personajes jugador | Player+ |
| PNJ | Generación de personajes no jugadores | Master+ |
| Enemigo | Generación de antagonistas y monstruos | Master+ |
| Vehículo | Naves, vehículos terrestres, monturas | Master+ |
| Misión | Generación de misiones y encargos | Master+ |
| Encuentro | Diseño de encuentros de combate y sociales | Master+ |
| Sistema Solar (Labs) | Sistemas planetarios con visualización orbital animada | Master+ |

### Funcionalidades Adicionales

- **Visualización Orbital**: Sistemas solares con planetas animados
- **Exportación PDF**: Generación y exportación de fichas de personaje con jsPDF
- **Galería de Entidades**: Navega, edita y gestiona todas las entidades de lore creadas
- **Importación de Documentos**: Importa PDFs/manuales para contexto RAG
- **Compresión de Imágenes**: Imágenes subidas comprimidas automáticamente a WebP
- **Consulta de Reglas**: Búsqueda de reglas potenciada por IA
- **Tirador de Dados**: Utilidad de tirada de dados integrada, visualización 3D con Three.js
- **Configuración Campaña**: Configuración y gestión de campaña
- **Sistemas de Juego**: Gestión de sistemas de juego compatibles

## Inicio Rápido

### Requisitos Previos

- Node.js 20+
- .NET 8 SDK
- Docker Desktop
- Google AI API Key ([Consíguela aquí](https://aistudio.google.com/apikey))

### 1. Clonar y Configurar

```bash
git clone <url-del-repositorio>
cd hefestai

# Copiar archivo de entorno
cp .env.example .env
# Editar .env y añadir tu GOOGLE_GENAI_API_KEY
```

### 2. Iniciar Base de Datos (Docker)

```bash
docker-compose -f docker-compose.dev.yml up -d
```

### 3. Ejecutar Backend

```bash
cd src/backend

# Restaurar paquetes
dotnet restore

# Ejecutar migraciones
dotnet ef database update --project Loremaster.Infrastructure --startup-project Loremaster.Api

# Iniciar la API
dotnet run --project Loremaster.Api
```

### 4. Ejecutar Servicio Genkit

```bash
cd src/genkit-service

# Instalar dependencias
npm install

# Iniciar en modo desarrollo
npm run dev
```

### 5. Ejecutar Frontend

```bash
cd src/frontend

# Instalar dependencias
npm install

# Iniciar servidor de desarrollo
npm run dev
```

Abre http://localhost:5173 en tu navegador.

## Estructura del Proyecto

```
hefestai/
├── src/
│   ├── frontend/                    # React 19 + Vite + TypeScript
│   │   ├── core/                    # Servicios principales y hooks
│   │   │   ├── context/             # AuthContext, CampaignContext
│   │   │   ├── hooks/               # useApi, useTransition, useCharacterSheetPdf
│   │   │   ├── services/            # Clientes API, generación PDF, almacenamiento
│   │   │   ├── types/               # Definiciones de tipos TypeScript
│   │   │   └── utils/               # Utilidades de seguridad, validación
│   │   ├── features/                # Módulos de funcionalidades
│   │   │   ├── auth/                # Páginas Login, Signup
│   │   │   ├── gallery/             # Galería y gestión de entidades
│   │   │   └── generators/          # 10 páginas de generadores
│   │   ├── shared/                  # Componentes compartidos
│   │   │   ├── components/          # UI, layout, feedback, modals, visualization
│   │   │   └── guards/              # AuthGuard, RoleGuard
│   │   └── components/              # DiceRoller, RuleQuery
│   │
│   ├── backend/                     # .NET 8 Clean Architecture
│   │   ├── Loremaster.Api/          # ASP.NET Core Web API
│   │   │   └── Controllers/         # Endpoints REST
│   │   ├── Loremaster.Application/  # Comandos/consultas CQRS (MediatR)
│   │   │   └── Features/            # Organización por funcionalidad
│   │   ├── Loremaster.Domain/       # Capa de dominio
│   │   │   ├── Entities/            # Campaign, User, LoreEntity, etc.
│   │   │   ├── Enums/               # UserRole, CampaignRole, etc.
│   │   │   └── ValueObjects/        # FieldDefinition, EntityGenerationConfig
│   │   ├── Loremaster.Infrastructure/  # Acceso datos y servicios externos
│   │   │   ├── Persistence/         # EF Core, repositorios, migraciones
│   │   │   ├── Identity/            # JWT, hash de contraseñas
│   │   │   └── Services/            # GenkitAiService, TextChunkingService
│   │   ├── Loremaster.Shared/       # Helpers, extensiones
│   │   ├── Loremaster.Tests.Unit/   # xUnit + FluentAssertions + Moq
│   │   └── Loremaster.Tests.Integration/  # Testcontainers tests integración
│   │
│   └── genkit-service/              # Microservicio IA Node.js
│       └── src/
│           ├── index.ts             # Servidor Express
│           ├── flows.ts             # Flujos AI Genkit (6 flujos)
│           ├── schemas.ts           # Validación Zod
│           └── middleware/          # Auth JWT
│
├── docker/
│   └── init.sql                     # Inicialización PostgreSQL
├── docker-compose.yml               # Stack completo (producción)
├── docker-compose.dev.yml           # Desarrollo (solo BD)
└── .env.example                     # Plantilla de entorno
```

## Modelo de Dominio

### Entidades Principales

| Entidad | Descripción |
|---------|-------------|
| **User** | Usuarios con roles (Player, Master, Admin) y auth JWT |
| **Campaign** | Campañas de juego con códigos de unión y sistema de juego |
| **CampaignMember** | Participación de usuario con roles específicos de campaña |
| **GameSystem** | Sistemas RPG soportados |
| **LoreEntity** | Entidad polimórfica para todo el contenido de lore |
| **EntityTemplate** | Plantillas reutilizables con definiciones de campos |
| **Document** | Documentos ingeridos para RAG |
| **RagSource** | Fuentes de documentos RAG (manuales, suplementos) |
| **GenerationRequest** | Seguimiento de peticiones de generación IA |
| **GenerationResult** | Salidas de generación IA con trazabilidad de fuentes |

### Categorías de Entidades

- **Personajes** - Personajes jugador, PNJs, enemigos
- **Planetas** - Localizaciones, sistemas solares
- **Vehículos** - Naves, vehículos, monturas
- **Eventos** - Misiones, encuentros

### Roles de Usuario

| Rol | Permisos |
|-----|----------|
| **Player** | Crear personajes, ver entidades permitidas, unirse a campañas |
| **Master** | Todos los permisos de jugador + crear campañas, todos los generadores, gestionar entidades |
| **Admin** | Acceso completo al sistema, gestión de usuarios |

## Endpoints API

### API Backend (Puerto 5000)

| Endpoint | Método | Descripción | Auth |
|----------|--------|-------------|------|
| `/api/auth/register` | POST | Registrar nuevo usuario | No |
| `/api/auth/login` | POST | Login, obtener tokens | No |
| `/api/auth/refresh-token` | POST | Refrescar access token | No |
| `/api/auth/me` | GET | Obtener usuario actual | Sí |
| `/api/auth/logout` | POST | Logout, revocar refresh token | Sí |
| `/api/campaigns` | GET/POST | Listar/crear campañas | Sí |
| `/api/campaigns/{id}` | GET/PUT/DELETE | CRUD de campaña | Sí |
| `/api/campaigns/{id}/join` | POST | Unirse a campaña con código | Sí |
| `/api/entities` | GET/POST | Listar/crear entidades de lore | Sí |
| `/api/entities/{id}` | GET/PUT/DELETE | CRUD de entidad | Sí |
| `/api/entity-templates` | GET/POST | Listar/crear plantillas | Sí |
| `/api/entity-templates/{id}` | GET/PUT/DELETE | CRUD de plantilla | Sí |
| `/api/game-systems` | GET/POST | Listar/crear sistemas de juego | Sí |
| `/api/documents/ingest` | POST | Ingerir documento para RAG | Sí |
| `/api/documents/search` | POST | Búsqueda semántica | Sí |
| `/api/ai/generate` | POST | Generar contenido de texto | Sí |
| `/api/ai/chat` | POST | Chat multi-turno | Sí |
| `/health` | GET | Health check | No |
| `/health/ready` | GET | Readiness probe | No |
| `/health/live` | GET | Liveness probe | No |

### Servicio Genkit (Puerto 3000)

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/generate` | POST | Generación de texto (Gemini 2.0 Flash) |
| `/api/chat` | POST | Conversación multi-turno |
| `/api/summarize` | POST | Resumen de texto (conciso/detallado/puntos) |
| `/api/embeddings` | POST | Embeddings vectoriales (gemini-embedding-001) |
| `/api/rag/generate` | POST | Generación basada en RAG |
| `/api/generate-image` | POST | Generación de imágenes (Gemini 2.5 Flash) |
| `/health` | GET | Health check |

## Configuración de Entorno

### Desarrollo (.env)

```bash
# Base de datos (Docker local)
DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=loremaster;Username=postgres;Password=postgres

# JWT (Claves de desarrollo - NO usar en producción)
JWT_SECRET=DEVELOPMENT_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32CHARS
SERVICE_JWT_SECRET=DEV_SERVICE_SECRET_KEY_DO_NOT_USE_IN_PRODUCTION_32

# Google AI
GOOGLE_GENAI_API_KEY=tu_api_key_aqui

# Servicios
GENKIT_SERVICE_URL=http://localhost:3000
VITE_API_URL=http://localhost:5000/api
```

### Producción (Railway Variables)

```bash
# Backend (loremaster-api)
ConnectionStrings__SupabaseConnection=Host=db.xxx.supabase.co;Port=6543;Database=postgres;Username=postgres.xxx;Password=xxx;SSL Mode=Require;Trust Server Certificate=true;Pooling=true
Jwt__Secret=tu-secreto-jwt-minimo-32-caracteres
Jwt__AccessTokenExpirationMinutes=30
ServiceJwt__Secret=tu-secreto-servicio
Cors__Origins__0=https://tu-app.vercel.app
GenkitService__ProductionUrl=https://tu-genkit.railway.app

# Genkit (loremaster-genkit)
GOOGLE_GENAI_API_KEY=tu-api-key
SERVICE_JWT_SECRET=tu-secreto-servicio
PORT=3000
```

## Despliegue

### 1. Configuración de Base de Datos Supabase

1. Crea un proyecto en [supabase.com](https://supabase.com)
2. Habilita la extensión pgvector:
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```
3. Obtén la cadena de conexión con pooling (Settings > Database > Connection string > URI, puerto 6543)

### 2. Desplegar Genkit en Railway

```bash
cd src/genkit-service

# Crear nuevo proyecto en Railway (desde dashboard o CLI)
railway init

# Añadir variables de entorno
railway variables set GOOGLE_GENAI_API_KEY=tu-api-key
railway variables set SERVICE_JWT_SECRET=tu-secreto-servicio
railway variables set PORT=3000

# Desplegar
railway up
```

### 3. Desplegar Backend en Railway

```bash
cd src/backend

# Crear nuevo proyecto en Railway
railway init

# Añadir variables de entorno
railway variables set ConnectionStrings__SupabaseConnection="Host=db.xxx.supabase.co;Port=6543;Database=postgres;Username=postgres.xxx;Password=xxx;SSL Mode=Require;Trust Server Certificate=true;Pooling=true"
railway variables set Jwt__Secret=tu-secreto-jwt-minimo-32-caracteres
railway variables set Jwt__AccessTokenExpirationMinutes=30
railway variables set ServiceJwt__Secret=tu-secreto-servicio
railway variables set Cors__Origins__0=https://tu-app.vercel.app
railway variables set GenkitService__ProductionUrl=https://tu-genkit.railway.app

# Desplegar
railway up
```

### 4. Desplegar Frontend en Vercel

1. Conecta el repositorio de GitHub a Vercel
2. Configura la variable de entorno:
   - `VITE_API_URL` = `https://tu-backend.railway.app/api`
3. Despliega

## Testing

### Tests Backend

```bash
cd src/backend

# Tests unitarios (36+ archivos de test)
dotnet test Loremaster.Tests.Unit

# Tests de integración (8+ archivos de test, requiere Docker)
dotnet test Loremaster.Tests.Integration
```

### Tests Frontend

```bash
cd src/frontend

# Ejecutar tests
npm run test

# Ejecutar tests una vez
npm run test:run

# Ejecutar con cobertura
npm run test:coverage
```

### Build Genkit

```bash
cd src/genkit-service
npm run build
```

## Flujo de Autenticación

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
     │                              │  Respuesta IA                │
     │                              │◀─────────────────────────────│
     │                              │                              │
     │  Entidades + Contenido IA    │                              │
     │◀─────────────────────────────│                              │
```

### Gestión de Sesión

- **Access Token**: Válido 30 minutos (producción)
- **Refresh Token**: Válido 7 días (producción)
- **Refresh Proactivo**: El frontend refresca automáticamente el token antes de que expire
- **Clock Skew**: Tolerancia de 30 segundos para desfases de reloj

## Tecnologías Principales

### Frontend

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| React | 19.x | Framework UI |
| Vite | 6.x | Build tool y servidor dev |
| TypeScript | 5.8.x | Seguridad de tipos |
| Three.js | 0.160.0 | Visualizaciones 3D |
| cannon-es | 0.20.0 | Motor de física |
| jsPDF | 4.x | Generación PDF |
| Vitest | 4.x | Framework de testing |
| Testing Library | 16.x | Testing de componentes |
| Tailwind CSS | 4.x | Framework CSS |

### Backend

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| .NET | 8.0 | Runtime |
| ASP.NET Core | 8.0 | Framework Web API |
| Entity Framework Core | 8.0.0 | ORM |
| MediatR | 12.2.0 | Patrón CQRS |
| FluentValidation | 11.9.0 | Validación de peticiones |
| Serilog | 8.0.0 | Logging estructurado |
| Polly | - | Políticas de resiliencia |
| BCrypt.Net-Next | 4.0.3 | Hash de contraseñas |
| PdfPig | 0.1.9 | Parsing PDF |
| Npgsql + pgvector | 8.0.0 | PostgreSQL + vectores |
| xUnit | 2.6.4 | Testing unitario |
| Testcontainers | 3.7.0 | Tests de integración |

### Servicio IA

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| Node.js | 20+ | Runtime |
| Express | 4.21.0 | Servidor HTTP |
| Genkit | 1.0.0 | Orquestación IA |
| @genkit-ai/google-genai | 1.28.0 | Integración Gemini |
| Zod | 3.23.8 | Validación de esquemas |
| Pino | 9.5.0 | Logging |
| Helmet | 8.0.0 | Cabeceras de seguridad |

### Modelos IA

| Modelo | Propósito |
|--------|-----------|
| Gemini 2.0 Flash | Generación de texto, chat, resumen |
| Gemini 2.5 Flash | Generación de imágenes |
| gemini-embedding-001 | Embeddings vectoriales para RAG (3072 dimensiones) |

### Base de Datos

- **PostgreSQL 16** - Base de datos relacional
- **pgvector** - Búsqueda de similitud vectorial
- **Supabase** - PostgreSQL gestionado (producción)

## Patrones de Arquitectura

### Clean Architecture (.NET Backend)

```
Domain (sin dependencias)
    ↑
Application (depende de Domain)
    ↑
Infrastructure (depende de Application)
    ↑
Api (compone todas las capas vía DI)
```

### CQRS con MediatR

- Comandos y Consultas son responsabilidades separadas
- Cada operación tiene su propio handler
- FluentValidation para validación de peticiones
- Pipeline behaviors para concerns transversales

### Seguridad

- Autenticación JWT (tokens de usuario + tokens servicio-a-servicio)
- Rotación de refresh token
- Rate limiting (por usuario, por endpoint)
- Configuración CORS
- Cabeceras de seguridad (X-Content-Type-Options, X-Frame-Options, etc.)
- Hash de contraseñas con BCrypt
- Sanitización de entrada

## Licencia

MIT

## Contribuir

1. Haz fork del repositorio
2. Crea una rama de funcionalidad
3. Realiza tus cambios
4. Ejecuta los tests
5. Envía un pull request
