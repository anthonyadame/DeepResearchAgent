# Grafana Monitoring Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DEEP RESEARCH AGENT (.NET 8)                          │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │                    Agent-Lightning APO                              │    │
│  │                                                                      │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │    │
│  │  │  Researcher  │  │  Supervisor  │  │    Master    │             │    │
│  │  │   Workflow   │  │   Workflow   │  │   Workflow   │             │    │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘             │    │
│  │         │                  │                  │                      │    │
│  │         └──────────────────┴──────────────────┘                      │    │
│  │                            │                                         │    │
│  │                  ┌─────────▼─────────┐                              │    │
│  │                  │ MetricsService    │                              │    │
│  │                  │  (APO Metrics)    │                              │    │
│  │                  └─────────┬─────────┘                              │    │
│  │                            │                                         │    │
│  └────────────────────────────┼─────────────────────────────────────────┘    │
│                               │                                              │
│                    ┌──────────▼───────────┐                                 │
│                    │ OpenTelemetry        │                                 │
│                    │ PrometheusExporter   │                                 │
│                    │  :9090/metrics       │                                 │
│                    └──────────┬───────────┘                                 │
└───────────────────────────────┼──────────────────────────────────────────────┘
                                │
                                │ HTTP Scrape
                                │ (every 15s)
                                │
            ┌───────────────────▼────────────────────┐
            │                                        │
            │         PROMETHEUS (Port 9091)         │
            │                                        │
            │  ┌──────────────────────────────────┐ │
            │  │  Metrics Storage (TSDB)          │ │
            │  │  - 30-day retention              │ │
            │  │  - 15s resolution                │ │
            │  └──────────────────────────────────┘ │
            │                                        │
            │  ┌──────────────────────────────────┐ │
            │  │  Alert Rules Engine              │ │
            │  │  - 16 pre-configured rules       │ │
            │  │  - Evaluates every 30s           │ │
            │  └──────────────┬───────────────────┘ │
            │                 │                      │
            └─────────────────┼──────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
            │         Firing Alerts             │
            │                 │                 │
            │                 ▼                 │
     ┌──────▼──────────────────────────────┐   │
     │  ALERTMANAGER (Port 9093)           │   │
     │                                     │   │
     │  ┌──────────────────────────────┐  │   │
     │  │  Alert Routing               │  │   │
     │  │  - Critical → Email + Slack  │  │   │
     │  │  - Warning → Slack           │  │   │
     │  │  - Info → Webhook            │  │   │
     │  └──────────────┬───────────────┘  │   │
     │                 │                   │   │
     └─────────────────┼───────────────────┘   │
                       │                       │
        ┌──────────────┼───────────────────────┼────────────┐
        │              │                       │            │
        ▼              ▼                       ▼            ▼
   ┌────────┐    ┌─────────┐            ┌─────────┐   ┌─────────┐
   │ Email  │    │  Slack  │            │  Teams  │   │Webhook  │
   │Oncall  │    │#alerts  │            │#critical│   │Custom   │
   └────────┘    └─────────┘            └─────────┘   └─────────┘

                       │
              Query API │
                       │
            ┌──────────▼──────────────────────────┐
            │                                     │
            │    GRAFANA (Port 3000)              │
            │                                     │
            │  ┌──────────────────────────────┐  │
            │  │  APO Performance Dashboard   │  │
            │  │                              │  │
            │  │  ┌─────────────────────────┐│  │
            │  │  │ 1. Task Throughput      ││  │
            │  │  │ 2. Latency Gauge        ││  │
            │  │  │ 3. Latency Percentiles  ││  │
            │  │  │ 4. Retry Attempts       ││  │
            │  │  │ 5. Concurrency          ││  │
            │  │  │ 6. VERL Verifications   ││  │
            │  │  │ 7. Success Rate         ││  │
            │  │  │ 8. By Strategy Table    ││  │
            │  │  └─────────────────────────┘│  │
            │  └──────────────────────────────┘  │
            │                                     │
            │  Auto-refresh: 5s                   │
            │  Time range: 15m default            │
            │                                     │
            └─────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │   Operators     │
                    │  Dashboards &   │
                    │     Alerts      │
                    └─────────────────┘
