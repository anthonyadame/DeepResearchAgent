# APO Monitoring - Quick Reference Card

## 🚀 Quick Start (30 seconds)

```bash
cd monitoring && ./setup.sh  # or setup.ps1 on Windows
```
**Access:** http://localhost:3000 (admin/admin)

---

## 📊 Key Metrics at a Glance

| Metric | Healthy | Warning | Critical |
|--------|---------|---------|----------|
| **P95 Latency** | < 1s | 1-5s | > 5s |
| **Success Rate** | > 99% | 95-99% | < 95% |
| **Concurrency** | 2-7 | 8-9 | 10 (max) |
| **Retry Rate** | < 1/sec | 1-10/sec | > 10/sec |

---

## 🎯 Dashboard Panels

| # | Panel | What to Watch |
|---|-------|---------------|
| 1 | **Throughput** | Submitted > Completed = backlog |
| 2 | **Latency Gauge** | P95 > 5000ms = investigate |
| 3 | **Latency Trend** | Upward trend = degradation |
| 4 | **Retries** | Spikes = server issues |
| 5 | **Concurrency** | Near 10 = scale up |
| 6 | **VERL** | Failures = quality issues |
| 7 | **Success Rate** | < 95% = critical |
| 8 | **By Strategy** | Compare performance |

---

## 🚨 Alert Reference

### Critical (Immediate Action)
- ⚠️ **ApoVeryHighLatency** - P95 > 10s
- ⚠️ **ApoCriticalFailureRate** - >25% failed
- ⚠️ **LightningServerDown** - Server offline

### Warning (Investigate Soon)
- ⚠️ **ApoHighLatency** - P95 > 5s
- ⚠️ **ApoHighFailureRate** - >10% failed
- ⚠️ **ApoConcurrencyLimitReached** - At max

### Info (Monitor)
- ℹ️ **ApoScaleUpRecommended** - Consider scaling
- ℹ️ **ApoScaleDownRecommended** - Over-provisioned

---

## 🔍 Useful Queries

### Current Throughput
```promql
rate(dra_apo_tasks_completed[5m])
```

### P95 Latency (Last Hour)
```promql
histogram_quantile(0.95, rate(dra_apo_task_latency_ms_bucket[1h]))
```

### Success Rate
```promql
rate(dra_apo_tasks_completed[5m]) / 
(rate(dra_apo_tasks_completed[5m]) + rate(dra_apo_tasks_failed[5m])) * 100
```

### Top Strategies by Throughput
```promql
topk(5, sum by (strategy) (rate(dra_apo_tasks_completed[5m])))
```

---

## 🛠️ Common Commands

### Start Stack
```bash
cd monitoring && docker-compose up -d
```

### Stop Stack
```bash
docker-compose down
```

### Disable Lightning Server Monitoring (if showing "Offline")
```bash
# Windows
.\disable-lightning-server.ps1

# Linux/macOS
chmod +x disable-lightning-server.sh
./disable-lightning-server.sh
```

### View Logs
```bash
docker-compose logs -f grafana
docker-compose logs -f prometheus
```

### Restart Service
```bash
docker-compose restart grafana
```

### Check Health
```bash
curl http://localhost:9091/-/healthy  # Prometheus
curl http://localhost:3000/api/health # Grafana
```

---

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| **No data** | Check agent running: `curl http://localhost:9090/metrics` |
| **Panels empty** | Verify Prometheus targets: http://localhost:9091/targets |
| **Alerts silent** | Check Alertmanager: http://localhost:9093/#/alerts |
| **High memory** | Reduce retention: `--storage.tsdb.retention.time=7d` |

---

## 📍 Service Endpoints

| Service | URL | Purpose |
|---------|-----|---------|
| **Grafana** | http://localhost:3000 | Dashboards |
| **Prometheus** | http://localhost:9091 | Metrics & queries |
| **Alertmanager** | http://localhost:9093 | Alert management |
| **Agent Metrics** | http://localhost:9090/metrics | Raw metrics |

**Default Login:** admin/admin (change immediately!)

---

## 🎨 Strategy Performance Guide

| Strategy | Expected P95 | Concurrency | VERL |
|----------|--------------|-------------|------|
| **HighPerformance** | < 500ms | High | Disabled |
| **Balanced** | < 1000ms | Medium | Enabled |
| **LowResource** | < 2000ms | Low | Enabled |
| **CostOptimized** | < 1500ms | Medium | Enabled |

---

## 💡 Quick Tips

✅ **Auto-refresh** Set to 5s for real-time monitoring  
✅ **Time range** Use 15m-1h for most investigations  
✅ **Annotations** Mark deployments/changes on timeline  
✅ **Alerts** Test with: `curl -XPOST http://localhost:9093/api/v1/alerts`  
✅ **Export** Download dashboard JSON for backup  

---

## 📞 Emergency Response

### P95 Latency > 10s
1. Check concurrency (likely at max)
2. Scale up instances immediately
3. Review recent deployments

### Success Rate < 95%
1. Check Lightning server health
2. Review error logs
3. Verify network connectivity

### Server Down
1. Restart Lightning server
2. Check logs for crash reason
3. Verify resource availability

---

**Full Documentation:** `monitoring/README.md`  
**Alert Config:** `monitoring/prometheus/alerts/apo-alerts.yml`  
**Dashboard Source:** `monitoring/grafana/dashboards/apo-performance.json`

---

*Print this card and keep near your workstation for quick reference!*
