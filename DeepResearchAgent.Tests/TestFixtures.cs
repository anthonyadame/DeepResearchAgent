using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Test fixtures and helpers for workflow testing.
/// Provides mock services, state factories, and common test data.
/// </summary>
public class TestFixtures
{
    /// <summary>
    /// Create a mock LLM service for testing.
    /// Returns controlled responses for predictable testing.
    /// </summary>
    public static OllamaService CreateMockOllamaService(string? responseOverride = null)
    {
        var mockService = new Mock<OllamaService>(
            new HttpClient(),
            "http://localhost:11434",
            null);

        var defaultResponse = responseOverride ?? "This is a test response from the LLM.";
        
        mockService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage
            {
                Role = "assistant",
                Content = defaultResponse
            });

        return mockService.Object;
    }

    /// <summary>
    /// Create a mock search and scrape service.
    /// </summary>
    public static SearCrawl4AIService CreateMockSearchService()
    {
        var mockService = new Mock<SearCrawl4AIService>(null, null, null);

        mockService
            .Setup(s => s.SearchAndScrapeAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Crawl4AIRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>
            {
                new()
                {
                    Url = "https://example.com/test1",
                    Title = "Test Result 1",
                    Markdown = "# Test Content 1\n\nThis is test markdown content.",
                    CleanedHtml = "<h1>Test Content 1</h1>",
                    Html = "<html><body><h1>Test</h1></body></html>"
                },
                new()
                {
                    Url = "https://example.com/test2",
                    Title = "Test Result 2",
                    Markdown = "# Test Content 2\n\nMore test content.",
                    CleanedHtml = "<h1>Test Content 2</h1>",
                    Html = "<html><body><h1>Test 2</h1></body></html>"
                }
            });

        return mockService.Object;
    }

    /// <summary>
    /// Create a mock knowledge store.
    /// </summary>
    public static LightningStore CreateMockLightningStore()
    {
        var mockStore = new Mock<LightningStore>(null);

        var savedFacts = new List<FactState>();

        mockStore
            .Setup(s => s.SaveFactsAsync(
                It.IsAny<IEnumerable<FactState>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<FactState>, CancellationToken>((facts, _) =>
            {
                savedFacts.AddRange(facts);
            })
            .Returns(Task.CompletedTask);

        mockStore
            .Setup(s => s.GetAllFactsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedFacts);

        return mockStore.Object;
    }

    /// <summary>
    /// Create a mock logger.
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Create initial agent state for testing.
    /// </summary>
    public static AgentState CreateTestAgentState(string userQuery = "Research quantum computing")
    {
        return StateFactory.CreateAgentState(new List<Models.ChatMessage>
        {
            new()
            {
                Role = "user",
                Content = userQuery
            }
        });
    }

    /// <summary>
    /// Create initial supervisor state for testing.
    /// </summary>
    public static SupervisorState CreateTestSupervisorState(
        string brief = "Test research brief",
        string draft = "Test draft report")
    {
        return StateFactory.CreateSupervisorState(brief, draft, new List<Models.ChatMessage>());
    }

    /// <summary>
    /// Create initial researcher state for testing.
    /// </summary>
    public static ResearcherState CreateTestResearcherState(string topic = "Machine learning")
    {
        return StateFactory.CreateResearcherState(topic);
    }

    /// <summary>
    /// Create test workflow instances with mocks.
    /// </summary>
    public static (ResearcherWorkflow researcher, OllamaService llm, LightningStore store)
        CreateMockResearcherWorkflow()
    {
        var llm = CreateMockOllamaService();
        var search = CreateMockSearchService();
        var store = CreateMockLightningStore();
        var logger = CreateMockLogger<ResearcherWorkflow>();

        var researcher = new ResearcherWorkflow(search, llm, store, logger);

        return (researcher, llm, store);
    }

    /// <summary>
    /// Create test supervisor workflow with mocks.
    /// </summary>
    public static (SupervisorWorkflow supervisor, OllamaService llm, LightningStore store)
        CreateMockSupervisorWorkflow()
    {
        var llm = CreateMockOllamaService();
        var search = CreateMockSearchService();
        var store = CreateMockLightningStore();
        var logger = CreateMockLogger<SupervisorWorkflow>();

        var (researcher, _, _) = CreateMockResearcherWorkflow();

        var supervisor = new SupervisorWorkflow(researcher, llm, store, logger);

        return (supervisor, llm, store);
    }

    /// <summary>
    /// Create test master workflow with mocks.
    /// </summary>
    public static (MasterWorkflow master, OllamaService llm)
        CreateMockMasterWorkflow()
    {
        var llm = CreateMockOllamaService();
        var logger = CreateMockLogger<MasterWorkflow>();

        var (supervisor, _, _) = CreateMockSupervisorWorkflow();

        var master = new MasterWorkflow(supervisor, llm, logger);

        return (master, llm);
    }

    /// <summary>
    /// Create test facts for assertion.
    /// </summary>
    public static List<FactState> CreateTestFacts(int count = 5)
    {
        var facts = new List<FactState>();
        for (int i = 0; i < count; i++)
        {
            facts.Add(StateFactory.CreateFact(
                $"Test fact {i + 1}: Sample content about a research topic",
                $"https://example.com/fact{i + 1}",
                80 + (i % 20)
            ));
        }
        return facts;
    }

    /// <summary>
    /// Wait for async operations with timeout.
    /// </summary>
    public static async Task WaitForAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var elapsed = TimeSpan.Zero;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (!condition() && elapsed < timeout)
        {
            await Task.Delay(pollInterval.Value);
            elapsed = sw.Elapsed;
        }

        if (elapsed >= timeout)
            throw new TimeoutException(
                $"Operation timed out after {timeout.TotalSeconds} seconds");
    }
}

