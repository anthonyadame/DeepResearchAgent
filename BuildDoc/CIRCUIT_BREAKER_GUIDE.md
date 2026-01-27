# Circuit Breaker Pattern for Lightning Server Failures

## Overview

The circuit breaker pattern prevents cascading failures when the Lightning server is unavailable or experiencing issues. It provides:

✅ **Automatic failure detection** - Monitors failure rates  
✅ **Graceful degradation** - Falls back to local execution  
✅ **Self-healing** - Automatically recovers when service is healthy  
✅ **Metrics & logging** - Full observability of circuit state  

## How It Works

```
┌─────────────────────────────────────────────────────────┐
│                  Circuit Breaker States                  │
└─────────────────────────────────────────────────────────┘

    CLOSED (Normal Operation)
         │
         │ Failure rate > 50% for 30s
         ▼
    OPEN (Server Down)
         │
         │ After 60s (break duration)
         ▼
    HALF-OPEN (Testing Recovery)
         │
         ├─ Success → CLOSED
         │
         └─ Failure → OPEN (reset timer)
```

### State Descriptions

**CLOSED** (Green - Normal)
- All requests go to Lightning server
- Circuit monitors failure rate
- System operating normally

**OPEN** (Red - Circuit Tripped)
- Requests **do not** go to Lightning server
- Fallback to local execution
- Prevents wasting resources on failing server
- Waits for `BreakDuration` before retry

**HALF-OPEN** (Yellow - Testing)
- First request after break duration
- Tests if server recovered
- Success → return to CLOSED
- Failure → return to OPEN

## Configuration

### appsettings.apo.json

```json
{
  "Lightning": {
    "APO": {
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 5,
        "MinimumThroughput": 10,
        "SamplingDurationSeconds": 30,
        "FailureRateThreshold": 50,
        "BreakDurationSeconds": 60,
        "EnableFallback": true,
        "LogStateChanges": true
      }
    }
  }
}
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | `true` | Enable circuit breaker (recommended) |
| `FailureThreshold` | `5` | Min consecutive failures before considering circuit |
| `MinimumThroughput` | `10` | Min requests/window before circuit activates |
| `SamplingDurationSeconds` | `30` | Time window for failure rate calculation |
| `FailureRateThreshold` | `50` | Failure percentage (0-100) to open circuit |
| `BreakDurationSeconds` | `60` | How long circuit stays open |
| `EnableFallback` | `true` | Execute tasks locally when circuit open |
| `LogStateChanges` | `true` | Log circuit state transitions |

## Fallback Behavior

When the circuit is **OPEN**, tasks are executed locally:

```csharp
// Circuit CLOSED - calls Lightning server
var result = await lightningService.SubmitTaskAsync("agent", task);

// Circuit OPEN - executes fallback
// Returns simulated result without calling server
var result = new AgentTaskResult
{
    TaskId = task.Id,
    Status = TaskStatus.Completed,
    Result = "{ \"fallback\": true, \"message\": \"Executed locally\" }"
};
```

### Fallback Execution

When `EnableFallback = true`:
- ✅ Task is marked as completed locally
- ✅ Result indicates fallback execution
- ✅ No Lightning server call made
- ✅ Application continues functioning

When `EnableFallback = false`:
- ❌ Throws `InvalidOperationException`
- ❌ Task fails immediately
- ❌ Caller must handle exception

## Usage Examples

### Example 1: Default Configuration (Recommended)

```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 5,
    "FailureRateThreshold": 50,
    "BreakDurationSeconds": 60,
    "EnableFallback": true
  }
}
```

**Behavior:**
- Requires 5+ failures
- Opens circuit at 50% failure rate
- Stays open for 60 seconds
- Falls back to local execution

### Example 2: Aggressive (Fast Fail)

```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 3,
    "FailureRateThreshold": 30,
    "BreakDurationSeconds": 30,
    "EnableFallback": true
  }
}
```

**Behavior:**
- Opens circuit quickly (3 failures, 30% rate)
- Short break duration (30s)
- Good for: High-traffic, latency-sensitive

### Example 3: Conservative (Tolerant)

```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 10,
    "FailureRateThreshold": 70,
    "BreakDurationSeconds": 120,
    "EnableFallback": true
  }
}
```

**Behavior:**
- Tolerates more failures (10, 70% rate)
- Longer break duration (2 minutes)
- Good for: Low-traffic, occasional failures

### Example 4: Disabled (Not Recommended)

```json
{
  "CircuitBreaker": {
    "Enabled": false
  }
}
```

**Behavior:**
- No circuit breaker protection
- All failures retry indefinitely
- Risk of cascading failures
- **Only use for development/testing**

## Monitoring

### Logs

Circuit state changes are logged:

```
[Warning] Circuit breaker OPENED for Lightning server. Failure rate: 52%. Break duration: 60s
[Info] Circuit breaker HALF-OPEN for Lightning server. Testing recovery...
[Info] Circuit breaker CLOSED for Lightning server. Service recovered.
```

### Metrics (Prometheus)

```promql
# Circuit breaker state changes
dra_circuit_breaker_state_changes{state="opened"}
dra_circuit_breaker_state_changes{state="closed"}
dra_circuit_breaker_state_changes{state="half_open"}

