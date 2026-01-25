# Phase 2: Basic Agent Implementations - KICKOFF

## Overview
Phase 2 focuses on implementing the first three agents that handle the **scoping and initialization** of the research process. These are the simplest agents and serve as the confidence builders for the more complex agents coming in Phase 4.

**Target Completion Time:** 2-3 days (9-12 hours)
**Dependency Chain:** All three agents are independent of each other but feed the MasterWorkflow sequentially

---

## Task 2.1: Implement ClarifyAgent ⭐ START HERE

### Purpose
The ClarifyAgent acts as a **gatekeeper** ensuring the user's request has enough detail before starting an expensive research process.

### Python Reference
```python
def clarify_with_user(state: AgentState) -> Command[Literal["write_research_brief", END]]:
    """
    This node acts as a gatekeeper. It determines if the user's request has enough detail to proceed.
    If not, it HALTS the graph and asks a clarifying question. If yes, it proceeds to the next step.
    """
    messages_text = get_buffer_string(state["messages"])
    current_date = get_today_str()
    
    structured_output_model = creative_model.with_structured_output(ClarifyWithUser)
    
    response = structured_output_model.invoke([
        HumanMessage(content=clarify_with_user_instructions.format(
            messages=messages_text, 
            date=current_date
        ))
    ])
    
    if response.need_clarification:
        return Command(goto=END, update={"messages": [AIMessage(content=response.question)]})
    else:
        return Command(
            goto="write_research_brief",
            update={"messages": [AIMessage(content=response.verification)]}
        )
```

### C# Implementation Specification

#### File: `Agents/ClarifyAgent.cs`

```csharp
namespace DeepResearchAgent.Agents;

/// <summary>
/// ClarifyAgent: Validates user intent and gatekeeper for research process.
/// 
/// Responsibility:
/// - Analyze the conversation history to determine if the user's request is sufficiently detailed
/// - If clarification is needed, ask a focused question
/// - If sufficient detail exists, confirm and move to research brief generation
/// 
/// Maps to Python's clarify_with_user node (line 870-920)
/// </summary>
public class ClarifyAgent
{
    private readonly OllamaService _llmService;
    private readonly ILogger<ClarifyAgent>? _logger;

    public ClarifyAgent(OllamaService llmService, ILogger<ClarifyAgent>? logger = null)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;
    }

    /// <summary>
    /// Analyze messages and determine if clarification is needed.
    /// 
    /// Returns:
    /// - need_clarification=true, question="..." → User must respond before proceeding
    /// - need_clarification=false, verification="..." → Ready to proceed to research brief
    /// </summary>
    public async Task<ClarificationResult> ClarifyAsync(
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var messagesText = FormatMessagesToString(conversationHistory);
        var currentDate = GetTodayString();
        
        var prompt = ClarifyPrompt.GenerateClarifyWithUserPrompt(messagesText, currentDate);
        
        var response = await _llmService.GetStructuredOutputAsync<ClarificationResult>(
            prompt, 
            cancellationToken);
        
        _logger?.LogInformation(
            "Clarify decision: NeedClarification={NeedClarification}", 
            response.NeedClarification);
        
        return response;
    }

    private static string FormatMessagesToString(List<ChatMessage> messages)
    {
        // Similar to Python's get_buffer_string()
        // Formats messages into readable string with role prefixes
    }

    private static string GetTodayString()
    {
        // Similar to Python's get_today_str()
        // Returns formatted date like "Mon Dec 25, 2024"
    }
}
```

### Input/Output Contract
```
INPUT: 
  - conversationHistory: List<ChatMessage> (user's messages with optional prior clarifications)

OUTPUT: 
  - ClarificationResult record:
    {
      need_clarification: bool,
      question: string (empty if not needed),
      verification: string (empty if clarification needed)
    }
```

### Integration Points
- **Uses:** OllamaService for LLM inference
- **Input Model:** List<ChatMessage>
- **Output Model:** ClarificationResult (already implemented ✅)
- **Next Step:** ResearchBriefAgent (if need_clarification=false)
- **Halt Point:** (if need_clarification=true, wait for user response)

### Testing Strategy
- Unit test: ClarifyAsync with detailed query (should return need_clarification=false)
- Unit test: ClarifyAsync with vague query (should return need_clarification=true)
- Integration test: Verify MasterWorkflow routing based on response

