using SOFIA.API.Middleware;
using SOFIA.Application;
using SOFIA.Infrastructure;
using SOFIA.Domain;
using SOFIA.SharedKernel;

var builder = WebApplication.CreateBuilder(args);

// --- Container Services ---
_ = builder.Services.AddOpenApi(); // Native .NET 9/10 support
_ = builder.Services.AddEndpointsApiExplorer();
_ = builder.Services.AddSwaggerGen();

// Clean Architecture Layers Registration
_ = builder.Services.AddSharedKernel();
_ = builder.Services.AddDomain();
_ = builder.Services.AddApplication();
_ = builder.Services.AddInfrastructure();

var app = builder.Build();

// --- HTTP Request Pipeline ---
if (app.Environment.IsDevelopment())
{
    // Enable native OpenAPI (json) endpoint at /openapi/v1.json
    _ = app.MapOpenApi();

    // Enable Swagger Middleware
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SOFIA API v1");
        options.RoutePrefix = "swagger"; // Access at http://localhost:PORT/swagger
        options.DocumentTitle = "SOFIA API Documentation";
    });
}

_ = app.UseMiddleware<ExceptionMiddleware>();
_ = app.UseHttpsRedirection();

// Health Check Endpoint
_ = app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Version = "1.0.0",
    Timestamp = DateTimeOffset.UtcNow,
    Environment = app.Environment.EnvironmentName
}))
.WithName("GetHealth");

app.Run();
