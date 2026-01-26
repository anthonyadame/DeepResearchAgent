using DeepResearchAgent.Services;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Services;

/// <summary>
/// Integration tests for ToolInvocationService.
/// Tests the tool routing, parameter handling, and execution.
/// </summary>
public class ToolInvocationServiceTests
{
    private readonly Mock<IWebSearchProvider> _mockSearchProvider;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ILogger<ToolInvocationService>> _mockLogger;
    private readonly ToolInvocationService _service;

    public ToolInvocationServiceTests()
    {
        _mockSearchProvider = new Mock<IWebSearchProvider>();
        _mockSearchProvider.Setup(x => x.ProviderName).Returns("test");
        _mockLlmService = new Mock<OllamaService>(null);
        _mockLogger = new Mock<ILogger<ToolInvocationService>>();
        _service = new ToolInvocationService(_mockSearchProvider.Object, _mockLlmService.Object, _mockLogger.Object);
    }

    #region Tool Discovery Tests

    [Fact]
    public void GetAvailableTools_ReturnsAllToolDescriptors()
    {
        // Act
        var tools = _service.GetAvailableTools();

        // Assert
        Assert.NotEmpty(tools);
        Assert.Equal(5, tools.Count);
        
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("websearch", toolNames);
        Assert.Contains("qualityevaluation", toolNames);
        Assert.Contains("summarize", toolNames);
        Assert.Contains("extractfacts", toolNames);
        Assert.Contains("refinedraft", toolNames);
    }

    [Fact]
    public void GetAvailableTools_EachToolHasDescriptionAndParameters()
    {
        // Act
        var tools = _service.GetAvailableTools();

        // Assert
        foreach (var tool in tools)
        {
            Assert.NotEmpty(tool.DisplayName);
            Assert.NotEmpty(tool.Description);
            Assert.NotEmpty(tool.Parameters);
        }
    }

    #endregion

    #region WebSearch Tool Invocation Tests

    [Fact]
    public async Task InvokeToolAsync_WebSearch_ExecutesSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "query", "quantum computing" },
            { "maxResults", 5 }
        };

        var searchResults = new List<WebSearchResult>
        {
            new WebSearchResult 
            { 
                Title = "Quantum Computing",
                Url = "https://example.com/quantum",
                Content = "Article about quantum computing...",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync("quantum computing", 5, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _service.InvokeToolAsync("websearch", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<WebSearchResult>>(result);
        var resultList = (List<WebSearchResult>)result;
        Assert.NotEmpty(resultList);
    }

    [Fact]
    public async Task InvokeToolAsync_WebSearch_CaseInsensitive()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "query", "test" },
            { "maxResults", 10 }
        };

        var searchResults = new List<WebSearchResult>();

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act - test different cases
        var result1 = await _service.InvokeToolAsync("WEBSEARCH", parameters);
        var result2 = await _service.InvokeToolAsync("WebSearch", parameters);
        var result3 = await _service.InvokeToolAsync("websearch", parameters);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }

    #endregion

    #region Quality Evaluation Tool Invocation Tests

    [Fact]
    public async Task InvokeToolAsync_QualityEvaluation_ExecutesSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "content", "Sample research content about AI..." },
            { "dimensions", new[] { "accuracy", "completeness" } }
        };

        var qualityResult = new QualityEvaluationResult
        {
            OverallScore = 8.5f,
            DimensionScores = new Dictionary<string, DimensionScore>
            {
                { "accuracy", new DimensionScore { Score = 8.0f, Reason = "Well-sourced" } },
                { "completeness", new DimensionScore { Score = 9.0f, Reason = "Comprehensive" } }
            },
            Summary = "Good quality content"
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<QualityEvaluationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(qualityResult);

        // Act
        var result = await _service.InvokeToolAsync("qualityevaluation", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<QualityEvaluationResult>(result);
        var evalResult = (QualityEvaluationResult)result;
        Assert.Equal(8.5f, evalResult.OverallScore);
    }

    #endregion

    #region Summarization Tool Invocation Tests

    [Fact]
    public async Task InvokeToolAsync_Summarize_ExecutesSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "pageContent", "Long article content..." },
            { "maxLength", 300 }
        };

        var summaryResult = new PageSummaryResult
        {
            Summary = "This article discusses...",
            KeyPoints = new List<string> { "Point 1", "Point 2", "Point 3" }
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryResult);

        // Act
        var result = await _service.InvokeToolAsync("summarize", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PageSummaryResult>(result);
        var summResult = (PageSummaryResult)result;
        Assert.NotEmpty(summResult.Summary);
        Assert.NotEmpty(summResult.KeyPoints);
    }

    #endregion

    #region Fact Extraction Tool Invocation Tests

    [Fact]
    public async Task InvokeToolAsync_ExtractFacts_ExecutesSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "content", "IBM announced a new quantum processor. Google made breakthroughs." },
            { "topic", "quantum computing" }
        };

        var factResult = new FactExtractionResult
        {
            Facts = new List<ExtractedFact>
            {
                new ExtractedFact 
                { 
                    Statement = "IBM announced a new quantum processor",
                    Confidence = 0.95f,
                    Source = "announcement",
                    Category = "product"
                },
                new ExtractedFact 
                { 
                    Statement = "Google made breakthroughs in quantum",
                    Confidence = 0.90f,
                    Source = "news",
                    Category = "research"
                }
            }
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(factResult);

        // Act
        var result = await _service.InvokeToolAsync("extractfacts", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<FactExtractionResult>(result);
        var factExResult = (FactExtractionResult)result;
        Assert.Equal(2, factExResult.Facts.Count);
    }

    #endregion

    #region Refine Draft Tool Invocation Tests

    [Fact]
    public async Task InvokeToolAsync_RefineDraft_ExecutesSuccessfully()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "draftReport", "Initial draft content..." },
            { "feedback", "Add more recent data and improve clarity" },
            { "iterationNumber", 2 }
        };

        var refineResult = new RefinedDraftResult
        {
            RefinedReport = "Improved draft with recent data and better clarity...",
            ChangesMade = new List<string> 
            { 
                "Added 2024 data",
                "Improved section structure"
            },
            ImprovementScore = 0.85f
        };

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<RefinedDraftResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refineResult);

        // Act
        var result = await _service.InvokeToolAsync("refinedraft", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RefinedDraftResult>(result);
        var refResult = (RefinedDraftResult)result;
        Assert.NotEmpty(refResult.RefinedReport);
        Assert.NotEmpty(refResult.ChangesMade);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvokeToolAsync_UnknownTool_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InvokeToolAsync("unknowntool", parameters));

        Assert.Contains("Unknown tool", ex.Message);
    }

    [Fact]
    public async Task InvokeToolAsync_MissingRequiredParameter_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "maxResults", 10 }
            // Missing "query" parameter
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InvokeToolAsync("websearch", parameters));

        Assert.Contains("Required parameter missing", ex.Message);
    }

    [Fact]
    public async Task InvokeToolAsync_WhenToolThrows_WrapsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "query", "test" },
            { "maxResults", 10 }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Search failed"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InvokeToolAsync("websearch", parameters));

        Assert.Contains("Tool execution failed", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    #endregion
}
