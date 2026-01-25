using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for DraftReportAgent.
/// Tests the agent's ability to generate initial draft reports.
/// </summary>
public class DraftReportAgentTests
{
    private readonly Mock<OllamaService> _mockOllamaService;
    private readonly Mock<ILogger<DraftReportAgent>> _mockLogger;
    private readonly DraftReportAgent _agent;

    public DraftReportAgentTests()
    {
        _mockOllamaService = new Mock<OllamaService>(null);
        _mockLogger = new Mock<ILogger<DraftReportAgent>>();
        _agent = new DraftReportAgent(_mockOllamaService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Test: Generate well-structured draft report from research brief.
    /// </summary>
    [Fact]
    public async Task GenerateDraftReportAsync_WithResearchBrief_ReturnsStructuredReport()
    {
        // Arrange
        var researchBrief = "Research quantum computing advances in 2024";
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Please research quantum computing." }
        };

        var draftContent = @"# Quantum Computing Advances in 2024

## Introduction
Quantum computing continues to advance rapidly in 2024...

## IBM's Quantum Roadmap
IBM announced significant progress with their quantum processors...

## Google's Quantum Developments
Google continues development of quantum error correction...

## Sources
[1] IBM Quantum Blog: https://ibmquantum.example.com
[2] Google Quantum AI: https://google.example.com/quantum";

        var rawResponse = new OllamaChatMessage
        {
            Role = "assistant",
            Content = draftContent
        };

        _mockOllamaService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await _agent.GenerateDraftReportAsync(researchBrief, conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Contains("Quantum", result.Content);
        Assert.NotEmpty(result.Sections);
        Assert.True(result.Sections.Count > 0, "Should extract sections from markdown");

        _mockOllamaService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Extract markdown sections from draft content.
    /// </summary>
    [Fact]
    public async Task GenerateDraftReportAsync_WithMarkdownSections_ParsesSectionsCorrectly()
    {
        // Arrange
        var researchBrief = "Research machine learning frameworks";
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Research ML frameworks." }
        };

        var draftContent = @"# Machine Learning Frameworks

## TensorFlow
TensorFlow is an open-source machine learning framework...

## PyTorch
PyTorch provides dynamic computational graphs...

## JAX
JAX brings functional programming to ML...";

        var rawResponse = new OllamaChatMessage
        {
            Role = "assistant",
            Content = draftContent
        };

        _mockOllamaService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await _agent.GenerateDraftReportAsync(researchBrief, conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Sections);
        
        // Verify sections were extracted
        var sectionTitles = result.Sections.Select(s => s.Title).ToList();
        Assert.Contains("TensorFlow", sectionTitles);
        Assert.Contains("PyTorch", sectionTitles);
        Assert.Contains("JAX", sectionTitles);

        // Verify section content
        var tensorflowSection = result.Sections.First(s => s.Title == "TensorFlow");
        Assert.NotEmpty(tensorflowSection.Content);
        Assert.Contains("open-source", tensorflowSection.Content);
    }

    /// <summary>
    /// Test: DraftReport includes metadata.
    /// </summary>
    [Fact]
    public async Task GenerateDraftReportAsync_IncludesMetadata_ForTrackingAndDebugging()
    {
        // Arrange
        var researchBrief = "Test brief";
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Test request" }
        };

        var draftContent = "# Test\n\n## Section 1\nContent here";

        var rawResponse = new OllamaChatMessage
        {
            Role = "assistant",
            Content = draftContent
        };

        _mockOllamaService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await _agent.GenerateDraftReportAsync(researchBrief, conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Metadata);
        Assert.Contains("generated_at", result.Metadata.Keys);
        Assert.Contains("phase", result.Metadata.Keys);
        Assert.Equal("initial_draft", result.Metadata["phase"]);
    }

    /// <summary>
    /// Test: LLM service exception handling.
    /// </summary>
    [Fact]
    public async Task GenerateDraftReportAsync_WhenLLMServiceThrows_WrapsException()
    {
        // Arrange
        var researchBrief = "Test brief";
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Test" }
        };

        var llmException = new InvalidOperationException("LLM service error");

        _mockOllamaService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(llmException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.GenerateDraftReportAsync(researchBrief, conversationHistory));

        Assert.NotNull(ex);
        Assert.Contains("draft", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(llmException, ex.InnerException);
    }

    /// <summary>
    /// Test: Handle content with no sections (single paragraph).
    /// </summary>
    [Fact]
    public async Task GenerateDraftReportAsync_WithoutMarkdownSections_HandlesGracefully()
    {
        // Arrange
        var researchBrief = "Quick summary needed";
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Quick summary" }
        };

        var draftContent = "This is a simple paragraph without any markdown sections. It's just plain text content.";

        var rawResponse = new OllamaChatMessage
        {
            Role = "assistant",
            Content = draftContent
        };

        _mockOllamaService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        // Act
        var result = await _agent.GenerateDraftReportAsync(researchBrief, conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Empty(result.Sections); // No sections extracted without markdown headers
    }

    /// <summary>
    /// Test: DraftReport creation with sections.
    /// </summary>
    [Fact]
    public void DraftReport_WithSections_ValidCreation()
    {
        // Arrange
        var content = "Full report content";
        var sections = new List<DraftReportSection>
        {
            new DraftReportSection 
            { 
                Title = "Section 1",
                Content = "Content 1",
                IdentifiedGaps = new List<string>()
            },
            new DraftReportSection 
            { 
                Title = "Section 2",
                Content = "Content 2",
                IdentifiedGaps = new List<string> { "Missing data" }
            }
        };

        // Act
        var result = new DraftReport
        {
            Content = content,
            Sections = sections,
            Metadata = new Dictionary<string, object> { { "key", "value" } }
        };

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.Content);
        Assert.Equal(2, result.Sections.Count);
        Assert.Contains("Section 1", result.Sections.Select(s => s.Title));
    }

    /// <summary>
    /// Test: DraftReportSection creation with gaps.
    /// </summary>
    [Fact]
    public void DraftReportSection_WithGaps_ValidCreation()
    {
        // Arrange
        var title = "Test Section";
        var content = "Section content";
        var gaps = new List<string> { "Gap 1", "Gap 2" };

        // Act
        var result = new DraftReportSection
        {
            Title = title,
            Content = content,
            IdentifiedGaps = gaps,
            QualityScore = 6
        };

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(content, result.Content);
        Assert.Equal(2, result.IdentifiedGaps.Count);
        Assert.Equal(6, result.QualityScore);
    }
}
