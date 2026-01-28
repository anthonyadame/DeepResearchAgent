# ClarifyIterativeAgent: PromptWizard-Inspired Clarification

## Overview

The **ClarifyIterativeAgent** implements PromptWizard's feedback-driven self-evolving prompt optimization for the clarification process. This advanced agent uses iterative refinement to ensure research requests meet quality thresholds before proceeding.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  ClarifyIterativeAgent: Iterative Clarification with Critique   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Iteration Loop (max 3-5 iterations)    │
        └─────────────────────────────────────────┘
                              │
        ┌─────────────────────┴─────────────────────┐
        │                                           │
        ▼                                           ▼
┌──────────────────┐                    ┌──────────────────────┐
│  Step 1: GENERATE│                    │  Step 4: EVALUATE    │
│  Clarification   │                    │  Quality Metrics     │
│  Question        │                    │  - Clarity (0-100)   │
│                  │                    │  - Completeness      │
└────────┬─────────┘                    │  - Actionability     │
         │                              └──────────┬───────────┘
         ▼                                         │
┌──────────────────┐                              │
│  Step 2: CRITIQUE│◄─────────────────────────────┘
│  Analyze Focus   │
│  & Coverage      │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Step 3: REFINE  │
│  Based on        │
│  Feedback        │
└────────┬─────────┘
         │
         └──────► Check Threshold ──► Continue or Exit
```

## Key Features

### 🎯 **PromptWizard-Inspired Mechanisms**

1. **Iterative Refinement** (3-5 iterations)
   - Generate → Critique → Refine → Synthesize
   - Continuous improvement through feedback loops

2. **Quality Metrics Tracking**
   - Clarity Score (0-100)
   - Completeness Score (0-100)
   - Actionability Score (0-100)
   - Overall threshold validation (default: 75+)

3. **Critique-Driven Optimization**
   - LLM critiques its own clarification questions
   - Identifies weaknesses and suggests improvements
   - Strengthens specific quality dimensions

4. **Performance Observability**
   - Iteration tracking with snapshots
   - Duration metrics per iteration
   - Quality score progression

## Configuration

### appsettings.json

```json
{
  "IterativeClarification": {
    "MaxIterations": 3,
    "QualityThreshold": 75.0,
    "EnableCritique": true,
    "EnableQualityMetrics": true,
    "MaxTimeoutSeconds": 60,
    "UseIterativeAgent": false,
    "StoreIterationHistory": true,
    "MinCritiqueConfidence": 0.6
  }
}
```

### Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaxIterations` | 3 | Maximum refinement cycles (PromptWizard: 3-5) |
| `QualityThreshold` | 75.0 | Minimum score to proceed (0-100) |
| `EnableCritique` | true | Enable feedback-driven critique |
| `EnableQualityMetrics` | true | Evaluate clarity/completeness/actionability |
| `MaxTimeoutSeconds` | 60 | Timeout for entire clarification process |
| `UseIterativeAgent` | false | Toggle between standard/iterative agent |
| `StoreIterationHistory` | true | Persist iteration snapshots for analysis |
| `MinCritiqueConfidence` | 0.6 | Minimum confidence for critique feedback |

## Usage

### Basic Usage

```csharp
// Setup
var config = new IterativeClarificationConfig
{
    MaxIterations = 3,
    QualityThreshold = 75.0
};

var agent = new ClarifyIterativeAgent(
    ollamaService,
    config,
    logger,
    metrics);

// Execute iterative clarification
var conversationHistory = new List<ChatMessage>
{
    new ChatMessage { Role = "user", Content = "I want to research AI" }
};

var result = await agent.ClarifyWithIterationsAsync(
    conversationHistory,
    cancellationToken);

// Analyze results
Console.WriteLine($"Iterations: {result.IterationCount}");
Console.WriteLine($"Quality Score: {result.QualityMetrics?.OverallScore:F1}/100");
Console.WriteLine($"Duration: {result.TotalDurationMs:F0}ms");
Console.WriteLine($"Question: {result.Question}");
```

### A/B Testing: Standard vs Iterative

```csharp
var comparisonService = new ClarificationComparisonService(
    standardAgent,
    iterativeAgent,
    logger);

var comparison = await comparisonService.CompareAgentsAsync(
    conversationHistory,
    cancellationToken);

Console.WriteLine(comparison.Summary);
// Output: Standard: 450ms | Iterative: 1350ms (3 iterations) | Quality: 82.5/100 | Overhead: 3.0x
```

## Performance Characteristics

### PromptWizard Efficiency

Based on PromptWizard research:
- **69 API calls** vs competitors' 1,730-18,600 (PromptBreeder)
- **~92% cost reduction** compared to baseline methods
- **Works with 5-25 training examples** (minimal data required)

### Expected Performance

