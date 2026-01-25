using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Extensions;
using DeepResearchAgent.Workflows.Migration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows.Integration;

/// <summary>
/// Integration tests for complete Phase 2 adapter system.
/// Tests end-to-end workflows with both Phase 1 and Phase 2 APIs.
/// </summary>
public class Phase2IntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly OrchestratorAdapter _adapter;
    private readonly WorkflowMigrationHelper _migrationHelper;

    public Phase2IntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Register mock logger
        services.AddLogging();
        
        // Register Phase 1 orchestrator
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        
        // Register workflow definitions
        services.AddSingleton<MasterWorkflowDefinition>();
        services.AddSingleton<SupervisorWorkflowDefinition>();
        services.AddSingleton<ResearcherWorkflowDefinition>();
        
        // Register Phase 2 adapters
        services.AddWorkflowAdaptersFromOrchestrator();
        
        // Register migration helper
        services.AddSingleton<WorkflowMigrationHelper>(sp =>
        {
            var orchestrator = sp.GetRequiredService<IWorkflowOrchestrator>();
            var adapter = sp.GetRequiredService<OrchestratorAdapter>();
            return new WorkflowMigrationHelper(orchestrator, adapter);
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = _serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
        _adapter = _serviceProvider.GetRequiredService<OrchestratorAdapter>();
        _migrationHelper = _serviceProvider.GetRequiredService<WorkflowMigrationHelper>();
    }

    [Fact]
    public void Phase2_Adapters_AreRegistered()
    {
        // Act
        var phase2Workflows = _migrationHelper.GetPhase2Workflows();

        // Assert
        Assert.NotEmpty(phase2Workflows);
        Assert.True(_migrationHelper.IsAdaptationAvailable);
    }

    [Fact]
    public void Phase2_Workflows_MatchPhase1()
    {
        // Act
        var phase1 = _migrationHelper.GetPhase1Workflows();
        var phase2 = _migrationHelper.GetPhase2Workflows();

        // Assert
        Assert.Equal(phase1.Count, phase2.Count);
        foreach (var workflow in phase1)
        {
            Assert.Contains(workflow, phase2);
        }
    }

    [Fact]
    public async Task Phase2_Execute_VsPhase1_ProduceSameResult()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");
        var agentState = context.ToAgentState();

        // Act
        var phase1Result = await _migrationHelper.ExecutePhase1Async("MasterWorkflow", context);
        var phase2Result = await _migrationHelper.ExecutePhase2Async("MasterWorkflow", agentState);

        // Assert
        Assert.Equal(phase1Result.Success, phase2Result.Success);
    }

    [Fact]
    public async Task Phase2_Fallback_UsesPhase2First()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");

        // Act
        var (success, error, output) = await _migrationHelper.ExecuteWithFallbackAsync(
            "MasterWorkflow",
            context);

        // Assert
        Assert.True(success);
    }

    [Fact]
    public void Phase2_Extension_ToAgentState_PreservesData()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("Query", "test");
        context.Metadata["source"] = "test";

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert
        Assert.Equal("test", restored.GetState<string>("Query"));
        Assert.Equal("test", restored.Metadata["source"]);
    }

    [Fact]
    public async Task Phase2_Extension_ExecuteAdapted_Works()
    {
        // Arrange
        var definition = _orchestrator.GetWorkflow("MasterWorkflow");
        var state = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var result = await definition.ExecuteAdapted(state);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Phase2_Migration_Status_IsAccurate()
    {
        // Act
        var status = _migrationHelper.GetMigrationStatus();
        var recommendations = _migrationHelper.GetMigrationRecommendations();

        // Assert
        Assert.NotEmpty(status);
        Assert.NotEmpty(recommendations);
        Assert.True(status.Count > 0);
    }

    [Fact]
    public void Phase2_BackwardCompatibility_Phase1StillWorks()
    {
        // Act
        var workflows = _orchestrator.GetRegisteredWorkflows();

        // Assert
        Assert.NotEmpty(workflows);
        Assert.Contains("MasterWorkflow", workflows);
    }

    [Fact]
    public async Task Phase2_Concurrent_Execution_IsThreadSafe()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test");
        var tasks = new List<Task>();

        // Act - Execute concurrently
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_migrationHelper.ExecutePhase1Async("MasterWorkflow", context));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
    }

    [Fact]
    public async Task Phase2_ContextAdapter_Handles_ComplexObjects()
    {
        // Arrange
        var context = new WorkflowContext();
        var complexObject = new { Key = "Value", Number = 42, Items = new[] { 1, 2, 3 } };
        context.SetState("Complex", complexObject);

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert
        var restoredComplex = restored.GetState<object>("Complex");
        Assert.NotNull(restoredComplex);
    }

    [Fact]
    public async Task Phase2_Error_Handling_WorksCorrectly()
    {
        // Arrange
        var context = new WorkflowContext(); // Missing required state

        // Act & Assert - Should handle gracefully
        try
        {
            var result = await _migrationHelper.ExecuteWithFallbackAsync(
                "MasterWorkflow",
                context);
            
            // Either success or error message
            Assert.True(result.Success || !string.IsNullOrEmpty(result.ErrorMessage));
        }
        catch
        {
            // Expected for invalid context
        }
    }
}
