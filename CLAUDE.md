# SOFIA Backend – Coding Rules

## Stack
- .NET 9, C# 13
- Clean Architecture: Domain → Application → Infrastructure → API
- MediatR for CQRS (commands/queries)
- FluentValidation for command validation
- Entity Framework Core with SQL Server
- JWT authentication with custom claims

## Architecture rules

### Domain entities
- All entities inherit `BaseEntity` (has `Id: Guid`, `TenantId: Guid?`)
- Use private setters — expose state only via methods (e.g., `Create()`, `Update()`, `Activate()`)
- Domain logic lives in the entity — never in handlers or services
- Use value objects and enums for domain concepts

### Application (CQRS)
- Commands return `Result<T>` or `Result` (use `Result.Success()` / `Result.Failure()`)
- Queries return `IEnumerable<T>` or `T?`
- Always create a `{Command}Validator` for every command using FluentValidation
- Handler: one handler per command/query — `IRequestHandler<TRequest, TResponse>`
- Never access `DbContext` directly from handlers — use repository interfaces or `IApplicationDbContext`

### Validation
- All validators use `AbstractValidator<T>`
- Required fields: `.NotEmpty()`
- String lengths: `.MaximumLength(n)`
- Regex: `.Matches(pattern)`
- Cross-field: use `RuleFor(...).Must(...)`

### Error codes
- Return error codes as dotted namespaced strings: `"Entity.Field.Reason"` (e.g., `"Empresa.RUC.Duplicado"`)
- These codes are used as i18n keys on the frontend — keep consistent

### Controllers
- Thin controllers — delegate everything to MediatR
- Use `[HasPermission("Resource", "Action")]` attribute for authorization
- Public endpoints use `[AllowAnonymous]`
- Return `Ok(value)` on success, `Problem(...)` on failure

### Database
- All EF configurations in `Configurations/` via `IEntityTypeConfiguration<T>`
- Use `HasQueryFilter` for soft deletes and tenant filtering
- Never use `SaveChanges()` in domain code — only in infrastructure

### Testing
- Unit tests in `SOFIA.UnitTests` project
- Test handlers, validators, and domain entities
- Use `xUnit` + `FluentAssertions`
- Mock external dependencies with `Moq`
- Naming: `MethodName_Scenario_ExpectedResult`
