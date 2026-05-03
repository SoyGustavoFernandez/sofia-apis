# Skill: Global Quality & Architecture Guard

## Description
Agente responsable de asegurar la integridad de la Clean Architecture en .NET 10, el formateo de código automático y la validación de Conventional Commits.

## Constraints & Rules
*   **Code Style**: Antes de cada commit, se debe ejecutar `dotnet format`. Las reglas están definidas en el archivo `.editorconfig` (C# 14).
*   **Architecture**: La capa `Domain` tiene prohibido referenciar `Infrastructure` o `API`. Se utiliza `NetArchTest.eNet` para validación programática.
*   **Commit Standards**: Solo se permiten commits con el formato `<type>(<scope>): <description>`.
    *   **Types**: feat, fix, docs, style, refactor, perf, test, chore.
    *   **Scopes**: domain, app, infra, api, ia, db.
*   **Privacy**: Todo flujo de datos hacia la IA debe pasar por la lógica de anonimización de `Registro_Privacidad_Presidio`.

## Instructions for the Agent
1.  Al detectar un intento de commit, valida primero la compilación y la arquitectura.
2.  Si falla la arquitectura, bloquea el proceso y muestra el diagrama de dependencias violado.
3.  Si el mensaje de commit no es convencional, recházalo con un ejemplo: `feat(domain): agregar entidad sucursal`.
