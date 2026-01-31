# End-to-End Testing Plan: Step-by-Step Chat Workflow

## Overview

This document describes the testing strategy for the step-by-step research workflow via the chat interface. The workflow allows users to submit a query and progress through 5 steps, with the UI controlling timing and displaying progress at each step.

---

## Architecture

### State Flow

```
User Query (UI)
    ↓
POST /api/chat/step (ChatStepRequest with initial AgentState)
    ↓
MasterWorkflow.ExecuteByStepAsync()
    ├─ Step 1: Clarify → Check if query needs clarification
    ├─ Step 2: ResearchBrief → Generate structured research direction
    ├─ Step 3: DraftReport → Generate initial draft
    ├─ Step 4: SupervisorLoop → Refine findings
    └─ Step 5: FinalReport → Polish and synthesize
    ↓
ChatStepResponse (updated AgentState + DisplayContent)
    ↓
UI updates with new content, stores updated AgentState
    ↓
User clicks "Continue" → POST /api/chat/step (with updated AgentState)
```

### Key Classes

- **AgentState**: Holds all workflow state (Messages, ResearchBrief, DraftReport, FinalReport, etc.)
- **ChatStepRequest**: Request DTO containing current AgentState and optional user response
- **ChatStepResponse**: Response DTO with updated AgentState and formatted content for display
- **ChatIntegrationService.ProcessChatStepAsync**: Executes one step of the workflow
- **MasterWorkflow.ExecuteByStepAsync**: Core step-by-step execution logic

---

## Testing Scenarios

### Scenario 1: Happy Path (No Clarification)

**Preconditions:**
- All supporting services running (Ollama, SearXNG, etc.)
- Fresh chat session

**Steps:**

| # | Action | Expected Result |
|---|--------|-----------------|
| 1 | User enters: "What are the effects of AI on job markets in 2024?" | Query accepted |
| 2 | Create AgentState with Messages[0].Content = query, NeedsQualityRepair=true | State created |
| 3 | POST /api/chat/step with AgentState | Step 1 executes: Clarification check |
|   | | Response: ClarificationQuestion=null, NeedsQualityRepair=false, CurrentStep=2 |
| 4 | UI displays: "Research brief generated. Click 'Continue' to generate draft." | |
| 5 | UI persists AgentState with ResearchBrief filled | |
| 6 | POST /api/chat/step with updated AgentState | Step 2 executes: ResearchBrief generation |
|   | | Response: CurrentStep=2, DisplayContent=first 250 chars of ResearchBrief |
| 7 | POST /api/chat/step (again, with ResearchBrief filled) | Step 3 executes: DraftReport generation |
|   | | Response: CurrentStep=3, DisplayContent=preview of DraftReport |
| 8 | POST /api/chat/step (again, with DraftReport filled) | Step 4 executes: Supervisor refinement |
|   | | Response: CurrentStep=4, DisplayContent=raw notes summary |
| 9 | POST /api/chat/step (again, with RawNotes filled) | Step 5 executes: Final report generation |
|   | | Response: CurrentStep=5, IsComplete=true, DisplayContent=FinalReport |
| 10 | UI displays final report, shows "Research complete!" badge | |

**Assertions:**
- ✅ Each step returns exactly after one execution
- ✅ Properties populate in order: ResearchBrief → DraftReport → RawNotes → FinalReport
- ✅ DisplayContent updates after each step
- ✅ IsComplete=true only in final step
- ✅ All metrics recorded (duration, content length)

---

### Scenario 2: Clarification Required

**Preconditions:**
- Same as Scenario 1

**Steps:**

