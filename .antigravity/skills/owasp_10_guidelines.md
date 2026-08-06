# Guía de Buenas Prácticas y Normas de Seguridad (OWASP Top 10)

Esta guía establece las prácticas obligatorias que se deben cumplir durante el desarrollo del backend de SOFIA para garantizar la seguridad de la aplicación, basándose en el OWASP Top 10.

## 1. Control de Acceso Roto (Broken Access Control)
- Todo endpoint de la API debe requerir autenticación y autorización explícita.
- Usar el atributo `[HasPermission("PermisoEspecifico")]` en los controladores para verificar permisos de forma granular.
- Validar siempre que el usuario logueado tenga los derechos necesarios sobre el recurso que intenta acceder o modificar (ej. que un paciente solo pueda ver sus propias recetas).
- No depender de que el cliente oculte elementos de UI; el backend siempre debe validar.

## 2. Fallos Criptográficos (Cryptographic Failures)
- Almacenar contraseñas utilizando algoritmos fuertes (ej. BCrypt, Argon2). Nunca en texto plano o con algoritmos débiles (MD5, SHA1).
- Toda la comunicación debe ser sobre HTTPS (TLS 1.2 o superior).
- No almacenar datos sensibles innecesariamente.
- Utilizar el módulo de Presidio para la anonimización de PII/PHI (Información Personal de Salud).

## 3. Inyección (Injection)
- Utilizar Entity Framework Core con parámetros tipados para todas las consultas a la base de datos (evita SQL Injection).
- Nunca concatenar strings directamente en consultas SQL sin procesar.
- Validar y sanitizar todas las entradas de usuario en la capa de Aplicación utilizando `FluentValidation`.

## 4. Diseño Inseguro (Insecure Design)
- Adoptar "Secure by Design". Modelar amenazas antes de implementar nuevas características.
- Implementar validaciones robustas de lógica de negocio (ej. asegurar que productos en cuarentena por DIGEMID no se puedan vender).
- Usar el patrón CQRS para separar comandos (mutaciones) de consultas (lecturas) reduciendo la superficie de ataque en operaciones sensibles.

## 5. Configuración de Seguridad Defectuosa (Security Misconfiguration)
- No exponer detalles de excepciones (StackTrace) en producción.
- Configurar adecuadamente CORS para permitir solo orígenes de confianza.
- Mantener los frameworks y librerías actualizadas.
- Deshabilitar características, servicios o puertos innecesarios.

## 6. Componentes Vulnerables y Desactualizados (Vulnerable and Outdated Components)
- Monitorear dependencias de NuGet y npm para detectar vulnerabilidades conocidas.
- Actualizar componentes de terceros de manera regular y planificada.

## 7. Fallos de Identificación y Autenticación (Identification and Authentication Failures)
- Implementar JWT (JSON Web Tokens) seguros, con tiempos de expiración cortos y mecanismos de rotación/refresco seguros.
- Restringir la cantidad de intentos de inicio de sesión fallidos (Rate Limiting).
- Evitar devolver mensajes detallados en fallos de login (ej. "Usuario no encontrado" vs "Credenciales inválidas").

## 8. Fallos en el Software y la Integridad de los Datos (Software and Data Integrity Failures)
- Utilizar el patrón Outbox (`Sistema_Outbox_Eventos`) para garantizar la consistencia eventual entre microservicios o procesos asíncronos.
- Validar la integridad de los datos recibidos (firmas digitales en recetas).
- Toda entidad de dominio debe ser inmutable en sus campos de auditoría (Creado, Modificado, etc.).

## 9. Fallos en el Registro y Monitoreo de Seguridad (Security Logging and Monitoring Failures)
- Registrar eventos de seguridad críticos (ej. inicios de sesión fallidos, intentos de acceso denegados, modificaciones a roles/permisos) en `Auditoria_Eventos_Seguridad`.
- Asegurar que los logs contengan contexto suficiente (quién, qué, cuándo, desde dónde) pero que no expongan información sensible (contraseñas, tokens).
- Revisar y monitorear proactivamente estos logs.

## 10. Falsificación de Solicitudes del Lado del Servidor (SSRF)
- Si el backend necesita hacer peticiones a URLs proporcionadas por el usuario, validar estrictamente la URL contra una lista blanca.
- Evitar realizar solicitudes directas desde el backend hacia redes internas no confiables.

## Resumen de Aplicación en Código
- **Validadores:** Usar `AbstractValidator` (FluentValidation) exhaustivamente en cada Comando.
- **Autorización:** Aplicar `[HasPermission]` rigurosamente.
- **Auditoría:** Todas las tablas derivan de `IAuditableEntity` con tracking automático de EF Core.
- **Privacidad:** Integrar flujos de desidentificación (Presidio) en `RegistroPrivacidadPresidio` antes de análisis secundarios o IA.
