using DeepResearchAgent.Services;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Models;
using DeepResearchAgent.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Tools;

/// <summary>
/// Unit tests for ResearchToolsImplementation.
/// Tests all 5 tools: WebSearch, QualityEvaluation, Summarization, FactExtraction, RefineDraft.
/// </summary>
public class ResearchToolsImplementationTests
{
    private readonly Mock<IWebSearchProvider> _mockSearchProvider;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ILogger<ResearchToolsImplementation>> _mockLogger;
    private readonly ResearchToolsImplementation _tools;

    public ResearchToolsImplementationTests()
    {
        _mockSearchProvider = new Mock<IWebSearchProvider>();
        _mockSearchProvider.Setup(x => x.ProviderName).Returns("test");
        _mockLlmService = new Mock<OllamaService>(null);
        _mockLogger = new Mock<ILogger<ResearchToolsImplementation>>();
        _tools = new ResearchToolsImplementation(_mockSearchProvider.Object, _mockLlmService.Object, _mockLogger.Object);
    }

    #region WebSearchTool Tests

    [Fact]
    public async Task WebSearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "quantum computing advances 2024";
        var webSearchResults = new List<WebSearchResult>
        {
            new WebSearchResult
            {
                Title = "Quantum Computing Breakthrough",
                Url = "https://example.com/quantum",
                Content = "IBM announced new quantum processor...",
                Engine = "test"
            },
            new WebSearchResult
            {
                Title = "Quantum Computing Market Report",
                Url = "https://example.com/report",
                Content = "Market analysis shows growth in quantum...",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(query, It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(webSearchResults);

        // Act
        var results = await _tools.WebSearchAsync(query, maxResults: 10);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Contains("Quantum", results[0].Title);

        _mockSearchProvider.Verify(
            s => s.SearchAsync(query, It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WebSearchAsync_RespectsMacResultLimit()
    {
        // Arrange
        var query = "test query";
        var manyResults = Enumerable.Range(1, 20)
            .Select(i => new WebSearchResult 
            { 
                Title = $"Result {i}", 
                Url = $"https://example.com/{i}",
                Engine = "test"
            })
            .ToList();

        _mockSearchProvider
            .Setup(s => s.SearchAsync(query, It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manyResults);

        // Act
        var results = await _tools.WebSearchAsync(query, maxResults: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task WebSearchAsync_WhenServiceThrows_WrapsException()
    {
        // Arrange
        var query = "test query";
        var exception = new HttpRequestException("Search failed");

        _mockSearchProvider
            .Setup(s => s.SearchAsync(query, It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tools.WebSearchAsync(query));

        Assert.Contains("search failed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(exception, ex.InnerException);
    }

    #endregion

    #region QualityEvaluationTool Tests

    [Fact]
    public async Task EvaluateQualityAsync_WithValidContent_ReturnsScores()
    {
        // Arrange
        var content = "The quantum computer uses qubits for processing...";
        var expectedResult = new QualityEvaluationResult
        {
            DimensionScores = new Dictionary<string, DimensionScore>
            {
                { "accuracy", new DimensionScore { Score = 8.5f, Reason = "Well-sourced claims" } },
                { "completeness", new DimensionScore { Score = 7.0f, Reason = "Some gaps in depth" } },
                { "relevance", new DimensionScore { Score = 9.0f, Reason = "Directly addresses topic" } },
                { "clarity", new DimensionScore { Score = 8.0f, Reason = "Clear explanation" } }
            },
            OverallScore = 8.1f,
            Summary = "Overall high quality content"
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.EvaluateQualityAsync(content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8.1f, result.OverallScore);
        Assert.Equal(4, result.DimensionScores.Count);

        _mockLlmService.Verify(
            s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateQualityAsync_WithCustomDimensions_UsesProvidedDimensions()
    {
        // Arrange
        var content = "test content";
        var customDimensions = new[] { "accuracy", "bias", "timeliness" };
        var expectedResult = new QualityEvaluationResult
        {
            DimensionScores = new Dictionary<string, DimensionScore>(),
            OverallScore = 7.5f
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.EvaluateQualityAsync(content, dimensions: customDimensions);

        // Assert
        Assert.NotNull(result);

        // Verify the prompt contained the custom dimensions
        _mockLlmService.Verify(
            s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.Is<List<OllamaChatMessage>>(msgs => msgs[0].Content.Contains("accuracy")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region WebpageSummarizationTool Tests

    [Fact]
    public async Task SummarizePageAsync_WithPageContent_ReturnsSummaryAndKeyPoints()
    {
        // Arrange
        var pageContent = "Lorem ipsum dolor sit amet... [long content]";
        var expectedResult = new PageSummaryResult
        {
            Summary = "A concise summary of the webpage content.",
            KeyPoints = new List<string>
            {
                "Key point 1",
                "Key point 2",
                "Key point 3"
            }
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.SummarizePageAsync(pageContent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Summary);
        Assert.Equal(3, result.KeyPoints.Count);
    }

    [Fact]
    public async Task SummarizePageAsync_RespectsMaxLength()
    {
        // Arrange
        var pageContent = "test content";
        var maxLength = 200;
        var expectedResult = new PageSummaryResult
        {
            Summary = "Short summary",
            KeyPoints = new List<string>()
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.SummarizePageAsync(pageContent, maxLength: maxLength);

        // Assert
        Assert.NotNull(result);

        // Verify max length was included in prompt
        _mockLlmService.Verify(
            s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.Is<List<OllamaChatMessage>>(msgs => msgs[0].Content.Contains("200")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region FactExtractionTool Tests

    [Fact]
    public async Task ExtractFactsAsync_WithContent_ExtractsStructuredFacts()
    {
        // Arrange
        var content = "IBM announced a new 127-qubit quantum processor. Google's Willow chip achieved error correction.";
        var expectedResult = new FactExtractionResult
        {
            Facts = new List<ExtractedFact>
            {
                new ExtractedFact
                {
                    Statement = "IBM announced a 127-qubit processor",
                    Confidence = 0.95f,
                    Source = "IBM announcement",
                    Category = "product launch"
                },
                new ExtractedFact
                {
                    Statement = "Google's Willow achieved error correction",
                    Confidence = 0.90f,
                    Source = "Google Willow paper",
                    Category = "breakthrough"
                }
            }
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.ExtractFactsAsync(content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Facts.Count);
        Assert.All(result.Facts, f => Assert.True(f.Confidence > 0 && f.Confidence <= 1));
    }

    [Fact]
    public async Task ExtractFactsAsync_WithTopic_IncludesTopicContext()
    {
        // Arrange
        var content = "test content";
        var topic = "quantum computing";
        var expectedResult = new FactExtractionResult { Facts = new List<ExtractedFact>() };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.ExtractFactsAsync(content, topic: topic);

        // Assert
        Assert.NotNull(result);

        // Verify topic was in prompt
        _mockLlmService.Verify(
            s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.Is<List<OllamaChatMessage>>(msgs => msgs[0].Content.Contains("quantum computing")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RefineDraftReportTool Tests

    [Fact]
    public async Task RefineDraftAsync_WithDraftAndFeedback_ReturnsRefinedReport()
    {
        // Arrange
        var draft = "Initial research report on quantum computing...";
        var feedback = "Add more recent 2024 developments and expand on error correction.";
        var expectedResult = new RefinedDraftResult
        {
            RefinedReport = "Enhanced research report with 2024 developments and expanded error correction section...",
            ChangesMade = new List<string>
            {
                "Added 2024 quantum breakthroughs",
                "Expanded error correction section",
                "Improved technical accuracy"
            },
            ImprovementScore = 0.85f
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<RefinedDraftResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.RefineDraftAsync(draft, feedback);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.RefinedReport);
        Assert.NotEmpty(result.ChangesMade);
        Assert.True(result.ImprovementScore > 0 && result.ImprovementScore <= 1);
    }

    [Fact]
    public async Task RefineDraftAsync_TrackIterationNumber()
    {
        // Arrange
        var draft = "draft";
        var feedback = "feedback";
        var iteration = 3;
        var expectedResult = new RefinedDraftResult
        {
            RefinedReport = "refined",
            ChangesMade = new List<string>(),
            ImprovementScore = 0.75f
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<RefinedDraftResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _tools.RefineDraftAsync(draft, feedback, iterationNumber: iteration);

        // Assert
        Assert.NotNull(result);

        // Verify iteration number was used in logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("iteration 3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task WebSearchAsync_WhenExceptionThrown_IncludesOriginalException()
    {
        // Arrange
        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tools.WebSearchAsync("query"));

        Assert.NotNull(ex.InnerException);
        Assert.IsType<Exception>(ex.InnerException);
    }

    [Fact]
    public async Task EvaluateQualityAsync_WhenLLMFails_WrapsAndRethrows()
    {
        // Arrange
        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("LLM unavailable"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tools.EvaluateQualityAsync("content"));

        Assert.Contains("evaluation failed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