| # | Action | Expected Result |
|---|--------|-----------------|
| 1 | User enters: "Tell me about technology" (too vague, < 30 chars) | |
| 2 | Create AgentState, POST /api/chat/step | Step 1 executes: ClarifyWithUserAsync |
|   | | Response: NeedsQualityRepair=true, ClarificationQuestion set |
|   | | DisplayContent contains clarification question |
| 3 | UI shows: "Clarification needed: Please provide more detail about..." | |
| 4 | UI displays input field for clarification | |
| 5 | User enters: "Focus on AI impact on job market and skill requirements" | |
| 6 | UI creates new ChatStepRequest with UserResponse set | |
| 7 | POST /api/chat/step with UserResponse | ChatIntegrationService updates Messages[0] |
|   | | Sets NeedsQualityRepair=false |
|   | | Calls ExecuteByStepAsync again |
|   | | Response: ClarificationQuestion=null, CurrentStep=2, ResearchBrief generated |
| 8 | UI proceeds to Step 2 (ResearchBrief display) | |

**Assertions:**
- ✅ Clarification question returned on vague input
- ✅ NeedsQualityRepair=true in clarification response
- ✅ UserResponse updates Messages[0].Content correctly
- ✅ After providing clarification, NeedsQualityRepair=false and step 2 executes
- ✅ Workflow continues normally from step 2

---

### Scenario 3: Error Recovery

**Preconditions:**
- Same as Scenario 1
- Network interruption or LLM timeout during Step 3

**Steps:**

| # | Action | Expected Result |
|---|--------|-----------------|
| 1 | Execute Steps 1-2 successfully | ResearchBrief generated |
| 2 | POST /api/chat/step for Step 3 | DraftReportAgent throws timeout exception |
| 3 | ChatIntegrationService catches exception | Response: DisplayContent="Error: timeout", IsComplete=false |
|   | | UpdatedState preserved in response |
| 4 | UI displays: "An error occurred during processing. Please try again." | |
| 5 | User clicks "Retry" (POST /api/chat/step with same state) | ExecuteByStepAsync retries Step 3 |
|   | | Request succeeds this time |
| 6 | Response: CurrentStep=3, DraftReport generated | |

**Assertions:**
- ✅ Exceptions handled gracefully
- ✅ AgentState preserved on error
- ✅ Retry from same step possible
- ✅ User-friendly error message returned

---

### Scenario 4: Session Persistence

**Preconditions:**
- Scenario 1 in progress (Steps 1-3 complete)

**Steps:**

| # | Action | Expected Result |
|---|--------|-----------------|
| 1 | User closes browser tab | Session state should be persisted somewhere (frontend, optional backend cache) |
| 2 | User reopens chat, retrieves session | UI loads AgentState with ResearchBrief and DraftReport filled |
| 3 | POST /api/chat/step with existing AgentState | Step 4 executes (supervisor), not re-running earlier steps |
| 4 | Response: CurrentStep=4, RawNotes generated | Workflow resumes from checkpoint |

**Assertions:**
- ✅ Frontend persists AgentState (localStorage or session storage)
- ✅ Resuming from intermediate state works correctly
- ✅ Steps not re-executed unnecessarily

---

### Scenario 5: Multiple Sessions

**Preconditions:**
- Same setup

**Steps:**

| # | Action | Expected Result |
|---|--------|-----------------|
| 1 | User A: POST /api/chat/sessions → Create session1 | session1.Id returned |
| 2 | User A: POST /api/chat/step (Query: "AI job market") | ResearchBrief generated for Query A |
| 3 | User B: POST /api/chat/sessions → Create session2 | session2.Id returned |
| 4 | User B: POST /api/chat/step (Query: "Climate change") | ResearchBrief generated for Query B |
| 5 | User A: POST /api/chat/sessions/{session1.Id} | Retrieve session1 with Query A |
| 6 | Verify session1 state ≠ session2 state | Each session has independent AgentState |

**Assertions:**
- ✅ Multiple sessions maintain separate state
- ✅ No data leakage between sessions
- ✅ Each session progresses independently

---

## Test Data / Fixtures

### Clear Query (No Clarification)
```
"What are the main impacts of artificial intelligence on employment 
in the next 5-10 years? Focus on job displacement, new roles created, 
and required skill transitions."
```
**Expected:** Accepted immediately, proceeds to Step 2

