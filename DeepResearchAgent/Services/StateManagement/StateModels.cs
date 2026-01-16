namespace DeepResearchAgent.Services.StateManagement;

/// <summary>
/// Represents a fact extracted during research with verification metadata.
/// </summary>
public class FactState
{
    /// <summary>Unique identifier for this fact.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The fact content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Source URL where fact was found.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Confidence score (0-1) for this fact.</summary>
    public double ConfidenceScore { get; set; }

    /// <summary>Whether this fact has been verified.</summary>
    public bool IsVerified { get; set; }

    /// <summary>When this fact was extracted.</summary>
    public DateTime ExtractedAt { get; set; }

    /// <summary>Verification status.</summary>
    public FactVerificationStatus VerificationStatus { get; set; }
}

public enum FactVerificationStatus
{
    Pending,
    Verified,
    Disputed,
    Failed
}

/// <summary>
/// Represents the state of an agent task execution.
/// </summary>
public class TaskExecutionState
{
    public string TaskId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public TaskExecutionStatus Status { get; set; }
    public Dictionary<string, object> Input { get; set; } = new();
    public Dictionary<string, object>? Output { get; set; }
    public List<string> ExecutionLogs { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    public string? ErrorMessage { get; set; }
}

public enum TaskExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Represents the state of a workflow execution.
/// </summary>
public class WorkflowExecutionState
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public enum WorkflowExecutionStatus
{
    NotStarted,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Represents the state of a supervision cycle.
/// </summary>
public class SupervisionState
{
    public string SupervisionId { get; set; } = string.Empty;
    public string ResearchId { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    public SupervisionStatus Status { get; set; }
    public double DraftQualityScore { get; set; }
    public double? PreviousQualityScore { get; set; }
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Recommendation { get; set; }
}

public enum SupervisionStatus
{
    Pending,
    InProgress,
    QualityCheckFailed,
    RedTeamReview,
    Approved,
    RejectedForRevision,
    Completed
}

/// <summary>
/// Aggregated state view across multiple entities.
/// </summary>
public class ApplicationStateSnapshot
{
    public string SnapshotId { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public Dictionary<string, AgentStateModel> AgentStates { get; set; } = new();
    public Dictionary<string, ResearchStateModel> ResearchStates { get; set; } = new();
    public Dictionary<string, WorkflowExecutionState> WorkflowStates { get; set; } = new();
    public List<string> ActiveTaskIds { get; set; } = new();
    public Dictionary<string, object> GlobalMetrics { get; set; } = new();
    
    /// <summary>Check if application is in healthy state.</summary>
    public bool IsHealthy =>
        AgentStates.All(a => a.Value.Status != AgentStatus.Failed) &&
        ResearchStates.All(r => r.Value.Status != ResearchStatus.Failed);
}

/// <summary>
/// State change event for event-based cache invalidation.
/// </summary>
public class StateChangeEvent
{
    public string EventId { get; set; } = string.Empty;
    public StateChangeType ChangeType { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public DateTime OccurredAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
}

public enum StateChangeType
{
    Created,
    Updated,
    Deleted,
    StatusChanged,
    ProgressUpdated
}
