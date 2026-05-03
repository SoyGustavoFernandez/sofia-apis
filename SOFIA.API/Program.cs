using SOFIA.API.Middleware;
using SOFIA.Application;
using SOFIA.Infrastructure;
using SOFIA.Domain;
using SOFIA.SharedKernel;
using SOFIA.API.Services;
using SOFIA.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Container Services ---
_ = builder.Services.AddControllers();
_ = builder.Services.AddOpenApi();
_ = builder.Services.AddEndpointsApiExplorer();
_ = builder.Services.AddSwaggerGen();

// Clean Architecture Layers Registration
_ = builder.Services.AddScoped<ICurrentUser, CurrentUser>();
_ = builder.Services.AddSharedKernel();
_ = builder.Services.AddDomain();
_ = builder.Services.AddApplication();
_ = builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// --- HTTP Request Pipeline ---
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SOFIA API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SOFIA API Documentation";
    });
}

_ = app.UseMiddleware<ExceptionMiddleware>();
_ = app.UseHttpsRedirection();

_ = app.MapControllers();

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
