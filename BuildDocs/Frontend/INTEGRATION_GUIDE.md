# Frontend Integration Guide: Step-by-Step Chat Workflow

## Overview

This guide shows how to integrate the step-by-step research workflow into a frontend application. The workflow executes one step at a time, with the UI controlling progression and displaying results.

## Architecture

```
Frontend (React/Vue/etc.)
    │
    ├─ State: AgentState (persisted in localStorage)
    │
    └─ Actions:
       1. Submit initial query → Create AgentState
       2. POST /api/chat/step → Get ChatStepResponse
       3. Display result → Store updated AgentState
       4. User clicks "Continue" → POST /api/chat/step again
       5. Repeat until IsComplete=true
```

## Key Types

### AgentState (Models)

```typescript
interface AgentState {
  messages: ChatMessage[];
  researchBrief?: string;
  draftReport?: string;
  finalReport?: string;
  supervisorMessages: ChatMessage[];
  rawNotes: string[];
  supervisorState?: SupervisorState;
  needsQualityRepair: boolean;
}

interface ChatMessage {
  id?: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp?: string;
}
```

### ChatStepRequest

```typescript
interface ChatStepRequest {
  currentState: AgentState;
  userResponse?: string;  // Only set when user provides clarification
  config?: ResearchConfig;
}
```

### ChatStepResponse

```typescript
interface ChatStepResponse {
  updatedState: AgentState;
  displayContent: string;      // Last-updated property preview
  currentStep: number;          // 1-5
  clarificationQuestion?: string;
  isComplete: boolean;
  statusMessage: string;
  metrics?: Record<string, any>;
}
```

## Implementation Steps

### 1. State Management Setup

Store the AgentState persistently so users can resume:

```typescript
// hooks/useResearchWorkflow.ts
import { useState, useEffect } from 'react';

const STORAGE_KEY = 'research_workflow_state';

export function useResearchWorkflow() {
  const [state, setState] = useState<AgentState>(() => {
    // Load from localStorage if exists
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      return JSON.parse(saved);
    }
    // Initialize fresh
    return {
      messages: [],
      supervisorMessages: [],
      rawNotes: [],
      needsQualityRepair: true,
    };
  });

  // Auto-persist to localStorage on change
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, [state]);

  return { state, setState };
}
```

### 2. API Client

Create a service to communicate with the backend:

```typescript
// services/chatService.ts
const API_BASE = 'http://localhost:5000/api/chat';

export async function executeStep(
  request: ChatStepRequest,
  signal?: AbortSignal
): Promise<ChatStepResponse> {
  const response = await fetch(`${API_BASE}/step`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.details || 'Failed to execute step');
  }

  return response.json();
}

export async function createSession(title?: string): Promise<ChatSession> {
  const response = await fetch(`${API_BASE}/sessions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title }),
  });

  if (!response.ok) throw new Error('Failed to create session');
  return response.json();
}
```

### 3. Main Chat Component

```typescript
// components/ResearchChat.tsx
import { useState } from 'react';
import { useResearchWorkflow } from '../hooks/useResearchWorkflow';
import { executeStep } from '../services/chatService';