### Vague Query (Requires Clarification)
```
"Tell me about technology"
```
**Expected:** Rejected, asks for clarification
**Clarification Response:**
```
"AI technology in healthcare, specifically diagnostic applications and patient outcomes"
```

### Very Short Query (Boundary Test)
```
"AI jobs"
```
**Expected:** Rejected if < 10 chars (per ClarifyWithUserAsync heuristic)

### Empty Query (Edge Case)
```
""
```
**Expected:** Rejected with "Please provide a more detailed research query"

### Long Complex Query
```
"Analyze the geopolitical implications of semiconductor supply chain 
disruptions, including impacts on US-China relations, Taiwan's strategic 
importance, and how this affects global technology innovation and economic 
competitiveness. Consider regulatory responses and market consolidation trends."
```
**Expected:** Accepted immediately

---

## API Contract Testing

### ChatStepRequest

```json
{
  "currentState": {
    "messages": [
      {
        "role": "user",
        "content": "What is quantum computing?",
        "timestamp": "2024-12-23T10:00:00Z"
      }
    ],
    "researchBrief": null,
    "draftReport": null,
    "finalReport": null,
    "supervisorMessages": [],
    "rawNotes": [],
    "needsQualityRepair": true
  },
  "userResponse": null,
  "config": null
}
```

### ChatStepResponse (Step 1 - Clarification Needed)

```json
{
  "updatedState": {
    "messages": [{ ... }],
    "researchBrief": "Clarification needed: Please be more specific...",
    "draftReport": null,
    "finalReport": null,
    "needsQualityRepair": true
  },
  "displayContent": "Clarification needed: Please be more specific...",
  "currentStep": 1,
  "clarificationQuestion": "Please be more specific...",
  "isComplete": false,
  "statusMessage": "Clarification required",
  "metrics": {
    "duration_ms": 250,
    "content_length": 85
  }
}
```

### ChatStepResponse (Step 2 - ResearchBrief)

```json
{
  "updatedState": {
    "messages": [{ ... }],
    "researchBrief": "## Research Brief...",
    "draftReport": null,
    "finalReport": null,
    "needsQualityRepair": false
  },
  "displayContent": "## Research Brief: Quantum Computing Research Brief generated. Click 'Continue' to...",
  "currentStep": 2,
  "clarificationQuestion": null,
  "isComplete": false,
  "statusMessage": "Research brief generated. Click 'Continue' to generate the initial draft.",
  "metrics": {
    "duration_ms": 2500,
    "content_length": 250
  }
}
```

---

## Manual Testing Checklist

### Pre-Test Setup
- [ ] All backing services running (Ollama, SearXNG, Crawl4AI, etc.)
- [ ] API server running on `http://localhost:8080` (or configured URL)
- [ ] Test user session created and sessionId captured
- [ ] Browser DevTools open to monitor network requests

### Test Execution
- [ ] **TC-001**: Submit clear query → Verify step 1 passes without clarification
- [ ] **TC-002**: Submit vague query → Verify clarification question appears
- [ ] **TC-002b**: Provide clarification response → Verify workflow continues to step 2
- [ ] **TC-003**: After each step, verify correct property populated (ResearchBrief, DraftReport, etc.)
- [ ] **TC-004**: Verify "Continue" button enabled only after step completes
- [ ] **TC-005**: Verify progress indicator shows 1/5, 2/5, 3/5, 4/5, 5/5 for each step
- [ ] **TC-006**: Simulate network error (e.g., kill Ollama) → Verify graceful error handling
- [ ] **TC-007**: Retry after error → Verify workflow resumes correctly
- [ ] **TC-008**: Create multiple sessions → Verify independent state
- [ ] **TC-009**: Close and reopen session → Verify state persistence (if implemented frontend-side)