| Metric | Standard Agent | Iterative Agent | Notes |
|--------|---------------|-----------------|-------|
| Latency | ~500ms | ~1500ms | 3x overhead for 3 iterations |
| API Calls | 1 | 3-5 | One per iteration |
| Quality Score | N/A | 75-95 | Threshold-driven |
| Iterations | 0 | 1-5 | Adaptive based on quality |

## Quality Metrics Explained

### Clarity Score (0-100)
- **90-100**: Crystal clear, no ambiguity
- **70-89**: Mostly clear, minor ambiguity
- **50-69**: Somewhat clear, notable ambiguity
- **0-49**: Unclear or highly ambiguous

### Completeness Score (0-100)
Evaluates whether all required dimensions are specified:
- Scope (what to research)
- Depth (how comprehensive)
- Format (how to present findings)
- Time constraints (if any)
- Domain context (if needed)

### Actionability Score (0-100)
- **90-100**: Can start immediately with confidence
- **70-89**: Can start with minor assumptions
- **50-69**: Would need educated guesses
- **0-49**: Too many unknowns to proceed

## Demo Program

Run the comparison demo:

```bash
cd DeepResearchAgent/Examples
dotnet run --project ClarificationAgentDemo
```

### Demo Scenarios

1. **Vague Request**: "I want to learn about AI"
2. **Moderate Clarity**: "Research LLMs for code generation"
3. **Detailed Request**: Full research parameters specified
4. **Ambiguous Multi-Part**: Multiple topics without priority
5. **Follow-up Conversation**: Context building over turns

## Best Practices

### When to Use Iterative Agent

✅ **Use Iterative Agent When:**
- Research quality is critical
- Users often provide vague requests
- Budget allows 2-3x latency overhead
- Need quantifiable quality metrics

❌ **Use Standard Agent When:**
- Latency is critical (real-time chat)
- Users typically provide detailed requests
- Cost optimization is priority
- Simple yes/no clarification sufficient

### Tuning Recommendations

1. **For Speed**: Set `MaxIterations = 2`, disable quality metrics
2. **For Quality**: Set `MaxIterations = 5`, `QualityThreshold = 85.0`
3. **Balanced**: Use defaults (3 iterations, 75.0 threshold)

### Integration with SupervisorWorkflow

```csharp
// In SupervisorWorkflow, choose agent based on config
var clarificationResult = config.UseIterativeAgent
    ? await iterativeAgent.ClarifyWithIterationsAsync(messages, cancellationToken)
    : await standardAgent.ClarifyAsync(messages, cancellationToken);

// Quality metrics available for iterative path
if (clarificationResult is IterativeClarificationResult iterResult)
{
    logger.LogInformation(
        "Quality: {Score:F1}/100 after {Iterations} iterations",
        iterResult.QualityMetrics?.OverallScore,
        iterResult.IterationCount);
}
```

## Research References

- **PromptWizard Paper**: [arXiv:2405.18369](https://arxiv.org/abs/2405.18369)
- **Microsoft Research Blog**: [PromptWizard Blog Post](https://www.microsoft.com/en-us/research/blog/promptwizard-the-future-of-prompt-optimization-through-feedback-driven-self-evolving-prompts/)
- **GitHub Repository**: [microsoft/PromptWizard](https://github.com/microsoft/PromptWizard)

## Future Enhancements

### Planned Features

- [ ] **Synthetic Example Generation**: Generate synthetic "bad asks" for training critique
- [ ] **Multi-Agent Ensemble**: Combine critique from multiple models
- [ ] **Adaptive Thresholds**: Learn optimal thresholds per research domain
- [ ] **Feedback Loop to Research Success**: Track clarification quality vs research outcome
- [ ] **Chain-of-Thought Critique**: Add reasoning chains to critique feedback
- [ ] **Benchmark Suite**: Curated test scenarios with human-labeled quality scores

### Experimental Ideas

- **Meta-Prompt Optimization**: Use PromptWizard to optimize the critique prompt itself
- **Domain-Specific Agents**: Specialized clarification for medical, legal, technical research
- **User Personalization**: Learn user communication patterns over time
- **Collaborative Filtering**: Share quality insights across users (privacy-preserving)

## Troubleshooting

### Common Issues

**High Latency**
- Reduce `MaxIterations` to 2
- Disable quality metrics: `EnableQualityMetrics = false`
- Increase `MaxTimeoutSeconds` if hitting timeout

**Low Quality Scores**
- Check if LLM supports structured output
- Review prompt templates in `PromptTemplates.cs`
- Adjust `QualityThreshold` to realistic value (70-80)

**Iterations Not Improving**
- Enable `StoreIterationHistory` and review snapshots
- Check critique feedback for actionable suggestions
- May indicate prompt template needs refinement

## Contributing

See main repository [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - See [LICENSE](../../LICENSE)
