# SOFIA — Sistema Optimizado Farmacéutico con Inteligencia Artificial

> API REST (backend) de SOFIA, plataforma SaaS de gestión para farmacias y cadenas de boticas en Perú.
> Multiempresa (cada **Empresa** es un tenant aislado) y multisucursal (**Sucursal**).
> La interfaz web vive en el repositorio hermano [sofia-web-spa](https://github.com/SoyGustavoFernandez/sofia-web-spa).

---

## 📋 Qué hace el sistema

SOFIA cubre la operación diaria de una farmacia: catálogos maestros, inventario por lotes y sucursal, punto de venta con caja, recetas médicas (con lectura y transcripción asistida por IA), devoluciones con nota de crédito, series fiscales SUNAT, cumplimiento DIGEMID y seguridad estricta basada en roles y permisos (RBAC granular).

Toda la información pertenece a una empresa: un usuario solo ve y modifica datos de su propia empresa mediante filtros globales de EF Core por `TenantId` aplicados a todas las consultas. Dentro de la empresa, el acceso a datos de sucursal depende de las sucursales permitidas al usuario (ver [Roles, permisos y sucursales](#roles-permisos-y-sucursales)).

---

## 🧩 Módulos y reglas de negocio

### Empresas (tenant) y suscripción

- **Autorregistro público** (`POST /api/v1/empresas/registrar`): crea en una transacción atómica la empresa, su sede principal, el empleado administrador, su cuenta y el rol `Admin` propio de esa empresa. El usuario queda con sesión iniciada.
  - La **dirección de la sede principal** (`direccionSede`, máximo 255 caracteres) es obligatoria. El número de licencia de esa primera sucursal es opcional: si no se envía se guarda vacío y pasa a ser obligatorio al editar la sucursal.
- La empresa nace en **periodo de prueba de 30 días** (`TrialActivo`). Estados posibles: `TrialActivo`, `Activo`, `Suspendido`, `Cancelado`.
- **Suscripción vigente**: solo se puede iniciar sesión, renovar sesión o consumir la API si la empresa está en estado `Activo`, o `TrialActivo` con la prueba sin vencer. Si la empresa deja de estar vigente (prueba vencida, `Suspendido` o `Cancelado`), los accesos abiertos se cortan en un máximo de 1 minuto (validado en la política de tokens con caché de 1 minuto). El estado `Activo` no vence por fecha. Hoy no hay endpoints para activar, suspender, cancelar o extender la prueba: el estado se cambia en la base de datos.
- El RUC es único en todo el sistema.
- Una empresa solo puede **ver y editar su propia ficha**. No hay eliminación ni carga masiva de empresas desde un tenant.

### Autenticación y sesión

| Regla | Detalle |
|---|---|
| Login | Usuario + contraseña. Devuelve un access token JWT de **15 minutos** y un refresh token de **7 días** en una cookie `HttpOnly`, `Secure`, `SameSite=None`. Mitigación de timing attack con cálculo BCrypt dummy constante si el usuario no existe. |
| Renovación | `POST /api/v1/auth/refresh` consumiendo la cookie HttpOnly. El refresh token **rota en cada uso**. Reutilizar un token ya rotado (pasados 30 segundos de la rotación, para tolerar dos pestañas renovando a la vez) se detecta como robo: revoca **todas** las sesiones de la cuenta e invalida el `SecurityStamp`. |
| Anti-CSRF | `refresh` y `logout` exigen obligatoriamente la cabecera `X-SOFIA-CSRF`. |
| Logout | Cierra **todas** las sesiones de la cuenta (revoca sus refresh tokens e invalida el `SecurityStamp`), **aunque el access token ya haya expirado**. |
| Bloqueo por intentos | **5 intentos fallidos** bloquean la cuenta durante **15 minutos**. La respuesta de la API es genérica (`Auth.InvalidCredentials`) para evitar la enumeración de usuarios. |
| Cambio de contraseña | `POST /api/v1/auth/change-password` (contraseña actual + nueva). Cierra todas las sesiones abiertas de la cuenta al rotar el `SecurityStamp`. |
| Cambio obligatorio | Las cuentas creadas por un administrador nacen con cambio forzado, y un administrador puede forzarlo en cualquier cuenta. Mientras esté pendiente, el middleware `PasswordChangeRequiredMiddleware` solo deja pasar `auth/me`, `auth/change-password` y los endpoints anónimos (login, refresh, logout, recuperación); el resto responde `403` (`Auth.CambioClaveRequerido`). |
| Recuperación | `POST /api/v1/auth/forgot-password` envía al correo del empleado un enlace con token en fragmento hash (`#token=...&usuario=...`, 1 hora de validez). `POST /api/v1/auth/reset-password` canjea el token y reinicia la clave. |
| Rate Limiting | Catch-all global: 200 req/min por IP. Auth (login, change, forgot, reset): 10 req/min por IP. Autorregistro: 5 req/hora por IP. IA recetas: 10 req/min por empresa (`empresaId`). |

### Roles, permisos y sucursales

- **RBAC granular por módulo y acción** (p. ej. `Ventas:Anular`, `Inventarios:Actualizar`, `Devoluciones:ProcesarDevolucion`).
- El rol **`Admin`** tiene bypass de permisos (acceso total). Solo un Admin puede gestionar otros administradores.
- **Sucursales accesibles**: Unión calculada de:
  1. Sucursal base del empleado.
  2. Sucursales asignadas directamente a la cuenta (`CuentasSucursales`).
  3. Sucursales asignadas a los roles del usuario (`RolesSucursales`).
- **Punto de venta y caja**: Operan siempre de forma estricta sobre la **sucursal base** del usuario en sesión.
- Las ventas (listado, detalle, anulación y devolución) y las transferencias se limitan a la **sucursal base** del usuario, incluso para el Admin.
- El Admin accede a todas las sucursales de su empresa. Los cambios de permisos se aplican de inmediato.
- **Cuentas de usuario**: alta (con cambio de contraseña obligatorio), activación/desactivación, forzar cambio de contraseña, desbloqueo, y asignación de roles y de sucursales adicionales.
- **Empleados**: el apellido materno es opcional; el correo es opcional y único por empresa (sin correo no hay recuperación de contraseña por e-mail).

### Catálogos maestros

Todos los maestros disponen de paginación, filtros de búsqueda, soft-delete y **exportación a Excel** (`POST /api/v1/{modulo}/exportar`; en DIGEMID, `digemid/catalogo/exportar`). La exportación se rechaza si supera 10 000 filas.

La baja es lógica. Bloquean la baja de un registro en uso (respuesta `409`, código `<Entidad>.InUse`): Medicamentos, Sucursales, Pacientes, Profesionales de Salud, Ingredientes Activos, Proveedores, Laboratorios, Unidades de Medida, Presentaciones de Venta, Lotes, Roles, Series Fiscales, Recetas y Empleados que son gerentes (`Empleado.EsGerente`). Aseguradoras, Jerarquías de UdM, Formulaciones Clínicas y el catálogo DIGEMID se dan de baja sin validar dependencias.

**Carga masiva** (plantilla `.xlsx` descargable → previsualización → confirmación transaccional): Máximo **5 MB** y **5 000 filas** (además se rechazan archivos que al descomprimirse superan 50 MB). *Excepción:* Aseguradoras todavía no aplica el tope de 5 MB ni el de filas en la confirmación.

| Maestro | Carga Masiva (.xlsx) | Exportación a Excel (.xlsx) |
|---|:---:|:---:|
| Sucursales | Sí | Sí |
| Roles | Sí | Sí |
| Unidades de Medida | Sí | Sí |
| Jerarquías de UdM | Sí | Sí |
| Laboratorios | Sí | Sí |
| Ingredientes Activos | Sí | Sí |
| Medicamentos | Sí | Sí |
| Proveedores | Sí | Sí |
| Aseguradoras (Seguros) | Sí | Sí |
| Pacientes | Sí | Sí |
| Profesionales de Salud | Sí | Sí |
| Catálogo DIGEMID | Sí | Sí |
| Empresas | No | Sí |
| Empleados | No | Sí |
| Presentaciones de Venta | No | Sí |
| Formulaciones Clínicas | No | Sí |
| Series Fiscales | No | Sí |

**Precios de proveedor**: `POST /api/v1/proveedores/precios` registra el costo neto de compra de un producto (precio × (1 − descuento/100), 4 decimales), vigente desde ese momento, y cierra el precio abierto anterior del mismo proveedor y producto. `GET /api/v1/proveedores/{id}/precios` devuelve el historial paginado (permiso `Proveedores:Leer`). Las ventas nuevas guardan el último costo vigente del producto; las anteriores no se recalculan.

### Inventario y trazabilidad

- **Lotes y vencimientos**: Número de lote del fabricante, fecha de fabricación y fecha de caducidad por producto.
- **Stock por sucursal**: Cantidad física por lote y sucursal. Registro de ingresos y ajustes; los ajustes directos de stock quedan auditados.
- **Transferencias entre sucursales**: Flujo `Iniciada` → `despachar` (descuenta stock en origen, estado `En_Transito`) → `recibir` (en destino: `Completada` o `Recibida_Parcial`). Cancelación disponible antes de recepción con devolución de stock.
- **DIGEMID**: registro de lotes en cuarentena (el estado `Retenido`, `Liberado`, `Destruido` o `Devuelto` se indica al registrar; aún no hay operación para cambiarlo después) y registro de actas de destrucción con regente responsable (solo cabecera, sin vínculo a lotes ni movimiento de stock). Un lote con cuarentena `Retenido` queda bloqueado para la venta.
- **Órdenes magistrales**: al iniciar se consumen los insumos indicados (stock de la propia sucursal; se rechazan lotes vencidos) y al completar se genera un lote nuevo del producto elaborado.

### Punto de venta (POS) y caja

- **Apertura de caja**: Una sesión abierta por cajero en su sucursal base.
- **Cierre y arqueo**: Solo el dueño de la caja o un Admin pueden cerrar. Efectivo esperado calculado por el servidor:  
  $$\text{Esperado} = \text{Apertura} + \text{Efectivo Cobrado} - \text{Vuelto Entregado} - \text{Reembolsos Efectivo}$$
  Si coincide queda `Cuadrada`; de lo contrario, `Cerrada` registrando el faltante o sobrante.
- **Venta**: Validación de stock en tiempo real y precios controlados por catálogo. Medios de pago: Efectivo, Yape/Plin, Tarjeta, Transferencia.
- **Pagos**: se admiten pagos divididos. Todo pago que no sea en efectivo exige **referencia de operación**. Solo hay vuelto si hay efectivo y no puede superar el efectivo recibido.
- **Ventas pendientes**: se pueden guardar sin pago, editar y cobrar después (`PUT /ventas/{id}/detalles`, `PUT /ventas/{id}/completar`); el cobro entra en la caja de quien la completa.
- **Lotes vencidos**: no se venden. Un lote es vendible hasta el final de su día de vencimiento.
- **Costo**: cada venta nueva guarda el último costo de compra vigente del producto (ver Precios de proveedor); las ventas anteriores no se recalculan.
- **Comprobante**: toda venta cobrada emite una boleta; se rechaza si la empresa no tiene RUC o la sucursal no tiene serie de Boleta activa. El cliente se identifica con el tipo y número de documento del paciente (DNI, CE, RUC, pasaporte); sin paciente se emite a "CLIENTE EVENTUAL".
- **Seguros**: la API admite una aseguradora y un monto cubierto (entre 0 y el total), que genera un reclamo marcado como `Aprobado` con un código interno; no hay integración real con aseguradoras y el POS aún no expone la opción.
- **Anulación**: requiere `Ventas:Anular`, un motivo y que la venta sea de tu sucursal base. Se bloquea si la venta tiene devoluciones. Anular una venta cobrada exige además tener **tu propia sesión de caja abierta** en la sucursal de la venta (error `Venta.Anular.SinSesionAbierta`); la venta guarda la sesión desde la que se anuló. Si se anula desde una sesión distinta de la que la cobró, la venta sigue contando en su sesión original y la sesión que anula descuenta el efectivo neto. Una venta pendiente (nunca cobrada) se anula sin sesión.
- **Control de recetas** (según la condición de venta del producto):
  - `VentaLibreOTC`: sin receta.
  - `RecetaSimple`: depende del indicador por empresa `ExigirRegistroReceta` (apagado por defecto; solo se cambia en la base de datos y se consulta con `GET /api/v1/ventas/configuracion`). Encendido, exige una receta registrada. Apagado, se puede vender sin receta registrada, pero al cobrar el cajero debe confirmar **"Receta verificada"**; queda guardado quién y cuándo lo confirmó.
  - `RecetaRetenida` y `Estupefaciente`: siempre exigen una receta registrada **con número de receta**, y admiten 1 sola dispensación.
  - `RecetaSimple` registrada: $1 + \text{RepeticionesMax}$ dispensaciones.
  - La receta debe ser del **mismo paciente** de la venta.
  - La anulación de la venta reintegra la dispensación y el stock. Una receta ya usada en ventas no se puede eliminar.

### Dashboard

`GET /api/v1/dashboard/{ventas|caja|inventario|cuarentena|devoluciones}`: un endpoint por sección, protegido con el permiso de lectura de su módulo (`Ventas`, `POS`, `Inventarios`, `DIGEMID`, `Devoluciones`). Las cifras cubren las sucursales accesibles del usuario; el Admin ve el total de la empresa con desglose por sucursal. `ventas`, `inventario` y `devoluciones` reciben `hoy` (inicio del día local del cliente).

### Comprobantes y series fiscales (SUNAT)

- Series por sucursal: `Boleta` (prefijo B), `Factura` (prefijo F), `Nota de Crédito` (prefijo B/F) y `Proforma`. Hoy solo se emiten **boletas** (toda venta cobrada) y **notas de crédito** (devoluciones); las series de Factura y Proforma se pueden configurar pero aún no se usan.
- Una sola serie activa por sucursal y tipo; el prefijo (4 caracteres) no se repite dentro del mismo tipo. Una serie que ya emitió comprobantes solo puede cambiar de estado y no se puede eliminar.
- Incremento atómico y concurrente de correlativos con row-locking.
- Impuestos: Precios con IGV incluido (tasa 18%, base gravada = $\text{Total} / 1.18$).
- *Nota:* Los comprobantes se numeran y se guardan con estado `NoEnviado`: **no hay envío a SUNAT** ni se genera hash, XML, CDR o URL de verificación. Al anular una venta su comprobante pasa a `Anulado`.

### Devoluciones y reembolsos

- Procesa devoluciones parciales o totales sobre ventas en estado `Completada` de la sucursal del usuario.
- Emisión automática de nota de crédito por las líneas devueltas.
- El reembolso se registra en la caja abierta del cajero que procesa la devolución (es obligatoria); solo los reembolsos en efectivo descuentan del arqueo. La parte cubierta por un seguro no se reembolsa al cliente. La cantidad por línea no puede superar lo vendido menos lo ya devuelto.
- Destino de mercancía: `Reingreso_Venta` (vuelve al stock) o `Cuarentena_DIGEMID` (no vuelve al stock; el registro de cuarentena se crea aparte en el módulo DIGEMID).

### Delivery

- Un despacho por venta; no se programa para ventas anuladas ni de sucursales no permitidas.
- Estados: `Preparando` → `En_Camino` → `Entregado`. `Devuelto` solo antes de entregar. `Entregado` y `Devuelto` son finales.

### Servicios

Agenda de servicios y registro de inmunizaciones (solo API). No se registra una inmunización con un lote que estaba vencido en la fecha de aplicación.

### Inteligencia Artificial (Gemini + Presidio)

- `POST /api/v1/recetas/analizar` (límite de carga: **10 MB**):
  1. Gemini procesa la imagen para OCR inicial.
  2. El microservicio `SOFIA.PresidioAPI` (FastAPI / spaCy) anonimiza nombres, DNI, RUC y teléfonos peruanos.
  3. Gemini razona sobre el texto anonimizado para extraer medicamentos, concentraciones y sugerencias de genéricos.
  4. Si Presidio falla o está inaccesible, la petición se aborta por diseño de privacidad (fail-closed).
  5. La API cruza cada medicamento detectado con el catálogo DIGEMID y con el maestro de la empresa, e informa stock en tu sucursal y equivalentes (máx. 20 medicamentos). Requiere el permiso `Recetas:Analizar`.

---

## 🏗️ Arquitectura de software

El backend está diseñado bajo los principios de **Clean Architecture**, **CQRS** con MediatR y **Result Pattern**.

```
sofia-apis/
├── SOFIA.Solution.slnx             # Definición moderna de la solución
├── src/
│   ├── SOFIA.Domain/               # Entidades, Enums, Value Objects, Result/Error, BaseEntity
│   ├── SOFIA.Application/          # Commands, Queries, MediatR Behaviors, FluentValidation
│   ├── SOFIA.Infrastructure/       # EF Core 10, SQL Server, Auth, MailKit, Gemini, Presidio
│   ├── SOFIA.API/                  # ASP.NET Core 10 Web API, Controllers REST, Middlewares
│   ├── SOFIA.SharedKernel/         # Punto de extensión para registros compartidos (hoy vacío)
│   ├── SOFIA.DbUp/                 # Consola DbUp con 28 scripts de migración SQL Server
│   ├── SOFIA.PresidioAPI/          # Microservicio Python 3.10 (FastAPI + spaCy)
│   ├── SOFIA.UnitTests/            # xUnit + Moq + FluentAssertions (Domain y Application)
│   ├── SOFIA.ArchitectureTests/    # NetArchTest (dependencias entre capas y autorización declarada)
│   └── SOFIA.IntegrationTests/     # Testcontainers (SQL Server 2022 real)
```

### Pipeline de MediatR
Toda petición pasa por estos behaviors, en este orden:
1. `SanitizationBehavior<,>`: elimina HTML de las cadenas de la petición con `HtmlSanitizer` (commands y queries).
2. `ValidationBehavior<,>`: ejecuta los validadores `FluentValidation`.
3. `TransactionBehavior<,>`: abre una transacción por cada command (`ICommand`); las queries no.
4. `AuditBehavior<,>`: para los commands sensibles (`IAuditableCommand`) registra un evento en `Auditoria_Eventos_Seguridad` dentro de la misma transacción.

**Operaciones auditadas** (quién, qué, cuándo, IP): anulación de ventas, devoluciones, ajustes directos de stock, apertura y cierre de caja, roles, permisos, cuentas y sucursales asignadas, cambio y restablecimiento de contraseña, series fiscales y edición de la empresa.

### Contrato de errores
Errores en formato `ProblemDetails` con la extensión `code` (clave i18n del frontend). Los conflictos de concurrencia y los duplicados responden `409`.

---

## 📦 Stack tecnológico

| Capa / Rol | Tecnología | Versión |
|---|---|---|
| **Runtime & SDK** | .NET SDK / C# | .NET 10.0 (`net10.0`), C# 14 (versión por defecto del SDK; no se fija `LangVersion`) |
| **API Framework** | ASP.NET Core Web API | 10.0 (`Microsoft.NET.Sdk.Web`) |
| **Versionado de API** | Asp.Versioning.Http | 10.0.0 (Prefijo `/api/v1/`) |
| **ORM / Acceso a Datos**| EF Core + SQL Server Provider | 10.0.7 |
| **Base de Datos** | Microsoft SQL Server | 2022 (`2022-latest`) |
| **Migraciones de Esquema**| DbUp SQL Server | 7.2.0 (28 scripts versionados) |
| **CQRS** | MediatR | 14.1.0 |
| **Validación** | FluentValidation | 11.11.0 |
| **Autenticación** | JWT Bearer + Refresh Token Cookie | 10.0.7 (Tokens 8.18.0) |
| **Hashing de Claves** | BCrypt.Net-Next | 4.1.0 |
| **Saneamiento HTML** | HtmlSanitizer | 9.2.1039 |
| **Exportación Excel** | ClosedXML | 0.104.2 |
| **Correo Electrónico** | MailKit / Azure Communication Email | MailKit 4.18.1 / ACS 1.1.0 |
| **Logging** | Serilog (AspNetCore, File, Enrichers)| Serilog 9.0.0 / Sinks.File 7.0.0 |
| **IA — OCR y Razonamiento**| Google Gemini API | Modelos `gemini-3.6-flash` / `gemini-2.5-pro` |
| **Anonimización PII** | Microsoft Presidio + FastAPI | Python 3.10.21, Presidio 2.2, spaCy 3.7.5 |
| **Pruebas Automatizadas**| xUnit, FluentAssertions, Moq, Testcontainers | xUnit 2.9.3, Testcontainers 4.15.0, NetArchTest 1.4.5 |
| **Calidad y Git Hooks** | Husky.Net + SonarAnalyzer | Husky.Net 0.9.1, SonarAnalyzer 9.32.0 |

---

## ⚙️ Variables de entorno y configuración

### Configuración obligatoria y opcional

Las variables en mayúsculas solo las lee `docker-compose.yml`, que las traduce a `Seccion__Clave` (excepto `SOFIA_TEST_DB_PASSWORD`, que lee directamente el proyecto de pruebas de integración). Si ejecutas la API fuera de Docker usa `Seccion__Clave` o user-secrets.

| Parámetro (`appsettings` / Secrets) | Variable de entorno (API) / variable del `.env` de Docker Compose | Obligatoria | Descripción |
|---|---|:---:|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | **Sí** | Cadena de conexión a SQL Server (`SofiaDb`). |
| `Jwt:SecretKey` | `Jwt__SecretKey` / `JWT_SECRET_KEY` | **Sí** | Clave simétrica de firma JWT (mínimo 32 caracteres / 256 bits). |
| `Jwt:Issuer` | `Jwt__Issuer` | **Sí** | Emisor del token (ej. `SOFIA.API`). |
| `Jwt:Audience` | `Jwt__Audience` | **Sí** | Audiencia del token (ej. `SOFIA.Clients`). |
| `Jwt:ExpiryMinutes` | `Jwt__ExpiryMinutes` | No | Minutos de vigencia del JWT (default: `15`). |
| `PresidioApi:BaseUrl` | `PresidioApi__BaseUrl` | **Sí** | URL del microservicio Presidio (ej. `http://localhost:8001`). |
| `PresidioApi:InternalKey` | `PresidioApi__InternalKey` / `PRESIDIO_INTERNAL_KEY` | No | Clave interna compartida con Presidio en cabecera `X-Internal-Key`. |
| `GeminiApi:ApiKey` | `GeminiApi__ApiKey` / `GEMINI_API_KEY` | **Sí (para IA)** | API Key para acceder a Google Gemini. |
| `GeminiApi:BaseUrl` | `GeminiApi__BaseUrl` / `GEMINI_BASE_URL` | **Sí (para IA)** | URL base de modelos de Gemini, terminada en `/v1beta/models/`. Sin valor por defecto en la API (solo `docker-compose.yml` aporta uno). |
| `GeminiApi:OcrModel` | `GeminiApi__OcrModel` / `GEMINI_OCR_MODEL` | No | Modelo para OCR (default: `gemini-3.6-flash`). |
| `GeminiApi:ReasoningModel` | `GeminiApi__ReasoningModel` / `GEMINI_REASONING_MODEL` | No | Modelo para interpretar el texto (p. ej. `gemini-2.5-pro`). Si falta, se usa `OcrModel`. |
| `App:FrontendBaseUrl` | `App__FrontendBaseUrl` / `APP_FRONTEND_BASE_URL` | **Sí** | URL base de la SPA (en Prod debe ser `https://...`). |
| `Email:Provider` | `Email__Provider` / `EMAIL_PROVIDER` | **Sí** | `Smtp`, `AzureCommunication` o `None` (este último solo en Dev/Testing). |
| `Email:FromAddress` | `Email__FromAddress` / `EMAIL_FROM_ADDRESS` | **Sí (si mail activo)** | Dirección de correo del remitente. |
| `Email:Smtp:Host` | `Email__Smtp__Host` / `EMAIL_SMTP_HOST` | **Sí (si Provider=Smtp)** | Servidor SMTP (ej. `localhost` o `mailpit`). |
| `Email:Smtp:Port` | `Email__Smtp__Port` / `EMAIL_SMTP_PORT` | **Sí (si Provider=Smtp)** | Puerto SMTP (default en código: `587`; `1025` en Mailpit). |
| `Email:Smtp:UseSsl` | `Email__Smtp__UseSsl` / `EMAIL_SMTP_USE_SSL` | No | `false` para Mailpit local, `true` para servidores externos. |
| `Email:FromDisplayName` | `Email__FromDisplayName` | No | Nombre visible del remitente (default `SOFIA`). |
| `Email:Smtp:UserName` / `Email:Smtp:Password` | `Email__Smtp__UserName` / `Email__Smtp__Password` | No | Opcionales; se configuran juntos. |
| `Email:AzureCommunication:ConnectionString` | `Email__AzureCommunication__ConnectionString` / `EMAIL_ACS_CONNECTION_STRING` | **Sí (si ACS)** | Connection string de Azure Communication Services. |
| `AllowedOrigins` | `AllowedOrigins__0` | **Sí en Producción** | Lista de dominios permitidos para CORS. |
| `AllowedHosts` | `AllowedHosts` | **Sí fuera de local** | Hosts aceptados; el valor por defecto solo admite `localhost`. |
| `ForwardedHeaders:KnownProxies` | `ForwardedHeaders__KnownProxies__0` | No | IPs de los proxies de confianza. Sin esto, detrás de un proxy todos los clientes comparten IP para el rate limiting y la auditoría. |
| — | `SA_PASSWORD` (`.env`) | **Sí (Docker)** | Contraseña de `sa` del contenedor SQL Server. |
| `Testing:ContainerDbPassword` | `SOFIA_TEST_DB_PASSWORD` | **Sí para IntegrationTests** | Password de SA para el contenedor de SQL Server en Testcontainers. |

---

## 🚀 Guía de ejecución paso a paso

### Requisitos previos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para SQL Server 2022, Presidio y Mailpit)
- Git 2.x

### Paso 1: Configurar variables y levantar servicios auxiliares con Docker
```bash
# Situarse en la carpeta del backend
cd sofia-apis

# Crear archivo de variables de entorno
cp .env.example .env   # completa SA_PASSWORD y JWT_SECRET_KEY

# Levantar SQL Server 2022, Presidio API y Mailpit
docker compose up -d sqlserver presidio-api mailpit
```

> También puedes levantar todo con `docker compose up -d`: incluye la API en `http://localhost:5000` y `sofia-dbup`, que aplica las migraciones al arrancar.

Puertos asignados en tu máquina:
- **SQL Server 2022:** `127.0.0.1,1434` (Usuario: `sa`, Contraseña: la configurada en `.env`)
- **Presidio API:** `http://localhost:8001` (Salud: `http://localhost:8001/health`)
- **Mailpit:** Bandeja Web `http://localhost:8025`, Servidor SMTP `127.0.0.1:1025`

### Paso 2: Configurar secretos de desarrollo (dotnet user-secrets)
Los proyectos `SOFIA.API` y `SOFIA.DbUp` comparten el mismo `UserSecretsId` (`d0590347-8e2f-47da-a582-fb5f430875ae`):
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,1434;Database=SofiaDb;User Id=sa;Password=<SA_PASSWORD de tu .env>;TrustServerCertificate=True;" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Jwt:SecretKey" "ClaveSuperSecretaDeMasDe32CaracteresLongitud!!" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "PresidioApi:BaseUrl" "http://localhost:8001" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "GeminiApi:ApiKey" "TU_GEMINI_API_KEY" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "GeminiApi:BaseUrl" "https://generativelanguage.googleapis.com/v1beta/models/" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "GeminiApi:OcrModel" "gemini-3.6-flash" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "GeminiApi:ReasoningModel" "gemini-2.5-pro" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "App:FrontendBaseUrl" "http://localhost:4200" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Email:Provider" "Smtp" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Email:FromAddress" "no-reply@sofia.local" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Email:Smtp:Host" "localhost" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Email:Smtp:Port" "1025" --project src/SOFIA.API/SOFIA.API.csproj
dotnet user-secrets set "Email:Smtp:UseSsl" "false" --project src/SOFIA.API/SOFIA.API.csproj
```

### Paso 3: Aplicar migraciones a la base de datos (DbUp)
Ejecuta la consola DbUp para aprovisionar la base `SofiaDb` y los 28 scripts versionados:
```bash
dotnet run --project src/SOFIA.DbUp/SOFIA.DbUp.csproj
```

### Paso 4: Iniciar la API
```bash
dotnet run --project src/SOFIA.API/SOFIA.API.csproj --launch-profile https
```
- API Base URL: `https://localhost:7300` (el puerto `http://localhost:5300` redirige a HTTPS)
- Health Check: `https://localhost:7300/health`
- CORS en Development permite automáticamente `http://localhost:4200` y `https://localhost:4200`.
- Para tener un primer usuario, registra una empresa desde la SPA (`/auth/register`) o con `POST /api/v1/empresas/registrar`.

---

## 🧪 Pruebas automatizadas y calidad

### Ejecución de pruebas
```bash
# 1. Pruebas Unitarias (Domain, Application, Seguridad, Validaciones)
dotnet test src/SOFIA.UnitTests/SOFIA.UnitTests.csproj

# 2. Pruebas de Arquitectura (Reglas de dependencias entre capas Clean Architecture)
dotnet test src/SOFIA.ArchitectureTests/SOFIA.ArchitectureTests.csproj

# 3. Pruebas de Integración (Requiere Docker Desktop activo)
# Configurar password del contenedor de pruebas:
dotnet user-secrets set "Testing:ContainerDbPassword" "<password>" --project src/SOFIA.IntegrationTests/SOFIA.IntegrationTests.csproj
dotnet test src/SOFIA.IntegrationTests/SOFIA.IntegrationTests.csproj
```

### Git Hooks (Husky.Net)
Se configuran automáticamente en el primer build/restore de `SOFIA.API.csproj`.
- **pre-commit**: Detección de credenciales en archivos staged (`detect-secrets`), formateo de código con `dotnet format` y ejecución de `SOFIA.ArchitectureTests`.
- **commit-msg**: Valida el formato de Conventional Commits (`tipo(alcance): descripción`). Para omitir hooks en entornos CI o Docker, definir `HUSKY=0`.
  - Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`. Alcances: `domain`, `app`, `infra`, `api`, `ia`, `db`, `test`. Ejemplo: `feat(api): agregar endpoint de sucursales`.

### Integración continua (GitHub Actions)
En push o PR a `main` y los lunes: build con SonarAnalyzer más pruebas unitarias y de arquitectura; OWASP Dependency Check (falla con CVSS ≥ 7); gitleaks sobre todo el historial.

---

## 🚧 Limitaciones conocidas y deuda técnica

| Área | Estado Actual |
|---|---|
| **Cifrado de PII en Reposo** | Los datos de pacientes y recetas no están cifrados a nivel de columna en SQL Server. |
| **Privilegios de Base de Datos** | El entorno local y `docker-compose` usan la cuenta de superusuario `sa`. Falta configurar un usuario de aplicación con mínimo privilegio. |
| **Endpoint de Anonimización** | `POST /api/v1/auditoria/anonimizar` responde `501 Not Implemented`. |
| **SUNAT** | No hay emisión electrónica: los comprobantes quedan como `NoEnviado`. Falta integrar un OSE/PSE homologado y la firma con certificado digital. |
| **Consulta de auditoría** | Los eventos se registran en `Auditoria_Eventos_Seguridad`, pero no existe ninguna consulta ni endpoint para leerlos: hoy solo se pueden revisar directamente en la base de datos. |
| **Imagen de receta** | La imagen original se envía a Gemini para el OCR; solo el texto extraído se anonimiza antes del segundo paso. |
| **Cuarentena DIGEMID** | No hay operación para liberar o resolver una cuarentena registrada. |
| **Seguros** | Reclamo auto-aprobado, sin integración. |
| **Suscripciones** | Estados de empresa solo por base de datos. |
| **Módulos solo API** | Transferencias, Delivery, Magistrales, Servicios y cuarentena/actas DIGEMID no tienen pantalla en la SPA. |
| **Eventos de dominio** | `VentaCompletadaEvent` se guarda en el outbox y se despacha, pero no tiene consumidores. |

---

## 🔗 Repositorio relacionado

- [sofia-web-spa](https://github.com/SoyGustavoFernandez/sofia-web-spa) — Frontend SPA Angular (cliente web que consume esta API).
