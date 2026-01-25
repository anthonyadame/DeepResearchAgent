using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for ClarifyAgent.
/// Tests the agent's ability to determine if user clarification is needed.
/// </summary>
public class ClarifyAgentTests
{
    private readonly Mock<OllamaService> _mockOllamaService;
    private readonly Mock<ILogger<ClarifyAgent>> _mockLogger;
    private readonly ClarifyAgent _agent;

    public ClarifyAgentTests()
    {
        _mockOllamaService = new Mock<OllamaService>(null);
        _mockLogger = new Mock<ILogger<ClarifyAgent>>();
        _agent = new ClarifyAgent(_mockOllamaService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Test: When user provides detailed query, no clarification should be needed.
    /// </summary>
    [Fact]
    public async Task ClarifyAsync_WithDetailedQuery_ReturnsNoClarificationNeeded()
    {
        // Arrange
        var detailedMessages = new List<ChatMessage>
        {
            new ChatMessage 
            { 
                Role = "user", 
                Content = "Please research the latest advances in quantum computing for 2024, including breakthroughs from companies like IBM, Google, and IonQ. I need to understand current NISQ applications and the timeline for fault-tolerant quantum computers."
            }
        };

        var clarificationResult = new ClarificationResult
        {
            NeedClarification = false,
            Question = string.Empty,
            Verification = "I have sufficient information to begin research on quantum computing advances in 2024."
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clarificationResult);

        // Act
        var result = await _agent.ClarifyAsync(detailedMessages);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedClarification);
        Assert.NotEmpty(result.Verification);
        Assert.Empty(result.Question);

        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: When user provides vague query, clarification should be requested.
    /// </summary>
    [Fact]
    public async Task ClarifyAsync_WithVagueQuery_ReturnsClarificationNeeded()
    {
        // Arrange
        var vagueMessages = new List<ChatMessage>
        {
            new ChatMessage 
            { 
                Role = "user", 
                Content = "Tell me about technology."
            }
        };

        var clarificationResult = new ClarificationResult
        {
            NeedClarification = true,
            Question = "Could you be more specific? Which area of technology interests you? For example: AI, cloud computing, cybersecurity, blockchain, etc.?",
            Verification = string.Empty
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clarificationResult);

        // Act
        var result = await _agent.ClarifyAsync(vagueMessages);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NeedClarification);
        Assert.NotEmpty(result.Question);
        Assert.Empty(result.Verification);

        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Empty conversation should request clarification.
    /// </summary>
    [Fact]
    public async Task ClarifyAsync_WithEmptyMessages_RequestsClarification()
    {
        // Arrange
        var emptyMessages = new List<ChatMessage>();

        var clarificationResult = new ClarificationResult
        {
            NeedClarification = true,
            Question = "Please provide your research request or topic you'd like me to research.",
            Verification = string.Empty
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clarificationResult);

        // Act
        var result = await _agent.ClarifyAsync(emptyMessages);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NeedClarification);
        Assert.NotEmpty(result.Question);

        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: LLM service exception should be wrapped and rethrown.
    /// </summary>
    [Fact]
    public async Task ClarifyAsync_WhenLLMServiceThrows_WrapsException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Test query" }
        };

        var llmException = new HttpRequestException("Connection refused");

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(llmException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ClarifyAsync(messages));

        Assert.NotNull(ex);
        Assert.Contains("clarification", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(llmException, ex.InnerException);
    }

    /// <summary>
    /// Test: Multiple messages in conversation history are properly formatted.
    /// </summary>
    [Fact]
    public async Task ClarifyAsync_WithMultipleMessages_FormatsAllMessages()
    {
        // Arrange
        var multipleMessages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "I want to research AI." },
            new ChatMessage { Role = "assistant", Content = "Could you provide more details?" },
            new ChatMessage { Role = "user", Content = "Specifically, I'm interested in LLMs and their applications in enterprise software." }
        };

        var clarificationResult = new ClarificationResult
        {
            NeedClarification = false,
            Question = string.Empty,
            Verification = "I have sufficient information to begin research on LLMs and enterprise applications."
        };

        _mockOllamaService
            .Setup(s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clarificationResult);

        // Act
        var result = await _agent.ClarifyAsync(multipleMessages);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedClarification);

        // Verify that InvokeWithStructuredOutputAsync was called with message list
        _mockOllamaService.Verify(
            s => s.InvokeWithStructuredOutputAsync<ClarificationResult>(
                It.Is<List<OllamaChatMessage>>(msgs => msgs.Count == 1),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: ClarificationResult with all required fields populated.
    /// </summary>
    [Fact]
    public void ClarificationResult_ValidCreation_AllFieldsPopulated()
    {
        // Arrange
        var question = "Could you specify the target audience for this research?";
        var verification = "I understand you need research on X.";

        // Act
        var result = new ClarificationResult
        {
            NeedClarification = true,
            Question = question,
            Verification = verification
        };

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NeedClarification);
        Assert.Equal(question, result.Question);
        Assert.Equal(verification, result.Verification);
    }
}
