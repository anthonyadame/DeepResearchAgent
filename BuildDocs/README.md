# Build Documentation - Deep Research Agent

This directory contains implementation details, architectural decisions, and testing guides for the Deep Research Agent system.

## 📄 Documents

### 🚀 Quick Start
- **[STREAMING_QUICK_REFERENCE.md](STREAMING_QUICK_REFERENCE.md)** - One-page cheat sheet for the streaming API
  - Copy-paste curl commands
  - Common troubleshooting
  - Quick code snippets

### 🧪 Testing & Integration
- **[END_TO_END_TESTING.md](END_TO_END_TESTING.md)** - Complete end-to-end testing guide
  - 6-phase testing strategy (health checks → E2E)
  - Browser test client (HTML)
  - CLI test examples
  - Integration test patterns
  - Performance testing
  - Detailed troubleshooting
  - **Duration:** ~30 minutes start-to-finish

### 🛠️ Implementation Details
- **[STREAMING_IMPLEMENTATION.md](STREAMING_IMPLEMENTATION.md)** - Technical implementation summary
  - What was built (API, client, helpers, tests)
  - Data flow diagrams
  - Performance characteristics
  - File manifest
  - Success metrics
  - Enhancement ideas

## 🎯 Quick Navigation

### I want to test the API
→ See [STREAMING_QUICK_REFERENCE.md](STREAMING_QUICK_REFERENCE.md) for curl commands

### I want a complete testing setup
→ Follow [END_TO_END_TESTING.md](END_TO_END_TESTING.md) phases 1-6

### I want to integrate into my UI
→ Use `MasterWorkflowStreamClient` (see [STREAMING_IMPLEMENTATION.md](STREAMING_IMPLEMENTATION.md))

### I want to understand the architecture
→ Start with [STREAMING_IMPLEMENTATION.md](STREAMING_IMPLEMENTATION.md), then [END_TO_END_TESTING.md](END_TO_END_TESTING.md)

## 📋 What Was Delivered

### 1. Streaming API Endpoint ✅
```
POST /api/workflows/master/stream
→ Returns: Server-Sent Events (StreamState objects)
```

### 2. Helper Services ✅
- `StreamStateFormatter` - Console display helpers
- `MasterWorkflowStreamClient` - Typed .NET client
- `WriteStreamStateField(s)` - UI formatting functions

### 3. Comprehensive Tests ✅
- 9 E2E test scenarios
- Browser test client (HTML)
- CLI test script
- Curl examples

### 4. Documentation ✅
- Complete testing guide (6 phases)
- Implementation details
- Quick reference
- Architecture diagrams
- Success criteria

## 🚦 Getting Started (5 minutes)

```bash
# 1. Start services
docker-compose up -d

# 2. Run API
cd DeepResearchAgent.Api
dotnet run

# 3. In another terminal, test the endpoint
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery": "What is machine learning?"}'

# 4. Run the test suite
dotnet test DeepResearchAgent.Api.Tests -k StreamingEndpointE2ETests
```

Expected output: Real-time progress updates from research pipeline!

## 📊 Data Flow

```
User Query
    ↓
POST /api/workflows/master/stream
    ↓
MasterWorkflow.StreamStateAsync() (5 phases)
    ├→ Clarify Query
    ├→ Write Brief
    ├→ Draft Report
    ├→ Supervisor Loop
    └→ Final Report
    ↓
StreamState Objects (via SSE)
    ↓
Client (Browser / CLI / App)
    ↓
StreamStateFormatter (display) or
MasterWorkflowStreamClient (consume)
```

## 🎓 Learning Path

**Beginner:** Read [STREAMING_QUICK_REFERENCE.md](STREAMING_QUICK_REFERENCE.md)
→ Run a curl test
→ View the response

**Intermediate:** Follow [END_TO_END_TESTING.md](END_TO_END_TESTING.md) phases 1-3
→ Run health checks
→ Run E2E tests
→ View detailed output

**Advanced:** Study [STREAMING_IMPLEMENTATION.md](STREAMING_IMPLEMENTATION.md)
→ Review the code
→ Integrate into UI
→ Deploy to production

## 🔍 Key Files by Purpose

| Purpose | File | Location |
|---------|------|----------|
| API Endpoint | `WorkflowsController.cs` | `DeepResearchAgent.Api/Controllers/` |
| Client Library | `MasterWorkflowStreamClient.cs` | `DeepResearchAgent.Api/Clients/` |
| Formatters | `StreamStateFormatter.cs` | `DeepResearchAgent/Services/` |
| Tests | `StreamingEndpointE2ETests.cs` | `DeepResearchAgent.Api.Tests/` |
| Model | `StreamState.cs` | `DeepResearchAgent/Models/` |

## ⏱️ Timing Expectations

| Phase | Duration | What You're Testing |
|-------|----------|-------------------|
| Health Checks | 1-2 min | Services accessible |
| Unit Tests | 2-3 min | Core logic |
| Integration Test (curl) | 1-2 min | Endpoint response |
| E2E Test (full pipeline) | 1-2 min | Complete workflow |
| **Total** | **~10 minutes** | Full stack |

## ✅ Success Indicators

When you run the E2E tests, you'll see:
```
✓ StreamEndpoint_ReturnsCorrectContentType
✓ StreamEndpoint_CompletesPipeline
✓ StreamEndpoint_HandlesClarificationNeeded
✓ StreamEndpoint_ProgressiveStateBuilding
✓ StreamEndpoint_CallbackPattern
✓ StreamEndpoint_HandlesPartialFailure
✓ StreamEndpoint_RespectsCancellation
✓ StreamEndpoint_PropagatesResearchId

9 passed, 0 failed
```

## 🐛 Troubleshooting

**API not starting?**
→ See [END_TO_END_TESTING.md](END_TO_END_TESTING.md) Phase 1

**Stream returns empty?**
→ See [END_TO_END_TESTING.md](END_TO_END_TESTING.md) Troubleshooting section

**Tests fail?**
→ Check that all services are running: `docker ps`

**Can't parse SSE?**
→ Review browser test client in [END_TO_END_TESTING.md](END_TO_END_TESTING.md) Phase 4A

## 📚 Additional Resources

- [StreamState Model](../DeepResearchAgent/Models/StreamState.cs)
- [MasterWorkflow](../DeepResearchAgent/Workflows/MasterWorkflow.cs)
- [WorkflowsController](../DeepResearchAgent.Api/Controllers/WorkflowsController.cs)
- [Program.cs (CLI consumer example)](../DeepResearchAgent/Program.cs) - see `RunWorkflowOrchestration()` method

## 🎯 Common Use Cases

### "How do I display progress in my UI?"
→ Use `StreamStateFormatter.GetProgressSummary(state)` or `GetPhaseContent(state)`

### "How do I consume the stream in my app?"
→ Use `MasterWorkflowStreamClient.StreamMasterWorkflowAsync()` with callback

### "How do I test this works?"
→ Run `dotnet test -k StreamingEndpointE2ETests`

### "How do I integrate with my web app?"
→ See HTML example in [END_TO_END_TESTING.md](END_TO_END_TESTING.md) Phase 4A

### "What if the stream times out?"
→ Increase timeout in your HTTP client: `httpClient.Timeout = TimeSpan.FromMinutes(10);`

## 📞 Support

For specific questions:
1. Check the relevant MD file (quick ref → full guide → implementation)
2. Search for the term in the code files
3. Run the E2E tests to see working examples
4. Check troubleshooting sections

---

**Last Updated:** January 2025
**Status:** ✅ Complete and tested
