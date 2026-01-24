# 🚀 Quick Start - Test Model Configuration in 5 Minutes

## Run Tests Now

```bash
# All tests
dotnet test --filter "WorkflowModel"

# Just configuration tests
dotnet test --filter "WorkflowModelConfigurationTests"

# With details
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

## What You Just Got

✅ **46+ Tests** across 4 test files
✅ **4 Documentation Guides**
✅ **100% Model Configuration Coverage**
✅ **Ready to Use** - No setup needed

## Test Files

| File | Tests | Purpose |
|------|-------|---------|
| WorkflowModelConfigurationTests | 13 | Configuration class |
| SupervisorWorkflowModelTests | 17 | Workflow integration |
| WorkflowModelIntegrationTests | 8 | Scenarios (cost/quality) |
| ModelConfigurationUsageExamples | 8 | Practical patterns |

## Common Commands

```bash
# Run configuration tests
dotnet test --filter "WorkflowModelConfigurationTests"

# Run workflow tests
dotnet test --filter "SupervisorWorkflowModelTests"

# Test brain function
dotnet test --filter "Name~Brain"

# Test cost optimization
dotnet test --filter "Name~CostOptimized"

# Test quality optimization
dotnet test --filter "Name~QualityOptimized"

# Run with details
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

## Default Models

```
Brain:       gpt-oss:20b      (powerful reasoning)
Tools:       mistral:latest   (fast coordination)
Evaluator:   gpt-oss:20b      (detailed analysis)
RedTeam:     gpt-oss:20b      (critical thinking)
Pruner:      mistral:latest   (quick extraction)
```

## Use It

```csharp
// Use defaults (already optimized)
var config = new WorkflowModelConfiguration();

// Or customize
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "my-model:13b",
    RedTeamModel = "critic:latest"
};

var supervisor = new SupervisorWorkflow(..., modelConfig: config);
```

## Read More

- **Quick Start:** `MODEL_CONFIGURATION_QUICK_START.md`
- **Complete Guide:** `MODEL_CONFIGURATION_TESTING_GUIDE.md`
- **Reference:** `MODEL_CONFIG_QUICK_REFERENCE.md`
- **Summary:** `TESTING_SUMMARY.md`

---

**Status: ✅ Ready to Test**
