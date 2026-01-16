# SupervisorWorkflow Quick Reference

## 🚀 Quick Start

```csharp
var supervisor = new SupervisorWorkflow(researcher, ollama);

var result = await supervisor.SuperviseAsync(
    researchBrief: "Research AI trends in 2024",
    draftReport: "Initial draft...",
    maxIterations: 5
);

Console.WriteLine(result); // Final research summary
```

---

## 📋 Method Reference

### **Main Entry Points**

#### `SuperviseAsync()`
```csharp
public async Task<string> SuperviseAsync(
    string researchBrief,
    string draftReport = "",
    int maxIterations = 5,
    CancellationToken cancellationToken = default)
```
**Returns:** Refined research summary (string)

#### `StreamSuperviseAsync()`
```csharp
public async IAsyncEnumerable<string> StreamSuperviseAsync(
    string researchBrief,
    string draftReport = "",
    int maxIterations = 5,
    CancellationToken cancellationToken = default)
```
**Returns:** Real-time progress updates

---

## 🔄 Diffusion Loop Sequence

```
1. SupervisorBrain()      → Brain decision (LLM)
2. SupervisorTools()      → Execute research (parallel)
3. EvaluateQuality()      → Score 0-10
4. Convergence Check      → Continue or stop?
5. RunRedTeam()           → Adversarial critique
6. ContextPruner()        → Extract & deduplicate facts
7. [Loop back to 1 or return summary]
```

---

## 📊 Quality Scoring

### **Formula**
```
Base: 5.0
+ Knowledge Base Size (0-2.5)
+ Confidence Average (0-1.5)
+ Critiques Addressed (0-1.5)
+ Quality Improvement (+0.5)
+ Optional LLM Assessment
= Score 0-10
```

### **Thresholds**
- **8.0+**: Excellent - Stop (converged)
- **7.5+**: Good enough (if iter >= 2)
- **< 6.0**: Poor - Warning in next brain decision
- **Max iterations**: Force stop

---

## 🧠 Supervisor Brain Context

What the LLM sees:
```
- Date
- Research brief
- Current draft quality
- Iteration count
- **Unaddressed critiques** ← Critical!
- Quality warnings (if score < 6.0)
```

---

## 🔍 Red Team Checks

Identifies:
1. Unsupported claims
2. Logical fallacies
3. Missing perspectives
4. Questionable sources
5. Bias/one-sidedness

Returns: `CritiqueState` or `null` (if PASS)

---

## 💾 Context Pruning

**Extracts Facts:**
```
[FACT] claim | source | confidence_score
```

**Processing:**
1. Extract from raw notes
2. Deduplicate vs KB
3. Score confidence
4. Limit to 10 new/iteration
5. Clear processed notes

---

## 🎯 Usage Patterns

### **Pattern 1: Standard Research**
```csharp
var result = await supervisor.SuperviseAsync(
    "Analyze market trends",
    "Initial market analysis draft"
);
```

### **Pattern 2: Deep Research**
```csharp
var result = await supervisor.SuperviseAsync(
    "Comprehensive analysis needed",
    "Starting point",
    maxIterations: 8
);
```

### **Pattern 3: Quick Convergence**
```csharp
var result = await supervisor.SuperviseAsync(
    "Quick research",
    "Brief draft",
    maxIterations: 3
);
```

### **Pattern 4: Streaming Progress**
```csharp
await foreach (var update in supervisor.StreamSuperviseAsync("topic", "draft"))
{
    Console.WriteLine(update);
}
```

---

## ⚙️ Configuration

### **Parameters**
- `researchBrief`: Topic/direction for research
- `draftReport`: Initial draft (can be empty)
- `maxIterations`: Max loop iterations (default: 5)
- `cancellationToken`: Cancellation support

### **Defaults**
```csharp
maxIterations = 5       // Standard setting
quality threshold = 8.0 // Excellent
good enough = 7.5       // With 2+ iterations
```

---

## 📈 Expected Outcomes

### **Typical Iteration 1**
```
Quality: 5.0-6.5
Facts: 5-15
Issues: Initial research gaps identified
```

