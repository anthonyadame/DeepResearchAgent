# ⚡ CORRECTED FRONTEND IMPLEMENTATION GUIDE

Based on the fixes applied for:
- ✅ Port: Changed from 8080 to 5000
- ✅ AutoMapper: Fixed version mismatch

---

## 🔌 Correct API Configuration

**Your API runs on port 5000, NOT 8080**

```typescript
// ✅ CORRECT - Use This
const API_BASE = 'http://localhost:5000/api/chat';

// ❌ WRONG - Don't Use This
const API_BASE = 'http://localhost:8080/api/chat'; // This is SearXNG!
```

---

## 📦 Complete Frontend Setup

### Step 1: Create Frontend Project

**PowerShell:**
```powershell
cd C:\RepoEx\DeepResearchAgent
npm create vite@latest DeepResearchAgent.UI -- --template react-ts
cd DeepResearchAgent.UI
npm install
```

**Bash/Linux:**
```sh
cd ~/DeepResearchAgent
npm create vite@latest DeepResearchAgent.UI -- --template react-ts
cd DeepResearchAgent.UI
npm install
```

### Step 2: Create Correct API Service

**File: `src/services/chatService.ts`**

```typescript
import type { ChatStepRequest, ChatStepResponse } from '../types';

// ✅ CORRECT PORT
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

### Step 3: Create Types Definition

**File: `src/types/index.ts`**

```typescript
export interface ChatMessage {
  id?: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp?: string;
}

export interface SupervisorState {
  draftReport?: string;
  supervisorMessages?: ChatMessage[];
  rawNotes?: string[];
}

export interface AgentState {
  messages: ChatMessage[];
  researchBrief?: string;
  draftReport?: string;
  finalReport?: string;
  supervisorMessages: ChatMessage[];
  rawNotes: string[];
  supervisorState?: SupervisorState;
  needsQualityRepair: boolean;
}

export interface ChatStepRequest {
  currentState: AgentState;
  userResponse?: string;
  config?: unknown;
}

export interface ChatStepResponse {
  updatedState: AgentState;
  displayContent: string;
  currentStep: number;
  clarificationQuestion?: string;
  isComplete: boolean;
  statusMessage: string;
  metrics?: Record<string, unknown>;
}
```

### Step 4: Create Custom Hook

**File: `src/hooks/useResearchWorkflow.ts`**

```typescript
import { useState, useEffect } from 'react';
import type { AgentState } from '../types';

const STORAGE_KEY = 'research_workflow_state';

const INITIAL_STATE: AgentState = {
  messages: [],
  supervisorMessages: [],
  rawNotes: [],
  needsQualityRepair: true,
};

export function useResearchWorkflow() {
  const [state, setState] = useState<AgentState>(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return INITIAL_STATE;
      }
    }
    return INITIAL_STATE;
  });

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, [state]);

  const reset = () => {
    localStorage.removeItem(STORAGE_KEY);
    setState(INITIAL_STATE);
  };

  return { state, setState, reset };
}
```

---

## 🧪 Testing the Correct Configuration

### Test 1: Backend Ready Check

**PowerShell:**
```powershell
# Ensure backend is running on correct port
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/chat/step" `
  -Method OPTIONS `
  -ErrorAction SilentlyContinue

if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 204) {
  Write-Host "✓ API responding on port 5000"
} else {
  Write-Host "✗ API not found on port 5000"
}
```

**Bash/Linux:**
```sh
curl -I http://localhost:5000/api/chat/step | head -n 1
```

### Test 2: Execute API Call

**PowerShell:**
```powershell
$uri = "http://localhost:5000/api/chat/step"
$body = @{
    currentState = @{
        messages = @(@{ role = "user"; content = "What is AI?" })
        supervisorMessages = @()
        rawNotes = @()
        needsQualityRepair = $true
    }
} | ConvertTo-Json -Depth 10

$response = Invoke-WebRequest -Uri $uri `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body $body

$response.Content | ConvertFrom-Json | Select-Object currentStep, isComplete, statusMessage
```

**Bash/Linux:**
```sh
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": {
      "messages": [{"role": "user", "content": "What is AI?"}],
      "supervisorMessages": [],
      "rawNotes": [],
      "needsQualityRepair": true
    }
  }' | jq '.currentStep, .isComplete'
```

**Expected Response:**
```json
{
  "currentStep": 2,
  "isComplete": false,
  "statusMessage": "Research brief generated..."
}
```

---

## 🎨 Complete Component Structure

```
DeepResearchAgent.UI/
├── src/
│   ├── components/
│   │   ├── QueryInput.tsx
│   │   ├── ClarificationDialog.tsx
│   │   ├── ContentDisplay.tsx
│   │   ├── ProgressBar.tsx
│   │   ├── ErrorAlert.tsx
│   │   └── ResearchChat.tsx
│   ├── hooks/
│   │   └── useResearchWorkflow.ts
│   ├── services/
│   │   └── chatService.ts          ← API_BASE = 'http://localhost:5000/api/chat'
│   ├── types/
│   │   └── index.ts
│   ├── App.tsx
│   ├── App.css
│   └── main.tsx
├── package.json
├── tsconfig.json
├── vite.config.ts
└── index.html
```

---

## 🚀 Running Everything Together

### Terminal 1: Start Backend API (Port 5000)
```powershell
cd C:\RepoEx\DeepResearchAgent
dotnet run --project DeepResearchAgent.Api
# Listening on: http://localhost:5000
```

### Terminal 2: Start Frontend Development Server (Port 5173)
```powershell
cd C:\RepoEx\DeepResearchAgent\DeepResearchAgent.UI
npm run dev
# Local: http://localhost:5173
```

### Browser
- Frontend: `http://localhost:5173`
- API: `http://localhost:5000/api/chat/step`
- SearXNG (separate): `http://localhost:8080` (don't use this for API!)

---

## ✅ Correct Configuration Checklist

- [ ] Backend API listens on **port 5000**
- [ ] Frontend `API_BASE = 'http://localhost:5000/api/chat'`
- [ ] AutoMapper versions matched (12.0.1 both)
- [ ] cURL tests use **port 5000**
- [ ] PowerShell tests use `localhost:5000`
- [ ] Browser frontend on **port 5173**
- [ ] Can successfully execute API calls from frontend

---

## 📋 Common Mistakes to Avoid

| ❌ Wrong | ✅ Right |
|---------|----------|
| `http://localhost:8080` | `http://localhost:5000` |
| AutoMapper 13.0.1 + Extension 12.0.1 | AutoMapper 12.0.1 + Extension 12.0.1 |
| `curl http://localhost:8080/api/chat/step` | `curl http://localhost:5000/api/chat/step` |
| Invoking to 8080 in PowerShell | Invoking to 5000 in PowerShell |

---

## 🎯 Summary

- **API Port**: 5000
- **Frontend Port**: 5173
- **SearXNG Port**: 8080 (don't confuse!)
- **API URL**: `http://localhost:5000/api/chat/step`
- **All fixes applied**: Ready to develop

**Next Step**: Follow the component implementation guide in INTEGRATION_GUIDE.md with port 5000 in mind.

---

**✅ Status: Ready for Frontend Development**
**🔧 Fixes Applied: Port 5000, AutoMapper 12.0.1**
**📱 Frontend Ready: Create React app and connect to port 5000**
