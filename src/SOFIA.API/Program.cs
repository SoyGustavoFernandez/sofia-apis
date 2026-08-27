using SOFIA.API.Services;
using SOFIA.Application.Common.Interfaces;
using SOFIA.API.Infrastructure;
using SOFIA.Application;
using SOFIA.Infrastructure;
using SOFIA.Domain;
using SOFIA.SharedKernel;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

using System.IdentityModel.Tokens.Jwt;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// --- Structured Logging with Serilog ---
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/sofia-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024));

// --- CORS Configuration ---
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
_ = builder.Services.AddCors(options =>
{
    options.AddPolicy("SofiaCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            _ = policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
        }
        else
        {
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("CRITICAL: 'AllowedOrigins' no está configurado para el entorno de Producción en appsettings.json.");
            }

            _ = policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // OWASP strict
        }
    });
});

// --- Container Services ---
_ = builder.Services.AddControllers();
_ = builder.Services.AddOpenApi();

_ = builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

_ = builder.Services.AddTransient<Microsoft.Extensions.Options.IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>, ConfigureSwaggerOptions>();

_ = builder.Services.AddSwaggerGen(options =>
{
    // JWT Bearer configuration for Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Modern Error Handling
_ = builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
_ = builder.Services.AddProblemDetails();

// --- Rate Limiting ---
_ = builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // AI endpoints (prescription digitization): strict limit due to processing cost
    _ = options.AddFixedWindowLimiter("ai-endpoints", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 2;
    });

    // General API: 200 requests per minute per IP
    _ = options.AddFixedWindowLimiter("general", o =>
    {
        o.PermitLimit = 200;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 10;
    });
});

// Infrastructure Services
_ = builder.Services.AddHttpContextAccessor();

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
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SOFIA API Documentation";
    });
}

_ = app.UseExceptionHandler();
_ = app.UseSerilogRequestLogging();
_ = app.UseHttpsRedirection();

_ = app.UseCors("SofiaCorsPolicy");

_ = app.UseAuthentication();
_ = app.UseAuthorization();
_ = app.UseRateLimiter();

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
