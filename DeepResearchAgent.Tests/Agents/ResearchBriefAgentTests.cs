using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for ResearchBriefAgent.
/// Tests the agent's ability to generate formal research briefs from conversations.
/// </summary>
public class ResearchBriefAgentTests
{
    private readonly Mock<OllamaService> _mockOllamaService;
    private readonly Mock<ILogger<ResearchBriefAgent>> _mockLogger;
    private readonly ResearchBriefAgent _agent;

    public ResearchBriefAgentTests()
    {
        _mockOllamaService = new Mock<OllamaService>(null);
        _mockLogger = new Mock<ILogger<ResearchBriefAgent>>();
        _agent = new ResearchBriefAgent(_mockOllamaService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Test: Generate research brief from clear conversation history.
    /// </summary>
    [Fact]
    public async Task GenerateResearchBriefAsync_WithClearConversation_ReturnsStructuredBrief()
    {
        // Arrange
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage 
            { 
                Role = "user", 
                Content = "I need research on quantum computing advances in 2024."
            },
            new ChatMessage 
            { 
                Role = "assistant", 
                Content = "I have enough information to begin research on quantum computing advances in 2024."
            }
        };

        var expectedBrief = new ResearchQuestion
        {
            ResearchBrief = "Research the latest breakthroughs and advancements in quantum computing technology during 2024, including developments from leading companies and research institutions.",
            Objectives = new List<string>
            {
                "Identify major quantum computing breakthroughs in 2024",
                "Document company milestones and achievements",
                "Analyze technical improvements in qubit stability",
                "Assess progress toward practical applications"
            },
            Scope = "Global quantum computing industry developments in 2024",
            CreatedAt = DateTime.UtcNow
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBrief);

        // Act
        var result = await _agent.GenerateResearchBriefAsync(conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ResearchBrief);
        Assert.NotEmpty(result.Objectives);
        Assert.NotNull(result.Scope);
        Assert.Equal(expectedBrief.ResearchBrief, result.ResearchBrief);
        Assert.Equal(expectedBrief.Objectives.Count, result.Objectives.Count);

        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Generate brief with specific requirements and constraints.
    /// </summary>
    [Fact]
    public async Task GenerateResearchBriefAsync_WithSpecificRequirements_IncludesObjectives()
    {
        // Arrange
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage 
            { 
                Role = "user", 
                Content = "Research the best machine learning frameworks for enterprise adoption, focusing on cost, scalability, and developer productivity."
            }
        };

        var expectedBrief = new ResearchQuestion
        {
            ResearchBrief = "Comprehensive analysis of machine learning frameworks suitable for enterprise adoption",
            Objectives = new List<string>
            {
                "Evaluate cost structure of different ML frameworks",
                "Assess scalability capabilities",
                "Compare developer productivity",
                "Analyze enterprise support and community"
            },
            Scope = "Enterprise-grade ML frameworks with focus on cost, scalability, and productivity",
            CreatedAt = DateTime.UtcNow
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBrief);

        // Act
        var result = await _agent.GenerateResearchBriefAsync(conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Objectives);
        Assert.True(result.Objectives.Count >= 3, "Should have multiple objectives");
        Assert.Contains("cost", result.ResearchBrief.ToLowerInvariant());
    }

    /// <summary>
    /// Test: Multiple messages in conversation are properly formatted.
    /// </summary>
    [Fact]
    public async Task GenerateResearchBriefAsync_WithMultipleMessages_FormatsAllMessages()
    {
        // Arrange
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "I want to research AI." },
            new ChatMessage { Role = "assistant", Content = "Could you be more specific?" },
            new ChatMessage { Role = "user", Content = "Specifically, LLM applications in healthcare." },
            new ChatMessage { Role = "assistant", Content = "That's helpful. I have enough information." }
        };

        var expectedBrief = new ResearchQuestion
        {
            ResearchBrief = "Research large language model applications in healthcare industry",
            Objectives = new List<string>
            {
                "Identify current healthcare LLM applications",
                "Analyze clinical use cases",
                "Review regulatory considerations"
            },
            Scope = "LLM applications in healthcare",
            CreatedAt = DateTime.UtcNow
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBrief);

        // Act
        var result = await _agent.GenerateResearchBriefAsync(conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("healthcare", result.ResearchBrief.ToLowerInvariant());

        // Verify that all messages were considered
        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.Is<List<OllamaChatMessage>>(msgs => msgs.Count == 1),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: LLM service exception handling.
    /// </summary>
    [Fact]
    public async Task GenerateResearchBriefAsync_WhenLLMServiceThrows_WrapsException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Test query" }
        };

        var llmException = new HttpRequestException("Connection failed");

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(llmException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.GenerateResearchBriefAsync(messages));

        Assert.NotNull(ex);
        Assert.Contains("brief", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(llmException, ex.InnerException);
    }

    /// <summary>
    /// Test: Research brief with empty objectives still valid.
    /// </summary>
    [Fact]
    public async Task GenerateResearchBriefAsync_WithMinimalObjectives_StillValid()
    {
        // Arrange
        var conversationHistory = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Research renewable energy." }
        };

        var expectedBrief = new ResearchQuestion
        {
            ResearchBrief = "Comprehensive overview of renewable energy technologies and initiatives",
            Objectives = new List<string>(),
            Scope = null,
            CreatedAt = DateTime.UtcNow
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBrief);

        // Act
        var result = await _agent.GenerateResearchBriefAsync(conversationHistory);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ResearchBrief);
        Assert.Empty(result.Objectives);
    }

    /// <summary>
    /// Test: ResearchQuestion data model creation.
    /// </summary>
    [Fact]
    public void ResearchQuestion_ValidCreation_AllFieldsPopulated()
    {
        // Arrange
        var brief = "Test research brief";
        var objectives = new List<string> { "Objective 1", "Objective 2" };
        var scope = "Test scope";

        // Act
        var result = new ResearchQuestion
        {
            ResearchBrief = brief,
            Objectives = objectives,
            Scope = scope,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(result);
        Assert.Equal(brief, result.ResearchBrief);
        Assert.Equal(2, result.Objectives.Count);
        Assert.Equal(scope, result.Scope);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
    }
}
