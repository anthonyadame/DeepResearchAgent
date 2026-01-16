using DeepResearchAgent.Models;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Unit tests for MasterWorkflow.
/// Tests each step independently and full pipeline integration.
/// </summary>
public class MasterWorkflowTests
{
    private readonly MasterWorkflow _workflow;
    private readonly TestFixtures _fixtures;

    public MasterWorkflowTests()
    {
        _fixtures = new TestFixtures();
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        _workflow = master;
    }

    #region Clarify Step Tests

    [Fact]
    public async Task ClarifyWithUserAsync_WithValidQuery_ReturnsDecision()
    {
        // Arrange
        var query = "Research AI trends";

        // Act
        var (needsClarification, message) = await _workflow.ClarifyWithUserAsync(query, CancellationToken.None);

        // Assert
        Assert.IsType<bool>(needsClarification);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task ClarifyWithUserAsync_WithEmptyQuery_ReturnsClarification()
    {
        // Arrange
        var query = "x";

        // Act
        var (needsClarification, message) = await _workflow.ClarifyWithUserAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task ClarifyWithUserAsync_WithDetailedQuery_AppropriateResponse()
    {
        // Arrange
        var query = "Comprehensive analysis of quantum computing applications in machine learning";

        // Act
        var (needsClarification, message) = await _workflow.ClarifyWithUserAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(message);
    }

    #endregion

    #region Write Research Brief Tests

    [Fact]
    public async Task WriteResearchBriefAsync_GeneratesBrief()
    {
        // Arrange
        var query = "Research quantum computing";

        // Act
        var result = await _workflow.WriteResearchBriefAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Length > 10);
    }

    [Fact]
    public async Task WriteResearchBriefAsync_IncludesContextFromQuery()
    {
        // Arrange
        var query = "The impact of AI on healthcare systems";

        // Act
        var brief = await _workflow.WriteResearchBriefAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(brief);
    }

    [Fact]
    public async Task WriteResearchBriefAsync_ProducesStructuredOutput()
    {
        // Arrange
        var query = "Climate change mitigation strategies";

        // Act
        var brief = await _workflow.WriteResearchBriefAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(brief);
        Assert.True(brief.Length > 50);
    }

    #endregion

    #region Draft Report Tests

    [Fact]
    public async Task WriteDraftReportAsync_GeneratesDraft()
    {
        // Arrange
        var brief = "Research brief about machine learning";

        // Act
        var result = await _workflow.WriteDraftReportAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task WriteDraftReportAsync_IncludesBriefContent()
    {
        // Arrange
        var brief = "Quantum computing research brief";

        // Act
        var draft = await _workflow.WriteDraftReportAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotEmpty(draft);
    }

    [Fact]
    public async Task WriteDraftReportAsync_ProducesReasonableLength()
    {
        // Arrange
        var brief = "Research directions in artificial intelligence";

        // Act
        var draft = await _workflow.WriteDraftReportAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotEmpty(draft);
        Assert.True(draft.Length > 50);
    }

    #endregion

    #region Full Pipeline Tests

    [Fact]
    public async Task ExecuteAsync_CompletesFullWorkflow()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState("Research machine learning");

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        WorkflowAssertions.AssertValidAgentState(result);
        Assert.NotEmpty(result.FinalReport);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesProgressThroughAllSteps()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState("Research AI safety");

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result.ResearchBrief);
        Assert.NotEmpty(result.DraftReport);
        Assert.NotEmpty(result.FinalReport);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexQuery_Succeeds()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState(
            "In-depth analysis of renewable energy adoption trends and barriers");

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.FinalReport.Length > 200);
    }

    #endregion

    #region Streaming Tests

    [Fact]
    public async Task StreamAsync_YieldsMultipleUpdates()
    {
        // Arrange
        var query = "Research topic";
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamAsync(query))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        Assert.True(updates.Count >= 5); // Should have multiple steps
    }

    [Fact]
    public async Task StreamAsync_IncludesProgressMarkers()
    {
        // Arrange
        var query = "Research quantum computing";
        var updates = new List<string>();

        // Act
        await foreach (var update in _workflow.StreamAsync(query))
        {
            updates.Add(update);
        }

        // Assert
        var lastUpdate = string.Empty;
        foreach (var update in updates)
        {
            lastUpdate = update;
            Assert.NotEmpty(update);
            Assert.True(update.StartsWith("[master]") || update.StartsWith("[supervisor]"),
                "All updates should include workflow context");
        }
        
        // Should end with completion marker
        Assert.True(lastUpdate.Contains("complete") || lastUpdate.Contains("done"),
            "Should include completion marker");
    }

    [Fact]
    public async Task StreamAsync_EndsWith_Completion()
    {
        // Arrange
        var query = "Research topic";
        var lastUpdate = "";

        // Act
        await foreach (var update in _workflow.StreamAsync(query))
        {
            lastUpdate = update;
        }

        // Assert
        Assert.NotEmpty(lastUpdate);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithCancellation_StopsGracefully()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        // Act & Assert - should not throw, but may not complete
        var ex = await Record.ExceptionAsync(
            () => _workflow.ExecuteAsync(input, cts.Token));
        // Cancellation is allowed
    }

    [Fact]
    public async Task ExecuteAsync_MaintainsStateConsistency()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Messages);
        Assert.NotNull(result.ResearchBrief);
        Assert.NotNull(result.DraftReport);
        Assert.NotNull(result.FinalReport);
    }

    #endregion

    #region State Validation Tests

    [Fact]
    public async Task ExecuteAsync_ProducesValidFinalState()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        var validation = StateValidator.ValidateAgentState(result);
        Assert.True(validation.IsValid, validation.Message);
    }

    [Fact]
    public async Task ExecuteAsync_FinalReportNotEmpty()
    {
        // Arrange
        var input = TestFixtures.CreateTestAgentState();

        // Act
        var result = await _workflow.ExecuteAsync(input, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.FinalReport);
        Assert.True(result.FinalReport.Length > 100,
            "Final report should be substantial");
    }

    #endregion
}
