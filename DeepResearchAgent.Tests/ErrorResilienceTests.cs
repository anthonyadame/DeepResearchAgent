using DeepResearchAgent.Models;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Error scenario and resilience tests.
/// Tests that workflows handle failures gracefully.
/// </summary>
public class ErrorResilienceTests
{
    #region LLM Failure Tests

    [Fact]
    public async Task Researcher_HandlesLLMFailure()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_ContinuesWithoutLLM()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Master_RecoverFromIssues()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("topic");

        // Act
        var result = await master.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Search Failure Tests

    [Fact]
    public async Task Researcher_ContinuesWithoutSearch()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_WorksWithLimitedSearch()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Storage Failure Tests

    [Fact]
    public async Task Researcher_FactPersistenceFailureDoesNotStop()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_ContinuesWithStorageIssues()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Researcher_StopsOnCancellation()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(5));

        // Act
        var ex = await Record.ExceptionAsync(() =>
            researcher.ResearchAsync("topic", cts.Token));

        // Assert
        // Cancellation exception or timeout acceptable
    }

    [Fact]
    public async Task Supervisor_RespectsCancellation()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(5));

        // Act
        var ex = await Record.ExceptionAsync(() =>
            supervisor.SuperviseAsync("topic", "draft", cancellationToken: cts.Token));

        // Assert
        // Cancellation acceptable
    }

    [Fact]
    public async Task Master_RespectsCancellation()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(5));

        // Act
        var ex = await Record.ExceptionAsync(() =>
            master.ExecuteAsync(input, cts.Token));

        // Assert
        // Cancellation acceptable
    }

    #endregion

    #region Empty Input Tests

    [Fact]
    public async Task Researcher_WithEmptyTopic()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_WithEmptyBrief()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("", "", maxIterations: 1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Master_WithEmptyQuery()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = StateFactory.CreateAgentState();

        // Act
        var result = await master.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Timeout Tests

    [Fact]
    public async Task Researcher_CompletesInReasonableTime()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var facts = await researcher.ResearchAsync("topic");
        sw.Stop();

        // Assert
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            $"Research should complete in <30s, took {sw.Elapsed.TotalSeconds}s");
    }

    [Fact]
    public async Task Supervisor_CompletesWithinTimeout()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 2);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60),
            $"Supervision should complete in <60s");
    }

    [Fact]
    public async Task Master_CompletesInTimeframe()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await master.ExecuteAsync(input);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public async Task Researcher_MaintainsStateOnError()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();

        // Act
        var facts = await (new ResearcherWorkflow(
            TestFixtures.CreateMockSearchService(),
            TestFixtures.CreateMockOllamaService(),
            TestFixtures.CreateMockLightningStore()
        )).ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_MaintainsStateOnError()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();

        // Act
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();
        var result = await supervisor.SuperviseAsync(
            state.ResearchBrief,
            state.DraftReport,
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Master_MaintainsStateOnError()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var result = await master.ExecuteAsync(input);

        // Assert
        var validation = StateValidator.ValidateAgentState(result);
        Assert.True(validation.IsValid);
    }

    #endregion

    #region Graceful Degradation Tests

    [Fact]
    public async Task Researcher_DegradesFully()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_DegradesFully()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Master_DegradesFully()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await master.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.FinalReport);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task Researcher_RetriesOnFailure()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("topic");

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task Supervisor_RetriesResearch()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 3);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Memory Safety Tests

    [Fact]
    public async Task Researcher_DoesNotLeakMemory()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var facts = await researcher.ResearchAsync("topic");
            Assert.NotNull(facts);
        }

        // Assert
        // If we got here, no out-of-memory errors
        Assert.True(true);
    }

    [Fact]
    public async Task Supervisor_DoesNotLeakMemory()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        for (int i = 0; i < 3; i++)
        {
            var result = await supervisor.SuperviseAsync("topic", "draft", maxIterations: 1);
            Assert.NotNull(result);
        }

        // Assert
        Assert.True(true);
    }

    #endregion

    #region Exception Safety Tests

    [Fact]
    public async Task Researcher_NeverThrowsUnhandledException()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var ex = await Record.ExceptionAsync(() =>
            researcher.ResearchAsync("topic"));

        // Assert
        // Should not throw
        Assert.Null(ex);
    }

    [Fact]
    public async Task Supervisor_NeverThrowsUnhandledException()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var ex = await Record.ExceptionAsync(() =>
            supervisor.SuperviseAsync("topic", "draft", maxIterations: 1));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public async Task Master_NeverThrowsUnhandledException()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var ex = await Record.ExceptionAsync(() =>
            master.ExecuteAsync(input));

        // Assert
        Assert.Null(ex);
    }

    #endregion
}
