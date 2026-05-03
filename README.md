# SOFIA — Sistema de Optimización Farmacéutica con IA

> Plataforma de gestión integral para farmacias construida sobre **.NET 10**,
> con arquitectura **Clean Architecture**, principios **DDD** y pipeline de calidad automatizado.

---

## 🏗️ Arquitectura

```
src/
├── SOFIA.Domain/              # Entidades, Value Objects, Result Pattern
├── SOFIA.Application/         # Casos de uso, Commands/Queries (MediatR), DTOs
├── SOFIA.Infrastructure/      # EF Core, SQL Server, configuraciones de BD
├── SOFIA.API/                 # Controllers REST, Swagger, DI root
├── SOFIA.SharedKernel/        # Contratos y abstracciones compartidas
└── SOFIA.ArchitectureTests/   # Tests de guardia de arquitectura (NetArchTest)
```

### Principios aplicados
- **Clean Architecture** — dependencias apuntan hacia adentro (Domain → Application → Infrastructure/API)
- **CQRS** con MediatR — separación estricta de Commands y Queries
- **Result Pattern** — sin excepciones de dominio, errores explícitos con `Result<T>`
- **Nullable Reference Types** — habilitado en todos los proyectos (`<Nullable>enable</Nullable>`)

---

## ⚙️ Requisitos previos

| Herramienta | Versión mínima |
|---|---|
| .NET SDK | 10.0 |
| SQL Server | 2019+ (o Docker) |
| Git | 2.x |

---

## 🚀 Configuración local (primer uso)

### 1. Clonar el repositorio

```bash
git clone <url-del-repo>
cd src
```

### 2. Restaurar dependencias

```bash
dotnet restore
```

### 3. Configurar la cadena de conexión (obligatorio)

La cadena de conexión **nunca se sube al repositorio**. Configúrala localmente con **dotnet user-secrets**:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=127.0.0.1,1433;Database=SOFIA;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" \
  --project SOFIA.API/SOFIA.API.csproj
```

> 💡 Los secrets se guardan en `%APPDATA%\Microsoft\UserSecrets\` — **fuera del repositorio**, solo en tu máquina.

### 4. Aplicar migraciones y ejecutar

```bash
# Aplicar migraciones de base de datos
dotnet ef database update --project SOFIA.Infrastructure/SOFIA.Infrastructure.csproj --startup-project SOFIA.API/SOFIA.API.csproj

# Iniciar la API
dotnet run --project SOFIA.API/SOFIA.API.csproj
```

La API estará disponible en:
- **Swagger UI**: `https://localhost:{puerto}/swagger`
- **OpenAPI spec**: `https://localhost:{puerto}/openapi/v1.json`

---

## 🔒 Seguridad y secretos

Este repositorio aplica una política de **cero secretos en el historial de Git**.

### Reglas
- ❌ Prohibido escribir contraseñas, API keys o tokens en cualquier archivo del repo
- ✅ **Desarrollo**: usar `dotnet user-secrets`
- ✅ **Producción**: usar variables de entorno o Azure Key Vault

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

## 🧪 Tests

```bash
# Tests de arquitectura
dotnet test SOFIA.ArchitectureTests/SOFIA.ArchitectureTests.csproj -c Release
```

---

## 📦 Stack tecnológico

| Capa | Tecnología |
|---|---|
| Runtime | .NET 10, C# 14 |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server (UNIQUEIDENTIFIER / NEWID()) |
| Mediador | MediatR 12 |
| Validación | FluentValidation |
| Documentación | Scalar / OpenAPI nativo |
| Calidad | Husky.Net, dotnet-format, NetArchTest |
| Secretos | dotnet user-secrets (dev) / Azure Key Vault (prod) |
