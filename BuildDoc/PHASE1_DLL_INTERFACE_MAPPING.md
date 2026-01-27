# 🗺️ PHASE 1: Complete DLL Interface Mapping

**Date**: 2024  
**Objective**: Map entire DeepResearchAgent.dll surface for comprehensive API exposure  
**Approach**: Full exposure now, scale back later based on UI needs  

---

## 📊 CORE ARCHITECTRE DISCOVERED

### Three Main Orchestrators (Workflows)

```
1. MasterWorkflow
   └─ Highest level - 5-step orchestration
   
2. SupervisorWorkflow  
   └─ Iterative refinement - Diffusion loop
   
3. ResearcherWorkflow
   └─ Detailed research - ReAct loop with tools
```

### Specialized Agents

```
Execution Agents:
├─ ResearcherAgent      → Plans & executes research
├─ AnalystAgent         → Analyzes findings  
└─ ReportAgent          → Generates final report

Gatekeeper Agents:
├─ ClarifyAgent         → Validates query clarity
├─ ResearchBriefAgent   → Transforms query to brief
└─ DraftReportAgent     → Creates initial draft
```

### Supporting Services

```
LLM Services:
└─ OllamaService        → Local LLM inference

Search & Scraping:
├─ SearCrawl4AIService  → Web search + scraping
├─ SearXNG Integration  → Meta-search engine
└─ Crawl4AI Integration → Web scraping

State Management:
├─ LightningStateService → State tracking
├─ LightningStore        → Data persistence
└─ LightningAPOConfig    → Agent optimization

Vector Database:
└─ QdrantVectorDatabaseService → Semantic search

Tools:
└─ ToolInvocationService → Research tool execution
└─ ResearchTools         → Tool definitions

Metrics:
└─ MetricsService        → Performance tracking
```

---

## 🔄 COMPREHENSIVE API SURFACE

### 1. WORKFLOWS - Full Orchestration

#### MasterWorkflow
```csharp
// Entry point for complete 5-step research pipeline
public async Task<string> RunAsync(
    string userQuery,
    CancellationToken cancellationToken = default)

// Input: User research query
// Output: Final synthesized report
// Steps: Clarify → Brief → Draft → Supervise → Final Report
```

#### SupervisorWorkflow
```csharp
// Iterative refinement loop (diffusion process)
public async Task<string> SuperviseAsync(
    string researchBrief,
    string draftReport = "",
    int maxIterations = 5,
    string? researchId = null,
    CancellationToken cancellationToken = default)

// Input: Research brief + draft report
// Output: Refined and evaluated report
// Process: Brain → Tools → Evaluate → Repeat
```

#### ResearcherWorkflow
```csharp
// Detailed research using ReAct pattern
public async Task<IReadOnlyList<FactState>> ResearchAsync(
    string topic,
    string? researchId = null,
    CancellationToken cancellationToken = default,
    ApoExecutionOptions? apoOptions = null)

// Input: Research topic
// Output: Extracted facts/findings
// Process: Plan → Search → Extract → Evaluate → Refine
```

---

### 2. AGENTS - Specialized Processing

#### ResearcherAgent
```csharp
public async Task<ResearchOutput> ExecuteAsync(
    ResearchInput input,
    CancellationToken cancellationToken = default)

// Input: 
// - Topic
// - MaxIterations
// - ConfigOptions

// Output: ResearchOutput
// - ResearchTopicsCovered
// - Findings
// - Quality metrics
```

#### AnalystAgent
```csharp
public async Task<AnalysisOutput> ExecuteAsync(
    AnalysisInput input,
    CancellationToken cancellationToken = default)

// Input:
// - Findings to analyze
// - Research context

// Output: AnalysisOutput
// - ThemesIdentified
// - Contradictions detected
// - Importance scores
// - Confidence metrics
```

#### ClarifyAgent
```csharp
public async Task<ClarificationResult> ClarifyAsync(
    List<ChatMessage> conversationHistory,
    CancellationToken cancellationToken = default)

// Input: Conversation history

// Output: ClarificationResult
// - NeedClarification: bool
// - Question (if clarification needed)
// - Verification (if ready to proceed)
```

#### ResearchBriefAgent
```csharp
// Transforms user query to structured research brief
public async Task<ResearchBrief> GenerateBriefAsync(
    string userQuery,
    CancellationToken cancellationToken = default)

// Input: User query
// Output: Structured research brief
```

