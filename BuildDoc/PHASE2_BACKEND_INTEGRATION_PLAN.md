# Phase 2: Backend Integration - Implementation Plan

## 🎯 Goals
1. Connect UI to running backend API
2. Implement chat session management
3. Test message sending/receiving
4. Verify file uploads
5. Test configuration persistence
6. Load real chat history

---

## 📋 Current Status

### ✅ Frontend Ready
- UI components built and tested
- API service configured
- Type definitions complete
- Error handling in place

### ⚠️ Backend Gaps
**Missing Endpoints:**
1. `POST /api/chat/sessions` - Create session
2. `GET /api/chat/sessions` - List sessions
3. `DELETE /api/chat/sessions/{id}` - Delete session
4. `POST /api/chat/{sessionId}/query` - Send message
5. `GET /api/chat/{sessionId}/history` - Get history
6. `POST /api/chat/{sessionId}/files` - Upload files
7. `GET /api/config/models` - Available models
8. `GET /api/config/search-tools` - Search providers
9. `POST /api/config/save` - Save configuration

**Existing Endpoints:**
- `/api/v1/research/execute` - Execute research pipeline
- Various agent and workflow endpoints

---

## 🛠️ Implementation Steps

### Step 1: Create ChatController (Backend)
**File:** `DeepResearchAgent.Api/Controllers/ChatController.cs`

**Purpose:** Handle chat session and message operations

**Endpoints to Implement:**
```csharp
[Route("api/chat")]
public class ChatController : ControllerBase
{
    // Session Management
    POST /api/chat/sessions        → CreateSession
    GET  /api/chat/sessions        → GetSessions
    GET  /api/chat/sessions/{id}   → GetSession
    DELETE /api/chat/sessions/{id} → DeleteSession
    
    // Messaging
    POST /api/chat/{sessionId}/query   → SendMessage
    GET  /api/chat/{sessionId}/history → GetHistory
    
    // File Management
    POST /api/chat/{sessionId}/files   → UploadFile
    GET  /api/chat/{sessionId}/files   → ListFiles
}
```

### Step 2: Create ConfigurationController (Backend)
**File:** `DeepResearchAgent.Api/Controllers/ConfigurationController.cs`

**Purpose:** Provide configuration options to UI

**Endpoints:**
```csharp
[Route("api/config")]
public class ConfigurationController : ControllerBase
{
    GET  /api/config/models       → GetAvailableModels
    GET  /api/config/search-tools → GetSearchTools
    POST /api/config/save         → SaveConfiguration
    GET  /api/config/current      → GetCurrentConfiguration
}
```

### Step 3: Create DTOs (Backend)
**File:** `DeepResearchAgent.Api/DTOs/ChatDtos.cs`

```csharp
public record ChatSession
{
    public string Id { get; init; }
    public string Title { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ChatMessage> Messages { get; init; }
    public ResearchConfig? Config { get; init; }
}

public record ChatMessage
{
    public string Id { get; init; }
    public string Role { get; init; } // user, assistant, system
    public string Content { get; init; }
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record CreateSessionRequest
{
    public string? Title { get; init; }
}

public record SendMessageRequest
{
    public string Message { get; init; }
    public ResearchConfig? Config { get; init; }
}

public record ResearchConfig
{
    public string Language { get; init; }
    public List<string> LlmModels { get; init; }
    public List<string> IncludedWebsites { get; init; }
    public List<string> ExcludedWebsites { get; init; }
    public List<string> Topics { get; init; }
    public int? MaxDepth { get; init; }
    public int? TimeoutSeconds { get; init; }
}
```

### Step 4: Create Session Service (Backend)
**File:** `DeepResearchAgent.Api/Services/ChatSessionService.cs`

**Purpose:** Manage session state and persistence

```csharp
public interface IChatSessionService
{
    Task<ChatSession> CreateSessionAsync(string? title);
    Task<ChatSession?> GetSessionAsync(string sessionId);
    Task<List<ChatSession>> GetSessionsAsync();
    Task DeleteSessionAsync(string sessionId);
    Task<ChatMessage> AddMessageAsync(string sessionId, ChatMessage message);
    Task<List<ChatMessage>> GetHistoryAsync(string sessionId);
}

public class ChatSessionService : IChatSessionService
{
    // Use LightningStateService for persistence
    private readonly ILightningStateService _stateService;
    private readonly ILogger<ChatSessionService> _logger;
    
    // Implementation using existing state management
}
```

### Step 5: Integrate with Existing Workflow
**File:** `DeepResearchAgent.Api/Services/ChatIntegrationService.cs`

**Purpose:** Bridge chat messages to research workflow

```csharp
public class ChatIntegrationService
{
    private readonly MasterWorkflow _masterWorkflow;
    private readonly ResearcherAgent _researcherAgent;
    private readonly AnalystAgent _analystAgent;
    private readonly ReportAgent _reportAgent;
    
    public async Task<string> ProcessChatMessageAsync(
        string sessionId,
        string userMessage,
        ResearchConfig? config)
    {
        // Convert chat message to research request
        // Execute workflow
        // Return assistant response
    }
}
```

### Step 6: Update CORS and Middleware
**File:** `DeepResearchAgent.Api/Program.cs`

