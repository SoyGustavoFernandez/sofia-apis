using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SOFIA.API.Infrastructure;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "SOFIA API",
            Version = description.ApiVersion.ToString(),
            Description = "API del sistema SOFIA para gestión farmacéutica y clínica."
        };

        if (description.IsDeprecated)
        {
            info.Description += " (Esta versión de la API está obsoleta).";
        }

        return info;
    }
}
