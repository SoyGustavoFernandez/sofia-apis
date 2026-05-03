# Skill: SOFIA Security & Secrets Protection

## Description
Agente responsable de garantizar que ninguna información sensible (claves de API, cadenas de conexión, secretos, tokens, etc.) sea expuesta en el código fuente o persistida en el control de versiones (Git).

## Constraints & Rules
*   **Prohibición de Hardcoding**: Queda estrictamente prohibido escribir cadenas de conexión, contraseñas o claves directamente en el código C# o en archivos `appsettings.json` que se suban al repositorio.
*   **Gestión de Configuración**: 
    *   En desarrollo: Usar `dotnet user-secrets` o variables de entorno locales.
    *   En producción: Usar Azure Key Vault o variables de entorno del servidor.
*   **Validación de .gitignore**: Antes de crear archivos de configuración nuevos, verificar que estén incluidos en el `.gitignore` si contienen datos locales.
*   **Sanitización de Logs**: Nunca incluir contraseñas o tokens en los mensajes de excepción o logs del sistema.
*   **Uso de User-Secrets**: Si se requiere una nueva clave para desarrollo, el agente debe instruir al usuario para que la agregue mediante el comando `dotnet user-secrets set`.

## Instructions for the Agent
1.  **Auditoría Preventiva**: Antes de aplicar cualquier cambio en `SOFIA.Infrastructure` o `SOFIA.API`, escanea el código en busca de patrones que parezcan secretos (ej: `Password=...`, `ApiKey: "..."`).
2.  **Reporte de Vulnerabilidad**: Si detectas un secreto expuesto en el historial o en el código actual, notifica al usuario inmediatamente y sugiere su rotación.
3.  **Configuración Segura**: Al implementar nuevos servicios (ej: Email, Cloud Storage), utiliza siempre `IOptions<T>` para leer la configuración de forma tipada y segura.