#### DraftReportAgent
```csharp
// Creates initial draft from brief
public async Task<DraftReport> GenerateDraftAsync(
    string researchBrief,
    CancellationToken cancellationToken = default)

// Input: Research brief
// Output: Initial draft report
```

#### ReportAgent
```csharp
// Generates final polished report
public async Task<string> GenerateFinalReportAsync(
    string research,
    string analysis,
    CancellationToken cancellationToken = default)

// Input: Research findings + Analysis
// Output: Final synthesized report
```

---

### 3. SERVICES - Core Infrastructure

#### OllamaService
```csharp
// LLM inference
public async Task<OllamaChatMessage> InvokeAsync(
    List<OllamaChatMessage> messages,
    string? model = null,
    CancellationToken cancellationToken = default)

// Structured output
public async Task<T> InvokeWithStructuredOutputAsync<T>(
    List<OllamaChatMessage> messages,
    CancellationToken cancellationToken = default)

// Available models
public string DefaultModel { get; }
```

#### SearCrawl4AIService
```csharp
// Web search
public async Task<SearXNGResponse> SearchAsync(
    string query,
    int maxResults = 10,
    CancellationToken cancellationToken = default)

// Web scraping
public async Task<Crawl4AIResponse> ScrapeAsync(
    string url,
    CancellationToken cancellationToken = default)

// Cached operations available
```

#### LightningStateService
```csharp
// State management
public async Task<AgentState> CreateStateAsync(string agentId)
public async Task<AgentState?> GetStateAsync(string stateId)
public async Task UpdateStateAsync(AgentState state)
public async Task DeleteStateAsync(string stateId)
public async Task<IEnumerable<AgentState>> ListStatesAsync()
```

#### LightningStore
```csharp
// Data persistence
public void Store(string key, object value)
public object? Retrieve(string key)
public void Remove(string key)
public IEnumerable<string> GetAllKeys()
```

#### MetricsService
```csharp
// Performance tracking
public void RecordRequest(string agent, string status)
public void TrackResearchRequest(string agent, string id, string status)
public Stopwatch StartTimer()
public IEnumerable<string> GetMetrics()
```

#### QdrantVectorDatabaseService
```csharp
// Vector operations
public async Task<IEnumerable<string>> SearchAsync(
    ReadOnlyMemory<float> vector,
    int limit = 10)

public async Task<IEnumerable<string>> SearchByFilterAsync(
    string filter,
    int limit = 10)

public async Task AddAsync(string id, ReadOnlyMemory<float> vector, string payload)
public async Task DeleteAsync(string id)
```

---

### 4. TOOLS & UTILITIES

#### ToolInvocationService
```csharp
// Tool execution
public async Task<string> InvokeToolAsync(
    string toolName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)

// Web search tool
public async Task<IEnumerable<SearchResult>> SearchAsync(
    string query,
    CancellationToken cancellationToken = default)

// Scraping tool
public async Task<ScrapedContent> ScrapeAsync(
    string url,
    CancellationToken cancellationToken = default)
```

#### ResearchTools
```csharp
// Tool definitions
- ConductResearch(researchTopic): string
- ResearchComplete(): string
- ThinkTool(reflection): string
- RefineDraftReport(brief, findings, draft): string
```

---

### 5. STATE & CONFIGURATION

#### State Models Available
```
- ResearcherState
- SupervisorState  
- FactState
- CritiqueState
- AgentState
- StateFactory
- StateValidator
- StateTransition
- StateAccumulator
```

#### Configuration Models
```
- WorkflowModelConfiguration
  ├─ SupervisorBrainModel
  ├─ SupervisorToolsModel
  ├─ QualityEvaluatorModel
  ├─ RedTeamModel
  └─ ContextPrunerModel

- LightningAPOConfig
  ├─ Enabled
  ├─ Strategy
  ├─ MaxTasks
  └─ TaskTimeout

- SearCrawl4AIConfig
  ├─ CachingEnabled
  └─ CacheDuration
```

#### Input/Output Models
```
ResearchInput:
- Topic
- MaxIterations
- ConfigOptions

ResearchOutput:
- ResearchTopicsCovered
- Findings
- IterationsUsed
- QualityScores

AnalysisInput:
- Findings
- ResearchContext

AnalysisOutput:
- ThemesIdentified
- Contradictions
- ImportanceScores
- Insights

ClarificationResult:
- NeedClarification
- Question
- Verification

DraftReport:
- Content
- Quality Score

ResearchBrief:
- Objectives
- Key Questions
- Research Scope
```

---

## 🎯 PROPOSED API EXPOSURE STRATEGY

