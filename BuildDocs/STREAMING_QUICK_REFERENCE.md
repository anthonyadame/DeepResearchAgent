# StreamState API - Quick Reference

## Endpoint

```
POST /api/workflows/master/stream
Content-Type: application/json

{
  "userQuery": "Your research question here"
}
```

**Response:** `text/event-stream` (Server-Sent Events)

## Quick Tests

### Browser Test
```html
<!-- See: BuildDocs/END_TO_END_TESTING.md Phase 4A -->
Open: DeepResearchAgent.UI/test-stream.html
```

### Curl Test
```bash
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery":"What is AI?"}'
```

### .NET Test
```bash
dotnet test DeepResearchAgent.Api.Tests \
  -k StreamingEndpointE2ETests --verbosity normal
```

### Quick Code Test
```csharp
var client = new MasterWorkflowStreamClient(new HttpClient());
await client.DisplayStreamAsync("Your question");
```

## Collect Results
```csharp
var states = await client.CollectStreamAsync("Your question");
var finalReport = states
    .FirstOrDefault(s => !string.IsNullOrEmpty(s.FinalReport))?
    .FinalReport;
```

## Display Results
```csharp
foreach (var state in states)
{
    // Option 1: Show all fields
    StreamStateFormatter.WriteStreamStateFields(state);
    
    // Option 2: Show summary
    var summary = StreamStateFormatter.GetProgressSummary(state);
    Console.WriteLine(summary);
    
    // Option 3: Show main content
    var content = StreamStateFormatter.GetPhaseContent(state);
    Console.WriteLine(content);
}
```

## StreamState Structure
```csharp
public class StreamState
{
    public string Status { get; set; }              // Phase status
    public string ResearchId { get; set; }          // Request ID
    public string UserQuery { get; set; }           // Original query
    public string BriefPreview { get; set; }        // First 150 chars
    public string ResearchBrief { get; set; }       // Full brief
    public string DraftReport { get; set; }         // Initial draft
    public string RefinedSummary { get; set; }      // Refined version
    public string FinalReport { get; set; }         // Final output
    public string SupervisorUpdate { get; set; }    // Live update
    public int SupervisorUpdateCount { get; set; }  // Update counter
}
```

## Typical Flow

```
1. StreamState { Status = "connected" }
   ↓
2. StreamState { Status = "clarifying user intent" }
   ↓
3. StreamState { Status = "completed", ResearchBrief = "..." }
   ↓
4. StreamState { Status = "generating initial draft report" }
   ↓
5. StreamState { DraftReport = "..." }
   ↓
6. [Many] StreamState { SupervisorUpdate = "Refining..." }
   ↓
7. StreamState { Status = "generating final polished report" }
   ↓
8. StreamState { FinalReport = "...", Status = "completed" }
```

## Troubleshooting

| Issue | Check |
|-------|-------|
| Connection refused | Is API running? `dotnet run` |
| Empty responses | Services running? `docker ps` |
| Timeout after 2 min | Network/server issue - check logs |
| JSON parse errors | Invalid userQuery format |
| Cancellation token | Ensure CancellationToken is passed |

## Files

| File | Purpose |
|------|---------|
| `WorkflowsController.cs` | Streaming endpoint |
| `StreamStateFormatter.cs` | UI helpers |
| `MasterWorkflowStreamClient.cs` | .NET client |
| `StreamingEndpointE2ETests.cs` | Test suite |
| `END_TO_END_TESTING.md` | Full guide |
| `STREAMING_IMPLEMENTATION.md` | Implementation details |

## Success Criteria

- [ ] API returns `Content-Type: text/event-stream`
- [ ] Receive initial `Status: connected` message
- [ ] Receive `ResearchBrief` content
- [ ] Receive `DraftReport` content
- [ ] Receive `SupervisorUpdate` messages
- [ ] Receive `FinalReport` content
- [ ] Stream completes without errors
- [ ] All phases complete in < 2 minutes

## Next Steps

1. **Start Services:** `docker-compose up -d`
2. **Run API:** `dotnet run --project DeepResearchAgent.Api`
3. **Test Endpoint:** Use curl/browser test
4. **Run Tests:** `dotnet test`
5. **Integrate UI:** Use `MasterWorkflowStreamClient` in your app

---
**Full Documentation:** See `BuildDocs/` folder for detailed guides
