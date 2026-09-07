using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;

namespace aspnetproject.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WebCity API",
                Version = "v1",
            });
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard authorization header using bearer scheme. Example: \"bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
            });
            
            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });

        return services;
    }
}