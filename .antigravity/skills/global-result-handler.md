# Skill: Global Result & Error Pattern Guard

## Description
Agente responsable de asegurar que SOFIA no utilice excepciones para controlar el flujo de negocio, implementando en su lugar el Result Pattern.

## Constraints & Rules
*   **No Exceptions for Flow**: Queda prohibido lanzar excepciones (`throw`) para errores de validación o lógica de negocio (ej. 'No hay stock'). Se debe devolver un objeto `Result`.
*   **Result Object**: Todo servicio o caso de uso debe devolver un `Result<T>` que contenga: `IsSuccess`, `Value`, `Error` y `StatusCode`.
*   **Global Mapping**: La capa de API debe tener un Middleware que convierta automáticamente estos objetos `Result` en respuestas HTTP estandarizadas (200, 400, 404, 500).
*   **Problem Details**: Los errores deben seguir el estándar RFC 7807 (Problem Details for HTTP APIs).

## Instructions for the Agent
1.  Al crear servicios en `Application`, verifica que el tipo de retorno sea `Task<Result<T>>`.
2.  Si detectas un `throw new Exception()` innecesario, sugiérele al desarrollador usar `Result.Failure(Error.Validation(...))`.
3.  Asegura que todos los mensajes de error sean profesionalmente redactados y amigables para el usuario final de la farmacia.
