# UI Streaming Integration Guide

## ✅ Changes Made

The DeepResearchAgent UI has been updated to support **Server-Sent Events (SSE) streaming** for real-time research workflow updates.

## 📁 Files Updated

### 1. `src/services/api.ts`
Added `streamQuery()` method for SSE streaming:

```typescript
streamQuery(
  sessionId: string,
  message: string,
  config: ResearchConfig | undefined,
  onUpdate: (update: string) => void,
  onComplete: () => void,
  onError: (error: Error) => void
): AbortController
```

**Features:**
- Connects to `/api/chat/{sessionId}/stream` endpoint
- Processes Server-Sent Events (SSE) in real-time
- Returns `AbortController` for cancellation support
- Handles `[DONE]`, `[CANCELLED]`, and error events

### 2. `src/hooks/useChat.ts`
Added streaming support with new methods:

```typescript
const {
  messages,              // All chat messages
  isLoading,             // Non-streaming loading state
  error,                 // Error messages
  loadHistory,           // Load chat history
  sendMessage,           // Send message (non-streaming)
  sendMessageStreaming,  // 🆕 Send message with streaming
  isStreaming,           // 🆕 Streaming state
  streamingMessage,      // 🆕 Current streaming content
  cancelStreaming        // 🆕 Cancel active stream
} = useChat(sessionId)
```

## 🎯 How to Use in Components

### Option 1: Enable Streaming in Existing ChatDialog

Update `ChatDialog.tsx` to use streaming:

```typescript
export default function ChatDialog({ sessionId }: ChatDialogProps) {
  const { 
    messages, 
    isLoading, 
    isStreaming,           // 🆕 Add this
    streamingMessage,      // 🆕 Add this
    sendMessageStreaming,  // 🆕 Use instead of sendMessage
    cancelStreaming,       // 🆕 For cancel button
    loadHistory 
  } = useChat(sessionId)

  const handleSendMessage = async () => {
    if (!input.trim()) return

    try {
      // Use streaming version
      await sendMessageStreaming(input, config)  // 🆕 Changed
      setInput('')
    } catch (err) {
      console.error('Error sending message:', err)
    }
  }

  // Add cancel handler
  const handleCancel = () => {
    cancelStreaming()
  }

  return (
    <div className="flex flex-col h-full">
      {/* Messages */}
      <MessageList 
        messages={messages} 
        isLoading={isLoading}
        streamingMessage={streamingMessage}  // 🆕 Pass streaming content
      />

      {/* Input bar with cancel support */}
      <InputBar
        value={input}
        onChange={setInput}
        onSend={handleSendMessage}
        onCancel={handleCancel}  // 🆕 Add cancel
        isLoading={isLoading || isStreaming}
        isStreaming={isStreaming}  // 🆕 Show streaming state
      />
    </div>
  )
}
```

### Option 2: Create Streaming Toggle

Add a setting to switch between streaming and non-streaming:

```typescript
const [useStreaming, setUseStreaming] = useState(true)

const handleSendMessage = async () => {
  if (!input.trim()) return

  try {
    if (useStreaming) {
      await sendMessageStreaming(input, config)
    } else {
      await sendMessage(input, config)
    }
    setInput('')
  } catch (err) {
    console.error('Error sending message:', err)
  }
}
```

## 📊 Update MessageList Component

Modify `MessageList.tsx` to display streaming content:

```typescript
interface MessageListProps {
  messages: ChatMessage[]
  isLoading: boolean
  streamingMessage?: string  // 🆕 Add this
}

export default function MessageList({ 
  messages, 
  isLoading,
  streamingMessage  // 🆕
}: MessageListProps) {
  return (
    <div className="flex-1 overflow-y-auto">
      {messages.map(msg => (
        <MessageBubble key={msg.id} message={msg} />
      ))}
      
      {/* Show streaming message in progress */}
      {streamingMessage && (
        <div className="streaming-message">
          <div className="animate-pulse">Assistant is researching...</div>
          <pre className="whitespace-pre-wrap font-mono text-sm">
            {streamingMessage}
          </pre>
        </div>
      )}
      
      {isLoading && !streamingMessage && (
        <div className="loading-indicator">Loading...</div>
      )}
    </div>
  )
}
```

