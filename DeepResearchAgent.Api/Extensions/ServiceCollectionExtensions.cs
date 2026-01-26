namespace DeepResearchAgent.Api.Extensions;

using DTOs.Validators;
using Services;
using Services.Implementations;
using FluentValidation;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for service registration in Dependency Injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all API services, validators, and mappings to the DI container.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<MasterWorkflowRequestValidator>();

        // Register orchestration service implementations
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<ICoreService, CoreService>();
        services.AddScoped<IHealthService, HealthService>();

        return services;
    }

    /// <summary>
    /// Add CORS configuration for API.
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, string policyName = "ApiCorsPolicy")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Add Swagger/OpenAPI documentation.
    /// </summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Deep Research Agent API",
                Version = "v1.0",
                Description = "Comprehensive API for research automation with 5-tier architecture",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Deep Research Agent Team"
                }
            });

            // Include XML comments from DTOs
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Add request/response compression.
    /// </summary>
    public static IServiceCollection AddApiCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }

    /// <summary>
    /// Add health checks.
    /// </summary>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}
