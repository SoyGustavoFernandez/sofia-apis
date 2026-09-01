# SOFIA — Sistema de Optimización Farmacéutica con IA

> Plataforma de gestión integral para farmacias peruanas construida sobre **.NET 10**,
> con arquitectura **Clean Architecture**, principios **DDD** y pipeline de calidad automatizado.

---

## ✅ Estado actual del backend

| Verificación | Resultado |
|---|---|
| `dotnet build` (Release) | ✅ 0 errores, 0 warnings |
| Unit Tests (46 tests) | ✅ 46/46 passed |
| Architecture Tests (2 tests) | ✅ 2/2 passed |
| Integration Tests | ⚠️ Requieren Docker Desktop corriendo |

> **Nota SUNAT:** La integración con SUNAT es simulada (más adelante se implementará). Los campos de hash, URL y CDR en el comprobante son placeholders. La integración real requiere un OSE/PSE homologado y firma digital con certificado.

---

## 🏗️ Arquitectura

```
src/
├── SOFIA.Domain/              # Entidades, Value Objects, Result Pattern
├── SOFIA.Application/         # Casos de uso, Commands/Queries (MediatR), DTOs
├── SOFIA.Infrastructure/      # EF Core, SQL Server, configuraciones de BD
├── SOFIA.API/                 # Controllers REST, Swagger, DI root
├── SOFIA.SharedKernel/        # Contratos y abstracciones compartidas
├── SOFIA.DbUp/                # Migraciones SQL versionadas
├── SOFIA.PresidioAPI/         # Microservicio Python — anonimización PII de recetas
├── SOFIA.UnitTests/           # Tests unitarios con xUnit + Moq
├── SOFIA.IntegrationTests/    # Tests de integración con Testcontainers (SQL Server real)
└── SOFIA.ArchitectureTests/   # Tests de guardia de arquitectura (NetArchTest)
```

### Principios aplicados
- **Clean Architecture** — dependencias apuntan hacia adentro (Domain → Application → Infrastructure/API)
- **CQRS** con MediatR — separación estricta de Commands y Queries
- **Result Pattern** — sin excepciones de dominio, errores explícitos con `Result<T>`
- **Outbox Pattern** — eventos de dominio con consistencia eventual
- **Transaction Behavior** — todos los Commands envueltos automáticamente en transacciones SQL
- **Nullable Reference Types** — habilitado en todos los proyectos (`<Nullable>enable</Nullable>`)

---

## ⚙️ Requisitos previos

| Herramienta | Versión mínima |
|---|---|
| .NET SDK | 10.0 |
| SQL Server | 2019+ (o Docker) |
| Docker Desktop | Para integration tests y docker-compose |
| Git | 2.x |

---

## 🐳 Levantar con Docker (recomendado)

La forma más fácil de correr todo el sistema:

```bash
# 1. Copiar y completar las variables de entorno
cp .env.example .env
# Editar .env y completar: SA_PASSWORD, JWT_SECRET_KEY, GEMINI_API_KEY

# 2. Levantar todos los servicios
docker-compose up -d
```

Servicios disponibles tras el arranque:

| Servicio | URL |
|---|---|
| API REST | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Presidio (PII) | http://localhost:8001 |
| SQL Server | localhost:1434 (user: `sa`) |

> El contenedor `sofia-dbup` aplica las migraciones automáticamente al arrancar. No hace falta correr nada extra.

---

## 🚀 Configuración local (sin Docker)

### 1. Clonar el repositorio

```bash
git clone <url-del-repo>
cd backend
```

### 2. Restaurar dependencias

```bash
dotnet restore
```

### 3. Configurar secretos locales (obligatorio)

La cadena de conexión y las API keys **nunca se suben al repositorio**. Configúralas con **dotnet user-secrets**:

```bash
# Base de datos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=127.0.0.1,1433;Database=SofiaDb;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;" \
  --project src/SOFIA.API/SOFIA.API.csproj

# JWT
dotnet user-secrets set "Jwt:SecretKey" "tu_secret_key_minimo_32_caracteres" \
  --project src/SOFIA.API/SOFIA.API.csproj

# Presidio (anonimización PII)
dotnet user-secrets set "PresidioApi:BaseUrl" "http://localhost:8000" \
  --project src/SOFIA.API/SOFIA.API.csproj

# Gemini (OCR de recetas)
dotnet user-secrets set "GeminiApi:ApiKey" "tu_api_key" \
  --project src/SOFIA.API/SOFIA.API.csproj
```