---

## Task 2.2: Implement ResearchBriefAgent

### Purpose
Transforms informal conversation into a **formal, structured research brief** that becomes the guidance signal for all subsequent research.

### Python Reference (Pseudocode)
```python
def write_research_brief(state: AgentState):
    """
    Transforms conversation into formal research brief.
    This brief becomes the north star for all research activities.
    """
    messages_text = get_buffer_string(state["messages"])
    structured_output_model = creative_model.with_structured_output(ResearchBrief)
    
    response = structured_output_model.invoke([
        HumanMessage(content=transform_messages_into_research_topic_prompt.format(
            messages=messages_text,
            date=get_today_str()
        ))
    ])
    
    return {"research_brief": response.research_brief, "messages": [AIMessage(...)]}
```

### C# Implementation Specification

#### File: `Agents/ResearchBriefAgent.cs`

```csharp
namespace DeepResearchAgent.Agents;

/// <summary>
/// ResearchBriefAgent: Transforms user intent into formal research brief.
/// 
/// Responsibility:
/// - Extract key research objectives from conversation
/// - Generate formal, unambiguous research brief
/// - Define scope constraints and boundaries
/// - Create the "guidance signal" for all research agents
/// 
/// Maps to Python's write_research_brief node
/// </summary>
public class ResearchBriefAgent
{
    private readonly OllamaService _llmService;
    private readonly ILogger<ResearchBriefAgent>? _logger;

    public ResearchBriefAgent(OllamaService llmService, ILogger<ResearchBriefAgent>? logger = null)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;
    }

    /// <summary>
    /// Generate a formal research brief from conversation history.
    /// This brief is used as the guidance signal for all research work.
    /// </summary>
    public async Task<ResearchQuestion> GenerateResearchBriefAsync(
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var messagesText = FormatMessagesToString(conversationHistory);
        var currentDate = GetTodayString();
        
        var prompt = ResearchBriefPrompt.GenerateResearchBriefPrompt(messagesText, currentDate);
        
        var response = await _llmService.GetStructuredOutputAsync<ResearchQuestion>(
            prompt,
            cancellationToken);
        
        _logger?.LogInformation(
            "Research brief generated with {ObjectiveCount} objectives",
            response.Objectives?.Count ?? 0);
        
        return response;
    }

    private static string FormatMessagesToString(List<ChatMessage> messages) { }
    private static string GetTodayString() { }
}
```

### Input/Output Contract
```
INPUT:
  - conversationHistory: List<ChatMessage> (confirmed user request)

OUTPUT:
  - ResearchQuestion record:
    {
      research_brief: string (formal brief text),
      objectives: List<string>,
      scope: string,
      created_at: DateTime
    }
```

### Integration Points
- **Depends on:** ClarifyAgent (clarification complete)
- **Uses:** OllamaService for LLM inference
- **Input Model:** List<ChatMessage>
- **Output Model:** ResearchQuestion (already enhanced ✅)
- **Next Step:** DraftReportAgent

---

## Task 2.3: Implement DraftReportAgent

### Purpose
Generates the initial "noisy" draft report that serves as the starting point for the diffusion loop.

### Python Reference (Pseudocode)
```python
def write_initial_draft_report(state: AgentState):
    """
    Create initial draft based on research brief.
    This is the 'noisy image' that diffusion will progressively denoise.
    """
    brief = state["research_brief"]
    structured_output_model = writer_model.with_structured_output(DraftReport)
    
    response = structured_output_model.invoke([
        HumanMessage(content=write_initial_draft_report_instructions.format(
            research_brief=brief
        ))
    ])
    
    return {"draft_report": response.draft_report, "messages": [AIMessage(...)]}
```

### C# Implementation Specification

#### File: `Agents/DraftReportAgent.cs`

