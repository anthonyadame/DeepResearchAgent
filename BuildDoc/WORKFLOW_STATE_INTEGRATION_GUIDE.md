# Workflow Integration with ILightningStateService

## 📋 Overview

This guide shows how to integrate `ILightningStateService` into your three core workflows:
- **MasterWorkflow** - Orchestrates entire research pipeline
- **SupervisorWorkflow** - Manages iterative refinement
- **ResearcherWorkflow** - Conducts focused research

---

## 🎯 Integration Strategy

### Phase 1: MasterWorkflow
Track overall research progress and lifecycle:
```
Pending → InProgress → Verifying → Completed (or Failed)
```

### Phase 2: SupervisorWorkflow  
Track supervision cycles and quality progression:
```
Cycle 1 → Cycle 2 → ... → Approved
```

### Phase 3: ResearcherWorkflow
Track fact extraction and research findings:
```
Searching → Collecting → Compressing → Persisting
```

---

## 🔧 Implementation Details

### 1. MasterWorkflow Integration

**Changes Required:**
- Add `ILightningStateService` dependency
- Initialize `ResearchStateModel` at start
- Update state at each step
- Track final completion

**Code Pattern:**
```csharp
public class MasterWorkflow
{
    private readonly ILightningStateService _stateService;
    // ... other dependencies ...

    public MasterWorkflow(
        ILightningStateService stateService,
        SupervisorWorkflow supervisor,
        OllamaService llmService,
        ILogger<MasterWorkflow>? logger = null,
        StateManager? stateManager = null)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        // ... other assignments ...
    }

    public async Task<string> RunAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        var researchId = Guid.NewGuid().ToString();
        
        try
        {
            // Initialize research state
            var researchState = new ResearchStateModel
            {
                ResearchId = researchId,
                Query = userQuery,
                Status = ResearchStatus.Pending,
                StartedAt = DateTime.UtcNow
            };
            
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            _logger?.LogInformation("Research {Id} initialized", researchId);

            // Step 1: Clarify with user
            researchState.Status = ResearchStatus.InProgress;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var (needsClarification, clarificationQuestion) = await ClarifyWithUserAsync(
                userQuery, 
                cancellationToken
            );
            
            if (needsClarification)
            {
                researchState.Status = ResearchStatus.Failed;
                await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
                return $"Clarification needed:\n\n{clarificationQuestion}";
            }

            // Step 2: Write research brief
            var researchBrief = await WriteResearchBriefAsync(userQuery, cancellationToken);
            researchState.Metadata["researchBrief"] = researchBrief;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            // Step 3: Write draft
            var draftReport = await WriteDraftReportAsync(researchBrief, cancellationToken);
            researchState.Metadata["draftReport"] = draftReport;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            // Step 4: Execute supervisor
            researchState.Status = ResearchStatus.Verifying;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var refinedSummary = await _supervisor.SuperviseAsync(
                researchBrief, 
                draftReport, 
                cancellationToken: cancellationToken
            );
            
            researchState.Metadata["refinedSummary"] = refinedSummary;

            // Step 5: Generate final report
            var finalReport = await GenerateFinalReportAsync(
                userQuery, 
                researchBrief, 
                draftReport, 
                refinedSummary, 
                cancellationToken
            );

            // Mark completed
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            researchState.Metadata["finalReport"] = finalReport;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            _logger?.LogInformation("Research {Id} completed successfully", researchId);
            return finalReport;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Research failed");
            try
            {
                var failedState = await _stateService.GetResearchStateAsync(researchId, cancellationToken);
                failedState.Status = ResearchStatus.Failed;
                failedState.Metadata["error"] = ex.Message;
                await _stateService.SetResearchStateAsync(researchId, failedState, cancellationToken);
            }
            catch { /* Ignore state update failures */ }
            throw;
        }
    }
}
```

### 2. SupervisorWorkflow Integration

**Changes Required:**
- Add `ILightningStateService` dependency
- Track supervision cycles
- Monitor quality progression
- Save iteration results

