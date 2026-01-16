using DeepResearchAgent.Services;
using DeepResearchAgent.Tests.Fixtures;
using DeepResearchAgent.Tests.Helpers;
using TaskStatus = DeepResearchAgent.Services.TaskStatus;

namespace DeepResearchAgent.Tests.Helpers;

/// <summary>
/// Fluent API for building test data with minimal boilerplate.
/// Use method chaining to create realistic test scenarios.
/// </summary>
public class TestDataBuilder
{
    private string _agentId = Guid.NewGuid().ToString();
    private string _agentType = "TestAgent";
    private Dictionary<string, object> _capabilities = new();
    private TaskStatus _status = TaskStatus.Submitted;
    private List<string> _facts = new();
    private double _confidence = 0.85;
    private int _stepNumber = 1;
    private string _taskName = "Test Task";
    private string _taskDescription = "Test task description";

    #region Builder Methods

    public TestDataBuilder WithAgentId(string agentId)
    {
        _agentId = agentId;
        return this;
    }

    public TestDataBuilder WithAgentType(string agentType)
    {
        _agentType = agentType;
        return this;
    }

    public TestDataBuilder WithCapability(string name, bool enabled = true)
    {
        _capabilities[name] = enabled;
        return this;
    }

    public TestDataBuilder WithCapabilities(Dictionary<string, object> capabilities)
    {
        _capabilities = new Dictionary<string, object>(capabilities);
        return this;
    }

    public TestDataBuilder WithStatus(TaskStatus status)
    {
        _status = status;
        return this;
    }

    public TestDataBuilder WithFacts(params string[] facts)
    {
        _facts = new List<string>(facts);
        return this;
    }

    public TestDataBuilder WithConfidence(double confidence)
    {
        if (confidence < 0 || confidence > 1)
            throw new ArgumentException("Confidence must be between 0 and 1", nameof(confidence));
        _confidence = confidence;
        return this;
    }

    public TestDataBuilder WithStepNumber(int stepNumber)
    {
        _stepNumber = stepNumber;
        return this;
    }

    public TestDataBuilder WithTaskName(string taskName)
    {
        _taskName = taskName;
        return this;
    }

    public TestDataBuilder WithTaskDescription(string taskDescription)
    {
        _taskDescription = taskDescription;
        return this;
    }

    #endregion

    #region Build Methods

    /// <summary>Build an agent registration.</summary>
    public AgentRegistration BuildAgentRegistration()
    {
        return new AgentRegistration
        {
            AgentId = _agentId,
            AgentType = _agentType,
            ClientId = $"client-{Guid.NewGuid():N}",
            Capabilities = _capabilities,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>Build an agent task.</summary>
    public AgentTask BuildAgentTask()
    {
        return new AgentTask
        {
            Id = Guid.NewGuid().ToString(),
            Name = _taskName,
            Description = _taskDescription,
            Status = _status,
            Priority = 0,
            Input = new Dictionary<string, object>(),
            SubmittedAt = DateTime.UtcNow,
            VerificationRequired = true
        };
    }

    /// <summary>Build a reasoning step.</summary>
    public ReasoningStep BuildReasoningStep(int? stepNumber = null)
    {
        return new ReasoningStep
        {
            StepNumber = stepNumber ?? _stepNumber,
            Description = $"Step {stepNumber ?? _stepNumber}: Analysis",
            Logic = "Logical reasoning applied",
            Conclusions = new List<string>(_facts),
            Confidence = _confidence
        };
    }

    /// <summary>Build a verification result.</summary>
    public VerificationResult BuildVerificationResult()
    {
        return new VerificationResult
        {
            TaskId = Guid.NewGuid().ToString(),
            IsValid = _confidence >= 0.75,
            Confidence = _confidence,
            Issues = new List<string>(),
            VerifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>Build a task result.</summary>
    public AgentTaskResult BuildTaskResult()
    {
        return new AgentTaskResult
        {
            TaskId = Guid.NewGuid().ToString(),
            Status = _status,
            Result = string.Join("; ", _facts),
            CompletedAt = _status == TaskStatus.Completed ? DateTime.UtcNow : null
        };
    }

    /// <summary>Build a reasoning chain validation.</summary>
    public ReasoningChainValidation BuildReasoningChainValidation()
    {
        return new ReasoningChainValidation
        {
            IsValid = _confidence >= 0.7,
            Score = _confidence,
            Errors = new List<string>(),
            Warnings = new List<string>(),
            ValidatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Build a confidence score.</summary>
    public ConfidenceScore BuildConfidenceScore()
    {
        return new ConfidenceScore
        {
            Score = _confidence,
            Factors = new Dictionary<string, double>
            {
                { "Clarity", 0.9 },
                { "Completeness", 0.85 },
                { "Consistency", _confidence }
            },
            Reasoning = "Based on analysis of logical flow and evidence"
        };
    }

    /// <summary>Build a fact check result.</summary>
    public FactCheckResult BuildFactCheckResult()
    {
        var totalFacts = Math.Max(_facts.Count, 3);
        var verifiedCount = (int)Math.Ceiling(totalFacts * _confidence);

        return new FactCheckResult
        {
            VerifiedCount = verifiedCount,
            TotalCount = totalFacts,
            UnreliableFacts = new List<string>(),
            VerificationScore = _confidence
        };
    }

    /// <summary>Build a consistency check result.</summary>
    public ConsistencyCheckResult BuildConsistencyCheckResult()
    {
        return new ConsistencyCheckResult
        {
            IsConsistent = _confidence >= 0.75,
            Score = _confidence,
            Contradictions = new List<string>()
        };
    }

    /// <summary>Build server info.</summary>
    public LightningServerInfo BuildServerInfo()
    {
        return new LightningServerInfo
        {
            Version = "1.0.0",
            ApoEnabled = true,
            VerlEnabled = true,
            RegisteredAgents = 5,
            ActiveConnections = 3,
            StartedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    #endregion
}
