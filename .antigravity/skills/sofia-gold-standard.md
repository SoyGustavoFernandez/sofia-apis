# Skill: SOFIA Gold Standard

## Description
Agente responsable de asegurar que SOFIA sea un sistema artesanal de alta calidad, siguiendo principios estrictos de Clean Architecture, Result Pattern y auditoría inmutable.

## Constraints & Rules
*   **Integridad de Dominio**: Prohibido que `SOFIA.Domain` referencie a otros proyectos o librerías de persistencia. El dominio es el corazón puro del negocio.
*   **Result Pattern Obligatorio**: Queda prohibido usar `throw` para errores de negocio (ej. 'Stock insuficiente'). Todo flujo de error debe ser comunicado mediante un objeto `Result<T>`.
*   **Auditoría SQL**: Todas las entidades deben heredar de `BaseEntity` y soportar los campos de auditoría definidos en el modelo relacional: `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`, `DeletedAt` y `DeletedBy`.
*   **Tipado Estricto**: Uso de `DateTimeOffset` para todas las marcas temporales para asegurar consistencia multiregional.
*   **Estilo de Código**: Cumplimiento estricto de `.editorconfig` (file-scoped namespaces, `var` preferido cuando el tipo es evidente).

## Instructions for the Agent
1.  Al generar entidades, mapea exactamente los nombres de las columnas del script SQL a las propiedades de C#.
2.  Asegura que las relaciones entre entidades (Navegación) sean consistentes con las Foreign Keys del SQL.
3.  Valida que ningún error de lógica de negocio se escape como una excepción; debe ser capturado en un `Error.Validation` o similar.
4.  Mantén el proyecto `SOFIA.ArchitectureTests` actualizado para reflejar estas reglas de hierro.