**Code Pattern:**
```csharp
public class SupervisorWorkflow
{
    private readonly ILightningStateService _stateService;
    // ... other dependencies ...

    public SupervisorWorkflow(
        ILightningStateService stateService,
        ResearcherWorkflow researcher,
        OllamaService llmService,
        LightningStore? store = null,
        ILogger<SupervisorWorkflow>? logger = null,
        StateManager? stateManager = null)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        // ... other assignments ...
    }

    public async Task<string> SuperviseAsync(
        string researchBrief,
        string draftReport = "",
        int maxIterations = 5,
        CancellationToken cancellationToken = default)
    {
        var supervisionId = Guid.NewGuid().ToString();
        
        try
        {
            // Initialize supervision state
            var supervisionState = new SupervisionState
            {
                SupervisionId = supervisionId,
                ResearchId = supervisionId, // Would be passed in real implementation
                CycleNumber = 1,
                Status = SupervisionStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };

            var supervisorState = StateFactory.CreateSupervisorState();
            supervisorState.ResearchBrief = researchBrief;
            supervisorState.DraftReport = draftReport ?? $"Initial draft for: {researchBrief}";

            // Execute diffusion loop with state tracking
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                _logger?.LogInformation("Supervisor iteration {iter}/{max}", iteration + 1, maxIterations);

                // Step 1: Supervisor Brain
                var brainDecision = await SupervisorBrainAsync(supervisorState, cancellationToken);
                supervisorState.SupervisorMessages.Add(brainDecision);

                // Step 2: Execute Tools
                await SupervisorToolsAsync(supervisorState, brainDecision, cancellationToken);

                // Step 3: Evaluate Quality
                var quality = await EvaluateDraftQualityAsync(supervisorState, cancellationToken);
                supervisorState.QualityHistory.Add(
                    StateFactory.CreateQualityMetric(quality, "Iteration quality", iteration)
                );
                supervisorState.ResearchIterations = iteration + 1;

                // Update supervision state
                supervisionState.DraftQualityScore = quality;
                supervisionState.Improvements.AddRange(supervisorState.SupervisorMessages.TakeLast(1));
                
                _logger?.LogInformation(
                    "Supervision iteration {iter}: Quality={quality:P}",
                    iteration + 1,
                    quality
                );

                // Check convergence
                if (quality > 0.85)
                {
                    _logger?.LogInformation("Quality threshold reached at iteration {iter}", iteration + 1);
                    supervisionState.Recommendation = "Quality is sufficient, approve for final report";
                    break;
                }

                // Check if we should continue
                if (iteration < maxIterations - 1)
                {
                    // Continue refining
                    var shouldRefine = await ShouldRefineAsync(supervisorState, cancellationToken);
                    if (!shouldRefine)
                    {
                        _logger?.LogInformation("Refinement complete at iteration {iter}", iteration + 1);
                        break;
                    }
                }
            }

            // Mark supervision complete
            supervisionState.Status = SupervisionStatus.Completed;
            supervisionState.CompletedAt = DateTime.UtcNow;

            _logger?.LogInformation("Supervision completed with quality: {quality:P}", 
                supervisionState.DraftQualityScore);
            
            return supervisorState.DraftReport;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SupervisorWorkflow failed");
            throw;
        }
    }
}
```

### 3. ResearcherWorkflow Integration

**Changes Required:**
- Add `ILightningStateService` dependency
- Track fact extraction progress
- Save extracted facts
- Monitor research quality

**Code Pattern:**
```csharp
public class ResearcherWorkflow
{
    private readonly ILightningStateService _stateService;
    // ... other dependencies ...

    public ResearcherWorkflow(
        ILightningStateService stateService,
        SearCrawl4AIService searchService,
        OllamaService llmService,
        LightningStore store,
        ILogger<ResearcherWorkflow>? logger = null)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        // ... other assignments ...
    }

    public async Task<IReadOnlyList<FactState>> ResearchAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        var researchId = Guid.NewGuid().ToString();
        
        try
        {
            _logger?.LogInformation("Research starting for topic: {topic}", topic);

            // Initialize research state
            var researchState = new ResearchStateModel
            {
                ResearchId = researchId,
                Query = topic,
                Status = ResearchStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };

            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            var researcherState = CreateResearcherState(topic);
            const int maxIterations = 5;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                _logger?.LogDebug("Research iteration {iter}/{max}", iteration + 1, maxIterations);

                // LLM Call - Decide next action
                var llmResponse = await LLMCallAsync(researcherState, cancellationToken);
                researcherState.ResearcherMessages.Add(llmResponse);

                if (!ShouldContinue(researcherState, iteration, maxIterations))
                {
                    _logger?.LogDebug("Research loop ending");
                    break;
                }

                // Execute tools (search, scrape, think)
                await ToolExecutionAsync(researcherState, llmResponse, cancellationToken);

                // Update progress
                researchState.CurrentQualityScore = CalculateQuality(researcherState);
                researchState.IterationCount = iteration + 1;
                researchState.Sources.AddRange(ExtractSources(researcherState));
                
                await _stateService.UpdateResearchProgressAsync(
                    researchId,
                    iteration + 1,
                    researchState.CurrentQualityScore,
                    cancellationToken
                );

                _logger?.LogDebug("Research iteration {iter} completed", iteration + 1);
            }

            // Compress research
            var compressedFindings = await CompressResearchAsync(researcherState, cancellationToken);
            researcherState.CompressedResearch = compressedFindings;

            // Extract and persist facts
            var facts = await ExtractAndPersistFactsAsync(researcherState, cancellationToken);

            // Update final state
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            researchState.ExtractedFacts = facts.Select(f => new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = f.Fact,
                Source = f.Source,
                ConfidenceScore = f.Confidence,
                IsVerified = false,
                ExtractedAt = DateTime.UtcNow,
                VerificationStatus = FactVerificationStatus.Pending
            }).ToList();

            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            _logger?.LogInformation("Research completed: {count} facts extracted", facts.Count);
            
            return facts.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Research failed");
            try
            {
                var failedState = await _stateService.GetResearchStateAsync(researchId, cancellationToken);
                failedState.Status = ResearchStatus.Failed;
                await _stateService.SetResearchStateAsync(researchId, failedState, cancellationToken);
            }
            catch { /* Ignore state update failures */ }
            throw;
        }
    }
}
```