## 🎨 Add Streaming UI Indicators

### Option 1: Progress Steps
Show which workflow step is active:

```typescript
const parseStreamingStep = (message: string): string => {
  if (message.includes('step 1')) return 'Clarifying intent'
  if (message.includes('step 2')) return 'Creating research brief'
  if (message.includes('step 3')) return 'Generating draft'
  if (message.includes('step 4')) return 'Supervisor refinement'
  if (message.includes('step 5')) return 'Final report'
  return 'Processing'
}

// In component
const currentStep = parseStreamingStep(streamingMessage)

<div className="flex items-center gap-2">
  <Loader2 className="animate-spin" />
  <span>{currentStep}</span>
</div>
```

### Option 2: Live Updates Display
Show real-time streaming updates:

```typescript
<div className="streaming-updates">
  {streamingMessage.split('\n').map((line, idx) => (
    <div key={idx} className="update-line fade-in">
      {line}
    </div>
  ))}
</div>

<style>
  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
  }
  
  .fade-in {
    animation: fadeIn 0.3s ease-out;
  }
</style>
```

## 🔧 Configuration

### Environment Variables
The API base URL is configured in `.env.local`:

```bash
VITE_API_BASE_URL=http://localhost:5000/api
```

This matches the API streaming endpoints:
- `http://localhost:5000/api/chat/{sessionId}/stream` ✅
- `http://localhost:5000/api/workflows/master/stream` ✅

## 🧪 Testing the UI Integration

### 1. Start the API
```bash
cd DeepResearchAgent.Api
dotnet run
```

### 2. Start the UI
```bash
cd DeepResearchAgent.UI
npm install  # First time only
npm run dev
```

### 3. Test Streaming
1. Open browser to `http://localhost:5173` (Vite default)
2. Create a new chat session
3. Send a message
4. Watch real-time streaming updates appear

### 4. Test Cancellation
1. Start a long-running query
2. Click cancel button (if implemented)
3. Verify streaming stops immediately

## 📝 Example Implementation

Here's a complete example of `ChatDialog.tsx` with streaming:

```typescript
import { useEffect, useState } from 'react'
import { Send, StopCircle } from 'lucide-react'
import { useChat } from '@hooks/useChat'
import MessageList from './MessageList'
import InputBar from './InputBar'

export default function ChatDialog({ sessionId }: { sessionId: string }) {
  const {
    messages,
    isStreaming,
    streamingMessage,
    sendMessageStreaming,
    cancelStreaming,
    loadHistory
  } = useChat(sessionId)
  
  const [input, setInput] = useState('')

  useEffect(() => {
    loadHistory()
  }, [sessionId, loadHistory])

  const handleSend = async () => {
    if (!input.trim() || isStreaming) return
    
    await sendMessageStreaming(input)
    setInput('')
  }

  return (
    <div className="flex flex-col h-full">
      <MessageList
        messages={messages}
        streamingMessage={streamingMessage}
        isLoading={isStreaming}
      />
      
      <div className="p-4 border-t">
        <div className="flex gap-2">
          <input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSend()}
            disabled={isStreaming}
            className="flex-1 px-4 py-2 border rounded"
            placeholder="Ask a research question..."
          />
          
          {isStreaming ? (
            <button
              onClick={cancelStreaming}
              className="px-4 py-2 bg-red-500 text-white rounded flex items-center gap-2"
            >
              <StopCircle className="w-4 h-4" />
              Cancel
            </button>
          ) : (
            <button
              onClick={handleSend}
              disabled={!input.trim()}
              className="px-4 py-2 bg-blue-500 text-white rounded flex items-center gap-2"
            >
              <Send className="w-4 h-4" />
              Send
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
```

