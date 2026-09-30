# SOFIA — Sistema Optimizado Farmacéutico con Inteligencia Artificial

> API REST (backend) de SOFIA, plataforma SaaS de gestión para farmacias y cadenas de boticas en Perú.
> Multiempresa (cada **Empresa** es un tenant aislado) y multisucursal (**Sucursal**).
> La interfaz web vive en el repositorio [sofia-web-spa](../sofia-web-spa).

---

## 📋 Qué hace el sistema

SOFIA cubre la operación diaria de una farmacia: catálogos maestros, inventario por lotes y sucursal, punto de venta con caja, recetas médicas (con lectura asistida por IA), devoluciones con nota de crédito, series fiscales SUNAT, cumplimiento DIGEMID y seguridad por roles y permisos.

Toda la información pertenece a una empresa: un usuario solo ve y modifica datos de su propia empresa. Dentro de la empresa, el acceso a datos de sucursal depende de las sucursales permitidas al usuario (ver [Roles, permisos y sucursales](#-roles-permisos-y-sucursales)).

---

## 🧩 Módulos y reglas de negocio

### Empresas (tenant) y suscripción

- **Autorregistro público** (`POST /empresas/registrar`): crea en un solo paso la empresa, su sede principal, el empleado administrador, su cuenta y un rol `Admin` propio de esa empresa. El usuario queda con sesión iniciada.
- La empresa nace en **periodo de prueba de 30 días** (`TrialActivo`). Estados: `TrialActivo`, `Activo`, `Suspendido`, `Cancelado`.
- **Suscripción vigente**: solo se puede iniciar sesión, renovar sesión o usar la API si la empresa está `Activo`, o `TrialActivo` con la prueba sin vencer. Si deja de estar vigente, los accesos ya abiertos se cortan.
- El RUC es único en todo el sistema.
- Una empresa solo puede **ver y editar su propia ficha**. No hay carga masiva ni eliminación de empresas desde un tenant.

### Autenticación y sesión

| Regla | Detalle |
|---|---|
| Login | Usuario + contraseña. Devuelve un access token JWT de **15 minutos** y un refresh token de **7 días** en una cookie `HttpOnly`, `Secure`. |
| Renovación | `POST /auth/refresh` con la cookie. El refresh token **rota** en cada uso; reutilizar uno ya rotado se trata como robo y cierra **todas** las sesiones de la cuenta. |
| Anti-CSRF | `refresh` y `logout` exigen la cabecera `X-SOFIA-CSRF`. |
| Logout | Revoca la sesión (refresh token) **aunque el access token ya haya expirado**. |
| Bloqueo | **5 intentos fallidos** bloquean la cuenta **15 minutos**. La respuesta es la misma que con credenciales inválidas (no revela si la cuenta existe). |
| Cambio de contraseña | `POST /auth/change-password` (contraseña actual + nueva). Cierra todas las sesiones de la cuenta. |
| Cambio obligatorio | Las cuentas creadas por un administrador nacen con cambio de contraseña obligatorio: hasta cambiarla, la API solo atiende los endpoints necesarios para hacerlo. |
| Recuperación | `forgot-password` (por nombre de usuario) envía al **correo del empleado** un enlace `{App:FrontendBaseUrl}/auth/reset-password#token=…&usuario=…` válido 1 hora y de un solo uso; el token va en el fragmento (`#`), que el navegador nunca envía al servidor. `reset-password` lo canjea. La respuesta es siempre la misma: no se envía nada si la cuenta no existe, está inactiva, la empresa no está vigente o el empleado no tiene correo (en ese caso un administrador fuerza el cambio de contraseña). El correo del empleado es opcional y único por empresa. |
| Límite de peticiones | Login, cambio y recuperación de contraseña: 10/min por cliente. Autorregistro: 5/hora. Análisis de recetas con IA: 10/min por empresa. |

### Roles, permisos y sucursales

- **RBAC por módulo y acción** (p. ej. `Ventas:Anular`, `Inventarios:Actualizar`). Cada endpoint declara su permiso; el catálogo de permisos disponible se obtiene de `GET /roles/permissions/catalog`.
- El rol **`Admin`** tiene acceso total sin evaluar permisos. Solo un Admin puede asignar/quitar el rol Admin o editar/eliminar cuentas administradoras.
- Los cambios de permisos o roles **se aplican de inmediato** (se invalida la caché de permisos).
- **Sucursales accesibles** de un usuario = su **sucursal base** (la del empleado) + sucursales asignadas a su **cuenta** + sucursales asignadas a sus **roles**. El Admin ve todas las de su empresa.
- **Punto de venta, caja y ventas operan siempre sobre la sucursal base** del usuario.
- Cuentas de usuario: alta (con cambio de contraseña obligatorio), activación/desactivación, forzar cambio de contraseña, desbloqueo (reinicio de intentos fallidos), asignación de roles y de sucursales adicionales.

### Catálogos maestros

Los maestros tienen búsqueda paginada con filtros, alta/edición/baja lógica y **exportación a Excel**. Un registro referenciado por otros no se puede eliminar (p. ej. un laboratorio o una unidad de medida con medicamentos asociados).

**Carga masiva** (plantilla descargable → previsualización → confirmación): solo archivos `.xlsx`, máximo **5 MB** y **5 000 filas**. Disponible en:

| Maestro | Carga masiva |
|---|---|
| Sucursales | Sí |
| Roles | Sí |
| Unidades de Medida | Sí |
| Jerarquías de Unidades de Medida | Sí |
| Laboratorios | Sí |
| Ingredientes Activos | Sí |
| Medicamentos | Sí |
| Proveedores | Sí |
| Aseguradoras (Seguros) | Sí |
| Pacientes | Sí |
| Profesionales de Salud | Sí |
| Catálogo DIGEMID | Sí |
| Empresas, Empleados, Presentaciones de Venta, Formulaciones Clínicas, Series Fiscales | No |

Notas de negocio de los catálogos:

- **Medicamentos**: código nacional, laboratorio, unidad base, **condición de venta** (`VentaLibreOTC`, `RecetaSimple`, `RecetaRetenida`, `Estupefaciente`) y **precio de venta base**.
- **Jerarquías de UdM** y **Presentaciones de Venta**: permiten vender por presentación (p. ej. "Caja x10") con su propio precio; el stock siempre se descuenta en unidades base.
- **Formulaciones Clínicas**: composición de un medicamento (ingrediente activo + concentración + unidad).
- **Proveedores**: además del maestro, registran **precios de proveedor con vigencia**; el costo vigente se guarda en cada línea de venta.

### Inventario

- **Lotes**: número de lote del fabricante, fechas de fabricación y caducidad por producto.
- **Stock por sucursal**: cantidad física por lote y sucursal. Registro de ingreso y **ajuste de stock** (el ajuste queda auditado). Las consultas se limitan a las sucursales accesibles del usuario.
- **Transferencias entre sucursales**: `Iniciada` → **despachar** (descuenta stock en origen, `En_Transito`) → **recibir** en destino (`Completada` o `Recibida_Parcial` si llega menos de lo enviado). Se puede **cancelar** antes de recibir (devuelve el stock al origen si ya había salido). Despachar/cancelar solo desde la sucursal de origen; recibir solo desde la de destino.
- **DIGEMID**: aislamiento de lotes en **cuarentena** (`Retenido`, `Liberado`, `Destruido`, `Devuelto`) y **actas de destrucción** con regente responsable. Un lote en cuarentena `Retenido` no se puede vender.
- **Órdenes magistrales** (preparados): iniciar la orden consume insumos del stock de la propia sucursal; completarla genera un lote nuevo con el producto elaborado.

### Punto de venta (POS) y caja

- **Sesión de caja por cajero**: cada cajero abre su propia caja en su sucursal base (una sola abierta a la vez) con un monto de apertura.
- **Cierre / arqueo**: solo el cajero dueño o un Admin puede cerrarla. El efectivo esperado lo calcula el servidor:
  `apertura + efectivo cobrado − vuelto entregado − reembolsos en efectivo`.
  Si el declarado coincide queda `Cuadrada`; si no, `Cerrada` con la diferencia registrada.
- **Venta**: requiere la caja abierta del propio cajero. Los **precios salen del catálogo** (precio base o precio de la presentación), nunca del cliente. Se valida stock del lote en la sucursal.
- **Pagos**: pagos divididos (`Efectivo`, `Yape/Plin`, `Tarjeta`, `Transferencia`). Solo hay vuelto si hay efectivo, y el **vuelto no puede superar el efectivo recibido**.
- **Ventas pendientes** (en espera): se pueden guardar sin pago, editar su detalle y completarlas después; el cobro entra en la caja de quien la completa.
- **Anulación**: requiere el permiso `Ventas:Anular`, un motivo y que la venta sea de tu sucursal; devuelve el stock. **Se bloquea si la venta tiene devoluciones.**
- **Receta obligatoria según condición de venta**: los productos con receta exigen una receta del **mismo paciente** de la venta.
  - `RecetaRetenida` y `Estupefaciente`: una sola dispensación.
  - `RecetaSimple`: `1 + RepeticionesMax` dispensaciones.
  - Las ventas anuladas devuelven su dispensación.
- **Lotes vencidos**: bloqueados en la venta (y ocultos en el selector del POS). **Lotes en cuarentena**: bloqueados.
- **Seguros**: la API admite registrar una aseguradora y un monto cubierto (limitado al total de la venta), que genera un reclamo al seguro. La interfaz del POS todavía no expone esta opción.

### Comprobantes y series fiscales (SUNAT)

- **Series Fiscales** (maestro por sucursal): tipos `Boleta`, `Factura`, `Nota de Crédito` y `Proforma`. Prefijo de 4 caracteres (Boleta empieza con `B`, Factura con `F`, Nota de Crédito con `B` o `F`).
- **Una sola serie activa por sucursal y tipo**; el correlativo se incrementa de forma atómica.
- Una serie que ya emitió comprobantes **queda bloqueada**: solo puede cambiar su estado y no se puede eliminar.
- Toda venta cobrada emite una **boleta**; la venta se rechaza si la sucursal **no tiene serie de Boleta activa**.
- **IGV**: los precios incluyen IGV; base gravada = `total / 1.18` y el IGV es la diferencia.

> **Nota SUNAT:** la emisión electrónica es **simulada**. El hash, las rutas XML/CDR y la URL de verificación son valores de ejemplo. La integración real requiere un OSE/PSE homologado y firma con certificado digital.

### Devoluciones

- Solo sobre ventas **`Completada`** de **tu sucursal**.
- La cantidad devuelta por línea está **limitada a lo vendido** menos lo ya devuelto (se admiten devoluciones parciales).
- Se emite una **nota de crédito solo por las líneas devueltas**, con la serie de Nota de Crédito activa de la sucursal.
- El **reembolso sale de la caja abierta del cajero** que procesa la devolución (entra en su arqueo). La parte cubierta por un seguro no se reembolsa al cliente.
- Destino de lo devuelto: `Reingreso_Venta` (vuelve al stock) o `Cuarentena_DIGEMID`.

### Delivery

- **Un despacho por venta**; no se programa para ventas anuladas ni para ventas de sucursales no permitidas.
- Estados: `Preparando` → `En_Camino` → `Entregado`. `Devuelto` solo antes de entregar. `Entregado` y `Devuelto` son finales.

### Pacientes, recetas y servicios

- **Pacientes** y **Profesionales de Salud** (maestros).
- **Recetas médicas**: paciente, médico, fecha de expedición, repeticiones máximas e indicaciones.
- **Análisis de recetas con IA** (`POST /recetas/analizar`): a partir de una foto, Gemini transcribe el texto (OCR), **Presidio anonimiza el texto** (nombres, DNI, RUC, etc.) y un segundo paso con Gemini identifica medicamentos, concentraciones, un nivel de confianza y sugerencias de genéricos del mercado peruano. Si Presidio no responde, el análisis falla en lugar de enviar el texto sin anonimizar.
- **Servicios**: agenda de servicios y registro de inmunizaciones (solo API).

### Auditoría de seguridad

Las operaciones sensibles registran un evento en `Auditoria_Eventos_Seguridad` (quién, qué, cuándo, IP), dentro de la misma transacción: anulación de ventas, devoluciones, ajustes y registros de stock, apertura/cierre de caja, cambios en roles, permisos, cuentas y sucursales asignadas, cambio y restablecimiento de contraseña, series fiscales y edición de la empresa.

El endpoint de anonimización de datos personales (`POST /auditoria/anonimizar`) responde **501 Not Implemented**: no modifica datos.

---

## 🏗️ Arquitectura

Clean Architecture + CQRS (MediatR) + Result Pattern.

```
src/
├── SOFIA.Domain/              # Entidades y reglas de negocio
├── SOFIA.Application/         # Commands/Queries (MediatR), validadores, DTOs
├── SOFIA.Infrastructure/      # EF Core, SQL Server, JWT, Excel, Gemini, Presidio
├── SOFIA.API/                 # Controllers REST, middleware, DI root
├── SOFIA.SharedKernel/        # Abstracciones compartidas
├── SOFIA.DbUp/                # Scripts SQL versionados (crea/migra el esquema)
├── SOFIA.PresidioAPI/         # Microservicio Python (FastAPI) de anonimización
├── SOFIA.UnitTests/           # xUnit + Moq + FluentAssertions
├── SOFIA.IntegrationTests/    # Testcontainers (SQL Server real)
└── SOFIA.ArchitectureTests/   # Reglas de dependencias entre capas (NetArchTest)
```

---

## 📦 Stack tecnológico

| Capa | Tecnología |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# 13 |
| API | ASP.NET Core, Asp.Versioning 10 (rutas `/api/v1/...`) |
| ORM | Entity Framework Core 10.0.7 (SQL Server) |
| Base de datos | SQL Server 2022 |
| Migraciones | DbUp (`dbup-sqlserver` 7.2) |
| CQRS | MediatR 14.1.0 |
| Validación | FluentValidation 11.11 |
| Autenticación | JWT Bearer + refresh token en cookie; contraseñas con BCrypt |
| Logging | Serilog 9 (JSON compacto, archivo con rotación diaria) |
| Excel | ClosedXML 0.104 |
| IA — recetas | Google Gemini (modelo configurable) |
| Anonimización PII | Microsoft Presidio (Python, FastAPI, spaCy en español) |
| Contenedores | Docker + Docker Compose |
| Tests | xUnit, Moq, MockQueryable, FluentAssertions, Testcontainers.MsSql, NetArchTest |
| Calidad | SonarAnalyzer.CSharp, dotnet-format, Husky.Net |

---

## 🚀 Ejecución local

### Requisitos previos

| Herramienta | Versión |
|---|---|
| .NET SDK | 10.0 |
| Docker Desktop | Para SQL Server, Presidio e integration tests |
| Git | 2.x |

### 1. Levantar SQL Server y Presidio

```bash
cp .env.example .env      # completa SA_PASSWORD (y PRESIDIO_INTERNAL_KEY si quieres)
docker compose up -d sqlserver presidio-api
```

| Servicio | Dirección en tu máquina |
|---|---|
| SQL Server | `127.0.0.1,1434` (usuario `sa`) |
| Presidio | `http://localhost:8001` |
| Mailpit (opcional, `docker compose up -d mailpit`) | SMTP `127.0.0.1:1025`, bandeja web `http://localhost:8025` |

> También puedes levantar todo el stack (`docker compose up -d`): incluye la API en `http://localhost:5000` y el contenedor `sofia-dbup`, que aplica las migraciones al arrancar. En ese caso completa además `JWT_SECRET_KEY` y las variables `GEMINI_*` del `.env`.

### 2. Configurar secretos (user-secrets)

Los secretos **nunca** se guardan en el repositorio. La API y DbUp comparten el mismo `UserSecretsId`, así que basta configurarlos una vez sobre `src/SOFIA.API`:

```bash
dotnet user-secrets set "<Clave>" "<valor>" --project src/SOFIA.API/SOFIA.API.csproj
```

| Clave | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server (base `SofiaDb`) |
| `Jwt:SecretKey` | Clave de firma JWT (mínimo 32 caracteres) |
| `PresidioApi:BaseUrl` | URL de Presidio (p. ej. `http://localhost:8001`) |
| `PresidioApi:InternalKey` | Opcional: clave compartida con Presidio (`PRESIDIO_INTERNAL_KEY`) |
| `GeminiApi:ApiKey` | API key de Google Gemini |
| `GeminiApi:BaseUrl` | URL base de modelos de Gemini, terminada en `/v1beta/models/` (la API llama a `{BaseUrl}{OcrModel}:generateContent`) |
| `GeminiApi:OcrModel` | Modelo de Gemini para el OCR de la receta (p. ej. `gemini-3.6-flash`) |
| `GeminiApi:ReasoningModel` | Opcional: modelo de Gemini para interpretar el texto (p. ej. `gemini-2.5-pro`); si falta, se usa `OcrModel` |
| `App:FrontendBaseUrl` | URL de la SPA para los enlaces de recuperación (local: `http://localhost:4200`; fuera de Development debe ser `https`) |
| `Email:Provider` | `Smtp`, `AzureCommunication` o `None` (`None` solo se acepta en Development/Testing: los correos se descartan) |
| `Email:FromAddress` / `Email:FromDisplayName` | Remitente de los correos |
| `Email:Smtp:Host` / `Email:Smtp:Port` / `Email:Smtp:UseSsl` | Servidor SMTP (local con Mailpit: `localhost`, `1025`, `false`) |
| `Email:Smtp:UserName` / `Email:Smtp:Password` | Opcionales, siempre juntos (Mailpit no usa autenticación) |
| `Email:AzureCommunication:ConnectionString` | Cadena de conexión de Azure Communication Services (proveedor `AzureCommunication`) |

La API **no arranca** si el proveedor elegido está incompleto o si falta `App:FrontendBaseUrl`.

**Correo en local (Mailpit):** `docker compose up -d mailpit` levanta un buzón de pruebas: SMTP en `127.0.0.1:1025` y bandeja web en `http://localhost:8025`. Configura `Email:Provider=Smtp`, `Email:FromAddress=no-reply@sofia.local`, `Email:Smtp:Host=localhost`, `Email:Smtp:Port=1025`, `Email:Smtp:UseSsl=false` y `App:FrontendBaseUrl=http://localhost:4200`.

**Correo en Azure (staging sin dominio propio):** crea un recurso *Communication Services* y un *Email Communication Service* con un **dominio administrado por Azure** (`<guid>.azurecomm.net`), conéctalos y define en el App Service `Email__Provider=AzureCommunication`, `Email__FromAddress=DoNotReply@<guid>.azurecomm.net`, `Email__AzureCommunication__ConnectionString` (como secreto, idealmente vía Key Vault) y `App__FrontendBaseUrl` con la URL `https` de la SPA. El nombre visible del remitente se configura en el dominio de ACS.

### 3. Crear / migrar el esquema

```bash
dotnet run --project src/SOFIA.DbUp/SOFIA.DbUp.csproj
```

Crea la base de datos si no existe y aplica los scripts pendientes de `src/SOFIA.DbUp/Scripts` (incluye roles base y el catálogo DIGEMID inicial).

### 4. Ejecutar la API

```bash
dotnet run --project src/SOFIA.API/SOFIA.API.csproj --launch-profile https
```

- API: `https://localhost:7300` (la SPA apunta a esta URL por defecto)
- Health check: `https://localhost:7300/health`
- En Development, CORS permite `http://localhost:4200` y `https://localhost:4200`.

Para tener un primer usuario, registra una empresa desde la SPA (`/auth/register`) o con `POST /api/v1/empresas/registrar`.

### 5. Tests

```bash
# Unit tests
dotnet test src/SOFIA.UnitTests/SOFIA.UnitTests.csproj

# Architecture tests
dotnet test src/SOFIA.ArchitectureTests/SOFIA.ArchitectureTests.csproj

# Integration tests (requiere Docker corriendo)
# Contraseña del contenedor: user-secret "Testing:ContainerDbPassword" en src/SOFIA.IntegrationTests
# o variable de entorno SOFIA_TEST_DB_PASSWORD
dotnet test src/SOFIA.IntegrationTests/SOFIA.IntegrationTests.csproj
```

---

## 🛡️ Git hooks (Husky.Net)

Los hooks **se instalan solos en el primer `dotnet restore`** (o `build`) gracias a un target en `SOFIA.API.csproj` que ejecuta `dotnet tool restore` y `dotnet husky install`. No se instalan fuera de un checkout de git (p. ej. en Docker). Para omitirlos, define la variable de entorno `HUSKY=0`.

**pre-commit**, en orden:

| # | Tarea | Qué hace |
|---|---|---|
| 1 | `detect-secrets` | Bloquea el commit si detecta credenciales en archivos staged |
| 2 | `dotnet-format` | Formatea los `.cs` / `.csproj` staged |
| 3 | `re-stage` | Vuelve a agregar los archivos formateados |
| 4 | `architecture-tests` | Corre `SOFIA.ArchitectureTests` |

**commit-msg**: valida el formato

```
<tipo>(<alcance>): <descripción>

Tipos    : feat, fix, docs, style, refactor, perf, test, chore
Alcances : domain, app, infra, api, ia, db, test
```

Ejemplo: `feat(api): agregar endpoint de sucursales con paginación`

---

## 🔄 CI (GitHub Actions)

`.github/workflows/security.yml` corre en cada push/PR a `master`/`main` y semanalmente (lunes 08:00 UTC):

| Job | Qué hace |
|---|---|
| Build & Static Analysis | Restore, build Release con SonarAnalyzer, unit tests y architecture tests |
| OWASP Dependency Check | Escanea CVEs en dependencias NuGet (falla con CVSS ≥ 7) y sube el reporte SARIF |
| Secret Scan (gitleaks) | Busca secretos en todo el historial |

---

## 🚧 Pendientes / limitaciones conocidas

| Tema | Estado |
|---|---|
| Cifrado de datos personales en reposo | Los datos personales (pacientes, etc.) no se cifran a nivel de columna. |
| Anonimización de la imagen de receta | La **imagen original se envía a Gemini** para el OCR; solo el texto extraído se anonimiza con Presidio antes del segundo paso. Decisión pendiente. |
| Login SQL de mínimo privilegio | La API se conecta con `sa` (ver `docker-compose.yml`); falta un usuario de base de datos con permisos mínimos. |
| Seguros | La lógica de aseguradoras y cobertura existe en la API, pero no está habilitada en la interfaz del POS ni en producción. |
| Anonimización de datos (Ley 29733) | `POST /auditoria/anonimizar` responde 501. |
| SUNAT | Emisión electrónica simulada (sin OSE/PSE ni firma digital). |
