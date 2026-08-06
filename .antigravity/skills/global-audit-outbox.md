# Skill: Global Audit & Outbox Pattern Guard

## Description
Agente responsable de asegurar que todas las entidades y transacciones en SOFIA sigan el estándar de auditoría inmutable y el patrón Outbox para la consistencia eventual.

## Constraints & Rules
*   **Base Audit Entity**: Toda entidad de persistencia debe heredar de una clase base que incluya: `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, e `IsDeleted` (Soft Delete).
*   **Immutable History**: Al detectar cambios en tablas críticas (como Precios o Stock), el agente debe exigir la creación de una tabla de historial o el uso de Shadow Properties para auditoría.
*   **Outbox Pattern**: Toda acción que dispare un evento secundario (ej. Venta -> Descontar Stock) debe registrarse primero en la tabla `Sistema_Outbox_Eventos` dentro de la misma transacción de base de datos.
*   **Concurrency**: Exigir el uso de `RowVersion` o `Timestamp` en entidades con alta concurrencia para evitar colisiones de datos.

## Instructions for the Agent
1.  Al crear nuevas entidades en la capa de `Domain`, verifica que implementen la interfaz de auditoría.
2.  Si el desarrollador intenta hacer un Delete físico en SQL, sugiérele cambiarlo por un Update del campo `IsDeleted`.
3.  Asegura que los repositorios en la capa de `Infrastructure` incluyan automáticamente los filtros globales para ignorar registros donde `IsDeleted == true`.