### Metrics to Verify
- [ ] Each step duration logged and returned in metrics
- [ ] Content length metrics accurate
- [ ] No excessive retries on transient failures
- [ ] Cache hit rates tracked (if applicable)

### UI/UX Verification
- [ ] Clarification question formatted clearly and is actionable
- [ ] ResearchBrief preview truncated to readable length
- [ ] DraftReport preview shows meaningful excerpt
- [ ] RawNotes summary displays top 2-3 insights
- [ ] FinalReport displayed in full with proper markdown formatting
- [ ] Progress bar updates after each step
- [ ] "Research complete!" badge shown on final step
- [ ] Error messages are user-friendly (no stack traces)

---

## Performance Benchmarks

| Step | Operation | Target Duration | Threshold |
|------|-----------|-----------------|-----------|
| 1 | Clarification check | < 500ms | 1000ms |
| 2 | ResearchBrief generation | 2-5 seconds | 15 seconds |
| 3 | DraftReport generation | 3-8 seconds | 20 seconds |
| 4 | SupervisorLoop refinement | 5-15 seconds | 30 seconds |
| 5 | FinalReport generation | 2-5 seconds | 15 seconds |
| **Total** | Full workflow | 12-33 seconds | 60 seconds |

---

## Implementation Checklist

### Backend
- [x] Fix `ExecuteByStepAsync` logic in MasterWorkflow
- [x] Create ChatStepRequest DTO
- [x] Create ChatStepResponse DTO
- [x] Implement `ProcessChatStepAsync` in ChatIntegrationService
- [x] Add `/api/chat/step` endpoint to ChatController
- [ ] Add unit tests for ExecuteByStepAsync state transitions
- [ ] Add integration tests for ChatIntegrationService
- [ ] Add API contract tests for ChatStepRequest/Response

### Frontend
- [ ] Create AgentState persistence layer (localStorage/sessionStorage)
- [ ] Build step-by-step UI component with progress tracker
- [ ] Implement clarification question handler
- [ ] Create "Continue" button with step validation
- [ ] Add error display with retry capability
- [ ] Display formatted content by step (previews for steps 1-4, full for step 5)
- [ ] Handle network errors gracefully

### Documentation
- [x] End-to-end testing plan (this document)
- [ ] API endpoint documentation (Swagger/OpenAPI)
- [ ] Frontend integration guide
- [ ] Troubleshooting guide for common errors

---

## Known Limitations & Future Work

1. **Session Persistence**: Currently relies on frontend to persist AgentState. Could add backend cache (Redis) for true session recovery.

2. **Concurrent Requests**: If user clicks "Continue" multiple times before response returns, could queue requests or ignore duplicates.

3. **Timeout Handling**: LLM timeouts during generation don't preserve partial results. Could implement streaming for incremental results.

4. **State Validation**: No validation that state transitions are legal (e.g., can't jump to step 5 without steps 1-4). Could add validation guards.

5. **Metric Aggregation**: Currently only returns per-step metrics. Could aggregate total time and resource usage across workflow.

---

## Appendix: cURL Examples

### Initialize Workflow (Step 1)
```bash
curl -X POST http://localhost:8080/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": {
      "messages": [{
        "role": "user",
        "content": "What is quantum computing?"
      }],
      "researchBrief": null,
      "draftReport": null,
      "finalReport": null,
      "supervisorMessages": [],
      "rawNotes": [],
      "needsQualityRepair": true
    },
    "userResponse": null
  }'
```

### Provide Clarification Response (Step 1b)
```bash
curl -X POST http://localhost:8080/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { ... updated state from previous response ... },
    "userResponse": "Focus on quantum computing applications in cryptography and optimization"
  }'
```

### Continue to Step 2
```bash
curl -X POST http://localhost:8080/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { ... with researchBrief filled ... },
    "userResponse": null
  }'
```

---

## Contact & Support

For issues or questions during testing, refer to the project's issue tracker or contact the development team.
