# Deep Research Agent - Grafana Monitoring Guide

## Overview

This monitoring stack provides comprehensive observability for Agent-Lightning APO (Automatic Performance Optimization) using Prometheus, Grafana, and Alertmanager.

## Architecture

```
┌─────────────────────┐
│  Deep Research      │
│  Agent (APO)        │──► Metrics (OpenTelemetry)
└─────────────────────┘
           │
           ▼
    ┌──────────────┐
    │  Prometheus  │──► Scrapes metrics every 15s
    │  :9091       │    Evaluates alerts
    └──────────────┘    Stores time-series data
           │
           ▼
    ┌──────────────┐
    │   Grafana    │──► Visualizes metrics
    │   :3000      │    APO Performance Dashboard
    └──────────────┘
           │
           ▼
    ┌──────────────┐
    │ Alertmanager │──► Manages alerts
    │   :9093      │    Routes notifications
    └──────────────┘
```

## Quick Start

### 1. Prerequisites

- Docker Desktop installed and running
- Deep Research Agent configured with APO
- Ports 3000, 9091, 9093 available

### 2. Start Monitoring Stack

**Windows (PowerShell):**
```powershell
cd monitoring
.\setup.ps1
```

**Linux/macOS:**
```bash
cd monitoring
chmod +x setup.sh
./setup.sh
```

**Manual Docker Compose:**
```bash
cd monitoring
docker-compose up -d
```

### 3. Access Grafana

1. Open browser to http://localhost:3000
2. Login with:
   - Username: `admin`
   - Password: `admin`
3. Change password when prompted
4. Navigate to: **Dashboards** → **Deep Research Agent** → **APO Performance**

### 4. Start Deep Research Agent

Ensure your agent is configured with metrics export:

```csharp
// In Program.cs - already configured
using var meterProvider = OpenTelemetry.Sdk.CreateMeterProviderBuilder()
    .AddMeter(MetricsService.MeterName)
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter(options =>
    {
        options.StartHttpListener = true;
        options.HttpListenerPrefixes = new[] { "http://localhost:9090/" };
    })
    .Build();
```

Start your agent and metrics will automatically flow to the dashboard.

> **⚠️ Lightning Server "Offline" Warning?**  
> If you see "Server connection Offline http://localhost:8090" in Grafana, this is **normal and expected**.  
> The Lightning Server is **optional** and not required for APO monitoring.  
>  
> **Quick Fix:** See [TROUBLESHOOTING_LIGHTNING.md](TROUBLESHOOTING_LIGHTNING.md) to disable the optional Lightning Server monitoring.

## Dashboard Panels

### 1. APO Task Throughput (req/s)
**Metrics:**
- `rate(dra_apo_tasks_submitted[5m])` - Tasks submitted per second
- `rate(dra_apo_tasks_completed[5m])` - Tasks completed per second
- `rate(dra_apo_tasks_failed[5m])` - Tasks failed per second

**Interpretation:**
- Submitted > Completed = backlog building up
- Failed rate > 5% = investigate errors
- Flat lines = system idle or broken

### 2. APO Task Latency
**Metrics:**
- `histogram_quantile(0.50, rate(dra_apo_task_latency_ms_bucket[5m]))` - P50
- `histogram_quantile(0.95, rate(dra_apo_task_latency_ms_bucket[5m]))` - P95
- `histogram_quantile(0.99, rate(dra_apo_task_latency_ms_bucket[5m]))` - P99

**Thresholds:**
- Green: < 1000ms (healthy)
- Yellow: 1000-5000ms (acceptable)
- Red: > 5000ms (investigate)

### 3. APO Task Latency Percentiles Over Time
**Purpose:** Track latency trends across all percentiles

**Use Cases:**
- Detect performance degradation over time
- Identify latency spikes
- Compare performance across strategies

### 4. APO Retry Attempts (5m window)
**Metrics:**
- `increase(dra_apo_retries[5m])` - Total retries by attempt number

**Interpretation:**
- Occasional retries = normal (transient failures)
- Sustained high retries = Lightning server issues
- Retries > 100/5min = investigate network/server

