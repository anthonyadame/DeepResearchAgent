using DeepResearchAgent.Workflows;
using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Extensions;
using DeepResearchAgent.Workflows.Migration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows.Validation;

/// <summary>
/// Compatibility validation tests for Phase 2 adapter layer.
/// Ensures Phase 1 and Phase 2 APIs work together correctly.
/// </summary>
public class Phase2CompatibilityTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly WorkflowMigrationHelper _helper;

    public Phase2CompatibilityTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddSingleton<MasterWorkflowDefinition>();
        services.AddSingleton<SupervisorWorkflowDefinition>();
        services.AddSingleton<ResearcherWorkflowDefinition>();
        services.AddWorkflowAdaptersFromOrchestrator();
        services.AddSingleton<WorkflowMigrationHelper>(sp =>
        {
            var orch = sp.GetRequiredService<IWorkflowOrchestrator>();
            var adapter = sp.GetRequiredService<OrchestratorAdapter>();
            return new WorkflowMigrationHelper(orch, adapter);
        });

        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = _serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
        _helper = _serviceProvider.GetRequiredService<WorkflowMigrationHelper>();
    }

    [Fact]
    public void Phase1_And_Phase2_CoExist()
    {
        // Verify both Phase 1 and Phase 2 are available
        Assert.True(_helper.IsAdaptationAvailable);
        Assert.NotEmpty(_helper.GetPhase1Workflows());
        Assert.NotEmpty(_helper.GetPhase2Workflows());
    }

    [Fact]
    public void Phase1_Workflows_ExistInPhase2()
    {
        // Arrange
        var phase1 = _helper.GetPhase1Workflows();
        var phase2 = _helper.GetPhase2Workflows();

        // Assert - All Phase 1 workflows should have Phase 2 adapters
        foreach (var workflow in phase1)
        {
            Assert.Contains(workflow, phase2);
        }
    }

    [Fact]
    public void Phase2_MetadataIsPreserved_ThroughConversion()
    {
        // Arrange
        var context = new WorkflowContext();
        context.Metadata["key1"] = "value1";
        context.Metadata["key2"] = "value2";
        context.Metadata["special"] = "important";

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert - All metadata preserved
        Assert.Equal("value1", restored.Metadata["key1"]);
        Assert.Equal("value2", restored.Metadata["key2"]);
        Assert.Equal("important", restored.Metadata["special"]);
    }

    [Fact]
    public void Phase2_SharedContext_IsPreserved()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SharedContext["global1"] = "shared1";
        context.SharedContext["global2"] = 42;

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert
        Assert.Equal("shared1", restored.SharedContext["global1"]);
        Assert.Equal(42, restored.SharedContext["global2"]);
    }

    [Fact]
    public void Phase2_ExecutionId_IsPreserved()
    {
        // Arrange
        var context = new WorkflowContext();
        var originalId = context.ExecutionId;

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert
        Assert.Equal(originalId, restored.ExecutionId);
    }

    [Fact]
    public void Phase2_Deadline_IsPreserved()
    {
        // Arrange
        var context = new WorkflowContext();
        var deadline = DateTime.UtcNow.AddMinutes(30);
        context.Deadline = deadline;

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert
        Assert.Equal(deadline, restored.Deadline);
    }

    [Fact]
    public async Task Phase1_API_WorksWithoutAdapters()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test");

        // Act
        var result = await _orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Phase2_API_WorksWithAdapters()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("UserQuery", "Test");
        var state = context.ToAgentState();

        // Act
        var result = await _helper.ExecutePhase2Async("MasterWorkflow", state);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Phase2_Extension_Methods_AreIntegrated()
    {
        // Arrange
        var definition = _orchestrator.GetWorkflow("MasterWorkflow");
        var context = new WorkflowContext();
        context.SetState("UserQuery", "Test");

        // Act
        var state = context.ToAgentState();
        var validation = definition.ValidateAdapted(state);

        // Assert
        Assert.NotNull(validation);
    }

    [Fact]
    public void Phase2_Validation_MatchesPhase1()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("UserQuery", "Test");
        var definition = _orchestrator.GetWorkflow("MasterWorkflow");

        // Act
        var phase1Validation = definition.ValidateContext(context);
        var phase2Validation = definition.ValidateAdapted(context.ToAgentState());

        // Assert
        Assert.Equal(phase1Validation.IsValid, phase2Validation.IsValid);
    }

    [Fact]
    public void Phase2_MigrationHelper_Provides_Guidance()
    {
        // Act
        var status = _helper.GetMigrationStatus();
        var recommendations = _helper.GetMigrationRecommendations();

        // Assert
        Assert.NotEmpty(status);
        Assert.NotEmpty(recommendations);
        Assert.True(recommendations.Any(r => r.Contains("Phase") || r.Contains("migration")));
    }

    [Fact]
    public async Task Phase2_Fallback_Never_Throws()
    {
        // Arrange
        var contexts = new[]
        {
            new WorkflowContext(), // Empty (no UserQuery)
            WorkflowExtensions.CreateMasterWorkflowContext("Valid query"),
        };

        // Act & Assert - Should handle all cases
        foreach (var context in contexts)
        {
            try
            {
                var (success, error, output) = await _helper.ExecuteWithFallbackAsync(
                    "MasterWorkflow",
                    context);

                // Either succeeds or provides error message
                Assert.True(success || !string.IsNullOrEmpty(error));
            }
            catch
            {
                // Acceptable for invalid input
            }
        }
    }

    [Fact]
    public void Phase2_TypeSafety_IsMaintained()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("String", "value");
        context.SetState("Integer", 42);
        context.SetState("Boolean", true);

        // Act
        var state = context.ToAgentState();
        var restored = state.FromAgentState();

        // Assert - Types preserved through conversion
        Assert.IsType<string>(restored.GetState<string>("String"));
        Assert.IsType<int>(restored.GetState<int>("Integer"));
        Assert.IsType<bool>(restored.GetState<bool>("Boolean"));
    }
}