### Tier 1: Full Workflows (High-Level)
- `POST /api/research/master` - Run complete pipeline
- `POST /api/research/supervisor` - Run refinement loop
- `POST /api/research/researcher` - Run research phase

### Tier 2: Agents (Mid-Level)
- `POST /api/agents/clarify` - Clarify query
- `POST /api/agents/brief` - Generate brief
- `POST /api/agents/draft` - Generate draft
- `POST /api/agents/researcher` - Run researcher agent
- `POST /api/agents/analyst` - Run analyst agent
- `POST /api/agents/report` - Generate final report

### Tier 3: Services (Low-Level)
- `POST /api/llm/invoke` - Raw LLM call
- `POST /api/search` - Web search
- `POST /api/scrape` - Web scraping
- `GET/POST /api/state/*` - State management
- `GET /api/metrics` - Performance metrics

### Tier 4: Tools (Utility)
- `POST /api/tools/search` - Tool search
- `POST /api/tools/scrape` - Tool scrape
- `GET /api/tools/available` - List tools

### Tier 5: Configuration & Diagnostics
- `GET /api/config/models` - Available LLM models
- `GET /api/config/workflows` - Workflow configs
- `GET /api/health` - System health
- `GET /api/diagnostics/metrics` - Full metrics
- `GET /api/diagnostics/state` - Full state

---

## 📋 DTO STRATEGY

### Philosophy
**Maximal Exposure**: Create comprehensive DTOs for every workflow, agent, service combination

**Structure**:
```
DeepResearchAgent.Api/DTOs/
├── Requests/
│   ├── Workflows/
│   │   ├── MasterWorkflowRequest.cs
│   │   ├── SupervisorWorkflowRequest.cs
│   │   └── ResearcherWorkflowRequest.cs
│   ├── Agents/
│   │   ├── ResearcherAgentRequest.cs
│   │   ├── AnalystAgentRequest.cs
│   │   ├── ClarifyAgentRequest.cs
│   │   └── ...
│   ├── Services/
│   │   ├── LlmInvokeRequest.cs
│   │   ├── SearchRequest.cs
│   │   ├── ScrapeRequest.cs
│   │   └── StateRequest.cs
│   ├── Tools/
│   │   └── ToolInvocationRequest.cs
│   └── Configuration/
│       ├── ConfigurationRequest.cs
│       └── ...
│
└── Responses/
    ├── Workflows/
    ├── Agents/
    ├── Services/
    ├── Tools/
    ├── Common/
    │   ├── ApiResponse<T>.cs
    │   ├── ApiError.cs
    │   └── ApiMetadata.cs
    └── ...
```

---

## ✅ NEXT STEPS

### Phase 1 Deliverables (This Session)
- [x] Map complete DLL interface
- [ ] Create all DTOs (Request/Response)
- [ ] Create mapping profiles (AutoMapper)
- [ ] Create orchestration service interfaces

### Phase 2 (Next Session)
- [ ] Implement orchestration services
- [ ] Implement controllers
- [ ] Add validation layer

### Phase 3+ (Future)
- [ ] Add middleware (logging, error handling)
- [ ] Add documentation (Swagger)
- [ ] Add tests

---

## 🔑 KEY PRINCIPLES

1. **Expose Everything** - Don't restrict access, let UI call what it needs
2. **Tier by Complexity** - Tier 1 (high-level workflows) → Tier 5 (diagnostics)
3. **Map All Inputs/Outputs** - Every parameter, every response
4. **Preserve Flexibility** - Allow advanced usage patterns
5. **Scale Back Later** - Remove what UI doesn't use in future sessions

---

**Status**: ✅ Mapping Complete  
**Next**: Begin Phase 2 with DTO Creation

---

## 📞 CLARIFICATIONS NEEDED FOR DTO CREATION

Before I create all DTOs, I need to know:

1. **Chat/Session Management**: How should the API handle sessions? Should we expose:
   - Session lifecycle (create, list, delete)?
   - Session state persistence?
   - Multiple queries per session?

2. **Error Handling**: What level of detail for errors?
   - Full exception details?
   - Wrapped errors with correlation IDs?
   - Structured error codes?

3. **Configuration Pass-Through**: Should the API accept:
   - Workflow configuration in requests?
   - LLM model selection per request?
   - Tool parameters customization?

4. **Async Patterns**: Should we support:
   - Fire-and-forget requests?
   - Polling for results?
   - WebSocket streaming?
   - Job queue patterns?

What are your preferences?
