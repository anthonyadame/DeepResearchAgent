using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows;

/// <summary>
/// Integration tests for the full Phase 5 pipeline.
/// Tests agent chaining and end-to-end workflow execution.
/// </summary>
public class Phase5PipelineIntegrationTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<SupervisorWorkflow> _mockSupervisor;
    private readonly Mock<ILogger<MasterWorkflow>> _mockLogger;
    private readonly MasterWorkflow _masterWorkflow;

    public Phase5PipelineIntegrationTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockStateService = new Mock<ILightningStateService>();
        _mockSupervisor = new Mock<SupervisorWorkflow>(
            _mockStateService.Object, 
            null, 
            _mockLlmService.Object);
        _mockLogger = new Mock<ILogger<MasterWorkflow>>();

        // Create complex agents
        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object, null);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object, null);
        var reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object, null);

        _masterWorkflow = new MasterWorkflow(
            _mockStateService.Object,
            _mockSupervisor.Object,
            _mockLlmService.Object,
            _mockLogger.Object,
            null,
            null,
            researcherAgent,
            analystAgent,
            reportAgent);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithValidInput_ReturnsCompleteReport()
    {
        // Arrange
        var topic = "Quantum Computing";
        var researchBrief = "Research recent quantum computing breakthroughs";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.NotEmpty(result.Sections);
        Assert.True(result.QualityScore >= 0);
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_AgentChaining_DataFlowsCorrectly()
    {
        // Arrange: Verify data flows from Researcher → Analyst → Report
        var topic = "AI Safety";
        var researchBrief = "Research AI safety concerns";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        // 1. Report should have sections (from Report Agent)
        Assert.NotEmpty(result.Sections);
        
        // 2. Report should have title (from Report Agent)
        Assert.NotEmpty(result.Title);
        
        // 3. Report should reference the topic
        Assert.Contains(topic, result.ExecutiveSummary, StringComparison.OrdinalIgnoreCase);
        
        // 4. Report should have quality score
        Assert.True(result.QualityScore >= 0 && result.QualityScore <= 1);
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_GeneratesProperMetadata()
    {
        // Arrange
        var topic = "Machine Learning";
        var researchBrief = "Research ML applications";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        Assert.NotNull(result.Title);
        Assert.NotNull(result.ExecutiveSummary);
        Assert.NotNull(result.Sections);
        Assert.NotNull(result.Citations);
        Assert.NotEmpty(result.CompletionStatus);
        Assert.True(result.CreatedAt != default);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_ResearchToAnalysisFinding_TransfersCorrectly()
    {
        // Arrange: Verify ResearchOutput → AnalysisInput transition
        var topic = "Blockchain";
        var researchBrief = "Research blockchain technology";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        // Research findings should be represented in the analysis and report
        Assert.NotNull(result);
        Assert.True(result.Sections.Any(s => s.Content.Contains("Finding")));
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_AnalysisToReportFinding_TransfersCorrectly()
    {
        // Arrange: Verify AnalysisOutput → ReportOutput transition
        var topic = "Climate Change";
        var researchBrief = "Research climate impacts";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        // Analysis insights should be reflected in the report
        Assert.NotNull(result);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.True(result.Sections.Count >= 3); // Introduction, Findings, Conclusion, etc.
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithResearcherAgentFailure_PropagatesError()
    {
        // Arrange
        var topic = "Test Topic";
        var researchBrief = "Test brief";
        
        _mockLlmService
            .Setup(s => s.InvokeAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("LLM service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief));
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithInvalidInput_HandlesGracefully()
    {
        // Arrange
        var topic = "";
        var researchBrief = "";
        SetupMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);

        // Assert
        Assert.NotNull(result);
        // Should still produce a report even with empty inputs
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_CompletesInReasonableTime()
    {
        // Arrange
        var topic = "Test Topic";
        var researchBrief = "Test brief";
        SetupMocks();

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(topic, researchBrief);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(result);
        // Should complete within reasonable time (even with mocks)
        Assert.True(duration.TotalSeconds < 60, $"Pipeline took {duration.TotalSeconds}s, expected < 60s");
    }

    #endregion

    #region Helper Methods

    private void SetupMocks()
    {
        // Setup LLM service to return valid responses
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<OllamaChatMessage> messages, string? _, CancellationToken _) =>
            {
                var content = messages.FirstOrDefault()?.Content ?? "";
                
                if (content.Contains("title"))
                    return new OllamaChatMessage { Role = "assistant", Content = "Comprehensive Research Report" };
                
                if (content.Contains("summary"))
                    return new OllamaChatMessage { Role = "assistant", Content = "This research explores the key findings." };
                
                if (content.Contains("quality") || content.Contains("importance"))
                    return new OllamaChatMessage { Role = "assistant", Content = "8.5" };
                
                if (content.Contains("theme"))
                    return new OllamaChatMessage { Role = "assistant", Content = "[\"Theme1\", \"Theme2\"]" };
                
                return new OllamaChatMessage { Role = "assistant", Content = "Research content" };
            });

        // Setup tool service
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string toolName, Dictionary<string, object> _, CancellationToken _) =>
            {
                return toolName switch
                {
                    "websearch" => new List<WebSearchResult>
                    {
                        new()
                        {
                            Title = "Test Result",
                            Url = "https://example.com",
                            Content = "Test content"
                        }
                    },
                    "summarize" => new PageSummaryResult
                    {
                        Summary = "Test summary",
                        KeyPoints = new List<string> { "Point 1" }
                    },
                    "extractfacts" => new FactExtractionResult
                    {
                        Facts = new List<ExtractedFact>
                        {
                            new()
                            {
                                Statement = "Test fact",
                                Confidence = 0.85f,
                                Source = "test source",
                                Category = "research"
                            }
                        }
                    },
                    _ => new List<WebSearchResult>()
                };
            });

        // Setup state service
        _mockStateService
            .Setup(s => s.SetResearchStateAsync(It.IsAny<string>(), It.IsAny<ResearchStateModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStateService
            .Setup(s => s.GetResearchStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResearchStateModel { ResearchId = "test", Status = ResearchStatus.Completed });
    }

    #endregion
}
