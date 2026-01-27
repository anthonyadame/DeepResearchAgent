# Agent-Lightning APO Quick Reference

## Configuration Presets

### Development (Fast Iteration)
```json
{
  "Lightning": {
    "APO": {
      "Enabled": true,
      "OptimizationStrategy": "HighPerformance",
      "ResourceLimits": {
        "MaxConcurrentTasks": 5,
        "TaskTimeoutSeconds": 60
      },
      "AutoScaling": { "Enabled": false }
    }
  }
}
```

### Production (Balanced)
```json
{
  "Lightning": {
    "APO": {
      "Enabled": true,
      "OptimizationStrategy": "Balanced",
      "ResourceLimits": {
        "MaxConcurrentTasks": 10,
        "TaskTimeoutSeconds": 300
      },
      "AutoScaling": {
        "Enabled": true,
        "MinInstances": 2,
        "MaxInstances": 10
      }
    }
  }
}
```

### Resource-Constrained
```json
{
  "Lightning": {
    "APO": {
      "Enabled": true,
      "OptimizationStrategy": "LowResource",
      "ResourceLimits": {
        "MaxCpuPercent": 50,
        "MaxMemoryMb": 1024,
        "MaxConcurrentTasks": 3,
        "TaskTimeoutSeconds": 600
      }
    }
  }
}
```

## Common Usage Patterns

### 1. Use App Configuration (Default)
```csharp
// Uses appsettings.apo.json automatically
var facts = await researcherWorkflow.ResearchAsync("topic");
```

### 2. Override Strategy Per Call
```csharp
var options = new ApoExecutionOptions
{
    StrategyOverride = OptimizationStrategy.HighPerformance
};
var facts = await researcherWorkflow.ResearchAsync("urgent", apoOptions: options);
```

### 3. Force Verification
```csharp
var options = new ApoExecutionOptions
{
    ForceVerification = true
};
await lightningService.SubmitTaskAsync("agent", task, options);
```

### 4. Custom Timeout
```csharp
var options = new ApoExecutionOptions
{
    Timeout = TimeSpan.FromMinutes(10)
};
await lightningService.SubmitTaskAsync("agent", longTask, options);
```

### 5. Disable APO for Specific Call
```csharp
var options = new ApoExecutionOptions { DisableApo = true };
await lightningService.SubmitTaskAsync("agent", task, options);
```

## Strategy Decision Matrix

| Requirement | Strategy | Rationale |
|------------|----------|-----------|
| User-facing API (<100ms) | HighPerformance | Skip VERL, max throughput |
| Production workload | Balanced | Quality + performance |
| Dev environment | LowResource | Conserve resources |
| Batch processing | CostOptimized | Balance cost/time |
| Critical data | Balanced + ForceVerification | Quality assurance |

## Metrics to Monitor

### Prometheus Queries

```promql
# APO task submission rate
rate(dra_apo_tasks_submitted[5m])

# APO task latency (p95)
histogram_quantile(0.95, rate(dra_apo_task_latency_ms_bucket[5m]))

# APO retry rate
rate(dra_apo_retries[5m])

# APO concurrent tasks
avg_over_time(dra_apo_concurrency[5m])
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "panels": [
      {
        "title": "APO Task Throughput",
        "targets": [{"expr": "rate(dra_apo_tasks_completed[5m])"}]
      },
      {
        "title": "APO Task Latency (p50, p95, p99)",
        "targets": [
          {"expr": "histogram_quantile(0.50, rate(dra_apo_task_latency_ms_bucket[5m]))"},
          {"expr": "histogram_quantile(0.95, rate(dra_apo_task_latency_ms_bucket[5m]))"},
          {"expr": "histogram_quantile(0.99, rate(dra_apo_task_latency_ms_bucket[5m]))"}
        ]
      }
    ]
  }
}
```

## Troubleshooting

### High Task Latency
1. Check `MaxConcurrentTasks` - increase if CPU is underutilized
2. Review retry count - reduce for HighPerformance
3. Consider disabling VERL for non-critical tasks

### Task Failures
1. Check Lightning server health: `/health` endpoint
2. Review retry policy in logs
3. Verify timeout settings are adequate

### Memory Pressure
1. Reduce `CacheSizeMb`
2. Lower `MaxConcurrentTasks`
3. Switch to `LowResource` strategy

### Auto-Scaling Not Triggering
1. Verify `AutoScaling.Enabled = true`
2. Check `ScaleUpThresholdPercent` is appropriate
3. Ensure cooldown period has elapsed

## Environment Variables

Override config with environment variables:

```bash
# Override server URL
export Lightning__ServerUrl=https://lightning.prod.example.com

# Override strategy
export Lightning__APO__OptimizationStrategy=HighPerformance

# Override concurrency limit
export Lightning__APO__ResourceLimits__MaxConcurrentTasks=20

# Disable APO
export Lightning__APO__Enabled=false
```

## Health Checks

### Lightning Server
```bash
curl http://localhost:8090/health
```

### APO Configuration
```csharp
var apo = serviceProvider.GetRequiredService<LightningAPOConfig>();
Console.WriteLine($"Strategy: {apo.Strategy}, Enabled: {apo.Enabled}");
```

### Metrics Endpoint
```bash
curl http://localhost:9090/metrics | grep apo
```

## Best Practices

1. ✅ **Start with Balanced** - Use default strategy in production
2. ✅ **Monitor Metrics** - Track latency, throughput, retries
3. ✅ **Test Strategies** - Benchmark each strategy for your workload
4. ✅ **Override Sparingly** - Use execution options only when necessary
5. ✅ **Enable Auto-Scaling** - For production, enable with appropriate thresholds
6. ✅ **Set Timeouts** - Configure based on P95 latency + buffer
7. ✅ **Log APO Events** - Monitor retry attempts and scaling decisions

## Performance Tuning

### Maximize Throughput
```json
{
  "OptimizationStrategy": "HighPerformance",
  "ResourceLimits": {
    "MaxConcurrentTasks": 20,
    "TaskTimeoutSeconds": 30
  }
}
```

### Minimize Resource Usage
```json
{
  "OptimizationStrategy": "LowResource",
  "ResourceLimits": {
    "MaxCpuPercent": 40,
    "MaxMemoryMb": 512,
    "MaxConcurrentTasks": 2
  }
}
```

### Optimize Cost
```json
{
  "OptimizationStrategy": "CostOptimized",
  "ResourceLimits": {
    "MaxConcurrentTasks": 5,
    "TaskTimeoutSeconds": 600
  },
  "AutoScaling": {
    "ScaleDownThresholdPercent": 20
  }
}
```