```csharp
namespace DeepResearchAgent.Agents;

/// <summary>
/// DraftReportAgent: Generates initial draft report.
/// 
/// Responsibility:
/// - Create initial draft based on research brief and conversation
/// - Structure draft into logical sections
/// - Identify gaps and areas needing research
/// - Mark starting quality baseline
/// 
/// Maps to Python's write_initial_draft_report node
/// </summary>
public class DraftReportAgent
{
    private readonly OllamaService _llmService;
    private readonly ILogger<DraftReportAgent>? _logger;

    public DraftReportAgent(OllamaService llmService, ILogger<DraftReportAgent>? logger = null)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;
    }

    /// <summary>
    /// Generate initial draft report from research brief and conversation context.
    /// </summary>
    public async Task<DraftReport> GenerateDraftReportAsync(
        string researchBrief,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var prompt = DraftReportPrompt.GenerateDraftReportPrompt(
            researchBrief,
            conversationHistory);
        
        var response = await _llmService.GetStructuredOutputAsync<DraftReport>(
            prompt,
            cancellationToken);
        
        _logger?.LogInformation(
            "Draft report generated with {SectionCount} sections",
            response.Sections?.Count ?? 0);
        
        return response;
    }
}
```

### Input/Output Contract
```
INPUT:
  - researchBrief: string (from ResearchBriefAgent)
  - conversationHistory: List<ChatMessage> (context)

OUTPUT:
  - DraftReport record:
    {
      draft_report: string (full draft text),
      sections: List<DraftReportSection>,
      created_at: DateTime,
      metadata: Dictionary<string, object>
    }
```

### Integration Points
- **Depends on:** ResearchBriefAgent (brief available)
- **Uses:** OllamaService for LLM inference
- **Input Models:** string (brief), List<ChatMessage> (history)
- **Output Model:** DraftReport (already enhanced ✅)
- **Next Step:** SupervisorWorkflow (diffusion loop begins)

---

## Implementation Order & Dependencies

```
Phase 2.1: ClarifyAgent ────┐
                             ├─→ MasterWorkflow Integration
Phase 2.2: ResearchBriefAgent┤
                             │
Phase 2.3: DraftReportAgent ─┘
```

**Recommended Implementation Sequence:**
1. **Day 1:** Implement ClarifyAgent (simplest, good confidence builder)
2. **Day 1-2:** Implement ResearchBriefAgent  
3. **Day 2:** Implement DraftReportAgent
4. **Day 2-3:** Integration testing with MasterWorkflow

---

## Prompt Templates Required

Create `Prompts/ClarifyPrompt.cs`:
```csharp
public static class ClarifyPrompt
{
    public static string GenerateClarifyWithUserPrompt(string messages, string currentDate)
    {
        // Maps Python's clarify_with_user_instructions
        // Instructs LLM to analyze messages and decide if clarification needed
    }
}
```

Create `Prompts/ResearchBriefPrompt.cs`:
```csharp
public static class ResearchBriefPrompt
{
    public static string GenerateResearchBriefPrompt(string messages, string currentDate)
    {
        // Maps Python's transform_messages_into_research_topic_human_msg_prompt
        // Instructs LLM to generate formal research brief with objectives
    }
}
```

Create `Prompts/DraftReportPrompt.cs`:
```csharp
public static class DraftReportPrompt
{
    public static string GenerateDraftReportPrompt(string brief, List<ChatMessage> history)
    {
        // Maps Python's write_initial_draft_report_instructions
        // Instructs LLM to create structured initial draft
    }
}
```

---

## Completion Criteria for Phase 2

- [ ] ClarifyAgent implemented and unit tested
- [ ] ResearchBriefAgent implemented and unit tested
- [ ] DraftReportAgent implemented and unit tested
- [ ] All three agents integrated with MasterWorkflow
- [ ] Integration tests pass
- [ ] Build successful (no compilation errors)
- [ ] Documentation complete

---

## Estimated Time Breakdown

| Task | Effort | Status |
|------|--------|--------|
| ClarifyAgent | 1.5 hrs | Ready to start |
| ResearchBriefAgent | 1.5 hrs | Ready to start |
| DraftReportAgent | 1.5 hrs | Ready to start |
| Prompt templates | 1 hr | Ready to start |
| Testing & Integration | 1.5 hrs | Ready to start |
| **Phase 2 Total** | **7 hours** | **READY** |

---

**Phase 2 Status:** READY TO START ✅

**Recommendation:** Start with ClarifyAgent as it's the simplest and has lowest dependencies.

Would you like me to implement ClarifyAgent now?
