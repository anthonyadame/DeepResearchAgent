using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Extensions;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows.Extensions;

/// <summary>
/// Unit tests for AdapterExtensions.
/// Tests fluent API and extension methods for adapters.
/// </summary>
public class AdapterExtensionsTests
{
    [Fact]
    public void ToAgentState_ConvertsContextToState()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("Query", "test");

        // Act
        var state = context.ToAgentState();

        // Assert
        Assert.NotNull(state);
        Assert.Equal("test", state["Query"]);
    }

    [Fact]
    public void FromAgentState_ConvertsStateToContext()
    {
        // Arrange
        var state = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var context = state.FromAgentState();

        // Assert
        Assert.NotNull(context);
        Assert.Equal("test", context.GetState<string>("Query"));
    }

    [Fact]
    public void AsAdapted_CreatesAdapter()
    {
        // Arrange
        var mockDef = CreateMockWorkflowDefinition("Test");

        // Act
        var adapter = mockDef.AsAdapted();

        // Assert
        Assert.NotNull(adapter);
        Assert.Equal("Test", adapter.WorkflowName);
    }

    [Fact]
    public void CreateAdaptedContext_ConfiguresAndConverts()
    {
        // Arrange
        var mockDef = CreateMockWorkflowDefinition("Test");

        // Act
        var state = mockDef.CreateAdaptedContext(ctx =>
        {
            ctx.SetState("Query", "test");
            ctx.Metadata["source"] = "test";
        });

        // Assert
        Assert.NotNull(state);
        Assert.Equal("test", state["Query"]);
    }

    [Fact]
    public async Task ExecuteAdapted_ExecutesWorkflow()
    {
        // Arrange
        var mockDef = CreateMockWorkflowDefinition("Test");
        var agentState = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var result = await mockDef.ExecuteAdapted(agentState);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task StreamAdapted_StreamsWorkflow()
    {
        // Arrange
        var mockDef = CreateMockWorkflowDefinition("Test");
        var agentState = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var updates = new List<WorkflowAdapterUpdate>();
        await foreach (var update in mockDef.StreamAdapted(agentState))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
    }

    [Fact]
    public void ValidateAdapted_ValidatesContext()
    {
        // Arrange
        var mockDef = CreateMockWorkflowDefinition("Test");
        var agentState = new Dictionary<string, object> { { "Query", "test" } };

        // Act
        var validation = mockDef.ValidateAdapted(agentState);

        // Assert
        Assert.NotNull(validation);
    }

    #region Helper Methods

    private IWorkflowDefinition CreateMockWorkflowDefinition(string name)
    {
        var mock = new Mock<IWorkflowDefinition>();
        mock.Setup(w => w.WorkflowName).Returns(name);
        mock.Setup(w => w.Description).Returns($"Description for {name}");
        mock.Setup(w => w.ValidateContext(It.IsAny<WorkflowContext>()))
            .Returns(new ValidationResult { IsValid = true });
        mock.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowExecutionResult { Success = true, Output = "Output" });
        mock.Setup(w => w.StreamExecutionAsync(It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
            .Returns(CreateMockUpdates());

        return mock.Object;
    }

    private async IAsyncEnumerable<WorkflowUpdate> CreateMockUpdates()
    {
        yield return new WorkflowUpdate { Type = WorkflowUpdateType.StepStarted, Content = "Starting" };
        yield return new WorkflowUpdate { Type = WorkflowUpdateType.Completed, Content = "Done" };
    }

    #endregion
}
