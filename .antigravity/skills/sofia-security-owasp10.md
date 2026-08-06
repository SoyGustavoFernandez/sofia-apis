# Skill: SOFIA OWASP Top 10 Security Standards

## Description
Agente responsable de asegurar que el desarrollo del backend de SOFIA cumpla con las directrices de seguridad de **OWASP Top 10**, garantizando que el diseño del software y la escritura de código mitiguen las vulnerabilidades más críticas en aplicaciones web.

## Constraints & Rules

*   **A01:2021 - Control de Acceso Truncado (Broken Access Control)**:
    *   Implementar siempre autorización basada en permisos mediante el atributo `[HasPermission]`.
    *   Validar la propiedad del recurso y el contexto jerárquico a nivel de aplicación (ej. asegurar que un usuario solo pueda operar en sucursales a las que pertenece o tiene permisos explícitos).
*   **A02:2021 - Fallas Criptográficas (Cryptographic Failures)**:
    *   No almacenar contraseñas en texto plano ni usar algoritmos obsoletos (MD5, SHA1). Utilizar siempre `IPasswordHasher` basado en BCrypt (con salt dinámico).
    *   Asegurar que los tokens JWT contengan firmas seguras usando claves almacenadas en variables de entorno o user-secrets, nunca quemadas en código.
*   **A03:2021 - Inyección (Injection)**:
    *   Utilizar Entity Framework Core y consultas LINQ parametrizadas para interactuar con la base de datos SQL Server.
    *   Evitar consultas SQL crudas construidas mediante concatenación de strings. Si es estrictamente necesario usar SQL crudo, utilizar placeholders parametrizados.
*   **A04:2021 - Diseño Inseguro (Insecure Design)**:
    *   Adherirse a Clean Architecture y DDD. Utilizar el Result Pattern (`Result<T>`) para la gestión de errores lógicos del negocio, evitando propagar excepciones crudas que puedan revelar la estructura interna del sistema.
*   **A05:2021 - Configuración de Seguridad Incorrecta (Security Misconfiguration)**:
    *   Asegurar la configuración de cabeceras de seguridad HTTP, políticas de CORS restrictivas y el manejo seguro de cookies en la capa de la API.
*   **A06:2021 - Componentes Vulnerables y Desactualizados (Vulnerable and Outdated Components)**:
    *   Utilizar dependencias NuGet oficiales, revisando periódicamente que no contengan vulnerabilidades conocidas.
*   **A07:2021 - Fallas de Identificación y Autenticación (Identification and Authentication Failures)**:
    *   Implementar validación activa del `SecurityStamp` del usuario en cada validación de token JWT (`OnTokenValidated`), permitiendo la revocación inmediata en caso de deactivación o cambio de clave.
    *   Controlar los intentos fallidos de inicio de sesión (`Intentos_Fallidos` y `Bloqueado_Hasta`).
*   **A08:2021 - Fallas en la Integridad de Software y Datos (Software and Data Integrity Failures)**:
    *   Validar rigurosamente los datos de entrada en los Commands/Queries de MediatR mediante FluentValidation antes de que alcancen la lógica del dominio.
*   **A09:2021 - Fallas en el Registro y Monitoreo de Seguridad (Security Logging and Monitoring Failures)**:
    *   Registrar eventos críticos y auditorías (como creación, despacho o recepción de transferencias) sin exponer datos sensibles (como hashes de claves o tokens) en los logs.
*   **A10:2021 - Falsificación de Solicitudes del Lado del Servidor (SSRF)**:
    *   Validar y sanitizar cualquier URL o endpoint externo proporcionado por el usuario antes de realizar peticiones HTTP desde el servidor.

## Instructions for the Agent
1.  **Revisión en Pull Request/Cambios**: En cada modificación de endpoints o lógica de negocio, evalúa mentalmente los riesgos asociados a los 10 puntos de OWASP y corrige proactivamente cualquier desviación.
2.  **Validaciones Estrictas**: Asegúrate de que cada comando de escritura (Command) cuente con su respectiva clase Validator (FluentValidation) para proteger la integridad de los datos entrantes.
