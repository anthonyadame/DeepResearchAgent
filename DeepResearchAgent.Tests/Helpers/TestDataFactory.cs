using DeepResearchAgent.Services;
using TaskStatus = DeepResearchAgent.Services.TaskStatus;

namespace DeepResearchAgent.Tests.Helpers;

/// <summary>
/// Factory methods for common test data scenarios.
/// Provides pre-configured test objects for frequently-used test situations.
/// </summary>
public static class TestDataFactory
{
    #region Agent Data

    /// <summary>Creates a valid research agent with full capabilities.</summary>
    public static AgentRegistration CreateValidResearchAgent(string? agentId = null)
        => new TestDataBuilder()
            .WithAgentId(agentId ?? "test-research-agent")
            .WithAgentType("ResearchOrchestrator")
            .WithCapability("research", true)
            .WithCapability("synthesis", true)
            .WithCapability("verification", true)
            .WithCapability("web_search", true)
            .WithCapability("content_scraping", true)
            .BuildAgentRegistration();

    /// <summary>Creates a minimal agent with only required fields.</summary>
    public static AgentRegistration CreateMinimalAgent()
        => new TestDataBuilder()
            .WithAgentId("minimal-agent")
            .WithAgentType("Agent")
            .BuildAgentRegistration();

    /// <summary>Creates an agent with specific capabilities.</summary>
    public static AgentRegistration CreateAgentWithCapabilities(params string[] capabilities)
    {
        var builder = new TestDataBuilder()
            .WithAgentId($"agent-{Guid.NewGuid():N}")
            .WithAgentType("SpecializedAgent");

        foreach (var capability in capabilities)
        {
            builder.WithCapability(capability, true);
        }

        return builder.BuildAgentRegistration();
    }

    #endregion

    #region Task Data

    /// <summary>Creates a research task with default parameters.</summary>
    public static AgentTask CreateResearchTask(string query = "test query")
        => new TestDataBuilder()
            .WithTaskName("Research Task")
            .WithTaskDescription($"Research: {query}")
            .WithStatus(TaskStatus.Submitted)
            .BuildAgentTask();

    /// <summary>Creates a task with specific status.</summary>
    public static AgentTask CreateTaskWithStatus(TaskStatus status)
        => new TestDataBuilder()
            .WithStatus(status)
            .BuildAgentTask();

    /// <summary>Creates multiple tasks for concurrent testing.</summary>
    public static List<AgentTask> CreateMultipleTasks(int count)
        => Enumerable.Range(0, count)
            .Select(_ => CreateResearchTask())
            .ToList();

    #endregion

    #region Reasoning Data

    /// <summary>Creates a valid reasoning chain with multiple steps.</summary>
    public static List<ReasoningStep> CreateValidReasoningChain()
        => new()
        {
            new TestDataBuilder()
                .WithStepNumber(1)
                .WithFacts("Fact 1", "Fact 2")
                .WithConfidence(0.95)
                .BuildReasoningStep(1),
            new TestDataBuilder()
                .WithStepNumber(2)
                .WithFacts("Fact 3", "Fact 4")
                .WithConfidence(0.85)
                .BuildReasoningStep(2),
            new TestDataBuilder()
                .WithStepNumber(3)
                .WithFacts("Conclusion 1")
                .WithConfidence(0.90)
                .BuildReasoningStep(3)
        };

    /// <summary>Creates a simple reasoning chain with given confidence.</summary>
    public static List<ReasoningStep> CreateReasoningChainWithConfidence(double confidence)
        => new()
        {
            new TestDataBuilder()
                .WithConfidence(confidence)
                .WithFacts("Finding 1")
                .BuildReasoningStep(1),
            new TestDataBuilder()
                .WithConfidence(confidence * 0.9)
                .WithFacts("Finding 2")
                .BuildReasoningStep(2)
        };

    /// <summary>Creates a single reasoning step.</summary>
    public static ReasoningStep CreateReasoningStep(int stepNumber = 1, double confidence = 0.85)
        => new TestDataBuilder()
            .WithStepNumber(stepNumber)
            .WithConfidence(confidence)
            .WithFacts("Analysis result")
            .BuildReasoningStep(stepNumber);

    #endregion

    #region Verification Data

    /// <summary>Creates a high-confidence verification result.</summary>
    public static VerificationResult CreateHighConfidenceVerification(double confidence = 0.95)
        => new TestDataBuilder()
            .WithConfidence(confidence)
            .BuildVerificationResult();

