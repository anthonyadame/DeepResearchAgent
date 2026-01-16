# State Management Quick Reference

## Summary: What's Ready to Use

✅ **Phase 1 Complete** - All state management components are production-ready.

---

## Core Classes & Methods

### StateFactory
```csharp
// Create states
StateFactory.CreateAgentState()
StateFactory.CreateAgentState(messages)
StateFactory.CreateSupervisorState()
StateFactory.CreateSupervisorState(brief, draft, messages)
StateFactory.CreateResearcherState()
StateFactory.CreateResearcherState(topic)
StateFactory.CreateFact(content, url, confidence)
StateFactory.CreateCritique(author, concern, severity)
StateFactory.CreateQualityMetric(score, feedback, iteration)

// Clone states
StateFactory.CloneAgentState(original)
StateFactory.CloneSupervisorState(original)
StateFactory.CloneResearcherState(original)
```

### StateValidator
```csharp
// Validate
StateValidator.ValidateAgentState(state)
StateValidator.ValidateSupervisorState(state)
StateValidator.ValidateResearcherState(state)
StateValidator.ValidateFact(fact)
StateValidator.ValidateCritique(critique)
StateValidator.ValidateQualityMetric(metric)

// Check convergence
StateValidator.ShouldContinueDiffusion(state, maxIterations)

// Get health
StateValidator.GetHealthReport(supervisorState)
StateValidator.ValidateKnowledgeBase(facts)
StateValidator.ValidateActiveCritiques(critiques)
```

### StateManager
```csharp
// Create manager
var manager = new StateManager();

// Capture snapshots
manager.CaptureSnapshot(state, "phase_name")
manager.CaptureSupervisorSnapshot(state, "phase_name")

// Merge states
manager.MergeSupervisorState(target, source)
manager.MergeAgentState(target, source)

// Get history
manager.GetHistory()
manager.GetSupervisorHistory()
manager.ClearHistory()

// Get iteration
manager.CurrentIteration
```

### StateAccumulator<T>
```csharp
// Create
var acc = new StateAccumulator<string>();

// Add
acc.Add(item)
acc.AddRange(items)

// Operations
acc.Replace(items)
acc.Clear()

// Query
acc.Items
acc.Count
acc.Any
acc.LastOrDefault

// Merge
var merged = acc1 + acc2

// Clone
acc.Clone()
```

### StateTransitionRouter
```csharp
// Create
var router = new StateTransitionRouter("start_node", "__end__");

// Register edges
router.RegisterEdge("from", "to")
router.RegisterConditionalEdge("from", state => "node")
router.RegisterParallelEdge("from", "node1", "node2")
router.RegisterTransition("from", state => transition)

// Get transition
router.GetNextTransition(currentNode, state)

// Query
router.GetStartNode()
router.IsTerminal(nodeName)
router.GetRegisteredNodes()
```

---

## State Models

### AgentState
```csharp
public class AgentState
{
    public List<ChatMessage> Messages { get; set; }
    public string? ResearchBrief { get; set; }
    public List<ChatMessage> SupervisorMessages { get; set; }
    public List<string> RawNotes { get; set; }
    public List<string> Notes { get; set; }
    public string DraftReport { get; set; }
    public string FinalReport { get; set; }
}
```

### SupervisorState
```csharp
public class SupervisorState
{
    public List<ChatMessage> SupervisorMessages { get; set; }
    public string ResearchBrief { get; set; }
    public string DraftReport { get; set; }
    public List<string> RawNotes { get; set; }
    public List<FactState> KnowledgeBase { get; set; }
    public int ResearchIterations { get; set; }
    public List<CritiqueState> ActiveCritiques { get; set; }
    public List<QualityMetric> QualityHistory { get; set; }
    public bool NeedsQualityRepair { get; set; }
}
```

### ResearcherState
```csharp
public class ResearcherState
{
    public List<ChatMessage> ResearcherMessages { get; set; }
    public int ToolCallIterations { get; set; }
    public string ResearchTopic { get; set; }
    public string CompressedResearch { get; set; }
    public List<string> RawNotes { get; set; }
}
```

### FactState
```csharp
public record FactState
{
    public required string Content { get; init; }
    public required string SourceUrl { get; init; }
    public required int ConfidenceScore { get; init; }  // 1-100
    public bool IsDisputed { get; init; } = false;
}
```

### CritiqueState
```csharp
public record CritiqueState
{
    public required string Author { get; init; }
    public required string Concern { get; init; }
    public required int Severity { get; init; }  // 1-10
    public bool Addressed { get; init; } = false;
}
```

### QualityMetric
```csharp
public record QualityMetric
{
    public required float Score { get; init; }  // 0-10
    public required string Feedback { get; init; }
    public required int Iteration { get; init; }
}
```

---

## Common Patterns

### 1. Initialize and Validate
```csharp
var state = StateFactory.CreateAgentState(userMessages);
var validation = StateValidator.ValidateAgentState(state);
if (!validation.IsValid)
    throw new InvalidOperationException(
        $"Invalid state: {string.Join("; ", validation.Errors)}");
```

### 2. Route Based on State
```csharp
var router = new StateTransitionRouter();
router.RegisterConditionalEdge("check_brief",
    state => string.IsNullOrEmpty(state.ResearchBrief) ? "clarify" : "research");

var transition = router.GetNextTransition("check_brief", state);
// transition.NextNode = "clarify" or "research"
```

### 3. Track Progress
```csharp
var manager = new StateManager();
var snapshot = manager.CaptureSnapshot(state, "iteration_1");
// ... do work ...
var history = manager.GetHistory();
foreach (var entry in history)
    Console.WriteLine($"{entry.Value.Phase}: Quality={entry.Value.QualityScores.LastOrDefault()}");
```

