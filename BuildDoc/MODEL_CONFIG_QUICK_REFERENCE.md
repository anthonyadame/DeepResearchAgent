# Model Configuration Testing - Quick Reference

## Essential Commands

```bash
# Run all tests
dotnet test --filter "WorkflowModel"

# Run with details
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"

# Run specific test class
dotnet test DeepResearchAgent.Tests/Configuration/WorkflowModelConfigurationTests.cs

# Run specific test
dotnet test --filter "Name~SupervisorBrainAsync_UsesBrainModel"
```

## Default Models

| Function | Model | Purpose |
|----------|-------|---------|
| Supervisor Brain | `gpt-oss:20b` | Complex reasoning |
| Supervisor Tools | `mistral:latest` | Fast coordination |
| Quality Evaluator | `gpt-oss:20b` | Detailed analysis |
| Red Team | `gpt-oss:20b` | Critical thinking |
| Context Pruner | `mistral:latest` | Quick extraction |

## Test Classes & Coverage

| Class | Tests | Command |
|-------|-------|---------|
| WorkflowModelConfigurationTests | 13 | `--filter "WorkflowModelConfigurationTests"` |
| SupervisorWorkflowModelTests | 17 | `--filter "SupervisorWorkflowModelTests"` |
| WorkflowModelIntegrationTests | 8 | `--filter "WorkflowModelIntegrationTests"` |
| ModelConfigurationUsageExamples | 8 | `--filter "ModelConfigurationUsageExamples"` |

## Test by Function

```bash
# Brain tests
dotnet test --filter "Name~Brain"

# Quality Evaluator tests
dotnet test --filter "Name~QualityEvaluator"

# Red Team tests
dotnet test --filter "Name~RedTeam"

# Context Pruner tests
dotnet test --filter "Name~ContextPruner"

# Custom configuration tests
dotnet test --filter "Name~Custom"
```

## Test by Scenario

```bash
# Cost optimization
dotnet test --filter "Name~CostOptimized"

# Quality optimization
dotnet test --filter "Name~QualityOptimized"

# Balanced/default
dotnet test --filter "Name~Default OR Name~Balanced"

# Specific scenario
dotnet test --filter "Name~Scenario"
```

## Usage Examples

### Default Configuration

```csharp
var config = new WorkflowModelConfiguration();
var supervisor = new SupervisorWorkflow(
    stateService, researcher, llmService,
    modelConfig: config);
```

### Custom Single Model

```csharp
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "neural-chat:13b"
};
```

### Cost-Optimized

```csharp
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "mistral:7b",
    SupervisorToolsModel = "mistral:latest",
    QualityEvaluatorModel = "mistral:7b",
    RedTeamModel = "mistral:7b",
    ContextPrunerModel = "orca-mini:latest"
};
```

### Quality-Optimized

```csharp
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "neural-chat:13b",
    SupervisorToolsModel = "neural-chat:7b",
    QualityEvaluatorModel = "neural-chat:13b",
    RedTeamModel = "neural-chat:13b",
    ContextPrunerModel = "neural-chat:7b"
};
```

## Model Categories

**Reasoning Models:** (Brain, Evaluator, Red Team)
- `gpt-oss:20b` - Balanced reasoning
- `neural-chat:13b` - Strong reasoning
- `mistral:7b` - Fast reasoning

**Coordination Models:** (Tools, Pruner)
- `mistral:latest` - Fast, reliable
- `orca-mini:latest` - Lightweight
- `neural-chat:7b` - Balanced

## Verification Checklist

- [ ] All tests pass: `dotnet test --filter "WorkflowModel"`
- [ ] Configuration initializes correctly
- [ ] Models route to correct functions
- [ ] Custom models are respected
- [ ] Brain uses brain model
- [ ] Evaluator uses evaluator model
- [ ] Red team uses red team model
- [ ] Pruner uses pruner model
- [ ] Full workflow uses all models
- [ ] Cost-optimized profile works
- [ ] Quality-optimized profile works
- [ ] Balanced profile works
- [ ] No null reference exceptions
- [ ] Dependency injection works

## Debug Commands

```bash
# Verbose output
dotnet test --filter "WorkflowModel" --logger "console;verbosity=diagnostic"

# Single test
dotnet test --filter "Name~YourTest"

# Test with tracing
dotnet test --filter "Name~YourTest" --logger "console;verbosity=detailed"

# Run multiple times (check stability)
dotnet test --filter "Name~YourTest" --repeat 5
```

## Expected Results

```
Configuration Tests:           13/13 ✅
Workflow Integration Tests:    17/17 ✅
Integration Scenario Tests:     8/8 ✅
Usage Example Tests:            8/8 ✅
─────────────────────────────────────
Total:                         46/46 ✅
Coverage:                      ~98% ✅
```

## Files & Locations

| File | Purpose |
|------|---------|
| `Configuration/WorkflowModelConfigurationTests.cs` | Configuration class tests |
| `Workflows/SupervisorWorkflowModelTests.cs` | Workflow integration tests |
| `Integration/WorkflowModelIntegrationTests.cs` | Scenario-based tests |
| `Examples/ModelConfigurationUsageExamples.cs` | Usage patterns |
| `MODEL_CONFIGURATION_TESTING_GUIDE.md` | Full testing guide |
| `MODEL_CONFIGURATION_QUICK_START.md` | Quick start guide |

## Common Patterns

### Verify Model Is Used
```csharp
_mockLlmService.Verify(
    s => s.InvokeAsync(
        It.IsAny<List<OllamaChatMessage>>(),
        expectedModel,
        It.IsAny<CancellationToken>()),
    Times.Once);
```

### Test Custom Config
```csharp
var customConfig = new WorkflowModelConfiguration 
{ 
    SupervisorBrainModel = "custom:model" 
};
var supervisor = new SupervisorWorkflow(..., modelConfig: customConfig);
```

### Test Scenario
```csharp
var config = scenario switch
{
    "fast" => CreateFastConfig(),
    "quality" => CreateQualityConfig(),
    _ => new WorkflowModelConfiguration()
};
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Tests fail locally | Clear bin/obj, rebuild |
| Model not found | Check model name spelling |
| Verify fails | Ensure model passed to constructor |
| CI/CD fails | Run in Release mode |
| Intermittent failures | Check async/await timing |

## Next Steps

1. **Run all tests:**
```
   dotnet test --filter "WorkflowModel"
```

2. **Run detailed tests:**
```
   dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

3. **Test specific function:**
```
   dotnet test --filter "Name~SupervisorBrainAsync"
```

4. **Integrate into CI/CD**

5. **Create custom profiles**