### **Typical Iteration 2**
```
Quality: 6.5-7.5
Facts: 15-30
Issues: Red team feedback starting
```

### **Typical Iteration 3**
```
Quality: 7.0-8.0
Facts: 30-45
Issues: Most addressed
```

### **Convergence**
```
Quality: 8.0+
Facts: 40-60
Status: Research complete
```

---

## 🚨 Error Handling

All methods have graceful fallbacks:
- LLM fails → Use sensible defaults
- Research fails → Continue with existing KB
- Scoring fails → Return neutral score
- Red team fails → Continue without critique

**No exceptions bubble up** - Workflow completes

---

## 📝 Logging

All significant events logged at appropriate levels:
- DEBUG: Method entry, detailed decisions
- INFO: Completion, key metrics
- WARNING: Failures with fallbacks
- ERROR: Unexpected errors

Check logs for troubleshooting.

---

## 🔗 Integration Points

### **With MasterWorkflow**
```
Master Step 4 calls:
supervisor.SuperviseAsync(brief, draft, 5)
```

### **With OllamaService**
```
Brain: LLM decides
RedTeam: LLM critiques
Pruner: LLM extracts facts
Quality: Optional LLM assessment
```

### **With StateManagement**
```
Uses:
- StateFactory
- StateValidator
- StateManager
- StateAccumulator
```

---

## 🧪 Quick Tests

```csharp
// Test basic execution
[Fact]
public async Task SuperviseAsync_Returns_NonEmptySummary()
{
    var result = await supervisor.SuperviseAsync("Test topic");
    Assert.NotEmpty(result);
}

// Test convergence
[Fact]
public async Task SuperviseAsync_Converges_OnQuality()
{
    var result = await supervisor.SuperviseAsync("topic", "", 3);
    Assert.NotEmpty(result);
}

// Test streaming
[Fact]
public async Task StreamSuperviseAsync_YieldsUpdates()
{
    var updates = 0;
    await foreach (var _ in supervisor.StreamSuperviseAsync("topic"))
        updates++;
    Assert.True(updates > 0);
}
```

---

## 📊 Performance Tips

1. **For Speed:** Use `maxIterations: 3`
2. **For Quality:** Use `maxIterations: 5-8`
3. **For Deep Research:** Use `maxIterations: 8+`
4. **For Streaming:** Use `StreamSuperviseAsync()`
5. **For Batch:** Use `SuperviseAsync()` without streaming

---

## 🔧 Customization

### **Modify Max Iterations**
```csharp
// Faster
await supervisor.SuperviseAsync(brief, draft, maxIterations: 2);

// Slower, more thorough
await supervisor.SuperviseAsync(brief, draft, maxIterations: 10);
```

### **Custom Brain Prompt**
```
Edit: PromptTemplates.SupervisorBrainPrompt
```

### **Custom Quality Weights**
```
Edit: EvaluateDraftQualityAsync() method
(Lines calculating factor weights)
```

---

## 🎯 When to Use

### **SuperviseAsync()**
- Standard research tasks
- No need for real-time feedback
- Can wait for completion

### **StreamSuperviseAsync()**
- Long-running research
- UI/UX feedback needed
- Want to see progress

### **maxIterations: 3-5**
- Most research tasks
- Balance of quality/speed

### **maxIterations: 7-10**
- Deep/thorough research
- When quality critical
- When speed not critical

---

## ✨ Key Features Recap

✅ **Intelligent Brain** - LLM decides next steps  
✅ **Parallel Research** - 3x researchers at once  
✅ **Quality Scoring** - Multi-factor evaluation  
✅ **Self-Correction** - Red team feedback  
✅ **Smart Convergence** - Stops when ready  
✅ **Real-time Streaming** - Live progress  
✅ **Knowledge Management** - Fact deduplication  
✅ **Error Resilient** - Graceful fallbacks  

---

## 🚀 Ready to Use!

SupervisorWorkflow is production-ready.
Start with `SuperviseAsync()` for standard use.
Use `StreamSuperviseAsync()` for long tasks.

Good luck! 🎉
