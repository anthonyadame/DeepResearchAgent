# 🔍 PHASE 1: PUBLIC SURFACE AREA DISCOVERED

## Core Workflows - Public Methods

### MasterWorkflow
```csharp
public class MasterWorkflow
{
    // ENTRY POINT: Complete 5-step research pipeline
    public async Task<string> RunAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    
    // STEPS EXECUTED:
    // 1. ClarifyWithUser - Ensure query is detailed enough
    // 2. WriteResearchBrief - Transform query to structured brief
    // 3. WriteDraftReport - Generate initial "noisy" draft
    // 4. ExecuteSupervisor - Hand off for iterative refinement
    // 5. GenerateFinalReport - Polish findings into final report
}
```

### SupervisorWorkflow
```csharp
public class SupervisorWorkflow
{
    // ITERATIVE REFINEMENT: Diffusion-based loop
    public async Task<string> SuperviseAsync(
        string researchBrief,
        string draftReport = "",
        int maxIterations = 5,
        string? researchId = null,
        CancellationToken cancellationToken = default)
    
    // PROCESS:
    // - Brain: LLM decides research direction
    // - Tools: Execute research in parallel
    // - Evaluate: Score quality and check convergence
    // - Repeat: Until quality acceptable or max iterations
}
```

### ResearcherWorkflow
```csharp
public class ResearcherWorkflow
{
    // FOCUSED RESEARCH: ReAct pattern (LLM → Tools → Loop)
    public async Task<IReadOnlyList<FactState>> ResearchAsync(
        string topic,
        string? researchId = null,
        CancellationToken cancellationToken = default,
        ApoExecutionOptions? apoOptions = null)
    
    // RETURNS: List of extracted facts/findings
    // SUPPORTS: Lightning APO optimization
}
```

---

## Specialized Agents - Public Methods

### ResearcherAgent
```csharp
public class ResearcherAgent
{
    public async Task<ResearchOutput> ExecuteAsync(
        ResearchInput input,
        CancellationToken cancellationToken = default)
    
    // ResearchInput properties:
    // - Topic: string
    // - MaxIterations: int
    // - [other config]
    
    // ResearchOutput properties:
    // - ResearchTopicsCovered: List<string>
    // - Findings: List<Finding>
    // - IterationsUsed: int
    // - QualityScores: List<double>
}
```

### AnalystAgent
```csharp
public class AnalystAgent
{
    public async Task<AnalysisOutput> ExecuteAsync(
        AnalysisInput input,
        CancellationToken cancellationToken = default)
    
    // AnalysisInput properties:
    // - Findings: List<Finding>
    // - ResearchContext: string
    
    // AnalysisOutput properties:
    // - ThemesIdentified: List<string>
    // - Contradictions: List<Contradiction>
    // - ImportanceScores: Dictionary<string, double>
    // - Insights: List<string>
}
```

### ClarifyAgent
```csharp
public class ClarifyAgent
{
    // ENTRY VALIDATION: Checks if query needs clarification
    public async Task<ClarificationResult> ClarifyAsync(
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    
    // ClarificationResult properties:
    // - NeedClarification: bool
    // - Question: string (if needs clarification)
    // - Verification: string (if ready to proceed)
}
```

### ResearchBriefAgent
```csharp
public class ResearchBriefAgent
{
    // TRANSFORMS: Query → Structured brief
    public async Task<ResearchBrief> GenerateBriefAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    
    // ResearchBrief properties:
    // - Objectives: List<string>
    // - KeyQuestions: List<string>
    // - ResearchScope: string
}
```

### DraftReportAgent
```csharp
public class DraftReportAgent
{
    // GENERATES: Initial draft from brief
    public async Task<DraftReport> GenerateDraftAsync(
        string researchBrief,
        CancellationToken cancellationToken = default)
    
    // DraftReport properties:
    // - Content: string
    // - QualityScore: double
}
```

### ReportAgent
```csharp
public class ReportAgent
{
    // SYNTHESIZES: Final polished report
    public async Task<string> GenerateFinalReportAsync(
        string research,
        string analysis,
        CancellationToken cancellationToken = default)
}
```

---

## Core Services - Public Methods

### OllamaService
```csharp
public class OllamaService
{
    // RAW LLM INVOCATION
    public async Task<OllamaChatMessage> InvokeAsync(
        List<OllamaChatMessage> messages,
        string? model = null,
        CancellationToken cancellationToken = default)
    
    // STRUCTURED OUTPUT (JSON parsing)
    public async Task<T> InvokeWithStructuredOutputAsync<T>(
        List<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default)
    
    // PROPERTIES
    public string DefaultModel { get; }
}
```

### SearCrawl4AIService
```csharp
public class SearCrawl4AIService : ISearCrawl4AIService
{
    // WEB SEARCH (via SearXNG)
    public async Task<SearXNGResponse> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    
    // WEB SCRAPING (via Crawl4AI)
    public async Task<Crawl4AIResponse> ScrapeAsync(
        string url,
        CancellationToken cancellationToken = default)
    
    // RESPONSE TYPES:
    // - SearXNGResponse: Contains search results + metadata
    // - Crawl4AIResponse: Contains scraped content + metadata
}
```

### LightningStateService
```csharp
public interface ILightningStateService
{
    // STATE LIFECYCLE
    public async Task<AgentState> CreateStateAsync(string agentId)
    public async Task<AgentState?> GetStateAsync(string stateId)
    public async Task UpdateStateAsync(AgentState state)
    public async Task DeleteStateAsync(string stateId)
    
    // STATE QUERIES
    public async Task<IEnumerable<AgentState>> ListStatesAsync()
    public async Task<IEnumerable<AgentState>> GetStatesByAgentAsync(string agentId)
}
```