    /// <summary>Creates a low-confidence verification result.</summary>
    public static VerificationResult CreateLowConfidenceVerification(double confidence = 0.45)
        => new TestDataBuilder()
            .WithConfidence(confidence)
            .BuildVerificationResult();

    /// <summary>Creates verification results with various confidence levels.</summary>
    public static VerificationResult CreateVerificationResult(
        double confidence = 0.95,
        bool isValid = true)
        => new TestDataBuilder()
            .WithConfidence(confidence)
            .BuildVerificationResult();

    /// <summary>Creates multiple verification results for batch testing.</summary>
    public static List<VerificationResult> CreateMultipleVerifications(
        int count,
        double averageConfidence = 0.90)
        => Enumerable.Range(0, count)
            .Select(i => CreateVerificationResult(
                averageConfidence + (Random.Shared.NextDouble() - 0.5) * 0.2
            ))
            .ToList();

    #endregion

    #region Consistency Data

    /// <summary>Creates a valid consistency check result.</summary>
    public static ConsistencyCheckResult CreateConsistentStatements()
        => new TestDataBuilder()
            .WithConfidence(0.95)
            .BuildConsistencyCheckResult();

    /// <summary>Creates an inconsistent check result.</summary>
    public static ConsistencyCheckResult CreateInconsistentStatements()
        => new ConsistencyCheckResult
        {
            IsConsistent = false,
            Score = 0.40,
            Contradictions = new List<string>
            {
                "Statement A contradicts Statement C",
                "Statement B conflicts with Statement D"
            }
        };

    #endregion

    #region Fact Data

    /// <summary>Creates a fact check result with extracted facts.</summary>
    public static FactCheckResult CreateFactCheckResult(int factCount = 5, double confidence = 0.90)
    {
        var facts = Enumerable.Range(1, factCount)
            .Select(i => $"Fact {i}")
            .ToList();

        return new TestDataBuilder()
            .WithFacts(facts.ToArray())
            .WithConfidence(confidence)
            .BuildFactCheckResult();
    }

    /// <summary>Creates a fact check result indicating unreliable sources.</summary>
    public static FactCheckResult CreateUnreliableFactsResult()
        => new FactCheckResult
        {
            VerifiedCount = 2,
            TotalCount = 5,
            UnreliableFacts = new List<string> { "Fact 1", "Fact 3", "Fact 5" },
            VerificationScore = 0.40
        };

    #endregion

    #region Server Data

    /// <summary>Creates a healthy server info response.</summary>
    public static LightningServerInfo CreateHealthyServerInfo()
        => new TestDataBuilder()
            .BuildServerInfo();

    /// <summary>Creates server info with specific configuration.</summary>
    public static LightningServerInfo CreateServerInfoWithConfig(
        bool apoEnabled = true,
        bool verlEnabled = true,
        int registeredAgents = 5)
        => new LightningServerInfo
        {
            Version = "1.0.0",
            ApoEnabled = apoEnabled,
            VerlEnabled = verlEnabled,
            RegisteredAgents = registeredAgents,
            ActiveConnections = 3,
            StartedAt = DateTime.UtcNow.AddHours(-2)
        };

    #endregion

    #region Confidence Data

    /// <summary>Creates confidence score with specific factors.</summary>
    public static ConfidenceScore CreateConfidenceScore(double score = 0.85)
        => new TestDataBuilder()
            .WithConfidence(score)
            .BuildConfidenceScore();

    #endregion

    #region Error Scenarios

    /// <summary>Creates test data representing a failed task.</summary>
    public static AgentTaskResult CreateFailedTask()
        => new TestDataBuilder()
            .WithStatus(TaskStatus.Failed)
            .BuildTaskResult();

    /// <summary>Creates test data representing a completed task.</summary>
    public static AgentTaskResult CreateCompletedTask()
        => new TestDataBuilder()
            .WithStatus(TaskStatus.Completed)
            .WithFacts("Task completed successfully")
            .BuildTaskResult();

    /// <summary>Creates test data for task requiring verification.</summary>
    public static AgentTask CreateTaskRequiringVerification()
        => new TestDataBuilder()
            .WithStatus(TaskStatus.VerificationRequired)
            .BuildAgentTask();

    #endregion
}