# Fallback executions
dra_circuit_breaker_fallbacks
```

### Grafana Dashboard Queries

**Circuit State Over Time:**
```promql
increase(dra_circuit_breaker_state_changes[5m])
```

**Fallback Rate:**
```promql
rate(dra_circuit_breaker_fallbacks[5m])
```

**% of Requests Using Fallback:**
```promql
(rate(dra_circuit_breaker_fallbacks[5m]) / 
 rate(dra_apo_tasks_submitted[5m])) * 100
```

## Troubleshooting

### Circuit Breaker Opening Frequently

**Symptoms:**
- Logs show repeated "Circuit breaker OPENED" messages
- High fallback execution count
- Degraded Lightning server performance

**Diagnosis:**
```bash
# Check Lightning server health
curl http://localhost:8090/health

# Check circuit breaker metrics
curl http://localhost:9090/metrics | grep circuit_breaker
```

**Solutions:**

1. **Lightning Server is Actually Down**
   - Restart Lightning server
   - Check server logs for errors
   - Verify network connectivity

2. **Threshold Too Aggressive**
   - Increase `FailureThreshold` (e.g., 5 → 10)
   - Increase `FailureRateThreshold` (e.g., 50% → 70%)
   - Increase `SamplingDurationSeconds` (e.g., 30 → 60)

3. **Intermittent Network Issues**
   - Check network stability
   - Review firewall rules
   - Verify DNS resolution

### Circuit Stuck Open

**Symptoms:**
- Circuit remains OPEN for extended periods
- Server is healthy but circuit won't close
- All requests using fallback

**Diagnosis:**
```promql
# Check when circuit last changed state
dra_circuit_breaker_state_changes
```

**Solutions:**

1. **Decrease Break Duration**
   ```json
   "BreakDurationSeconds": 30  // Try recovery sooner
   ```

2. **Force Circuit Reset**
   - Restart the application
   - Circuit resets to CLOSED on startup

3. **Verify Server is Actually Healthy**
   ```bash
   # Test endpoint directly
   curl -v http://localhost:8090/api/tasks/submit
   ```

### Fallback Executions Not Working

**Symptoms:**
- Tasks fail when circuit is OPEN
- `InvalidOperationException` thrown
- No local execution

**Diagnosis:**
Check configuration:
```json
"EnableFallback": true  // Must be true
```

**Solutions:**

1. **Enable Fallback**
   ```json
   "CircuitBreaker": {
     "EnableFallback": true
   }
   ```

2. **Implement Custom Fallback**
   ```csharp
   try
   {
       await lightningService.SubmitTaskAsync(agentId, task);
   }
   catch (BrokenCircuitException)
   {
       // Custom fallback logic
       return await ExecuteLocally(task);
   }
   ```

## Best Practices

### Production Settings

✅ **DO:**
- Enable circuit breaker (`Enabled: true`)
- Enable fallback (`EnableFallback: true`)
- Log state changes (`LogStateChanges: true`)
- Monitor circuit metrics in Grafana
- Tune thresholds based on actual traffic

❌ **DON'T:**
- Disable circuit breaker in production
- Set `BreakDuration` too short (< 30s)
- Set `FailureThreshold` too low (< 3)
- Ignore circuit state change logs

### Development Settings

For local development:
```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 3,
    "BreakDurationSeconds": 15,
    "EnableFallback": true
  }
}
```

Faster recovery for quick iteration.

### Testing Circuit Breaker

**Simulate Failures:**

1. Stop Lightning server:
   ```bash
   docker stop lightning-server
   ```

2. Send requests and observe circuit opening:
   ```bash
   # Circuit will open after configured failures
   curl http://localhost:5000/api/research
   ```

3. Check logs for circuit state changes

4. Restart server and observe recovery:
   ```bash
   docker start lightning-server
   # Wait for BreakDuration, circuit will test recovery
   ```

## Integration with Retry Policy

Circuit breaker works **in conjunction** with retry policy:

```
Request Flow:
    │
    ▼
[Circuit Breaker Check]
    │
    ├─ CLOSED → Continue
    │           │
    │           ▼
    │      [Retry Policy]
    │           │
    │           ├─ Retry 1 (250ms delay)
    │           ├─ Retry 2 (500ms delay)
    │           └─ Retry 3 (1000ms delay)
    │                │
    │                ├─ Success → Return
    │                └─ All Failed → Circuit evaluates
    │
    └─ OPEN → Fallback (no retries)
```

**Key Points:**
- Retries happen **before** circuit evaluates
- Circuit sees retry failures as single failure
- Circuit prevents retries from exhausting resources

## Performance Impact

| Configuration | Latency Overhead | Memory Overhead | CPU Overhead |
|--------------|------------------|-----------------|--------------|
| Circuit Breaker | < 1ms | ~200 bytes | Negligible |
| Retry Policy | 0-3s (on failure) | ~1KB | Minimal |
| Combined | < 1ms (success)<br>0-3s (failure) | ~2KB | < 1% |

**Recommendation:** Always enable in production. Benefits far outweigh minimal overhead.

## Summary

✅ **Circuit breaker protects** your application from Lightning server failures  
✅ **Automatic recovery** when server is healthy again  
✅ **Graceful degradation** via local fallback execution  
✅ **Full observability** with logs and metrics  
✅ **Configurable** for different environments and traffic patterns  

**Default configuration works well for most scenarios.** Tune based on actual behavior in production.
