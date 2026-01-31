# API Quick Reference: Step-by-Step Chat Workflow

## Endpoint

```
POST /api/chat/step
```

## Request

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
    "supervisorState": null,
    "needsQualityRepair": true
  },
  "userResponse": null,
  "config": null
}
```

## Response

```json
{
  "updatedState": {
    "messages": [
      {
        "role": "user",
        "content": "What is quantum computing?"
      }
    ],
    "researchBrief": null,
    "draftReport": null,
    "finalReport": null,
    "supervisorMessages": [],
    "rawNotes": [],
    "supervisorState": null,
    "needsQualityRepair": true
  },
  "displayContent": "Clarification needed: Please be more specific about quantum computing. What aspect interests you (e.g., theory, applications, hardware)?",
  "currentStep": 1,
  "clarificationQuestion": "Please be more specific about quantum computing. What aspect interests you (e.g., theory, applications, hardware)?",
  "isComplete": false,
  "statusMessage": "Clarification required",
  "metrics": {
    "duration_ms": 245,
    "content_length": 135
  }
}
```

## Flow Examples

### Example 1: Query Needs Clarification

**Request 1:**
```json
{
  "currentState": {
    "messages": [{"role": "user", "content": "Tell me about AI"}],
    "needsQualityRepair": true,
    "supervisorMessages": [],
    "rawNotes": []
  },
  "userResponse": null
}
```

**Response 1:** (Clarification Needed)
```json
{
  "updatedState": { /* same state */ },
  "displayContent": "Clarification needed: Please specify what aspect of AI interests you...",
  "currentStep": 1,
  "clarificationQuestion": "Please specify what aspect of AI interests you...",
  "isComplete": false,
  "statusMessage": "Clarification required"
}
```

**Request 2:** (With Clarification)
```json
{
  "currentState": { /* from Response 1 */ },
  "userResponse": "AI impact on healthcare diagnostics"
}
```

**Response 2:** (Step 2 - ResearchBrief)
```json
{
  "updatedState": {
    "messages": [{"role": "user", "content": "AI impact on healthcare diagnostics"}],
    "researchBrief": "## Research Brief: AI in Healthcare Diagnostics...",
    "needsQualityRepair": false,
    "supervisorMessages": [],
    "rawNotes": []
  },
  "displayContent": "## Research Brief: AI in Healthcare Diagnostics Generated research direction with 3 objectives... Click 'Continue' to generate the initial draft.",
  "currentStep": 2,
  "clarificationQuestion": null,
  "isComplete": false,
  "statusMessage": "Research brief generated. Click 'Continue' to generate the initial draft."
}
```

### Example 2: Clear Query (No Clarification)

**Request 1:**
```json
{
  "currentState": {
    "messages": [
      {
        "role": "user",
        "content": "What are the effects of machine learning on financial market prediction and trading strategies?"
      }
    ],
    "needsQualityRepair": true,
    "supervisorMessages": [],
    "rawNotes": []
  },
  "userResponse": null
}
```

**Response 1:** (Step 2 - Brief Already Generated)
```json
{
  "updatedState": {
    "messages": [{ /* same */ }],
    "researchBrief": "## Research Brief: ML in Financial Markets...",
    "needsQualityRepair": false,
    "supervisorMessages": [],
    "rawNotes": []
  },
  "displayContent": "## Research Brief: Machine Learning in Financial Markets Research direction with objectives on prediction models, strategy optimization...",
  "currentStep": 2,
  "clarificationQuestion": null,
  "isComplete": false,
  "statusMessage": "Research brief generated. Click 'Continue' to generate the initial draft.",
  "metrics": {
    "duration_ms": 2450,
    "content_length": 245
  }
}
```

**Request 2:** (Continue to Step 3)
```json
{
  "currentState": { /* from Response 1 with researchBrief filled */ },
  "userResponse": null
}
```

**Response 2:** (Step 3 - DraftReport)
```json
{
  "updatedState": {
    "messages": [{ /* same */ }],
    "researchBrief": "## Research Brief: ML in Financial Markets...",
    "draftReport": "# Initial Draft: ML in Financial Markets\n\n## Overview\nMachine learning has significantly...",
    "supervisorMessages": [],
    "rawNotes": []
  },
  "displayContent": "# Initial Draft: Machine Learning in Financial Markets\n\n## Overview\nMachine learning has significantly transformed... Click 'Continue' to refine findings with the supervisor.",
  "currentStep": 3,
  "clarificationQuestion": null,
  "isComplete": false,
  "statusMessage": "Initial draft generated. Click 'Continue' to refine findings with the supervisor.",
  "metrics": {
    "duration_ms": 3200,
    "content_length": 250
  }
}
```

**Request 3:** (Continue to Step 4)
```json
{
  "currentState": { /* with draftReport filled */ },
  "userResponse": null
}
```

**Response 3:** (Step 4 - Supervisor Refinement)
```json
{
  "updatedState": {
    "messages": [{ /* same */ }],
    "researchBrief": "...",
    "draftReport": "...",
    "supervisorMessages": [
      {"role": "supervisor", "content": "Refinement 1..."},
      {"role": "draft", "content": "Updated draft..."}
    ],
    "rawNotes": [
      "ML models achieve 65-78% accuracy in short-term price prediction",
      "Risk management integration critical for institutional adoption",
      "Regulatory concerns around algorithmic trading impact market stability"
    ]
  },
  "displayContent": "Refined findings: ML models achieve 65-78% accuracy in short-term price prediction; Risk management integration critical for institutional adoption. Click 'Continue' to generate the final polished report.",
  "currentStep": 4,
  "clarificationQuestion": null,
  "isComplete": false,
  "statusMessage": "Findings refined. Click 'Continue' to generate the final polished report.",
  "metrics": {
    "duration_ms": 8500,
    "content_length": 250
  }
}
```

**Request 4:** (Continue to Step 5)
```json
{
  "currentState": { /* with rawNotes filled */ },
  "userResponse": null
}
```

**Response 4:** (Step 5 - Final Report) - **FINAL**
```json
{
  "updatedState": {
    "messages": [{ /* same */ }],
    "researchBrief": "...",
    "draftReport": "...",
    "finalReport": "# Final Report: Machine Learning in Financial Markets\n\n## Executive Summary\nMachine learning has revolutionized financial...",
    "supervisorMessages": [ /* ... */ ],
    "rawNotes": [ /* ... */ ]
  },
  "displayContent": "# Final Report: Machine Learning in Financial Markets\n\n## Executive Summary\nMachine learning has revolutionized financial markets...",
  "currentStep": 5,
  "clarificationQuestion": null,
  "isComplete": true,
  "statusMessage": "Research complete! Your final report is ready.",
  "metrics": {
    "duration_ms": 3800,
    "content_length": 2450
  }
}
```

## State Property Meanings

| Property | Type | Purpose |
|----------|------|---------|
| `messages` | ChatMessage[] | User-assistant conversation history |
| `researchBrief` | string | Structured research direction (Step 2) |
| `draftReport` | string | Initial draft report (Step 3) |
| `supervisorMessages` | ChatMessage[] | Supervisor refinement conversation |
| `rawNotes` | string[] | Key findings from supervisor loop |
| `finalReport` | string | Final polished report (Step 5) |
| `needsQualityRepair` | bool | Query needs clarification? |
| `supervisorState` | SupervisorState | Internal supervisor workflow state |

## Response Status Values

- **currentStep = 1**: Clarification check phase
- **currentStep = 2**: Research brief generation
- **currentStep = 3**: Draft report generation
- **currentStep = 4**: Supervisor refinement loop
- **currentStep = 5**: Final report generation

## Error Responses

### Validation Error (400)
```json
{
  "error": "Invalid request",
  "details": "CurrentState is required"
}
```

### Processing Error (500)
```json
{
  "error": "Failed to execute step",
  "details": "LLM service timeout after 30 seconds",
  "updatedState": { /* partial state if available */ }
}
```

## Status Message Examples

| Status | Meaning |
|--------|---------|
| "Clarification required" | User must answer question |
| "Research brief generated. Click 'Continue' to generate the initial draft." | Waiting for user to continue |
| "Initial draft generated. Click 'Continue' to refine findings with the supervisor." | Ready for refinement step |
| "Findings refined. Click 'Continue' to generate the final polished report." | Ready for final step |
| "Research complete! Your final report is ready." | Workflow finished |
| "An error occurred during processing. Please try again." | Error occurred, retry possible |

## Common Workflows

### Workflow A: No Clarification Needed
```
POST step 1 → Step 2 (ResearchBrief returned)
POST step 2 → Step 3 (DraftReport returned)
POST step 3 → Step 4 (RawNotes returned)
POST step 4 → Step 5 (FinalReport returned, isComplete=true)
```

### Workflow B: Clarification Required
```
POST step 1 → clarificationQuestion returned
POST step 1b (with userResponse) → Step 2 (ResearchBrief returned)
POST step 2 → Step 3 (DraftReport returned)
POST step 3 → Step 4 (RawNotes returned)
POST step 4 → Step 5 (FinalReport returned, isComplete=true)
```

### Workflow C: Error & Retry
```
POST step 3 → Error response (state preserved)
POST step 3 (retry) → Step 4 succeeds (RawNotes returned)
POST step 4 → Step 5 (FinalReport returned, isComplete=true)
```

## Response Headers

```
Content-Type: application/json
```

## Metrics in Response

Each response includes `metrics` object with:
- `duration_ms`: Total time for this step
- `content_length`: Length of displayContent
- `error` (optional): Error message if present

## Timeout Handling

- Step 1 (Clarification): 2 seconds
- Step 2 (ResearchBrief): 15 seconds
- Step 3 (DraftReport): 20 seconds
- Step 4 (Supervisor): 30 seconds
- Step 5 (FinalReport): 15 seconds

If exceeded, error response returned with preserved state for retry.

## Integration Examples

### JavaScript/TypeScript
```typescript
const response = await fetch('/api/chat/step', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(chatStepRequest)
});

const result: ChatStepResponse = await response.json();

if (result.clarificationQuestion) {
  // Show clarification dialog
  showClarificationDialog(result.clarificationQuestion);
} else if (result.isComplete) {
  // Show final report
  displayFinalReport(result.updatedState.finalReport);
} else {
  // Show progress and continue button
  showProgress(result.currentStep, 5);
  showContent(result.displayContent);
}
```

### cURL
```bash
curl -X POST http://localhost:8080/api/chat/step \
  -H "Content-Type: application/json" \
  -d @request.json | jq .
```

---

**Last Updated**: 2024
**Version**: 1.0