### 5. APO Concurrency
**Metrics:**
- `avg_over_time(dra_apo_concurrency[5m])` - Average concurrent tasks
- `max_over_time(dra_apo_concurrency[5m])` - Peak concurrent tasks

**Thresholds:**
- Max near limit (10) = need to scale up
- Avg < 2 for extended period = over-provisioned

### 6. VERL Verifications (5m window)
**Metrics:**
- `increase(dra_apo_verifications{success="true"}[5m])` - Successful verifications
- `increase(dra_apo_verifications{success="false"}[5m])` - Failed verifications

**Interpretation:**
- High failure rate = quality issues in outputs
- Balanced strategy should have ~100% verification
- HighPerformance strategy = 0 verifications (expected)

### 7. APO Task Success Rate
**Formula:**
```
rate(dra_apo_tasks_completed[5m]) / 
(rate(dra_apo_tasks_completed[5m]) + rate(dra_apo_tasks_failed[5m])) * 100
```

**Thresholds:**
- Green: > 99%
- Yellow: 95-99%
- Red: < 95%

### 8. APO Performance by Strategy
**Table showing:**
- Submitted/s per strategy
- Completed/s per strategy
- Failed/s per strategy

**Use Cases:**
- Compare strategy performance
- Identify which strategies are being used
- Detect strategy-specific issues

## Alerts

### Critical Alerts

#### ApoVeryHighLatency
- **Condition:** P95 latency > 10s for 1 minute
- **Action:** Immediate investigation required
- **Common Causes:**
  - Lightning server overloaded
  - Network issues
  - Database bottleneck

#### ApoCriticalFailureRate
- **Condition:** Failure rate > 25% for 1 minute
- **Action:** Stop deployments, investigate immediately
- **Common Causes:**
  - Lightning server down
  - Configuration error
  - Breaking code change

#### LightningServerDown
- **Condition:** Lightning server unreachable for 1 minute
- **Action:** Restart Lightning server
- **Impact:** All APO functionality offline

### Warning Alerts

#### ApoHighLatency
- **Condition:** P95 latency > 5s for 2 minutes
- **Action:** Monitor closely, prepare to scale up
- **Common Causes:**
  - Load increasing
  - Concurrency limit reached

#### ApoHighFailureRate
- **Condition:** Failure rate > 10% for 3 minutes
- **Action:** Investigate error logs
- **Common Causes:**
  - Intermittent network issues
  - Validation failures

#### ApoHighRetryRate
- **Condition:** > 10 retries/sec for 5 minutes
- **Action:** Check Lightning server health
- **Common Causes:**
  - Server degraded
  - Network latency

#### ApoConcurrencyLimitReached
- **Condition:** Max concurrency = 10 for 2 minutes
- **Action:** Scale up immediately
- **Impact:** Tasks queuing, increased latency

### Info Alerts

#### ApoConcurrencyLimitApproaching
- **Condition:** Max concurrency > 8 for 5 minutes
- **Action:** Plan to scale up soon

#### ApoScaleUpRecommended
- **Condition:** High concurrency + backlog for 5 minutes
- **Action:** Consider scaling up

#### ApoScaleDownRecommended
- **Condition:** Low concurrency < 2 for 30 minutes
- **Action:** Consider scaling down to save resources

## Configuring Alerts

### Email Notifications

Edit `monitoring/alertmanager/alertmanager.yml`:

```yaml
global:
  smtp_smarthost: 'smtp.gmail.com:587'
  smtp_from: 'alerts@example.com'
  smtp_auth_username: 'your-email@gmail.com'
  smtp_auth_password: 'your-app-password'
  smtp_require_tls: true

receivers:
  - name: 'critical-alerts'
    email_configs:
      - to: 'oncall@example.com'
```

### Slack Notifications

```yaml
receivers:
  - name: 'critical-alerts'
    slack_configs:
      - api_url: 'YOUR_SLACK_WEBHOOK_URL'
        channel: '#critical-alerts'
        title: '🚨 Critical APO Alert'
        text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
```

### Microsoft Teams