### 4. Accumulate Multi-Agent Results
```csharp
var allNotes = new StateAccumulator<string>();
var researcher1Results = await ResearchAsync("topic1");
var researcher2Results = await ResearchAsync("topic2");

allNotes.AddRange(researcher1Results.RawNotes);
allNotes.AddRange(researcher2Results.RawNotes);

state.RawNotes.AddRange(allNotes.Items);
```

### 5. Check Convergence
```csharp
if (StateValidator.ShouldContinueDiffusion(supervisorState, maxIterations: 10))
{
    // Continue research loop
    var nextTransition = GetNextTransition();
}
else
{
    // Done - generate final report
}
```

### 6. Get Health Report
```csharp
var health = StateValidator.GetHealthReport(supervisorState);
Console.WriteLine(health);
// Prints:
// - Valid: True/False
// - Iterations: N
// - Active Critiques: N
// - Knowledge Base Size: N
// - Draft Quality: X/10
// - Disputed Facts: N
```

---

## Validation Rules

### FactState
- Content: non-empty
- SourceUrl: must be specified
- ConfidenceScore: 1-100 (auto-clamped)

### CritiqueState
- Author: non-empty
- Concern: non-empty
- Severity: 1-10

### QualityMetric
- Score: 0-10
- Feedback: non-empty
- Iteration: >= 0

### SupervisorState
- ResearchBrief: non-empty (before diffusion)
- DraftReport: non-empty (before diffusion)
- Quality scores < 7.0 trigger repair flag
- Iteration limits prevent infinite loops

---

## Testing

### Run All Tests
```bash
dotnet test DeepResearchAgent.Tests/
```

### Run Specific Test File
```bash
dotnet test DeepResearchAgent.Tests/StateManagementTests.cs
```

### Test Coverage
- StateFactory: 6 tests
- StateValidator: 10 tests
- StateManager: 3 tests
- StateAccumulator: 5 tests
- StateTransition: 3 tests
- **Total: 40+ tests**

---

## Phase 2 Entry Points

When starting Phase 2 (Workflow Executors):

1. **Use StateFactory** to create states in workflow nodes
2. **Use StateValidator** to validate before transitions
3. **Use StateTransitionRouter** to define workflow routes
4. **Use StateManager** to track progression
5. **Use StateAccumulator** to merge multi-agent results

See **PHASE2_IMPLEMENTATION_GUIDE.md** for detailed instructions.

---

## Architecture Decisions

### Why StateAccumulator instead of direct list operations?
- Thread-safe for concurrent agents
- Enforces accumulation (never loses old data)
- Provides clear semantics (matches Python's `operator.add`)

### Why StateValidator with all checks?
- Compile-time safety isn't enough (state comes from user/LLM)
- Validation rules documented in code
- Health reports help debugging
- Convergence detection built-in

### Why StateManager snapshots?
- Track workflow progression
- Enables rollback if needed
- Useful for analysis and debugging
- Immutable point-in-time views

### Why StateTransition classes instead of lambdas?
- Type-safe routing
- Easier to test
- Extensible (add new transition types)
- Declarative workflow definition

---

## Error Messages & Recovery

### Invalid State Transition
```
InvalidOperationException: AgentState must have at least one user message
→ Use StateFactory.CreateAgentState(messages) to initialize
```

### Confidence Score Out of Range
```
Auto-clamped to 1-100 (no exception)
→ Check StateValidator.ValidateFact() for validation
```

### Research Exceeded Max Iterations
```
ShouldContinueDiffusion() returns false
→ Check convergence before continuing loop
```

### Knowledge Base Deduplication
```
Use StateValidator.ValidateKnowledgeBase() to check
→ Context Pruner handles deduplication in Phase 2
```

---

## Performance Notes

### StateAccumulator Thread Safety
- Uses ReaderWriterLockSlim equivalent (lock-based)
- Safe for concurrent reads/writes
- Lock contention minimal for typical usage

### StateManager History
- ConcurrentDictionary for thread-safe storage
- Memory grows with iterations (clear periodically for long runs)
- Snapshots are shallow copies (not deep)

### StateValidator Performance
- O(n) for validating knowledge base (n = fact count)
- O(m) for validating critiques (m = critique count)
- Run validation once per state transition (acceptable)

---

## Quick Start Example

```csharp
using DeepResearchAgent.Models;

// 1. Create initial state
var messages = new List<ChatMessage>
{
    new() { Role = "user", Content = "Research quantum computing" }
};
var state = StateFactory.CreateAgentState(messages);

// 2. Validate
var validation = StateValidator.ValidateAgentState(state);
Assert.True(validation.IsValid);

// 3. Create supervisor state
var supervisorState = StateFactory.CreateSupervisorState(
    "Analyze quantum computing advancements",
    "Initial draft...",
    new List<ChatMessage>()
);

// 4. Check health
var health = StateValidator.GetHealthReport(supervisorState);
Console.WriteLine(health);

// 5. Define workflow routes
var router = new StateTransitionRouter();
router.RegisterEdge("clarify", "research_brief");
router.RegisterConditionalEdge("decision",
    s => s.ResearchBrief != null ? "research" : "clarify");

// 6. Track progress
var manager = new StateManager();
var snapshot = manager.CaptureSnapshot(state, "start");

// Done! Ready for Phase 2 workflows.
```

---

**Ready to implement workflows?** See **PHASE2_IMPLEMENTATION_GUIDE.md**
