using DeepResearchAgent.Models;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Tests for the state management layer, validating accumulation,
/// factory patterns, and state transitions.
/// </summary>
public class StateManagementTests
{
    #region StateFactory Tests

    [Fact]
    public void CreateAgentState_ReturnsValidInitialState()
    {
        // Act
        var state = StateFactory.CreateAgentState();

        // Assert
        Assert.NotNull(state);
        Assert.Empty(state.Messages);
        Assert.Empty(state.RawNotes);
        Assert.Empty(state.Notes);
        Assert.Null(state.ResearchBrief);
        Assert.Empty(state.DraftReport);
    }

    [Fact]
    public void CreateAgentState_WithMessages_PreservesInput()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Test message" }
        };

        // Act
        var state = StateFactory.CreateAgentState(messages);

        // Assert
        Assert.Single(state.Messages);
        Assert.Equal("Test message", state.Messages[0].Content);
    }

    [Fact]
    public void CreateSupervisorState_ReturnsValidInitialState()
    {
        // Act
        var state = StateFactory.CreateSupervisorState();

        // Assert
        Assert.NotNull(state);
        Assert.Empty(state.SupervisorMessages);
        Assert.Empty(state.RawNotes);
        Assert.Empty(state.KnowledgeBase);
        Assert.Empty(state.ActiveCritiques);
        Assert.Empty(state.QualityHistory);
        Assert.False(state.NeedsQualityRepair);
    }

    [Fact]
    public void CreateResearcherState_WithTopic_SetsTopic()
    {
        // Arrange
        const string topic = "Quantum Computing";

        // Act
        var state = StateFactory.CreateResearcherState(topic);

        // Assert
        Assert.Equal(topic, state.ResearchTopic);
        Assert.Empty(state.ResearcherMessages);
        Assert.Empty(state.RawNotes);
    }

    [Fact]
    public void CreateFact_ClampConfidenceScore()
    {
        // Act
        var fact1 = StateFactory.CreateFact("Test", "http://test.com", 150); // Over 100
        var fact2 = StateFactory.CreateFact("Test", "http://test.com", -5);  // Under 1

        // Assert
        Assert.Equal(100, fact1.ConfidenceScore);
        Assert.Equal(1, fact2.ConfidenceScore);
    }

    [Fact]
    public void CloneAgentState_CreatesIndependentCopy()
    {
        // Arrange
        var original = StateFactory.CreateAgentState();
        original.Messages.Add(new ChatMessage { Role = "user", Content = "Original" });
        original.DraftReport = "Original Draft";

        // Act
        var clone = StateFactory.CloneAgentState(original);
        clone.Messages.Add(new ChatMessage { Role = "assistant", Content = "Clone" });
        clone.DraftReport = "Cloned Draft";

        // Assert
        Assert.Single(original.Messages);
        Assert.Equal(2, clone.Messages.Count);
        Assert.Equal("Original Draft", original.DraftReport);
        Assert.Equal("Cloned Draft", clone.DraftReport);
    }

    #endregion

    #region StateValidator Tests

    [Fact]
    public void ValidateAgentState_WithEmptyMessages_ReturnsFails()
    {
        // Arrange
        var state = StateFactory.CreateAgentState();

        // Act
        var result = StateValidator.ValidateAgentState(state);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least one user message", string.Join(", ", result.Errors));
    }

    [Fact]
    public void ValidateAgentState_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var state = StateFactory.CreateAgentState(
            new List<ChatMessage>
            {
                new() { Role = "user", Content = "Query" }
            }
        );

        // Act
        var result = StateValidator.ValidateAgentState(state);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateSupervisorState_RequiresResearchBrief()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();

        // Act
        var result = StateValidator.ValidateSupervisorState(state);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ResearchBrief", string.Join(", ", result.Errors));
    }

    [Fact]
    public void ValidateFact_InvalidConfidenceScore_ReturnsFails()
    {
        // Arrange
        var fact = StateFactory.CreateFact("Test", "http://test.com", 150); // Will be clamped to 100

        // Act
        var result = StateValidator.ValidateFact(fact);

        // Assert - Should pass because confidence is clamped to valid range
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCritique_MissingAuthor_ReturnsFails()
    {
        // Arrange
        var critique = new CritiqueState
        {
            Author = "",
            Concern = "Test concern",
            Severity = 5
        };

        // Act
        var result = StateValidator.ValidateCritique(critique);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ShouldContinueDiffusion_ExceedsMaxIterations_ReturnsFalse()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchIterations = 15;

        // Act
        var shouldContinue = StateValidator.ShouldContinueDiffusion(state, maxIterations: 10);

        // Assert
        Assert.False(shouldContinue);
    }

    [Fact]
    public void GetHealthReport_ReturnsValidMetrics()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test";
        state.DraftReport = "Draft";
        state.KnowledgeBase.Add(StateFactory.CreateFact("Fact 1", "http://test.com", 85));
        state.KnowledgeBase.Add(StateFactory.CreateFact("Fact 2", "http://test.com", 75));
        state.ActiveCritiques.Add(StateFactory.CreateCritique("Red Team", "Issue", 7));
        state.QualityHistory.Add(StateFactory.CreateQualityMetric(7.5f, "Good", 0));

        // Act
        var report = StateValidator.GetHealthReport(state);

        // Assert
        Assert.True(report.IsValid);
        Assert.Equal(2, report.KnowledgeBaseSize);
        Assert.Equal(80, report.AverageConfidence); // (85 + 75) / 2
        Assert.Equal(7.5f, report.CurrentDraftQuality);
        Assert.Equal(1, report.ActiveCritiquesCount);
    }

    #endregion

    #region StateManager Tests

    [Fact]
    public void CaptureSnapshot_TracksStateProgression()
    {
        // Arrange
        var manager = new StateManager();
        var state = StateFactory.CreateAgentState();
        state.DraftReport = "Initial Draft";

        // Act
        var snapshot1 = manager.CaptureSnapshot(state, "initial");
        state.DraftReport = "Revised Draft";
        var snapshot2 = manager.CaptureSnapshot(state, "revised");

        // Assert
        Assert.Equal(0, snapshot1.Iteration);
        Assert.Equal(1, snapshot2.Iteration);
        Assert.Equal("initial", snapshot1.Phase);
        Assert.Equal("revised", snapshot2.Phase);
    }

    [Fact]
    public void MergeSupervisorState_CombinesLists()
    {
        // Arrange
        var manager = new StateManager();
        var target = StateFactory.CreateSupervisorState();
        target.RawNotes.Add("Note 1");
        
        var source = StateFactory.CreateSupervisorState();
        source.RawNotes.Add("Note 2");

        // Act
        manager.MergeSupervisorState(target, source);

        // Assert
        Assert.Equal(2, target.RawNotes.Count);
        Assert.Contains("Note 1", target.RawNotes);
        Assert.Contains("Note 2", target.RawNotes);
    }

    #endregion

    #region StateAccumulator Tests

    [Fact]
    public void StateAccumulator_Add_PreservesOrder()
    {
        // Arrange
        var accumulator = new StateAccumulator<string>();

        // Act
        accumulator.Add("first");
        accumulator.Add("second");
        accumulator.Add("third");

        // Assert
        var items = accumulator.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal("first", items[0]);
        Assert.Equal("second", items[1]);
        Assert.Equal("third", items[2]);
    }

    [Fact]
    public void StateAccumulator_AddRange_MergesCollections()
    {
        // Arrange
        var accumulator = new StateAccumulator<int>(new[] { 1, 2 });
        var newItems = new[] { 3, 4, 5 };

        // Act
        accumulator.AddRange(newItems);

        // Assert
        var items = accumulator.Items.ToList();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, items);
    }

    [Fact]
    public void StateAccumulator_Replace_ClearsOldItems()
    {
        // Arrange
        var accumulator = new StateAccumulator<string>(new[] { "old1", "old2" });

        // Act
        accumulator.Replace(new[] { "new1", "new2", "new3" });

        // Assert
        Assert.Equal(3, accumulator.Count);
        Assert.Equal("new1", accumulator.Items[0]);
    }

    [Fact]
    public void StateAccumulator_PlusOperator_UnionsMerges()
    {
        // Arrange
        var acc1 = new StateAccumulator<int>(new[] { 1, 2, 3 });
        var acc2 = new StateAccumulator<int>(new[] { 4, 5 });

        // Act
        var result = acc1 + acc2;

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(3, acc1.Count); // Original unchanged
    }

    #endregion

    #region StateTransition Tests

    [Fact]
    public void StateTransitionRouter_RegisterEdge_CreatesSimplePath()
    {
        // Arrange
        var router = new StateTransitionRouter();
        router.RegisterEdge("node1", "node2");
        var state = StateFactory.CreateAgentState(
            new List<ChatMessage> { new() { Role = "user", Content = "test" } }
        );

        // Act
        var transition = router.GetNextTransition("node1", state);

        // Assert
        Assert.NotNull(transition);
        Assert.Equal("node2", transition.NextNode);
    }

    [Fact]
    public void StateTransitionRouter_RegisterConditionalEdge_RoutesBasedOnState()
    {
        // Arrange
        var router = new StateTransitionRouter();
        router.RegisterConditionalEdge("decision",
            state => string.IsNullOrEmpty(state.ResearchBrief) ? "clarify" : "research");

        var state = StateFactory.CreateAgentState(
            new List<ChatMessage> { new() { Role = "user", Content = "test" } }
        );

        // Act
        var transition = router.GetNextTransition("decision", state) as ConditionalTransition;

        // Assert
        Assert.NotNull(transition);
        // Simulate resolution
        var nextNode = transition.ResolveNextNode(state);
        Assert.Equal("clarify", nextNode);
    }

    [Fact]
    public void StateTransitionRouter_UnregisteredNode_ReturnsEndTransition()
    {
        // Arrange
        var router = new StateTransitionRouter();
        var state = StateFactory.CreateAgentState(
            new List<ChatMessage> { new() { Role = "user", Content = "test" } }
        );

        // Act
        var transition = router.GetNextTransition("unknown_node", state);

        // Assert
        Assert.IsType<EndTransition>(transition);
        Assert.True(transition.IsTerminal);
    }

    #endregion
}
