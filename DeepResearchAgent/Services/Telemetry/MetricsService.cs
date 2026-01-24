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
    }
}
