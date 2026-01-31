# Dashboard Troubleshooting Guide

## ❌ Dashboard Not Appearing in Grafana

### Check 1: Verify Container is Running

```powershell
docker ps
```

**Expected output:**
```
CONTAINER ID   IMAGE                        STATUS
abc123         grafana/grafana:10.1.5      Up 2 minutes
def456         prom/prometheus:v2.47.0     Up 2 minutes
```

**Fix if container is not running:**
```powershell
cd monitoring
docker-compose up -d
```

---

### Check 2: View Grafana Logs

```powershell
docker logs deep-research-grafana
```

**Look for:**
```
✅ "provisioning dashboards from configuration"
✅ "dashboard successfully provisioned"
```

**If you see errors:**
```
❌ "failed to provision dashboards"
❌ "no dashboard provider found"
```

**Fix:**
```powershell
docker-compose restart grafana
```

---

### Check 3: Verify Dashboard Files Exist

**Windows:**
```powershell
dir monitoring\grafana\dashboards\
```

**Linux/macOS:**
```bash
ls -la monitoring/grafana/dashboards/
```

**Expected files:**
```
dashboard-provider.yml   # Provisioning config
apo-performance.json     # Dashboard definition
```

---

### Check 4: Manual Dashboard Import

If auto-provisioning fails, import manually:

1. Open Grafana: http://localhost:3001
2. Click **"+"** → **"Import dashboard"**
3. Click **"Upload JSON file"**
4. Select: `monitoring/grafana/dashboards/apo-performance.json`
5. Select datasource: **Prometheus**
6. Click **"Import"**

---

### Check 5: Verify Datasource is Connected

1. Open Grafana: http://localhost:3001
2. Go to: **☰** → **Administration** → **Data sources**
3. Click **"Prometheus"**
4. Scroll down and click **"Test"**
5. Should see: **✅ "Data source is working"**

**If datasource fails:**
```powershell
docker-compose restart prometheus grafana
```

---

## ❌ Dashboard Shows "No Data"

### Check 1: Is Deep Research Agent Running?

```powershell
# In DeepResearchAgent directory
dotnet run
```

**Agent should show:**
```
✓ OpenTelemetry metrics exporting at http://localhost:9090/
```

---

### Check 2: Verify Metrics are Being Exported

```powershell
curl http://localhost:9090/metrics
```

**Expected output:**
```
# HELP dra_apo_tasks_submitted APO tasks submitted
# TYPE dra_apo_tasks_submitted counter
dra_apo_tasks_submitted{strategy="parallel"} 5

# HELP dra_apo_task_latency_ms APO task latency
# TYPE dra_apo_task_latency_ms histogram
dra_apo_task_latency_ms_bucket{le="100"} 3
```

**If no metrics:**
- Agent is not running
- Metrics endpoint not configured
- Check `Program.cs` for OpenTelemetry setup

---

### Check 3: Is Prometheus Scraping the Endpoint?

1. Open Prometheus: http://localhost:9091
2. Go to: **Status** → **Targets**
3. Find target: **deep-research-agent**
4. Status should be: **🟢 UP**

**If status is 🔴 DOWN:**
- Verify agent is running
- Check `prometheus.yml` has correct scrape config:
  ```yaml
  scrape_configs:
    - job_name: 'deep-research-agent'
      static_configs:
        - targets: ['host.docker.internal:9090']
  ```

**Fix:**
```powershell
docker-compose restart prometheus
```

---

### Check 4: Query Metrics Directly in Prometheus

1. Open Prometheus: http://localhost:9091
2. Enter query: `dra_apo_tasks_submitted`
3. Click **"Execute"**

**If query returns no data:**
- Agent hasn't executed any workflows yet
- Run a workflow to generate metrics

**If query shows data:**
- Prometheus is working
- Issue is with Grafana dashboard queries
- Try refreshing Grafana dashboard

---

## ❌ Dashboard Panels Show Errors

### Error: "Datasource not found"

**Fix:**
1. Edit panel (click panel title → Edit)
2. Change datasource to: **Prometheus**
3. Click **"Apply"**
4. Save dashboard

---

### Error: "Bad Gateway" or "502"

**Fix:**
```powershell
docker-compose restart grafana prometheus
```

---

### Error: "Query timeout"

**Cause:** Time range too large or too much data

**Fix:**
1. Reduce time range (top-right): 15m → 5m
2. Increase query timeout in Grafana:
   - Go to: **Configuration** → **Data sources** → **Prometheus**
   - Increase **Timeout** to 60 seconds
   - Click **"Save & test"**

---

## 🔄 Complete Reset

If all else fails, nuclear option:

```powershell
cd monitoring

# Stop and remove all containers and volumes
docker-compose down -v

# Remove any stuck data
docker volume rm monitoring_grafana-data
docker volume rm monitoring_prometheus-data

# Start fresh
docker-compose up -d

# Wait 30 seconds
Start-Sleep -Seconds 30

# Check logs
docker logs deep-research-grafana
```

Then manually import dashboard as described in "Check 4: Manual Dashboard Import" above.

---

## 📞 Still Having Issues?

### Collect Diagnostic Info

```powershell
# Container status
docker ps

# Grafana logs
docker logs deep-research-grafana > grafana.log

# Prometheus logs
docker logs deep-research-prometheus > prometheus.log

# Network connectivity
curl http://localhost:3001/api/health
curl http://localhost:9091/-/healthy
curl http://localhost:9090/metrics
```

### Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Port 3001 already in use | Change port in `docker-compose.yml` or stop conflicting service |
| Dashboard shows old data | Click 🔄 refresh icon or change time range |
| Metrics delayed | Normal - Prometheus scrapes every 15s, allow 30s for data to appear |
| "Error reading Prometheus" | Restart Prometheus: `docker-compose restart prometheus` |

---

## ✅ Verification Checklist

Before reporting issues, verify:

- [ ] Docker Desktop is running
- [ ] All 3 containers are **Up**: `docker ps`
- [ ] Grafana accessible: http://localhost:3001
- [ ] Prometheus accessible: http://localhost:9091
- [ ] Agent metrics endpoint working: `curl http://localhost:9090/metrics`
- [ ] Prometheus target **UP**: http://localhost:9091/targets
- [ ] Dashboard file exists: `monitoring/grafana/dashboards/apo-performance.json`
- [ ] Ran setup script: `.\setup.ps1` or `./setup.sh`

---

## 🎯 Expected Behavior

**After running setup script:**
1. 3 Docker containers start (Grafana, Prometheus, Alertmanager)
2. Grafana is accessible at http://localhost:3001
3. Dashboard **"APO Performance"** appears in folder **"Deep Research Agent"**
4. Datasource **"Prometheus"** is pre-configured
5. Panels show "No data" until you run the agent

**After running Deep Research Agent:**
1. Metrics appear at http://localhost:9090/metrics
2. Prometheus scrapes metrics every 15 seconds
3. Dashboard panels populate with data within 30 seconds
4. Data updates every 5 seconds (auto-refresh)

If this flow doesn't work, use the troubleshooting steps above! 🔧