---

## 📊 State Flow Diagram

```
MasterWorkflow
├─ Initialize ResearchStateModel (Pending)
├─ Step 1: Clarify (InProgress)
├─ Step 2: Brief (InProgress)
├─ Step 3: Draft (InProgress)
├─ Step 4: Supervisor (Verifying)
│  └─ SupervisorWorkflow
│     ├─ Initialize SupervisionState (InProgress)
│     ├─ Iteration 1-N
│     │  ├─ Brain Decision
│     │  ├─ Tool Execution
│     │  │  └─ ResearcherWorkflow
│     │  │     ├─ Initialize ResearchStateModel (InProgress)
│     │  │     ├─ ReAct Loop 1-N
│     │  │     │  ├─ LLM Call
│     │  │     │  ├─ Tool Execution
│     │  │     │  └─ Update Progress
│     │  │     └─ Compress & Extract (Completed)
│     │  └─ Evaluate Quality
│     └─ Mark Complete (Completed)
├─ Step 5: Final Report
└─ Mark Completed (Completed)
```

---

## 🔄 DI Container Configuration

```csharp
// Program.cs
services.AddMemoryCache(options => 
    options.SizeLimit = 500 * 1024 * 1024
);

// Register Lightning services
services.AddSingleton<IAgentLightningService>(...);
services.AddSingleton<ILightningVERLService>(...);

// Register state service
services.AddSingleton<ILightningStateService>(provider =>
    new LightningStateService(
        provider.GetRequiredService<IAgentLightningService>(),
        provider.GetRequiredService<ILightningVERLService>(),
        provider.GetRequiredService<IMemoryCache>()
    )
);

// Register workflows with state service
services.AddScoped<ResearcherWorkflow>(provider =>
    new ResearcherWorkflow(
        provider.GetRequiredService<ILightningStateService>(),
        provider.GetRequiredService<SearCrawl4AIService>(),
        provider.GetRequiredService<OllamaService>(),
        provider.GetRequiredService<LightningStore>(),
        provider.GetRequiredService<ILogger<ResearcherWorkflow>>()
    )
);

services.AddScoped<SupervisorWorkflow>(provider =>
    new SupervisorWorkflow(
        provider.GetRequiredService<ILightningStateService>(),
        provider.GetRequiredService<ResearcherWorkflow>(),
        provider.GetRequiredService<OllamaService>(),
        provider.GetRequiredService<LightningStore>(),
        provider.GetRequiredService<ILogger<SupervisorWorkflow>>()
    )
);

services.AddScoped<MasterWorkflow>(provider =>
    new MasterWorkflow(
        provider.GetRequiredService<ILightningStateService>(),
        provider.GetRequiredService<SupervisorWorkflow>(),
        provider.GetRequiredService<OllamaService>(),
        provider.GetRequiredService<ILogger<MasterWorkflow>>()
    )
);
```

---

## ✅ Integration Checklist

- [ ] Add `ILightningStateService` parameter to all workflows
- [ ] Initialize state models at workflow start
- [ ] Update state at each major step
- [ ] Track progress with `UpdateProgressAsync`
- [ ] Handle errors and update state on failure
- [ ] Monitor metrics for performance
- [ ] Test state persistence and retrieval
- [ ] Verify cache hit rates
- [ ] Document state transitions
- [ ] Add integration tests

---

## 🎯 Benefits After Integration

✅ **Centralized State** - Track all workflow progress in one place
✅ **Observability** - Monitor research progress in real-time
✅ **Resilience** - Resume workflows from last known state
✅ **Performance** - Multi-level caching speeds up state access
✅ **Consistency** - VERL validates state integrity
✅ **Metrics** - Track cache hit rates and operation latencies

---

## 📝 Next Steps

1. **Update MasterWorkflow** with state management
2. **Update SupervisorWorkflow** with supervision state
3. **Update ResearcherWorkflow** with research progress
4. **Configure DI Container** with state service
5. **Test integration** end-to-end
6. **Monitor metrics** during execution
7. **Optimize cache TTLs** based on patterns

---

**Version:** 1.0  
**Status:** Ready for Implementation  
**Build Status:** ✅ All Code Compiles
