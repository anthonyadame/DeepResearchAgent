using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace DeepResearchAgent.Workflows.Extensions;

/// <summary>
/// Extension methods for registering workflow adapters in DI container.
/// Enables gradual migration from Phase 1 to Phase 2 preview APIs.
/// </summary>
public static class AdapterRegistrationExtensions
{
    /// <summary>
    /// Register all workflow adapters for Phase 2 migration.
    /// Supports both Phase 1 (custom) and Phase 2 (preview) patterns.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="useAdapters">If true, register adapters; if false, use original implementations</param>
    public static IServiceCollection AddWorkflowAdapters(
        this IServiceCollection services,
        bool useAdapters = true)
    {
        if (!useAdapters)
        {
            // Phase 1: Keep original implementation
            return services;
        }

        // Register adapter layer
        services.AddSingleton<WorkflowContextAdapter>();
        services.AddSingleton<WorkflowDefinitionAdapter>();
        services.AddSingleton<OrchestratorAdapter>();

        return services;
    }

    /// <summary>
    /// Register workflow adapters with factory method for orchestrator.
    /// Allows creating adapters from existing orchestrator instance.
    /// </summary>
    public static IServiceCollection AddWorkflowAdaptersFromOrchestrator(
        this IServiceCollection services)
    {
        services.AddSingleton<OrchestratorAdapter>(provider =>
        {
            var orchestrator = provider.GetRequiredService<IWorkflowOrchestrator>();
            return new OrchestratorAdapter(orchestrator);
        });

        return services;
    }

    /// <summary>
    /// Add both Phase 1 (original) and Phase 2 (adapter) implementations.
    /// Enables gradual migration with feature flags.
    /// </summary>
    public static IServiceCollection AddDualWorkflowSupport(
        this IServiceCollection services)
    {
        // Register Phase 1
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddSingleton<WorkflowPipelineOrchestrator>();

        // Register Phase 1 definitions
        services.AddSingleton<MasterWorkflowDefinition>();
        services.AddSingleton<SupervisorWorkflowDefinition>();
        services.AddSingleton<ResearcherWorkflowDefinition>();

        // Register Phase 2 adapters
        services.AddWorkflowAdaptersFromOrchestrator();

        return services;
    }
}