## 🎯 Streaming Message Format

The streaming endpoint sends messages in this format:

```
data: [master] received query: What is machine learning?
data: [master] step 1: clarifying user intent...
data: [master] query is sufficiently detailed
data: [master] step 2: writing research brief...
data: [supervisor] Starting supervision loop...
data: [supervisor] Iteration 1/5: Evaluating quality...
data: [master] step 5: generating final polished report...
data: [DONE]
```

You can parse these to show:
- Current agent: `[master]`, `[supervisor]`, `[researcher]`
- Current step: Extract from message content
- Progress: Count iterations

## 🚀 Advanced Features

### 1. Progress Bar
```typescript
const calculateProgress = (message: string): number => {
  const steps = ['step 1', 'step 2', 'step 3', 'step 4', 'step 5']
  for (let i = 0; i < steps.length; i++) {
    if (message.includes(steps[i])) {
      return ((i + 1) / steps.length) * 100
    }
  }
  return 0
}

<div className="w-full bg-gray-200 rounded-full h-2">
  <div
    className="bg-blue-500 h-2 rounded-full transition-all"
    style={{ width: `${calculateProgress(streamingMessage)}%` }}
  />
</div>
```

### 2. Typing Indicator
```typescript
{isStreaming && (
  <div className="flex items-center gap-2 text-gray-500">
    <div className="flex gap-1">
      <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
      <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
      <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
    </div>
    <span>Researching...</span>
  </div>
)}
```

### 3. Message Stats
```typescript
const [stats, setStats] = useState({ messages: 0, duration: 0 })

useEffect(() => {
  if (isStreaming) {
    const startTime = Date.now()
    const interval = setInterval(() => {
      setStats({
        messages: streamingMessage.split('\n').length,
        duration: Math.floor((Date.now() - startTime) / 1000)
      })
    }, 100)
    return () => clearInterval(interval)
  }
}, [isStreaming, streamingMessage])

<div className="text-xs text-gray-500">
  {stats.messages} updates • {stats.duration}s
</div>
```

## ✅ Validation Checklist

Before deploying UI changes:

- [ ] API endpoint configured correctly (`/api/chat/{sessionId}/stream`)
- [ ] Streaming messages display in real-time
- [ ] Cancel button stops streaming
- [ ] Error handling works (network issues, API errors)
- [ ] Loading states are clear to user
- [ ] Final message saved to chat history
- [ ] Scroll behavior works during streaming
- [ ] Mobile responsive

## 🐛 Troubleshooting

### Issue: No streaming updates appear
**Solution:**
- Check browser console for errors
- Verify API endpoint is `/api/chat/{sessionId}/stream`
- Check CORS settings in API
- Verify VITE_API_BASE_URL in `.env.local`

### Issue: Messages appear but then disappear
**Solution:**
- Check that `streamingMessage` state is properly managed
- Verify `onComplete` callback saves message to `messages` array
- Don't reset `streamingMessage` before saving

### Issue: Cancel doesn't work
**Solution:**
- Verify `AbortController` is stored in ref
- Check that `abort()` is called on the controller
- Ensure streaming state is reset after cancel

## 📚 Next Steps

1. **Implement in ChatDialog.tsx**
   - Replace `sendMessage` with `sendMessageStreaming`
   - Add cancel button UI
   - Update MessageList to show streaming content

2. **Add Visual Enhancements**
   - Progress indicators
   - Step-by-step display
   - Animated transitions

3. **Testing**
   - Test with various query types
   - Test cancellation
   - Test error scenarios
   - Test on mobile devices

4. **Performance**
   - Optimize re-renders during streaming
   - Consider virtualization for long streams
   - Add debouncing if needed

## 🎉 Ready to Integrate!

The streaming infrastructure is now in place. Update your components to use the new streaming methods and enjoy real-time research workflow updates!