### LightningStore
```csharp
public class LightningStore
{
    // KEY-VALUE PERSISTENCE
    public void Store(string key, object value)
    public object? Retrieve(string key)
    public void Remove(string key)
    public void Clear()
    
    // QUERIES
    public IEnumerable<string> GetAllKeys()
    public bool Contains(string key)
}
```

### MetricsService
```csharp
public class MetricsService
{
    // REQUEST TRACKING
    public void RecordRequest(string agent, string status)
    public void TrackResearchRequest(string agent, string id, string status)
    
    // TIMING
    public Stopwatch StartTimer()
    
    // RETRIEVAL
    public IEnumerable<string> GetMetrics()
    public void Clear()
}
```

### QdrantVectorDatabaseService
```csharp
public class QdrantVectorDatabaseService
{
    // VECTOR SEARCH
    public async Task<IEnumerable<string>> SearchAsync(
        ReadOnlyMemory<float> vector,
        int limit = 10)
    
    // FILTERED SEARCH
    public async Task<IEnumerable<string>> SearchByFilterAsync(
        string filter,
        int limit = 10)
    
    // VECTOR STORAGE
    public async Task AddAsync(
        string id,
        ReadOnlyMemory<float> vector,
        string payload)
    
    public async Task DeleteAsync(string id)
}
```

### ToolInvocationService
```csharp
public class ToolInvocationService
{
    // TOOL EXECUTION
    public async Task<string> InvokeToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    
    // SPECIFIC TOOLS
    public async Task<IEnumerable<SearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    
    public async Task<ScrapedContent> ScrapeAsync(
        string url,
        CancellationToken cancellationToken = default)
}
```

---

## Available Tools

### ResearchTools (Static Methods)
```csharp
public static class ResearchTools
{
    // Delegate research task
    [Description("Conduct research on a specific topic")]
    public static string ConductResearch(string researchTopic)
    
    // Signal completion
    [Description("Signal that research is complete")]
    public static string ResearchComplete()
    
    // Strategic thinking
    [Description("Think strategically about research progress")]
    public static string ThinkTool(string reflection)
    
    // Report refinement
    [Description("Refine draft report with findings")]
    public static string RefineDraftReport(
        string researchBrief,
        string findings,
        string draftReport)
}
```

---

## Models & Configuration

### State Models Available
```csharp
// State classes for tracking research progress
ResearcherState
SupervisorState
FactState
CritiqueState
AgentState
StateFactory (creates states)
StateValidator (validates state transitions)
StateTransition (tracks state changes)
StateAccumulator (accumulates state)
StateManager (manages state lifecycle)
```

### Configuration Models
```csharp
WorkflowModelConfiguration
// - SupervisorBrainModel
// - SupervisorToolsModel
// - QualityEvaluatorModel
// - RedTeamModel
// - ContextPrunerModel

LightningAPOConfig
// - Enabled: bool
// - Strategy: OptimizationStrategy
// - MaxTasks: int
// - TaskTimeout: int

SearCrawl4AIConfig
// - CachingEnabled: bool
// - CacheDuration: TimeSpan

WebSearchOptions
// - MaxResults: int
// - TimeoutSeconds: int
```

### Input/Output Models
```csharp
// RESEARCH PHASE
ResearchInput
├─ Topic: string
├─ MaxIterations: int
└─ [config options]

ResearchOutput
├─ ResearchTopicsCovered: List<string>
├─ Findings: List<Finding>
├─ IterationsUsed: int
└─ QualityScores: List<double>

// ANALYSIS PHASE
AnalysisInput
├─ Findings: List<Finding>
└─ ResearchContext: string

AnalysisOutput
├─ ThemesIdentified: List<string>
├─ Contradictions: List<Contradiction>
├─ ImportanceScores: Dictionary<string, double>
└─ Insights: List<string>

// CLARIFICATION
ClarificationResult
├─ NeedClarification: bool
├─ Question: string
└─ Verification: string

// DRAFT/BRIEF/REPORT
ResearchBrief
DraftReport
WebSearchResult
ScrapedContent
ChatMessage
```

---

## 🎯 What We Can Expose

### Maximum Exposure API (5 Tiers)

```
TIER 1: Full Orchestration (Easy)
- Master Workflow (complete pipeline)
- Supervisor Workflow (refinement)
- Researcher Workflow (research phase)

TIER 2: Agent Operations (Medium)
- Clarify Agent
- Research Brief Agent
- Draft Report Agent
- Researcher Agent
- Analyst Agent
- Report Agent

TIER 3: Service Operations (Advanced)
- LLM Invocation (raw)
- Web Search
- Web Scraping
- State Management
- Data Persistence
- Vector Search
- Tool Invocation
- Metrics Access

TIER 4: Tool Operations (Utility)
- Direct tool calls
- Tool discovery
- Tool metadata

TIER 5: Diagnostics (Testing)
- Health checks
- Metrics dumps
- State snapshots
- Configuration inspection
```

---

## ✅ Ready for Phase 2

All public surface discovered. Ready to create:
- [ ] Request DTOs (60+ classes)
- [ ] Response DTOs (60+ classes)
- [ ] AutoMapper profiles
- [ ] Service interfaces
- [ ] Controllers

**Awaiting your input on 4 clarification questions** to guide DTO design.

See: `PHASE1_DLL_INTERFACE_MAPPING.md`
