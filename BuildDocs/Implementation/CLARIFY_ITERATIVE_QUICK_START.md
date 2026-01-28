# 🚀 ClarifyIterativeAgent Quick Start Guide

## 🎯 What Is This?

A **PromptWizard-inspired iterative clarification agent** that uses feedback-driven refinement to ensure research requests meet quality thresholds.

## ⚡ Quick Start (3 Steps)

### 1. **Configure** (appsettings.json)

```json
{
  "IterativeClarification": {
    "UseIterativeAgent": true,
    "MaxIterations": 3,
    "QualityThreshold": 75.0
  }
}
```

### 2. **Create Agent**

```csharp
var config = new IterativeClarificationConfig();
configuration.GetSection("IterativeClarification").Bind(config);

var agent = new ClarifyIterativeAgent(
    ollamaService,
    config,
    logger,
    metrics);
```

### 3. **Execute**

```csharp
var result = await agent.ClarifyWithIterationsAsync(
    conversationHistory,
    cancellationToken);

Console.WriteLine($"Quality: {result.QualityMetrics?.OverallScore:F1}/100");
Console.WriteLine($"Iterations: {result.IterationCount}");
Console.WriteLine($"Question: {result.Question}");
```

## 📊 Compare Agents (A/B Testing)

```csharp
var comparison = new ClarificationComparisonService(
    standardAgent,
    iterativeAgent,
    logger);

var result = await comparison.CompareAgentsAsync(
    conversationHistory,
    cancellationToken);

Console.WriteLine(result.Summary);
// Output: Standard: 450ms | Iterative: 1350ms (3 iterations) | Quality: 82.5/100 | Overhead: 3.0x
```

## 🔧 Configuration Parameters

| Parameter | Default | When to Change |
|-----------|---------|----------------|
| `MaxIterations` | 3 | Increase to 5 for higher quality, reduce to 2 for speed |
| `QualityThreshold` | 75.0 | Raise to 85 for stricter validation, lower to 65 for lenient |
| `EnableCritique` | true | Disable for speed (skips feedback loop) |
| `EnableQualityMetrics` | true | Disable for minimal overhead |
| `MaxTimeoutSeconds` | 60 | Increase if hitting timeout |
| `UseIterativeAgent` | false | Set true to use by default |

## 🎓 Understanding Quality Scores

### Clarity Score (0-100)
```
90-100: Crystal clear ✅
70-89:  Mostly clear 👍
50-69:  Some ambiguity ⚠️
0-49:   Unclear ❌
```

### Completeness Score (0-100)
```
Checks for:
- Scope (what to research)
- Depth (how comprehensive)
- Format (how to present)
- Time constraints
- Domain context
```

### Actionability Score (0-100)
```
90-100: Start immediately ✅
70-89:  Minor assumptions 👍
50-69:  Educated guesses ⚠️
0-49:   Too many unknowns ❌
```

### Overall Score
```
Average of 3 scores
Default threshold: 75.0
```

## 🔄 How It Works

```
┌─────────────────────────────────────┐
│  User Message: "I want to learn AI" │
└────────────┬────────────────────────┘
             │
             ▼
    ┌────────────────────┐
    │  Iteration 1       │
    │  Generate Question │ ── Quality: 60/100 ❌
    └────────┬───────────┘
             │
             ▼
    ┌────────────────────┐
    │  Critique          │ ── Weaknesses: Too vague
    └────────┬───────────┘
             │
             ▼
    ┌────────────────────┐
    │  Refine            │ ── New question
    └────────┬───────────┘
             │
             ▼
    ┌────────────────────┐
    │  Iteration 2       │ ── Quality: 75/100 ✅
    └────────┬───────────┘
             │
             ▼
    ┌────────────────────┐
    │  Threshold Met!    │
    │  Return Result     │
    └────────────────────┘
```

## 📈 Performance Expectations

| Metric | Standard Agent | Iterative Agent |
|--------|---------------|-----------------|
| **Latency** | ~500ms | ~1500ms (3x) |
| **API Calls** | 1 | 3-5 |
| **Quality Score** | N/A | 75-95/100 |
| **Iterations** | 0 | 1-5 (adaptive) |

## 🎯 When to Use Each Agent

### Use Standard Agent ✅
- Real-time chat (latency critical)
- Users provide detailed requests
- Cost optimization priority
- Simple yes/no clarification

### Use Iterative Agent ✅
- Research quality critical
- Users often vague
- Budget allows 3x latency
- Need quality metrics

## 🧪 Run the Demo

```csharp
// Add to your Program.cs
await ClarificationAgentDemo.RunDemoAsync();
```

**Demo Tests**:
1. Vague request: "I want to learn about AI"
2. Moderate: "Research LLMs for code generation"
3. Detailed: Full parameters specified
4. Ambiguous: Multiple topics
5. Multi-turn: Context building

## 🐛 Troubleshooting

### High Latency?
```json
{
  "MaxIterations": 2,
  "EnableQualityMetrics": false
}
```

### Low Quality Scores?
```json
{
  "QualityThreshold": 70.0
}
```

### Hitting Timeouts?
```json
{
  "MaxTimeoutSeconds": 90
}
```

### Not Improving?
- Check iteration history
- Review critique feedback
- Adjust prompt templates

## 📚 Full Documentation

- **Comprehensive Guide**: `BuildDocs/CLARIFY_ITERATIVE_AGENT_README.md`
- **Implementation Summary**: `BuildDocs/CLARIFY_ITERATIVE_IMPLEMENTATION_SUMMARY.md`
- **PromptWizard Paper**: https://arxiv.org/abs/2405.18369

## 🎉 You're Ready!

Start with the demo, compare agents, then integrate into your workflow!
