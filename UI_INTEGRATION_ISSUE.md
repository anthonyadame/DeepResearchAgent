# ⚠️ UI Integration Issue - What's Different

## Problem Identified

The UI is still using the **old chat system** (`/api/chat/...`) instead of the **new streaming research endpoint** (`/api/workflows/master/stream`).

### Old Flow (Chat System)
```
ChatDialog
  ↓
useChat hook (old)
  ↓
apiService.submitQuery() or apiService.streamQuery()
  ↓
POST /api/chat/{sessionId}/query (old endpoint)
```

### New Flow (Research Streaming)
```
ChatDialog (or new ResearchPanel)
  ↓
useMasterWorkflowStream hook (new)
  ↓
MasterWorkflowStreamClient.streamMasterWorkflow()
  ↓
POST /api/workflows/master/stream (new endpoint)
```

---

## What I Created vs What Exists

### ✅ I Created (New Research System)
- `src/hooks/useMasterWorkflowStream.ts` - Hook for research streaming
- `src/services/masterWorkflowStreamClient.ts` - Streaming client
- `src/components/ResearchProgressCard.tsx` - UI component
- `src/utils/streamStateFormatter.ts` - Helper functions
- Updated `src/types/index.ts` - Added StreamState type
- Updated `src/services/api.ts` - Added streamMasterWorkflow method

### ✅ Already Exists (Old Chat System)
- `src/hooks/useChat.ts` - Chat hook (uses `/api/chat`)
- `src/components/ChatDialog.tsx` - Chat component
- `src/services/api.ts` - Has submitQuery, streamQuery methods

### ❌ Missing
The new components are NOT integrated into ChatDialog!

---

## Why curl Works But UI Doesn't

### curl Command Works ✅
```bash
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery": "How much would it cost..."}'
```

**Why:** Direct call to the working endpoint

### UI Doesn't Use It ❌
```typescript
// ChatDialog calls OLD endpoint
const response = await apiService.submitQuery(sessionId, content, config)
// This uses: POST /api/chat/{sessionId}/query (old chat system)

// NOT the new endpoint!
// Missing: POST /api/workflows/master/stream
```

---

## How to Fix: 2 Options

### Option 1: Create New Research Panel Component (Recommended)

Create a separate component that uses the new streaming hook:

```typescript
// src/components/ResearchPanel.tsx
import { useMasterWorkflowStream } from '@hooks/useMasterWorkflowStream'
import ResearchProgressCard from './ResearchProgressCard'

export default function ResearchPanel() {
  const { currentState, progress, isStreaming, error, startStream } = useMasterWorkflowStream()
  const [query, setQuery] = useState('')

  const handleResearch = async () => {
    await startStream(query)
  }

  return (
    <div>
      <input value={query} onChange={(e) => setQuery(e.target.value)} />
      <button onClick={handleResearch} disabled={isStreaming}>Research</button>
      
      {currentState && (
        <ResearchProgressCard
          state={currentState}
          progress={progress}
          isStreaming={isStreaming}
          error={error}
        />
      )}
    </div>
  )
}
```

Then add to App.tsx as a tab/mode.

### Option 2: Update ChatDialog to Support Both (More Complex)

Add the new streaming to ChatDialog:

```typescript
import { useMasterWorkflowStream } from '@hooks/useMasterWorkflowStream'
import ResearchProgressCard from './ResearchProgressCard'

export default function ChatDialog({ sessionId }: ChatDialogProps) {
  const { messages, sendMessage } = useChat(sessionId)
  const { currentState, progress, isStreaming, startStream } = useMasterWorkflowStream()
  
  const handleSendMessage = async () => {
    if (!input.trim()) return
    
    // NEW: Use research streaming
    try {
      await startStream(input)
      setInput('')
    } catch (err) {
      console.error('Error:', err)
    }
  }

  return (
    <>
      {currentState && (
        <ResearchProgressCard {...} />
      )}
      {/* existing UI */}
    </>
  )
}
```

---

## Testing

### What Curl Does (Works)
```bash
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery": "How much would it cost..."}'

# Response: SSE stream with real-time updates ✅
```

### What UI Should Do (Doesn't Currently)
```typescript
// Current: Calls OLD chat endpoint
await apiService.submitQuery(sessionId, content, config)  // ❌ Wrong endpoint

// Should: Call NEW research endpoint
await startStream(content)  // ✅ Correct endpoint
```

---

## The Root Cause

I created all the necessary **new components and hooks** for the research streaming system, but they're NOT being used anywhere in the UI!

**The files are there but unused:**
- ✅ `useMasterWorkflowStream.ts` - Created but not imported anywhere
- ✅ `ResearchProgressCard.tsx` - Created but not displayed
- ✅ `masterWorkflowStreamClient.ts` - Created but not called
- ✅ `streamStateFormatter.ts` - Created but not used

**The existing chat system is still in place:**
- ✅ `useChat.ts` - Still calling old chat endpoint
- ✅ `ChatDialog.tsx` - Still using old sendMessage
- ✅ `apiService.submitQuery()` - Still calling `/api/chat`

---

## Solution Summary

### Create a new component that uses the streaming hook:

**File: `src/components/ResearchStreamingPanel.tsx`**
```typescript
import { useMasterWorkflowStream } from '@hooks/useMasterWorkflowStream'
import ResearchProgressCard from './ResearchProgressCard'

export default function ResearchStreamingPanel() {
  const { currentState, progress, isStreaming, error, startStream } = useMasterWorkflowStream()
  const [query, setQuery] = useState('')

  const handleResearch = async () => {
    if (!query.trim()) return
    try {
      await startStream(query)
      setQuery('')
    } catch (err) {
      console.error('Error:', err)
    }
  }

  return (
    <div className="flex flex-col h-full gap-4 p-4">
      <div className="flex gap-2">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Enter research query..."
          className="flex-1 px-4 py-2 border rounded"
        />
        <button
          onClick={handleResearch}
          disabled={isStreaming}
          className="px-6 py-2 bg-blue-600 text-white rounded disabled:bg-gray-400"
        >
          Research
        </button>
      </div>

      {currentState && (
        <ResearchProgressCard
          state={currentState}
          progress={progress}
          isStreaming={isStreaming}
          error={error}
          supervisorUpdateCount={currentState.supervisorUpdateCount}
        />
      )}
    </div>
  )
}
```

Then use it in App.tsx or add as a mode in ChatDialog.

---

## Why This Matters

**curl works** because it directly calls the API endpoint that's working.

**UI doesn't work** because the UI components haven't been wired to call that same endpoint yet.

**The fix:** Use the `useMasterWorkflowStream` hook and `ResearchProgressCard` component I created to integrate the working endpoint into the UI.

---

## Files Ready to Use

All these files are ready and just need to be imported/used:

1. ✅ `useMasterWorkflowStream()` - Main hook
2. ✅ `ResearchProgressCard` - Display component
3. ✅ `masterWorkflowStreamClient.ts` - Client library
4. ✅ `streamStateFormatter.ts` - Helpers
5. ✅ `apiService.streamMasterWorkflow()` - API method

**Just need to wire them together in a component!**

---

## Next Step

Create `ResearchStreamingPanel.tsx` and import the new hook/components to use the working `/api/workflows/master/stream` endpoint.
