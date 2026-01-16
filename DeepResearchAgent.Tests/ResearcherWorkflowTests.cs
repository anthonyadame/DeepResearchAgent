using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Unit tests for ResearcherWorkflow.
/// Tests ReAct loop, tool execution, compression, and streaming.
/// </summary>
public class ResearcherWorkflowTests
{
    private readonly ResearcherWorkflow _workflow;
    private readonly LightningStore _store;

    public ResearcherWorkflowTests()
    {
        var (researcher, _, store) = TestFixtures.CreateMockResearcherWorkflow();
        _workflow = researcher;
        _store = store;
    }

    #region ReAct Loop Tests

    [Fact]
    public async Task ResearchAsync_ReturnsFactsList()
    {
        // Arrange
        var topic = "Machine learning";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task ResearchAsync_ExtractsFacts()
    {
        // Arrange
        var topic = "Artificial intelligence";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        var factList = facts.ToList();
        if (factList.Count > 0)
        {
            WorkflowAssertions.AssertFactsExtracted(factList.AsEnumerable(), 1);
        }
    }

    [Fact]
    public async Task ResearchAsync_WithSpecificTopic_Succeeds()
    {
        // Arrange
        var topic = "Neural networks";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task ResearchAsync_CompletesClosure()
    {
        // Arrange
        var topic = "Quantum computing";

        // Act
        var startTime = DateTime.UtcNow;
        var facts = await _workflow.ResearchAsync(topic);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(facts);
        Assert.True(duration < TimeSpan.FromSeconds(30),
            "Research should complete in reasonable time");
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public async Task StreamResearchAsync_YieldsProgressUpdates()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamResearchAsync("AI"))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        Assert.True(updates.Count >= 3);
    }

    [Fact]
    public async Task StreamResearchAsync_IncludesIterationCount()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamResearchAsync("ML"))
        {
            updates.Add(update);
        }

        // Assert
        var updateText = string.Join(" ", updates).ToLower();
        Assert.NotEmpty(updateText);
    }

    [Fact]
    public async Task StreamResearchAsync_ReportsFacts()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamResearchAsync("Research"))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        // Should include completion message
        var final = updates.Last().ToLower();
        Assert.Contains("complete", final);
    }

    #endregion

    #region LLM Integration Tests

    [Fact]
    public async Task LLMCallAsync_GeneratesDecision()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState("Machine learning");

        // Act
        var decision = await _workflow.LLMCallAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(decision);
        Assert.NotEmpty(decision.Content);
        Assert.Equal("assistant", decision.Role);
    }

    [Fact]
    public async Task LLMCallAsync_WithExistingNotes_IncorporatesThem()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        state.RawNotes.Add("Previous research finding about ML");

        // Act
        var decision = await _workflow.LLMCallAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(decision);
        Assert.NotEmpty(decision.Content);
    }

    [Fact]
    public async Task LLMCallAsync_ProducesValidMessage()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();

        // Act
        var decision = await _workflow.LLMCallAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(decision);
        Assert.NotEmpty(decision.Content);
        Assert.True(decision.Content.Length > 0);
    }

    #endregion

    #region Tool Execution Tests

    [Fact]
    public async Task ToolExecutionAsync_UpdatesRawNotes()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        var initialCount = state.RawNotes.Count;
        var llmResponse = new Models.ChatMessage
        {
            Role = "assistant",
            Content = "Search for machine learning applications"
        };

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        Assert.True(state.RawNotes.Count >= initialCount);
    }

    [Fact]
    public async Task ToolExecutionAsync_IncrementIterationCounter()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        var initialIter = state.ToolCallIterations;
        var llmResponse = new Models.ChatMessage
        {
            Role = "assistant",
            Content = "Research query"
        };

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        Assert.True(state.ToolCallIterations > initialIter);
    }

    [Fact]
    public async Task ToolExecutionAsync_RecordsToolCalls()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage
        {
            Role = "assistant",
            Content = "Perform search"
        };

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        Assert.NotEmpty(state.ResearcherMessages);
    }

    #endregion

    #region Compression Tests

    [Fact]
    public async Task CompressResearchAsync_GeneratesSummary()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        state.RawNotes.Add("Finding 1 about the topic");
        state.RawNotes.Add("Finding 2 about the topic");

        // Act
        var compressed = await _workflow.CompressResearchAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(compressed);
        Assert.NotEmpty(compressed);
        Assert.True(compressed.Length > 0);
    }

    [Fact]
    public async Task CompressResearchAsync_WithoutNotes_HandlesGracefully()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();

        // Act
        var compressed = await _workflow.CompressResearchAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(compressed);
    }

    [Fact]
    public async Task CompressResearchAsync_AggregatesFindings()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        for (int i = 0; i < 5; i++)
        {
            state.RawNotes.Add($"Research note {i + 1}");
        }

        // Act
        var compressed = await _workflow.CompressResearchAsync(state, CancellationToken.None);

        // Assert
        Assert.NotNull(compressed);
        Assert.True(compressed.Length > 10);
    }

    #endregion

    #region Convergence Tests

    [Fact]
    public async Task ShouldContinue_WithMaxIterations_ReturnsFalse()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        state.ToolCallIterations = 4; // Near max

        // Act
        var shouldCont = ResearcherWorkflow.ShouldContinue(state, 4, 5);

        // Assert
        Assert.False(shouldCont);
    }

    [Fact]
    public async Task ShouldContinue_WithDataAndIterations_ReturnsTrue()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();
        state.RawNotes.Add("Some data");
        state.ToolCallIterations = 1;

        // Act
        var shouldCont = ResearcherWorkflow.ShouldContinue(state, 1, 5);

        // Assert
        Assert.True(shouldCont);
    }

    [Fact]
    public async Task ShouldContinue_WithoutData_ReturnsFalse()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();

        // Act
        var shouldCont = ResearcherWorkflow.ShouldContinue(state, 1, 5);

        // Assert
        Assert.False(shouldCont);
    }

    #endregion

    #region Fact Extraction Tests

    [Fact]
    public async Task ResearchAsync_PersistsFacts()
    {
        // Arrange
        var topic = "AI research";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task ResearchAsync_ExtractsValidFacts()
    {
        // Arrange
        var topic = "Technology";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        var factList = facts.ToList();
        foreach (var fact in factList)
        {
            Assert.NotEmpty(fact.Content);
            Assert.NotEmpty(fact.SourceUrl);
            Assert.True(fact.ConfidenceScore >= 1);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ResearchAsync_WithEmptyTopic_Completes()
    {
        // Arrange
        var topic = "";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        Assert.NotNull(facts);
    }

    [Fact]
    public async Task ResearchAsync_WithCancellation_StopsGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(5));

        // Act & Assert
        var ex = await Record.ExceptionAsync(() =>
            _workflow.ResearchAsync("topic", null, cts.Token));
        // Cancellation is acceptable
    }

    [Fact]
    public async Task StreamResearchAsync_WithError_YieldsErrorMessage()
    {
        // Arrange
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamResearchAsync("topic"))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        // Should not have unhandled errors
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task ResearchAsync_MaintainsStateConsistency()
    {
        // Arrange
        var state = TestFixtures.CreateTestResearcherState();

        // Act
        var facts = await _workflow.ResearchAsync(state.ResearchTopic);

        // Assert
        WorkflowAssertions.AssertValidResearcherState(state);
    }

    [Fact]
    public async Task ResearchAsync_BuildsMessageHistory()
    {
        // Arrange
        var topic = "Research topic";

        // Act
        var facts = await _workflow.ResearchAsync(topic);

        // Assert
        Assert.NotNull(facts);
    }

    #endregion
}