```yaml
receivers:
  - name: 'critical-alerts'
    webhook_configs:
      - url: 'YOUR_TEAMS_WEBHOOK_URL'
        send_resolved: true
```

## Custom Queries

### Top 10 Slowest Operations
```promql
topk(10, dra_apo_task_latency_ms)
```

### Throughput per Strategy
```promql
sum by (strategy) (rate(dra_apo_tasks_completed[5m]))
```

### Error Rate Trend (1h)
```promql
rate(dra_apo_tasks_failed[1h]) / rate(dra_apo_tasks_submitted[1h]) * 100
```

### Concurrency Utilization
```promql
avg_over_time(dra_apo_concurrency[5m]) / 10 * 100
```

## Troubleshooting

### No Data in Grafana

1. **Check Prometheus is scraping:**
   ```bash
   curl http://localhost:9091/api/v1/targets
   ```
   Should show `deep-research-agent` target with `state: up`

2. **Check Deep Research Agent metrics endpoint:**
   ```bash
   curl http://localhost:9090/metrics | grep apo
   ```
   Should show APO metrics

3. **Check Grafana datasource:**
   - Navigate to Configuration → Data Sources
   - Click on Prometheus
   - Click "Test" button
   - Should show "Data source is working"

### Alerts Not Firing

1. **Check Prometheus rules:**
   ```bash
   curl http://localhost:9091/api/v1/rules
   ```

2. **Check Alertmanager:**
   ```bash
   curl http://localhost:9093/api/v2/status
   ```

3. **Verify alert configuration:**
   ```bash
   docker exec deep-research-prometheus promtool check config /etc/prometheus/prometheus.yml
   ```

### High Memory Usage

Prometheus retention is set to 30 days by default. To reduce:

Edit `docker-compose.yml`:
```yaml
command:
  - '--storage.tsdb.retention.time=7d'  # Change from 30d
```

## Production Recommendations

### 1. Secure Grafana
```yaml
environment:
  - GF_SECURITY_ADMIN_PASSWORD=STRONG_PASSWORD_HERE
  - GF_SERVER_CERT_FILE=/etc/ssl/certs/grafana.crt
  - GF_SERVER_CERT_KEY=/etc/ssl/private/grafana.key
```

### 2. Persistent Volumes
Current setup uses Docker volumes. For production, use named volumes or host mounts:
```yaml
volumes:
  - /opt/prometheus/data:/prometheus
  - /opt/grafana/data:/var/lib/grafana
```

### 3. Resource Limits
```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 4G
```

### 4. High Availability
For production, run multiple Prometheus instances with remote write to Thanos/Cortex.

### 5. Backup
```bash
# Backup Grafana dashboards
docker exec deep-research-grafana grafana-cli admin export-dashboards > backup.json

# Backup Prometheus data
docker exec deep-research-prometheus tar czf - /prometheus > prometheus-backup.tar.gz
```

## Advanced Configuration

### Custom Dashboard

Create new dashboard in `monitoring/grafana/dashboards/`:
```json
{
  "title": "Custom APO Dashboard",
  "panels": [...]
}
```

Dashboard will auto-import on Grafana restart.

### Recording Rules

Add to `prometheus/prometheus.yml` for frequently-used queries:
```yaml
groups:
  - name: apo_recording
    interval: 30s
    rules:
      - record: apo:task_success_rate
        expr: |
          rate(dra_apo_tasks_completed[5m]) / 
          (rate(dra_apo_tasks_completed[5m]) + rate(dra_apo_tasks_failed[5m])) * 100
```

## Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Review Prometheus targets: http://localhost:9091/targets
3. Check alert rules: http://localhost:9091/alerts
4. Review Alertmanager: http://localhost:9093

## Summary

This monitoring stack provides:
- ✅ Real-time APO performance visualization
- ✅ 16 pre-configured alerts for critical issues
- ✅ 8 comprehensive dashboard panels
- ✅ Automated email/Slack/Teams notifications
- ✅ 30-day metric retention
- ✅ Production-ready configuration

All metrics automatically flow from your Deep Research Agent to Grafana with zero code changes required!
