# Quick Reference: Step-by-Step Chat Workflow

## At a Glance

**What**: Step-by-step research workflow where each API call executes one phase

**Endpoint**: `POST /api/chat/step`

**Request**: `ChatStepRequest` with `AgentState` and optional `userResponse`

**Response**: `ChatStepResponse` with updated state and formatted content

**Steps**: 5 phases (Clarify → Brief → Draft → Refine → Final)

## The 5-Step Process

| Step | What Happens | Returns | UI Shows |
|------|---|---|---|
| 1 | Check if query is clear | Clarification Q or ResearchBrief | Question (if needed) or acceptance |
| 2 | Generate research brief | Structured research direction | 250 char preview |
| 3 | Generate draft report | Initial draft (500-1000 words) | 250 char preview |
| 4 | Refine with supervisor | Key findings and insights | Summary of notes |
| 5 | Polish final report | Publication-ready report | Full formatted report |

## API Request

```json
{
  "currentState": AgentState,
  "userResponse": "User's answer to clarification (optional)",
  "config": null
}
```

## API Response

```json
{
  "updatedState": AgentState,
  "displayContent": "Content for UI display",
  "currentStep": 1,
  "clarificationQuestion": "If clarification needed",
  "isComplete": false,
  "statusMessage": "Human-readable status",
  "metrics": {"duration_ms": 250, "content_length": 500}
}
```

## State Properties

| Property | Type | Meaning |
|----------|------|---------|
| `messages` | ChatMessage[] | Conversation history |
| `researchBrief` | string | Step 2 output |
| `draftReport` | string | Step 3 output |
| `finalReport` | string | Step 5 output |
| `supervisorMessages` | ChatMessage[] | Step 4 conversation |
| `rawNotes` | string[] | Step 4 findings |
| `needsQualityRepair` | bool | Needs clarification? |

## Frontend Flow

```
1. User submits query
   ↓
2. POST /api/chat/step with initial AgentState
   ↓
3. Response: Check clarificationQuestion
   ├─ If set: Show dialog, wait for response
   └─ Else: Show content, show "Continue" button
   ↓
4. User clicks "Continue"
   ↓
5. POST /api/chat/step with updated AgentState from step 3
   ↓
6. Repeat steps 3-5 until isComplete=true
```

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success (check `isComplete` flag) |
| 400 | Bad request (invalid JSON) |
| 500 | Server error (preserved state for retry) |

## Error Handling

**If error occurs:**
- State is preserved in response
- User sees error message
- Click "Retry" → POST same request again
- Workflow continues from same step

## Performance

| Step | Speed | Max |
|------|-------|-----|
| 1 | < 500ms | 1s |
| 2 | 2-5s | 15s |
| 3 | 3-8s | 20s |
| 4 | 5-15s | 30s |
| 5 | 2-5s | 15s |
| Total | 12-33s | 60s |

## cURL Examples

### Step 1: Initial Query
```bash
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": {
      "messages": [{"role": "user", "content": "What is artificial intelligence?"}],
      "supervisorMessages": [],
      "rawNotes": [],
      "needsQualityRepair": true
    }
  }' | jq .
```

### Step 1b: Clarification Response
```bash
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { /* previous response.updatedState */ },
    "userResponse": "Specifically AI in healthcare"
  }' | jq .
```

### Step 2+: Continue
```bash
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { /* previous response.updatedState */ }
  }' | jq .
```

## Testing Queries

**Quick (no clarification):**
```
What are the effects of artificial intelligence on job markets?
```

**Needs clarification:**
```
Tell me about technology
```

**Complex:**
```
Analyze semiconductor supply chain disruptions, their impact on US-China relations, Taiwan's strategic importance, and effects on global innovation.
```

## Common Issues

| Issue | Solution |
|-------|----------|
| Getting same response repeatedly | Check that `currentState` is updated from response |
| Clarification not disappearing | Check that `userResponse` is set and `currentState` is updated |
| No progress after step 2 | Check that `researchBrief` is populated in state |
| Endpoint returns 500 | Check backend logs, verify Ollama is running |

## Implementation Checklist

**Frontend:**
- [ ] Save state to localStorage
- [ ] Implement QueryInput component
- [ ] Implement ClarificationDialog
- [ ] Implement ContentDisplay
- [ ] Implement ProgressBar
- [ ] Test happy path (5 steps)
- [ ] Test clarification flow
- [ ] Test error handling
- [ ] Test resume/persistence

**QA:**
- [ ] Test clear query (no clarification)
- [ ] Test vague query (with clarification)
- [ ] Test error recovery
- [ ] Test session persistence
- [ ] Test multiple sessions
- [ ] Verify performance benchmarks
- [ ] Check error messages are user-friendly

## File Locations

```
README_STEP_BY_STEP.md - Overview
BuildDocs/
  ├── STEP_BY_STEP_INDEX.md - Navigation guide
  ├── Implementation/
  │   └── STEP_BY_STEP_IMPLEMENTATION.md - Technical details
  ├── Frontend/
  │   └── INTEGRATION_GUIDE.md - Code examples
  ├── API/
  │   └── STEP_API_REFERENCE.md - Full API contract
  └── Testing/
      ├── E2E_TESTING_PLAN.md - Test scenarios
      └── VERIFICATION_CHECKLIST.md - QA procedures
```

## Key Endpoints

```
POST /api/chat/step
  └─ Execute one workflow step

POST /api/chat/sessions
  └─ Create new chat session (optional)

GET /api/chat/sessions/{id}
  └─ Get session details (optional)

GET /api/chat/{sessionId}/history
  └─ Get chat history (optional)
```

## Response Properties

| Property | Type | Always Included? | Example |
|----------|------|---|---|
| `updatedState` | AgentState | Yes | {...} |
| `displayContent` | string | Yes | "## Research Brief: ..." |
| `currentStep` | int | Yes | 2 |
| `clarificationQuestion` | string | Optional | "Please be more specific..." |
| `isComplete` | bool | Yes | false |
| `statusMessage` | string | Yes | "Research brief generated..." |
| `metrics` | object | Yes | {duration_ms: 2450} |

## Decision Tree

```
Is clarificationQuestion set?
├─ Yes: Show dialog for user response, include in next POST as userResponse
└─ No: Show displayContent, show "Continue" button

Is isComplete true?
├─ Yes: Show completion badge, workflow done
└─ No: Show "Continue" button

Did error occur?
├─ Yes: Show error message, show "Retry" button
└─ No: Show content and continue
```

## Quick Facts

✅ **Stateless API** - All state passed in request/response
✅ **Resumable** - Can save state and resume later
✅ **Retryable** - Failed steps can be retried
✅ **Progressive** - Each step builds on previous
✅ **Transparent** - User sees each phase's output
✅ **Fast** - Typical total < 30 seconds
✅ **Safe** - No data loss on failure

## More Information

- **Full API contract**: See `STEP_API_REFERENCE.md`
- **Frontend code examples**: See `INTEGRATION_GUIDE.md`
- **Test scenarios**: See `E2E_TESTING_PLAN.md`
- **Technical details**: See `STEP_BY_STEP_IMPLEMENTATION.md`
- **QA procedures**: See `VERIFICATION_CHECKLIST.md`

---

**Last Updated**: 2024  
**Version**: 1.0  
**For more details**: See [`README_STEP_BY_STEP.md`](../README_STEP_BY_STEP.md)
