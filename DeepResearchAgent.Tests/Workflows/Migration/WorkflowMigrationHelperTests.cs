using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Migration;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows.Migration;

/// <summary>
/// Unit tests for WorkflowMigrationHelper.
/// Tests gradual migration patterns and fallback mechanisms.
/// </summary>
public class WorkflowMigrationHelperTests
{
    [Fact]
    public void Helper_IdentifiesAdaptationAvailability()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator();
        var adapter = new OrchestratorAdapter(mockOrchestrator);
        var helper = new WorkflowMigrationHelper(mockOrchestrator, adapter);

        // Act & Assert
        Assert.True(helper.IsAdaptationAvailable);
    }

    [Fact]
    public void Helper_IdentifiesNoAdaptation()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator();
        var helper = new WorkflowMigrationHelper(mockOrchestrator, null);

        // Act & Assert
        Assert.False(helper.IsAdaptationAvailable);
    }

    [Fact]
    public void Helper_ListsPhase1Workflows()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master", "Supervisor");
        var helper = new WorkflowMigrationHelper(mockOrchestrator);

        // Act
        var workflows = helper.GetPhase1Workflows();

        // Assert
        Assert.Equal(2, workflows.Count);
        Assert.Contains("Master", workflows);
    }

    [Fact]
    public void Helper_ListsPhase2Workflows()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var adapter = new OrchestratorAdapter(mockOrchestrator);
        var helper = new WorkflowMigrationHelper(mockOrchestrator, adapter);

        // Act
        var workflows = helper.GetPhase2Workflows();

        // Assert
        Assert.Single(workflows);
        Assert.Contains("Master", workflows);
    }

    [Fact]
    public void Helper_ReturnsEmptyPhase2_WithoutAdapter()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var helper = new WorkflowMigrationHelper(mockOrchestrator, null);

        // Act
        var workflows = helper.GetPhase2Workflows();

        // Assert
        Assert.Empty(workflows);
    }

    [Fact]
    public async Task Helper_ExecutesPhase1()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var helper = new WorkflowMigrationHelper(mockOrchestrator);
        var context = new WorkflowContext();

        // Act
        var result = await helper.ExecutePhase1Async("Master", context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Helper_ExecutesPhase2()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var adapter = new OrchestratorAdapter(mockOrchestrator);
        var helper = new WorkflowMigrationHelper(mockOrchestrator, adapter);
        var agentState = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var result = await helper.ExecutePhase2Async("Master", agentState);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Helper_Phase2ThrowsWithoutAdapter()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var helper = new WorkflowMigrationHelper(mockOrchestrator, null);
        var agentState = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.ExecutePhase2Async("Master", agentState));
    }

    [Fact]
    public async Task Helper_FallbackUsesPhase1()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var helper = new WorkflowMigrationHelper(mockOrchestrator, null);
        var context = new WorkflowContext();

        // Act
        var (success, error, output) = await helper.ExecuteWithFallbackAsync("Master", context);

        // Assert
        Assert.True(success);
    }

    [Fact]
    public void Helper_GetsMigrationStatus()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master", "Supervisor");
        var adapter = new OrchestratorAdapter(mockOrchestrator);
        var helper = new WorkflowMigrationHelper(mockOrchestrator, adapter);

        // Act
        var status = helper.GetMigrationStatus();

        // Assert
        Assert.NotEmpty(status);
        Assert.True(status.ContainsKey("Master"));
        Assert.True(status.ContainsKey("Supervisor"));
    }

    [Fact]
    public void Helper_GetsMigrationRecommendations()
    {
        // Arrange
        var mockOrchestrator = CreateMockOrchestrator("Master");
        var helper = new WorkflowMigrationHelper(mockOrchestrator, null);

        // Act
        var recommendations = helper.GetMigrationRecommendations();

        // Assert
        Assert.NotEmpty(recommendations);
        Assert.True(recommendations.Any(r => r.Contains("adapter")));
    }

    [Fact]
    public void Helper_ThrowsOnNullOrchestrator()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowMigrationHelper(null));
    }

    #region Helper Methods

    private IWorkflowOrchestrator CreateMockOrchestrator(params string[] workflowNames)
    {
        var mock = new Mock<IWorkflowOrchestrator>();
        mock.Setup(o => o.GetRegisteredWorkflows()).Returns(workflowNames.ToList());

        foreach (var name in workflowNames)
        {
            var def = CreateMockWorkflowDefinition(name);
            mock.Setup(o => o.GetWorkflow(name)).Returns(def);
        }

        mock.Setup(o => o.ExecuteWorkflowAsync(It.IsAny<string>(), It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowExecutionResult { Success = true });

        return mock.Object;
    }

    private IWorkflowDefinition CreateMockWorkflowDefinition(string name)
    {
        var mock = new Mock<IWorkflowDefinition>();
        mock.Setup(w => w.WorkflowName).Returns(name);
        mock.Setup(w => w.Description).Returns($"Description for {name}");
        mock.Setup(w => w.ValidateContext(It.IsAny<WorkflowContext>()))
            .Returns(new ValidationResult { IsValid = true });
        mock.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowExecutionResult { Success = true });

        return mock.Object;
    }

    #endregion
}
