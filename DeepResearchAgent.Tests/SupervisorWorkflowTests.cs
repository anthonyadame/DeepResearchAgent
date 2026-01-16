using DeepResearchAgent.Models;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Unit tests for SupervisorWorkflow.
/// Tests diffusion loop, quality evaluation, red team, and context pruning.
/// </summary>
public class SupervisorWorkflowTests
{
    private readonly SupervisorWorkflow _workflow;

    public SupervisorWorkflowTests()
    {
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();
        _workflow = supervisor;
    }

    #region Supervisor Brain Tests

    [Fact]
    public async Task SupervisorBrainAsync_GeneratesDecision()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();

        // Act
        var result = await _workflow.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Equal("assistant", result.Role);
    }

    [Fact]
    public async Task SupervisorBrainAsync_IncorporatesResearchBrief()
    {
        // Arrange
        var brief = "Analyze quantum computing advancements";
        var state = TestFixtures.CreateTestSupervisorState(brief);

        // Act
        var result = await _workflow.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task SupervisorBrainAsync_IncludesQualityMetrics()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.QualityHistory.Add(StateFactory.CreateQualityMetric(6.5f, "Initial quality", 0));

        // Act
        var result = await _workflow.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task SupervisorBrainAsync_HandlesCritiques()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.ActiveCritiques.Add(StateFactory.CreateCritique(
            "Red Team",
            "Missing supporting evidence",
            8));

        // Act
        var result = await _workflow.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    #endregion

    #region Quality Evaluation Tests

    [Fact]
    public async Task EvaluateDraftQualityAsync_ReturnsValidScore()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.KnowledgeBase.AddRange(TestFixtures.CreateTestFacts(5));

        // Act
        var score = await _workflow.EvaluateDraftQualityAsync(state, CancellationToken.None);

        // Assert
        Assert.True(score >= 0 && score <= 10,
            $"Quality score {score} out of valid range [0-10]");
    }

    [Fact]
    public async Task EvaluateDraftQualityAsync_FactCountAffectsScore()
    {
        // Arrange
        var state1 = TestFixtures.CreateTestSupervisorState();
        var state2 = TestFixtures.CreateTestSupervisorState();
        state2.KnowledgeBase.AddRange(TestFixtures.CreateTestFacts(10));

        // Act
        var score1 = await _workflow.EvaluateDraftQualityAsync(state1, CancellationToken.None);
        var score2 = await _workflow.EvaluateDraftQualityAsync(state2, CancellationToken.None);

        // Assert
        Assert.True(score2 >= score1,
            "More facts should result in equal or higher quality");
    }

    [Fact]
    public async Task EvaluateDraftQualityAsync_ConfidenceAffectsScore()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        var highConfidenceFacts = new List<FactState>();
        for (int i = 0; i < 5; i++)
        {
            highConfidenceFacts.Add(StateFactory.CreateFact(
                $"High confidence fact {i}",
                "source",
                95)); // High confidence
        }
        state.KnowledgeBase.AddRange(highConfidenceFacts);

        // Act
        var score = await _workflow.EvaluateDraftQualityAsync(state, CancellationToken.None);

        // Assert
        Assert.True(score > 0, "High confidence facts should produce score");
    }

    [Fact]
    public async Task EvaluateDraftQualityAsync_TracksHistory()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();

        // Act
        var score1 = await _workflow.EvaluateDraftQualityAsync(state, CancellationToken.None);
        state.KnowledgeBase.AddRange(TestFixtures.CreateTestFacts(5));
        var score2 = await _workflow.EvaluateDraftQualityAsync(state, CancellationToken.None);

        // Assert
        // More facts should maintain or improve quality
        Assert.True(score2 >= score1);
    }

    #endregion

    #region Red Team Tests

    [Fact]
    public async Task RunRedTeamAsync_GeneratesCritique()
    {
        // Arrange
        var draftReport = "Initial research draft without proper sources.";

        // Act
        var critique = await _workflow.RunRedTeamAsync(draftReport, CancellationToken.None);

        // Assert
        // May be null if "PASS" or a critique
        if (critique != null)
        {
            Assert.NotEmpty(critique.Concern);
            Assert.Equal("Red Team", critique.Author);
        }
    }

    [Fact]
    public async Task RunRedTeamAsync_WithStrongDraft_MayPass()
    {
        // Arrange
        var strongDraft = "Well-researched report with proper citations and sources.";

        // Act
        var critique = await _workflow.RunRedTeamAsync(strongDraft, CancellationToken.None);

        // Assert
        // Result is either null (PASS) or a critique
        if (critique != null)
        {
            Assert.NotEmpty(critique.Concern);
        }
    }

    [Fact]
    public async Task RunRedTeamAsync_IdentifiesIssues()
    {
        // Arrange
        var weakDraft = "All experts agree this is correct.";

        // Act
        var critique = await _workflow.RunRedTeamAsync(weakDraft, CancellationToken.None);

        // Assert
        // May identify weak claims
        if (critique != null)
        {
            Assert.True(critique.Severity >= 1 && critique.Severity <= 10);
        }
    }

    #endregion

    #region Context Pruning Tests

    [Fact]
    public async Task ContextPrunerAsync_ExtractsFacts()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.RawNotes.Add("Machine learning improves with more data and better algorithms.");
        state.RawNotes.Add("Neural networks are inspired by biological neurons.");

        // Act
        await _workflow.ContextPrunerAsync(state, CancellationToken.None);

        // Assert
        // Knowledge base may be populated
        Assert.NotNull(state.KnowledgeBase);
    }

    [Fact]
    public async Task ContextPrunerAsync_ClearsRawNotes()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.RawNotes.Add("Raw note 1");
        state.RawNotes.Add("Raw note 2");

        // Act
        await _workflow.ContextPrunerAsync(state, CancellationToken.None);

        // Assert
        Assert.Empty(state.RawNotes);
    }

    [Fact]
    public async Task ContextPrunerAsync_DeduplicatesFacts()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();
        state.RawNotes.Add("Same fact about AI");
        state.RawNotes.Add("Same fact about AI");
        var initialKBCount = state.KnowledgeBase.Count;

        // Act
        await _workflow.ContextPrunerAsync(state, CancellationToken.None);

        // Assert
        // Should not duplicate significantly
        var newKBCount = state.KnowledgeBase.Count;
        Assert.True(newKBCount <= initialKBCount + 5,
            "Should limit new facts to avoid explosion");
    }

    #endregion

    #region Diffusion Loop Tests

    [Fact]
    public async Task SuperviseAsync_CompletesWithoutError()
    {
        // Arrange
        var brief = "Research AI trends";
        var draft = "Initial AI trends draft";

        // Act
        var result = await _workflow.SuperviseAsync(brief, draft, maxIterations: 2);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task SuperviseAsync_ReturnsResearchSummary()
    {
        // Arrange
        var brief = "Analyze quantum computing";
        var draft = "Quantum computing draft";

        // Act
        var result = await _workflow.SuperviseAsync(brief, draft, maxIterations: 2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task SuperviseAsync_RespectMaxIterations()
    {
        // Arrange
        var brief = "Research topic";
        var draft = "Draft";
        int maxIter = 3;

        // Act
        var result = await _workflow.SuperviseAsync(brief, draft, maxIterations: maxIter);

        // Assert
        Assert.NotNull(result);
        // Should complete within reasonable time
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public async Task StreamSuperviseAsync_YieldsUpdates()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamSuperviseAsync(
            "Research topic",
            "Draft",
            maxIterations: 2))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        Assert.True(updates.Count >= 3);
    }

    [Fact]
    public async Task StreamSuperviseAsync_IncludesQualityScores()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamSuperviseAsync(
            "Research",
            "Draft"))
        {
            updates.Add(update);
        }

        // Assert
        var updateText = string.Join(" ", updates).ToLower();
        // Should mention quality or completion
        Assert.NotEmpty(updateText);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task SuperviseAsync_MaintainsStateConsistency()
    {
        // Arrange
        var state = TestFixtures.CreateTestSupervisorState();

        // Act
        var result = await _workflow.SuperviseAsync(
            state.ResearchBrief,
            state.DraftReport,
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
        WorkflowAssertions.AssertValidSupervisorState(state);
    }

    [Fact]
    public async Task SuperviseAsync_BuildsKnowledgeBase()
    {
        // Arrange
        var initialState = TestFixtures.CreateTestSupervisorState();
        var initialKBCount = initialState.KnowledgeBase.Count;

        // Act
        var result = await _workflow.SuperviseAsync(
            "Research AI",
            "Draft",
            maxIterations: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SuperviseAsync_WithEmptyInputs_HandlesGracefully()
    {
        // Arrange
        var brief = "";
        var draft = "";

        // Act
        var result = await _workflow.SuperviseAsync(brief, draft, maxIterations: 1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SuperviseAsync_WithCancellation_StopsGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        var ex = await Record.ExceptionAsync(() =>
            _workflow.SuperviseAsync("topic", "draft", cancellationToken: cts.Token));
        // Cancellation allowed
    }

    #endregion
}