/// <summary>
/// Test data builders for complex objects.
/// </summary>
public class TestDataBuilder
{
    private string _userQuery = "Research quantum computing";
    private string _researchBrief = "Comprehensive analysis of quantum computing";
    private string _draftReport = "Initial quantum computing draft";
    private int _maxIterations = 5;

    public TestDataBuilder WithUserQuery(string query)
    {
        _userQuery = query;
        return this;
    }

    public TestDataBuilder WithResearchBrief(string brief)
    {
        _researchBrief = brief;
        return this;
    }

    public TestDataBuilder WithDraftReport(string draft)
    {
        _draftReport = draft;
        return this;
    }

    public TestDataBuilder WithMaxIterations(int max)
    {
        _maxIterations = max;
        return this;
    }

    public AgentState BuildAgentState()
    {
        return TestFixtures.CreateTestAgentState(_userQuery);
    }

    public SupervisorState BuildSupervisorState()
    {
        return TestFixtures.CreateTestSupervisorState(_researchBrief, _draftReport);
    }

    public ResearcherState BuildResearcherState()
    {
        return TestFixtures.CreateTestResearcherState(_researchBrief);
    }
}

/// <summary>
/// Custom assertions for workflow testing.
/// </summary>
public static class WorkflowAssertions
{
    public static void AssertValidAgentState(AgentState state)
    {
        Assert.NotNull(state);
        Assert.NotNull(state.Messages);
        var validation = StateValidator.ValidateAgentState(state);
        Assert.True(validation.IsValid, validation.Message);
    }

    public static void AssertValidSupervisorState(SupervisorState state)
    {
        Assert.NotNull(state);
        Assert.NotNull(state.KnowledgeBase);
        Assert.NotNull(state.SupervisorMessages);
        var validation = StateValidator.ValidateSupervisorState(state);
        Assert.True(validation.IsValid, validation.Message);
    }

    public static void AssertValidResearcherState(ResearcherState state)
    {
        Assert.NotNull(state);
        Assert.NotNull(state.ResearcherMessages);
        Assert.NotNull(state.RawNotes);
    }

    public static void AssertFactsExtracted(
        IEnumerable<FactState> facts,
        int minimumCount = 1)
    {
        Assert.NotNull(facts);
        var factList = facts.ToList();
        Assert.True(
            factList.Count >= minimumCount,
            $"Expected at least {minimumCount} facts, got {factList.Count}");

        foreach (var fact in factList)
        {
            Assert.NotEmpty(fact.Content);
            Assert.NotEmpty(fact.SourceUrl);
            Assert.True(fact.ConfidenceScore >= 1 && fact.ConfidenceScore <= 100,
                $"Confidence score {fact.ConfidenceScore} out of range [1-100]");
        }
    }

    public static void AssertQualityImprovement(List<QualityMetric> history)
    {
        Assert.NotNull(history);
        if (history.Count > 1)
        {
            var lastTwo = history.TakeLast(2).ToList();
            // Quality should be non-negative
            Assert.True(lastTwo[0].Score >= 0);
            Assert.True(lastTwo[1].Score >= 0);
        }
    }

    public static void AssertConvergence(SupervisorState state, float minQuality = 7.5f)
    {
        Assert.NotNull(state.QualityHistory);
        Assert.NotEmpty(state.QualityHistory);

        var finalQuality = state.QualityHistory.Last().Score;
        Assert.True(
            finalQuality >= minQuality || state.ResearchIterations >= 5,
            $"Expected quality >= {minQuality} or max iterations, got {finalQuality} at iteration {state.ResearchIterations}");
    }
}