export function ResearchChat() {
  const { state, setState } = useResearchWorkflow();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [clarificationInput, setClarificationInput] = useState('');

  // Step 1: Submit initial query
  const handleSubmitQuery = async (query: string) => {
    setError(undefined);
    const newState: AgentState = {
      messages: [{ role: 'user', content: query }],
      supervisorMessages: [],
      rawNotes: [],
      needsQualityRepair: true,
    };
    setState(newState);
    await handleExecuteStep(newState);
  };

  // Execute one step
  const handleExecuteStep = async (currentState: AgentState = state) => {
    setLoading(true);
    setError(undefined);

    try {
      const response = await executeStep({
        currentState,
        userResponse: clarificationInput || undefined,
      });

      setState(response.updatedState);
      setClarificationInput('');

      if (response.error) {
        setError(response.statusMessage);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  // Step 2: Handle clarification response
  const handleProvideClarification = async (clarification: string) => {
    setClarificationInput(clarification);
    setLoading(true);

    try {
      const response = await executeStep({
        currentState: state,
        userResponse: clarification,
      });

      setState(response.updatedState);
      setClarificationInput('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const isClarificationNeeded =
    state.needsQualityRepair && state.researchBrief?.includes('Clarification needed');

  return (
    <div className="research-chat">
      {/* Query Input */}
      {!state.researchBrief && (
        <QueryInput onSubmit={handleSubmitQuery} disabled={loading} />
      )}

      {/* Clarification Question */}
      {isClarificationNeeded && (
        <ClarificationDialog
          question={extractQuestion(state.researchBrief)}
          onSubmit={handleProvideClarification}
          disabled={loading}
        />
      )}

      {/* Content Display */}
      {state.researchBrief && !isClarificationNeeded && (
        <ContentDisplay
          state={state}
          loading={loading}
          onContinue={() => handleExecuteStep()}
        />
      )}

      {/* Error Display */}
      {error && (
        <ErrorAlert
          message={error}
          onRetry={() => handleExecuteStep()}
          dismissible
        />
      )}

      {/* Progress Indicator */}
      {state.researchBrief && (
        <ProgressBar
          currentStep={determineCurrentStep(state)}
          totalSteps={5}
        />
      )}
    </div>
  );
}

function determineCurrentStep(state: AgentState): number {
  if (!state.researchBrief) return 0;
  if (!state.draftReport) return 2;
  if (!state.supervisorMessages?.length) return 3;
  if (!state.finalReport) return 4;
  return 5;
}

function extractQuestion(brief?: string): string {
  if (!brief) return '';
  const prefix = 'Clarification needed: ';
  return brief.startsWith(prefix) ? brief.substring(prefix.length) : brief;
}
```

### 4. Content Display Component

```typescript
// components/ContentDisplay.tsx
interface Props {
  state: AgentState;
  loading: boolean;
  onContinue: () => void;
}

export function ContentDisplay({ state, loading, onContinue }: Props) {
  const currentStep = determineCurrentStep(state);

  return (
    <div className="content-display">
      {currentStep === 2 && (
        <Section title="Research Brief" isLoading={loading}>
          <p>{truncate(state.researchBrief, 300)}</p>
        </Section>
      )}

      {currentStep === 3 && (
        <Section title="Initial Draft" isLoading={loading}>
          <p>{truncate(state.draftReport, 300)}</p>
        </Section>
      )}

      {currentStep === 4 && (
        <Section title="Refined Findings" isLoading={loading}>
          <ul>
            {state.rawNotes?.slice(0, 3).map((note, i) => (
              <li key={i}>{note}</li>
            ))}
          </ul>
        </Section>
      )}

      {currentStep === 5 && (
        <Section title="Final Report" isLoading={loading}>
          <div className="markdown-content">{state.finalReport}</div>
        </Section>
      )}

      {currentStep < 5 && (
        <button
          onClick={onContinue}
          disabled={loading}
          className="btn-primary"
        >
          {loading ? 'Processing...' : 'Continue'}
        </button>
      )}

      {currentStep === 5 && (
        <div className="completion-badge">
          ✓ Research Complete!
        </div>
      )}
    </div>
  );
}

function truncate(text: string, length: number): string {
  return text.length > length ? text.substring(0, length) + '...' : text;
}
```

### 5. Clarification Dialog Component

```typescript
// components/ClarificationDialog.tsx
interface Props {
  question: string;
  onSubmit: (response: string) => void;
  disabled: boolean;
}

export function ClarificationDialog({ question, onSubmit, disabled }: Props) {
  const [input, setInput] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (input.trim()) {
      onSubmit(input);
      setInput('');
    }
  };

  return (
    <div className="clarification-dialog">
      <div className="clarification-question">
        <h3>Clarification Needed</h3>
        <p>{question}</p>
      </div>

      <form onSubmit={handleSubmit}>
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Please provide more details..."
          disabled={disabled}
          rows={4}
        />
        <button type="submit" disabled={disabled || !input.trim()}>
          {disabled ? 'Processing...' : 'Submit'}
        </button>
      </form>
    </div>
  );
}
```

## Error Handling

```typescript
// Handle specific error cases
try {
  const response = await executeStep(request);
  
  if (!response.isComplete && response.clarificationQuestion) {
    // User must answer clarification
    displayClarificationDialog(response.clarificationQuestion);
  } else if (response.statusMessage.includes('error')) {
    // Show error with retry
    showErrorAlert(response.statusMessage, () => retryStep());
  }
} catch (error) {
  if (error instanceof TypeError && error.message.includes('fetch')) {
    // Network error
    showNetworkError('Unable to reach server. Check your connection.');
  } else {
    // Other errors
    showError(error.message);
  }
}
```

## localStorage Persistence

The AgentState is automatically saved to localStorage after each step. This allows users to:
- Close the browser and resume later
- Navigate away and return to the same research
- Use the "back" button without losing progress

To clear stored state:
```typescript
localStorage.removeItem('research_workflow_state');
```

## Testing with cURL

Before implementing the frontend, test the backend API directly:

```bash
# Step 1: Initial query (should ask for clarification if vague)
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": {
      "messages": [{"role": "user", "content": "Tell me about tech"}],
      "supervisorMessages": [],
      "rawNotes": [],
      "needsQualityRepair": true
    }
  }' | jq .

# Step 1b: Provide clarification
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { ... previous response.updatedState ... },
    "userResponse": "AI impact on job market"
  }' | jq .

# Step 2: Continue with ResearchBrief filled
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": { ... with researchBrief set ... },
    "userResponse": null
  }' | jq .
```

## Performance Optimization

1. **Debounce Continue Button**: Prevent duplicate requests
```typescript
const handleContinue = debounce(() => handleExecuteStep(), 300);
```

2. **Cancel in-flight Requests**: If user navigates away
```typescript
const controller = new AbortController();
return executeStep(request, controller.signal);
```

3. **Lazy Load Content**: For large final reports
```typescript
const FinalReportSection = lazy(() => import('./FinalReportSection'));
```

## Accessibility

- Use semantic HTML (`<section>`, `<article>`, `<button>`)
- Add ARIA labels for clarification dialog
- Ensure buttons have visible focus states
- Provide keyboard navigation (Tab, Enter)

## Example Styles

```css
.research-chat {
  max-width: 900px;
  margin: 0 auto;
  padding: 20px;
  font-family: system-ui, -apple-system, sans-serif;
}

.content-display {
  background: #f5f5f5;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
}

.clarification-dialog {
  background: #fff3cd;
  border-left: 4px solid #ffc107;
  padding: 20px;
  border-radius: 4px;
  margin: 20px 0;
}

.completion-badge {
  background: #d4edda;
  color: #155724;
  padding: 12px;
  border-radius: 4px;
  text-align: center;
  font-weight: bold;
  margin-top: 20px;
}

.btn-primary {
  background: #007bff;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
}

.btn-primary:disabled {
  background: #ccc;
  cursor: not-allowed;
}
```

## Next Steps

1. Implement the components in your framework (React, Vue, Svelte, etc.)
2. Test with the cURL examples provided
3. Run through the manual test scenarios in E2E_TESTING_PLAN.md
4. Monitor performance with browser DevTools
5. Iterate on UI/UX based on user feedback
