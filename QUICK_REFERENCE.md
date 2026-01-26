# 🗺️ QUICK REFERENCE: DLL Interface at a Glance

## 3 WORKFLOWS

```
┌─────────────────────────────────────────────────────┐
│           MasterWorkflow.RunAsync()                │
│  (Complete 5-step: Clarify → Brief → Draft →      │
│   Supervise → Final Report)                       │
│  Input: userQuery                                 │
│  Output: finalReport (string)                     │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│       SupervisorWorkflow.SuperviseAsync()           │
│  (Iterative refinement: Brain → Tools → Evaluate) │
│  Input: researchBrief, draftReport, maxIterations  │
│  Output: refinedReport (string)                    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│       ResearcherWorkflow.ResearchAsync()            │
│  (ReAct pattern: Plan → Search → Extract → Loop)  │
│  Input: topic                                      │
│  Output: List<FactState>                          │
└─────────────────────────────────────────────────────┘
```

---

## 6 AGENTS

```
┌───────────────────────┐
│  ClarifyAgent         │  Validates query clarity
│  .ClarifyAsync()      │  → ClarificationResult
└───────────────────────┘

┌───────────────────────┐
│  ResearchBriefAgent   │  Query → Structured brief
│  .GenerateBriefAsync()│  → ResearchBrief
└───────────────────────┘

┌───────────────────────┐
│  DraftReportAgent     │  Brief → Initial draft
│  .GenerateDraftAsync()│  → DraftReport
└───────────────────────┘

┌───────────────────────┐
│  ResearcherAgent      │  Plans & executes research
│  .ExecuteAsync()      │  → ResearchOutput
└───────────────────────┘

┌───────────────────────┐
│  AnalystAgent         │  Analyzes findings
│  .ExecuteAsync()      │  → AnalysisOutput
└───────────────────────┘

┌───────────────────────┐
│  ReportAgent          │  Final polished report
│  .GenerateFinalReport │  → string
└───────────────────────┘
```

---

## 8 SERVICES

```
┌─────────────────────┐
│ OllamaService       │  LLM: .InvokeAsync()
│                     │  LLM: .InvokeWithStructuredOutputAsync<T>()
└─────────────────────┘

┌─────────────────────┐
│ SearCrawl4AIService │  Search: .SearchAsync()
│                     │  Scrape: .ScrapeAsync()
└─────────────────────┘

┌─────────────────────┐
│ LightningState      │  Create/Get/Update/Delete state
│ Service             │  List states by agent
└─────────────────────┘

┌─────────────────────┐
│ LightningStore      │  Store/Retrieve/Remove data
│                     │  GetAllKeys()
└─────────────────────┘

┌─────────────────────┐
│ MetricsService      │  Record requests
│                     │  Track by agent/research ID
│                     │  GetMetrics()
└─────────────────────┘

┌─────────────────────┐
│ QdrantVector        │  Search by vector
│ DatabaseService     │  Search by filter
│                     │  Add/Delete vectors
└─────────────────────┘

┌─────────────────────┐
│ ToolInvocation      │  .InvokeToolAsync()
│ Service             │  .SearchAsync()
│                     │  .ScrapeAsync()
└─────────────────────┘

┌─────────────────────┐
│ AgentLightning      │  APO/VERL optimization
│ Service             │  Lightning integration
└─────────────────────┘
```

---

## 4 TOOLS

```
ResearchTools.ConductResearch(topic)      → Delegate research
ResearchTools.ResearchComplete()           → Signal done
ResearchTools.ThinkTool(reflection)        → Strategic thinking
ResearchTools.RefineDraftReport(...)       → Report refinement
```

---

## 5-TIER API MAPPING