```csharp
// Add chat services
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddScoped<ChatIntegrationService>();

// Ensure CORS allows UI
app.UseCors("AllowUI");
```

### Step 7: Frontend Updates
**File:** `DeepResearchAgent.UI/.env`

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

**File:** `DeepResearchAgent.UI/src/services/api.ts`

Update if needed to match actual backend endpoints.

---

## 🔄 Integration Flow

### Chat Message Flow
```
User Input (UI)
    ↓
ChatDialog Component
    ↓
apiService.submitQuery()
    ↓
POST /api/chat/{sessionId}/query
    ↓
ChatController.SendMessage()
    ↓
ChatIntegrationService.ProcessChatMessageAsync()
    ↓
MasterWorkflow.ExecuteFullPipelineAsync()
    ↓
Return Report
    ↓
ChatController returns ChatMessage
    ↓
UI displays assistant response
```

### Session Management Flow
```
App Initialization
    ↓
Check localStorage for lastSessionId
    ↓
If exists: GET /api/chat/sessions/{id}
    ↓
If not: POST /api/chat/sessions
    ↓
Store sessionId
    ↓
Load ChatDialog with sessionId
```

---

## 🧪 Testing Plan

### Phase 2.1: Backend API Setup
- [ ] Create ChatController
- [ ] Create ConfigurationController
- [ ] Implement DTOs
- [ ] Create ChatSessionService
- [ ] Test endpoints with Swagger/Postman
- [ ] Verify CORS configuration

### Phase 2.2: Frontend Integration
- [ ] Update .env with backend URL
- [ ] Test session creation
- [ ] Test message sending
- [ ] Test history loading
- [ ] Test session deletion
- [ ] Test file upload

### Phase 2.3: End-to-End Testing
- [ ] Create session from UI
- [ ] Send research query
- [ ] Verify workflow execution
- [ ] Check response in UI
- [ ] Test session persistence
- [ ] Test multiple sessions
- [ ] Test configuration save/load

### Phase 2.4: Error Handling
- [ ] Backend offline scenario
- [ ] Invalid session ID
- [ ] Network timeout
- [ ] API rate limiting
- [ ] Large file upload
- [ ] Concurrent requests

---

## 📊 Success Criteria

### Must Have ✅
1. Create chat session from UI
2. Send message and receive response
3. View chat history
4. Delete sessions
5. Session persistence across page refreshes
6. Basic error handling

### Should Have 🎯
1. File upload working
2. Configuration persistence
3. Multiple concurrent sessions
4. Loading states
5. Error messages to user
6. Offline mode graceful degradation

### Nice to Have 🌟
1. Real-time streaming responses
2. Progress indicators during research
3. Export chat to file
4. Share session links
5. Auto-save drafts

---

## 🚧 Development Approach

### Option A: Full Backend Implementation (Recommended)
**Pros:**
- Complete feature set
- Proper persistence
- Production-ready

**Cons:**
- More time needed
- Requires backend changes

**Timeline:** 2-3 hours

### Option B: Mock Backend First
**Pros:**
- Quick UI testing
- No backend changes
- Can develop in parallel

**Cons:**
- Need to swap later
- Not production ready

**Timeline:** 30 minutes

### Option C: Hybrid Approach
**Pros:**
- Partial functionality quickly
- Incremental rollout
- Test each piece

**Cons:**
- More complexity
- Mixed mock/real data

**Timeline:** 1-2 hours per endpoint

---

## 📝 Next Steps

### Immediate (Do Now)
1. **Create ChatController** skeleton
2. **Implement CreateSession** endpoint
3. **Test from UI** - verify session creation works
4. **Implement SendMessage** endpoint
5. **Connect to existing workflow**
6. **Test end-to-end** chat flow

### Short Term (This Session)
1. Complete all chat endpoints
2. Add configuration endpoints
3. Implement file upload
4. Test with real research queries

### Medium Term (Next Session)
1. Add session persistence to database
2. Implement real-time streaming
3. Add progress indicators
4. Enhance error handling
5. Performance optimization

---

## 🔧 Development Tools

### Backend Testing
- Swagger UI: `http://localhost:5000/swagger`
- Postman collection (to be created)
- curl commands (documented below)

### Frontend Testing
- Browser DevTools Network tab
- React DevTools
- Console logs with debug mode

### End-to-End Testing
- Playwright (future)
- Manual testing checklist
- Integration test suite

---

## 📦 Deliverables

### Code
- [ ] ChatController.cs
- [ ] ConfigurationController.cs
- [ ] ChatDtos.cs
- [ ] ChatSessionService.cs
- [ ] ChatIntegrationService.cs
- [ ] Updated Program.cs
- [ ] Frontend .env file
- [ ] Updated API service (if needed)

### Documentation
- [ ] API endpoint documentation
- [ ] Integration guide
- [ ] Testing guide
- [ ] Error handling guide
- [ ] Deployment notes

### Testing
- [ ] Unit tests for services
- [ ] Integration tests for controllers
- [ ] E2E test scenarios
- [ ] Performance benchmarks

---

## 🎬 Let's Begin!

Ready to start? Let's create the **ChatController** first!