> 💡 Los secrets se guardan en `%APPDATA%\Microsoft\UserSecrets\` — **fuera del repositorio**, solo en tu máquina.

### 4. Aplicar migraciones y ejecutar

```bash
# Aplicar schema
dotnet run --project src/SOFIA.DbUp/SOFIA.DbUp.csproj

# Iniciar la API
dotnet run --project src/SOFIA.API/SOFIA.API.csproj
```

La API estará disponible en:
- **Swagger UI**: `https://localhost:{puerto}/swagger`
- **Health check**: `https://localhost:{puerto}/health`

---

## 🧪 Tests

```bash
# Unit Tests
dotnet test src/SOFIA.UnitTests/SOFIA.UnitTests.csproj -c Release

# Architecture Tests
dotnet test src/SOFIA.ArchitectureTests/SOFIA.ArchitectureTests.csproj -c Release

# Integration Tests (requiere Docker Desktop corriendo)
$env:SOFIA_TEST_DB_PASSWORD="TuPassword123!"
dotnet test src/SOFIA.IntegrationTests/SOFIA.IntegrationTests.csproj -c Release
```

---

## 🔒 Seguridad y secretos

Este repositorio aplica una política de **cero secretos en el historial de Git**.

### Reglas
- ❌ Prohibido escribir contraseñas, API keys o tokens en cualquier archivo del repo
- ✅ **Desarrollo**: usar `dotnet user-secrets`
- ✅ **Docker**: variables de entorno en `.env` (nunca commiteado)
- ✅ **Producción**: variables de entorno del host / Azure Key Vault

### Pre-commit hook automático

El hook `.husky/scripts/detect-secrets.sh` escanea cada commit en busca de patrones sospechosos:

```
Password=...   ApiKey=...   secret=...   token=...   Bearer ...   -----BEGIN PRIVATE KEY-----
```

Si detecta un secreto, **bloquea el commit** con un mensaje explicativo:

```
[Secret Scanner] ❌ POTENTIAL SECRET DETECTED!
  File   : SOFIA.API/appsettings.json
  Pattern: Password=[A-Za-z0-9...]{4,}
  Match  : "DefaultConnection": "Password=MiPassword123!;..."

[Secret Scanner] 🚫 COMMIT BLOCKED — Remove all secrets before committing.
```

---

## 🛡️ Guardianes de calidad (pre-commit)

Cada `git commit` ejecuta automáticamente, en orden:

| # | Tarea | Descripción |
|---|---|---|
| 1 | `detect-secrets` | Escanea archivos staged en busca de credenciales hardcodeadas |
| 2 | `dotnet-format` | Aplica formato estándar a archivos `.cs` y `.csproj` staged |
| 3 | `re-stage` | Re-stagea archivos formateados automáticamente |
| 4 | `architecture-tests` | Corre los tests de `SOFIA.ArchitectureTests` (dependencias, naming, layers) |

El mensaje de commit también es validado (`commit-msg` hook):

```
<tipo>(<alcance>): <descripción en imperativo>

Tipos válidos : feat, fix, docs, style, refactor, perf, test, chore
Alcances válidos: domain, app, infra, api, ia, db
```

**Ejemplo**: `feat(api): agregar endpoint de sucursales con paginación`

---

## 📦 Stack tecnológico

| Capa | Tecnología |
|---|---|
| Runtime | .NET 10 LTS, C# 13 |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server 2022 |
| Mediador / CQRS | MediatR 14 |
| Validación | FluentValidation |
| Autenticación | JWT Bearer + SecurityStamp |
| Logging | Serilog (CompactJSON, rotación diaria) |
| Rate Limiting | ASP.NET Core built-in |
| Documentación | Swagger / OpenAPI 3 |
| IA — OCR recetas | Google Gemini 1.5 |
| Privacidad PII | Presidio (Python FastAPI) |
| Contenedores | Docker + Docker Compose |
| Migraciones | DbUp |
| Tests unitarios | xUnit + Moq + MockQueryable + FluentAssertions |
| Tests integración | Testcontainers.MsSql |
| Tests arquitectura | NetArchTest |
| Calidad / CI local | Husky.Net, dotnet-format |