```
TIER 1: Workflows
  POST /api/workflows/master
  POST /api/workflows/supervisor
  POST /api/workflows/researcher

TIER 2: Agents
  POST /api/agents/clarify
  POST /api/agents/brief
  POST /api/agents/draft
  POST /api/agents/researcher
  POST /api/agents/analyst
  POST /api/agents/report

TIER 3: Services
  POST /api/llm/invoke
  POST /api/search
  POST /api/scrape
  POST /api/state/create
  GET  /api/state/{stateId}
  POST /api/store/{key}
  GET  /api/store/{key}
  POST /api/vector/search

TIER 4: Tools
  POST /api/tools/search
  POST /api/tools/scrape
  GET  /api/tools

TIER 5: Diagnostics
  GET  /api/health
  GET  /api/metrics
  GET  /api/config/models
  GET  /api/config/workflows
```

---

## DATA FLOW EXAMPLE: Master Workflow

```
UI sends request:
  POST /api/workflows/master
  {
    "userQuery": "What is climate change?"
  }

API Request Flow:
  1. Controller receives MasterWorkflowRequest
  2. Service maps to domain model
  3. MasterWorkflow.RunAsync(userQuery) called
  
  Inside MasterWorkflow:
    ├─ ClarifyAgent.ClarifyAsync(history) → Need clarification?
    ├─ ResearchBriefAgent.GenerateBriefAsync(query) → Brief
    ├─ DraftReportAgent.GenerateDraftAsync(brief) → Draft
    ├─ SupervisorWorkflow.SuperviseAsync(brief, draft)
    │   └─ Loop: Brain → Tools → Evaluate
    │       └─ ResearcherWorkflow.ResearchAsync(topic)
    │           └─ Loop: Plan → Search → Extract
    └─ ReportAgent.GenerateFinalReportAsync(...) → Final report
  
  4. Service maps result to MasterWorkflowResponse
  5. Controller returns response

UI receives:
  200 OK
  {
    "researchId": "guid",
    "finalReport": "Complete research synthesis...",
    "metadata": {
      "duration": "45s",
      "quality": 0.87
    }
  }
```

---

## KEY TAKEAWAYS

### Maximum Surface Exposed
- All 3 workflows → Tier 1 API
- All 6 agents → Tier 2 API
- All 8 services → Tier 3 API
- All tools → Tier 4 API
- All diagnostics → Tier 5 API

### Clean Architecture
- Workflows orchestrate
- Agents specialize
- Services support
- Tools execute

### HTTP Already Built-In
- OllamaService → calls Ollama HTTP
- SearCrawl4AIService → calls SearXNG + Crawl4AI HTTP
- LightningAPOConfig → calls Lightning Server HTTP
- All external HTTP already handled

### Ready for API
- Rich DTOs available
- State management ready
- Metrics available
- Error handling built-in

### Ready for UI
- Synchronous operations available
- Async patterns supported
- Configuration parameters exposed
- Full diagnostics available

---

## PHASE 2 WILL CREATE

```
RequestDTOs (per tier):
  Workflows/              3 classes
  Agents/                 6 classes
  Services/               8 classes
  Tools/                  1 class
  Configuration/          5+ classes

ResponseDTOs (per tier):
  Workflows/              3 classes
  Agents/                 6 classes
  Services/               8 classes
  Tools/                  1 class
  Configuration/          5+ classes

Common/Support:
  ApiResponse<T>          (generic response wrapper)
  ApiError                (error response)
  ApiMetadata             (operation metadata)
  Input/Output Models     (30+ classes)

Total: ~150 DTO classes
```

---

## READY FOR PHASE 2!

**Answer these 4 questions:**

1. Sessions: Stateful / Independent / Hybrid?
2. Config: Fixed / Per-Request / Mixed?
3. Async: Sync-Only / Fire-and-Forget / Hybrid?
4. Errors: Minimal / Standard / Detailed?

**Then Phase 2 generates all 150 DTOs** 🚀

---

**See detailed documents for complete information**

- `PHASE1_DLL_INTERFACE_MAPPING.md` - Full mapping
- `PHASE1_PUBLIC_SURFACE_DISCOVERED.md` - Method signatures
- `PHASE2_ACTION_PLAN.md` - Next phase details
