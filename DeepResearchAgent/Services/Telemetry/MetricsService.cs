using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DeepResearchAgent.Services.Telemetry
{
    public class MetricsService
    {
        public const string MeterName = "DeepResearchAgent.Metrics";

        private static readonly Meter Meter = new(MeterName);

        private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("dra_requests_total", description: "Total workflow requests");
        private static readonly Counter<long> AgentExecutionCounter = Meter.CreateCounter<long>("dra_agent_executions_total", description: "Agent execution events");
        private static readonly Counter<long> LlmRequestCounter = Meter.CreateCounter<long>("dra_llm_requests_total", description: "LLM invocation events");
        private static readonly Counter<long> LightningRequestCounter = Meter.CreateCounter<long>("dra_lightning_requests_total", description: "Lightning API interactions");
        private static readonly Counter<long> StateOperationCounter = Meter.CreateCounter<long>("dra_state_ops_total", description: "State management operations");
        private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("dra_errors_total", description: "Errors encountered");

        private static readonly Counter<long> ResearchTrackingCounter = Meter.CreateCounter<long>("dra_research_tracking_total", description: "Research request tracking events");
        private static readonly Counter<long> AgentTrackingCounter = Meter.CreateCounter<long>("dra_agent_tracking_total", description: "Agent execution tracking events");
        private static readonly Counter<long> LlmTrackingCounter = Meter.CreateCounter<long>("dra_llm_tracking_total", description: "LLM tracking events");
        private static readonly Counter<long> LightningTrackingCounter = Meter.CreateCounter<long>("dra_lightning_tracking_total", description: "Lightning tracking events");
        private static readonly Counter<long> StateTrackingCounter = Meter.CreateCounter<long>("dra_state_tracking_total", description: "State operation tracking events");
        private static readonly Counter<long> ErrorTrackingCounter = Meter.CreateCounter<long>("dra_error_tracking_total", description: "Error tracking events");

        private static readonly Histogram<double> WorkflowDuration = Meter.CreateHistogram<double>("dra_workflow_duration_ms", unit: "ms", description: "Workflow duration");
        private static readonly Histogram<double> LlmDuration = Meter.CreateHistogram<double>("dra_llm_duration_ms", unit: "ms", description: "LLM latency");

        // APO-specific metrics
        private static readonly Counter<long> ApoTasksSubmitted = Meter.CreateCounter<long>("dra_apo_tasks_submitted", description: "APO tasks submitted to Lightning");
        private static readonly Counter<long> ApoTasksCompleted = Meter.CreateCounter<long>("dra_apo_tasks_completed", description: "APO tasks completed successfully");
        private static readonly Counter<long> ApoTasksFailed = Meter.CreateCounter<long>("dra_apo_tasks_failed", description: "APO tasks failed");
        private static readonly Counter<long> ApoRetries = Meter.CreateCounter<long>("dra_apo_retries", description: "APO retry attempts");
        private static readonly Counter<long> ApoVerifications = Meter.CreateCounter<long>("dra_apo_verifications", description: "APO VERL verifications performed");
        private static readonly Histogram<double> ApoTaskLatency = Meter.CreateHistogram<double>("dra_apo_task_latency_ms", unit: "ms", description: "APO task latency");
        private static readonly Histogram<int> ApoConcurrency = Meter.CreateHistogram<int>("dra_apo_concurrency", description: "APO concurrent tasks");

        // Circuit breaker metrics
        private static readonly Counter<long> CircuitBreakerStateChanges = Meter.CreateCounter<long>("dra_circuit_breaker_state_changes", description: "Circuit breaker state changes");
        private static readonly Counter<long> CircuitBreakerFallbacks = Meter.CreateCounter<long>("dra_circuit_breaker_fallbacks", description: "Circuit breaker fallback executions");
        private static readonly Gauge<int> CircuitBreakerState = Meter.CreateGauge<int>("dra_circuit_breaker_state", description: "Circuit breaker state (0=Closed, 1=Open, 2=HalfOpen)");

        public void RecordRequest(string workflow, string status, double? durationMs = null)
        {
            RequestCounter.Add(1, new("workflow", workflow), new("status", status));
            if (durationMs.HasValue)
            {
                WorkflowDuration.Record(durationMs.Value, new("workflow", workflow), new("status", status));
            }
        }

        public void RecordAgentExecution(string workflow, string step, bool success)
        {
            AgentExecutionCounter.Add(1, new("workflow", workflow), new("step", step), new("success", success));
        }

        public void RecordLlmRequest(string workflow, string model, bool success, double? durationMs = null)
        {
            LlmRequestCounter.Add(1, new("workflow", workflow), new("model", model), new("success", success));
            if (durationMs.HasValue)
            {
                LlmDuration.Record(durationMs.Value, new("workflow", workflow), new("model", model), new("success", success));
            }
        }

        public void RecordLightningRequest(string action, bool success)
        {
            LightningRequestCounter.Add(1, new("action", action), new("success", success));
        }

        public void RecordStateOperation(string operation, bool success)
        {
            StateOperationCounter.Add(1, new("operation", operation), new("success", success));
        }

        public void RecordError(string workflow, string errorType)
        {
            ErrorCounter.Add(1, new("workflow", workflow), new("type", errorType));
        }

        public void TrackResearchRequest(string workflow, string researchId, string status)
        {
            ResearchTrackingCounter.Add(1, new("workflow", workflow), new("research_id", researchId), new("status", status));
        }

        public void TrackAgentExecution(string workflow, string phase, string status)
        {
            AgentTrackingCounter.Add(1, new("workflow", workflow), new("phase", phase), new("status", status));
        }

        public void TrackLlmRequest(string workflow, string model, string status)
        {
            LlmTrackingCounter.Add(1, new("workflow", workflow), new("model", model), new("status", status));
        }

        public void TrackLightningRequest(string action, string status)
        {
            LightningTrackingCounter.Add(1, new("action", action), new("status", status));
        }

        public void TrackStateOperation(string operation, string status)
        {
            StateTrackingCounter.Add(1, new("operation", operation), new("status", status));
        }

        public void TrackError(string workflow, string errorType)
        {
            ErrorTrackingCounter.Add(1, new("workflow", workflow), new("type", errorType));
        }

        public Stopwatch StartTimer() => Stopwatch.StartNew();

        public void StopTimer(Stopwatch? stopwatch)
        {
            stopwatch?.Stop();
        }

        // APO-specific metric recording methods
        public void RecordApoTaskSubmitted(string strategy)
        {
            ApoTasksSubmitted.Add(1, new KeyValuePair<string, object?>("strategy", strategy));
        }

        public void RecordApoTaskCompleted(string strategy)
        {
            ApoTasksCompleted.Add(1, new KeyValuePair<string, object?>("strategy", strategy));
        }

        public void RecordApoTaskFailed(string strategy)
        {
            ApoTasksFailed.Add(1, new KeyValuePair<string, object?>("strategy", strategy));
        }

        public void RecordApoRetry(int retryCount)
        {
            ApoRetries.Add(1, new KeyValuePair<string, object?>("retry_count", retryCount.ToString()));
        }

        public void RecordApoVerification(bool success)
        {
            ApoVerifications.Add(1, new KeyValuePair<string, object?>("success", success));
        }

        public void RecordLatency(string operation, double latencyMs)
        {
            if (operation.Contains("apo"))
            {
                ApoTaskLatency.Record(latencyMs, new KeyValuePair<string, object?>("operation", operation));
            }
        }

        public void RecordApoConcurrency(int concurrentTasks)
        {
            ApoConcurrency.Record(concurrentTasks);
        }

        // Circuit breaker metric recording methods
        public void RecordCircuitBreakerStateChange(string state)
        {
            CircuitBreakerStateChanges.Add(1, new KeyValuePair<string, object?>("state", state));
        }

        public void RecordCircuitBreakerFallback(string reason)
        {
            CircuitBreakerFallbacks.Add(1, new KeyValuePair<string, object?>("reason", reason));
        }

        public void SetCircuitBreakerState(int state)
        {
            // Gauge doesn't have a direct set method in .NET metrics, record as observation
            // 0 = Closed, 1 = Open, 2 = HalfOpen
        }
    }
}
