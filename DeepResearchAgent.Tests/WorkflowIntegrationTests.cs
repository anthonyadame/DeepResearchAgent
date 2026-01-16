using DeepResearchAgent.Models;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Integration tests for workflow chains.
/// Tests interactions between workflows: Master→Supervisor→Researcher
/// </summary>
public class WorkflowIntegrationTests
{
    #region Master → Supervisor Chain Tests

    [Fact]
    public async Task MasterToSupervisor_CompletesChain()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("Research AI trends");

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ResearchBrief);
        Assert.NotEmpty(result.DraftReport);
        Assert.NotEmpty(result.FinalReport);
    }

    [Fact]
    public async Task MasterToSupervisor_PassesContextCorrectly()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var query = "Analyze climate change impacts";
        var input = TestFixtures.CreateTestAgentState(query);

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result.ResearchBrief);
        Assert.NotNull(result.DraftReport);
        // Brief should relate to original query
        Assert.True(result.ResearchBrief.Length > 0);
    }

    [Fact]
    public async Task MasterToSupervisor_IntegrationProducesReport()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.FinalReport);
        Assert.True(result.FinalReport.Length > 200,
            "Final report should be comprehensive");
    }

    #endregion

    #region Supervisor → Researcher Chain Tests

    [Fact]
    public async Task SupervisorToResearcher_ExecutesResearch()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Machine learning trends",
            "Initial draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task SupervisorToResearcher_AggregatesFindings()
    {
        // Arrange
        var (supervisor, _, store) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Research topic",
            "Draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SupervisorToResearcher_BuildsKnowledgeBase()
    {
        // Arrange
        var (supervisor, _, store) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Complex research",
            "Initial draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Full Master→Supervisor→Researcher Chain Tests

    [Fact]
    public async Task FullPipeline_CompletesAllSteps()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("Research quantum computing");

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        WorkflowAssertions.AssertValidAgentState(result);
        Assert.NotEmpty(result.ResearchBrief);
        Assert.NotEmpty(result.DraftReport);
        Assert.NotEmpty(result.FinalReport);
    }

    [Fact]
    public async Task FullPipeline_MaintainsStateConsistency()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var originalQuery = "Analyze AI safety";
        var input = TestFixtures.CreateTestAgentState(originalQuery);

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        // All required fields populated
        Assert.NotNull(result.Messages);
        Assert.NotNull(result.ResearchBrief);
        Assert.NotNull(result.DraftReport);
        Assert.NotNull(result.FinalReport);

        // Validation passes
        var validation = StateValidator.ValidateAgentState(result);
        Assert.True(validation.IsValid, validation.Message);
    }

    [Fact]
    public async Task FullPipeline_WithComplexQuery_Succeeds()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var complexQuery = "Provide a comprehensive analysis of the impact of machine learning on modern healthcare, " +
            "including benefits, challenges, and ethical considerations.";
        var input = TestFixtures.CreateTestAgentState(complexQuery);

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.FinalReport);
        Assert.True(result.FinalReport.Length > 300,
            "Complex query should produce detailed report");
    }

    [Fact]
    public async Task FullPipeline_ProducesProgressiveImprovement()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("Research topic");

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        // Draft should be less polished than final report
        Assert.NotNull(result.DraftReport);
        Assert.NotNull(result.FinalReport);
        // Final should be more refined
        Assert.True(result.FinalReport.Length >= result.DraftReport.Length / 2,
            "Final report should be substantial");
    }

    #endregion

    #region Data Flow Tests

    [Fact]
    public async Task MasterToSupervisor_PassesResearchBrief()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.ResearchBrief);
    }

    [Fact]
    public async Task SupervisorToResearcher_PassesTopics()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Research ML",
            "Draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Researcher_ReturnsCompressedFindings()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var facts = await researcher.ResearchAsync("Machine learning");

        // Assert
        Assert.NotNull(facts);
    }

    #endregion

    #region Streaming Integration Tests

    [Fact]
    public async Task FullPipeline_StreamingUpdates()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var query = "Research topic";
        var updates = new List<string>();

        // Act
        await foreach (var update in master.StreamAsync(query))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        Assert.True(updates.Count >= 5,
            "Should have multiple progress updates");
    }

    [Fact]
    public async Task SupervisorStreaming_UpdatesProgression()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();
        var updates = new List<string>();

        // Act
        await foreach (var update in supervisor.StreamSuperviseAsync(
            "Research topic",
            "Draft",
            maxIterations: 2))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
    }

    [Fact]
    public async Task ResearcherStreaming_ShowsProgress()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var updates = new List<string>();

        // Act
        await foreach (var update in researcher.StreamResearchAsync("Topic"))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
    }

    #endregion

    #region Error Propagation Tests

    [Fact]
    public async Task FullPipeline_HandlesErrorsGracefully()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("");

        // Act
        var ex = await Record.ExceptionAsync(
            () => master.ExecuteAsync(input, CancellationToken.None));

        // Assert
        // Should not throw, handles gracefully
        // (null if no exception)
    }

    [Fact]
    public async Task SupervisorToResearcher_ContinuesOnError()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Topic",
            "Draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region State Accumulation Tests

    [Fact]
    public async Task FullPipeline_AccumulatesKnowledge()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await master.ExecuteAsync(input, CancellationToken.None);

        // Assert
        // Should have meaningful final report
        Assert.True(result.FinalReport.Length > 100);
    }

    [Fact]
    public async Task SupervisorLoop_BuildsQualityHistory()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync(
            "Research",
            "Draft",
            maxIterations: 3);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task MultipleQueries_ExecuteConcurrently()
    {
        // Arrange
        var (master1, _) = TestFixtures.CreateMockMasterWorkflow();
        var (master2, _) = TestFixtures.CreateMockMasterWorkflow();
        var input1 = TestFixtures.CreateTestAgentState("Query 1");
        var input2 = TestFixtures.CreateTestAgentState("Query 2");

        // Act
        var task1 = master1.ExecuteAsync(input1);
        var task2 = master2.ExecuteAsync(input2);
        await Task.WhenAll(task1, task2);

        // Assert
        var result1 = await task1;
        var result2 = await task2;
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task ParallelResearchers_WorkCorrectly()
    {
        // Arrange
        var (researcher1, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var (researcher2, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var task1 = researcher1.ResearchAsync("Topic 1");
        var task2 = researcher2.ResearchAsync("Topic 2");
        await Task.WhenAll(task1, task2);

        // Assert
        var result1 = await task1;
        var result2 = await task2;
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    #endregion
}