```

## Data Flow

1. **Metrics Generation** (.NET Agent)
   - `MetricsService` records APO events
   - OpenTelemetry formats metrics
   - Prometheus exporter exposes HTTP endpoint

2. **Metrics Collection** (Prometheus)
   - Scrapes `/metrics` every 15 seconds
   - Stores in time-series database (TSDB)
   - Evaluates alert rules every 30 seconds

3. **Alert Routing** (Alertmanager)
   - Receives firing alerts from Prometheus
   - Routes by severity to appropriate channels
   - Suppresses duplicate/redundant alerts

4. **Visualization** (Grafana)
   - Queries Prometheus for metric data
   - Renders 8 dashboard panels
   - Auto-refreshes every 5 seconds
   - Provides interactive exploration

## Metric Categories

```
APO Metrics
├── Counters
│   ├── dra_apo_tasks_submitted (by strategy)
│   ├── dra_apo_tasks_completed (by strategy)
│   ├── dra_apo_tasks_failed (by strategy)
│   ├── dra_apo_retries (by retry_count)
│   └── dra_apo_verifications (by success)
│
└── Histograms
    ├── dra_apo_task_latency_ms (buckets)
    └── dra_apo_concurrency (buckets)
```

## Alert Flow

```
Metric Threshold Breach
         │
         ▼
  Prometheus Evaluates Rule
         │
         ├─ No breach → Continue monitoring
         │
         └─ Breach for duration → Fire alert
                   │
                   ▼
            Alertmanager Receives
                   │
                   ├─ Inhibited → Suppress
                   │
                   └─ Active → Route
                              │
                   ┌──────────┼──────────┐
                   │          │          │
                   ▼          ▼          ▼
              Critical    Warning     Info
                   │          │          │
                   │          │          │
              Email+Slack  Slack     Webhook
```

## Dashboard Update Cycle

```
Every 5 seconds:
  Grafana → Query Prometheus → Render Panels → Display
  
Every 15 seconds:
  Prometheus → Scrape Metrics → Store TSDB
  
Every 30 seconds:
  Prometheus → Evaluate Alerts → Send to Alertmanager
```

## Storage & Retention

```
┌─────────────────────────────────────┐
│  Prometheus TSDB                    │
│  ┌───────────────────────────────┐ │
│  │  Day 1-7:   Full resolution   │ │
│  │  Day 8-30:  Downsampled 1m    │ │
│  │  Day 30+:   Deleted           │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Grafana                            │
│  ┌───────────────────────────────┐ │
│  │  Dashboards: Persistent       │ │
│  │  Settings: Persistent         │ │
│  │  Data: Queried on-demand      │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

## Network Topology

```
Host Machine (Windows/Linux/macOS)
├── Deep Research Agent     :9090 (metrics)
├── Lightning Server        :8090 (optional metrics)
└── Docker Network (monitoring)
    ├── Prometheus          :9091 (UI & API)
    ├── Grafana             :3000 (UI)
    └── Alertmanager        :9093 (UI & API)
```

## Service Dependencies

```
Grafana
  └─ requires: Prometheus (datasource)

Prometheus
  ├─ scrapes: Deep Research Agent
  ├─ scrapes: Lightning Server
  └─ sends: Alertmanager (alerts)

Alertmanager
  ├─ receives: Prometheus (alerts)
  └─ sends: Email/Slack/Teams (notifications)

Deep Research Agent
  └─ exposes: Metrics endpoint (independent)
```

## Performance Characteristics

| Component | CPU | Memory | Disk | Network |
|-----------|-----|--------|------|---------|
| Prometheus | Low | ~1GB | ~5GB/month | ~10KB/s |
| Grafana | Low | ~200MB | ~100MB | Minimal |
| Alertmanager | Minimal | ~50MB | ~10MB | Minimal |
| **Total** | ~2% | ~1.5GB | ~5GB/month | ~10KB/s |

*Suitable for running on development machines alongside agent*
