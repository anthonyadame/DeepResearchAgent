# Grafana Dashboard Quick Start

## 🚀 One-Command Setup

### Windows (PowerShell)
```powershell
cd monitoring
.\setup.ps1
```

### Linux/macOS
```bash
cd monitoring
chmod +x setup.sh
./setup.sh
```

## 📊 Access Dashboard

1. **Open Grafana**: http://localhost:3001
2. **Login**:
   - Username: `admin`
   - Password: `admin`
   - *(Change password when prompted)*
3. **Navigate to Dashboard**:
   - Click **"☰"** menu (top-left)
   - Select **"Dashboards"**
   - Click folder **"Deep Research Agent"**
   - Open **"Deep Research Agent - APO Performance"**

## ✅ Verify Dashboard is Working

The dashboard will automatically appear if:
- ✅ Docker is running
- ✅ Grafana container is healthy
- ✅ Prometheus datasource is connected

### Check Container Health
```powershell
docker ps
```

You should see:
```
deep-research-grafana     Up
deep-research-prometheus  Up
deep-research-alertmanager Up
```

### Check Grafana Logs (if dashboard missing)
```powershell
docker logs deep-research-grafana
```

Look for:
```
✅ "provisioning dashboards from configuration"
✅ "dashboard.json successfully provisioned"
```

## 🔧 Troubleshooting

### Dashboard Not Appearing?

**Option 1: Restart Grafana**
```powershell
cd monitoring
docker-compose restart grafana
```

**Option 2: Full Restart**
```powershell
docker-compose down
docker-compose up -d
```

**Option 3: Manual Dashboard Import**

If auto-provisioning fails:
1. Open Grafana: http://localhost:3001
2. Click **"☰"** → **"Dashboards"** → **"Import"**
3. Click **"Upload JSON file"**
4. Select: `monitoring/grafana/dashboards/apo-performance.json`
5. Click **"Import"**

### No Data in Dashboard?

**Check if Deep Research Agent is running:**
```powershell
# In DeepResearchAgent directory
dotnet run
```

**Verify metrics endpoint:**
```powershell
curl http://localhost:9090/metrics
```

You should see metrics like:
```
# HELP dra_apo_tasks_submitted APO tasks submitted
# TYPE dra_apo_tasks_submitted counter
dra_apo_tasks_submitted{strategy="parallel"} 42
```

**Check Prometheus scraping:**
1. Open Prometheus: http://localhost:9091
2. Go to **Status** → **Targets**
3. Verify `deep-research-agent` target is **UP**

## 🎯 Dashboard Panels

Once loaded, you'll see:

### 1️⃣ **APO Task Throughput** (req/s)
Real-time request rate by strategy (submitted/completed/failed)

### 2️⃣ **APO Task Latency** (Gauge)
p50, p95, p99 latency percentiles

### 3️⃣ **Latency Percentiles Over Time** (Graph)
Latency trends (p50, p75, p95, p99)

### 4️⃣ **Retry Attempts** (5m window)
Retry count distribution

### 5️⃣ **APO Concurrency**
Average and max concurrent tasks

### 6️⃣ **VERL Verifications** (5m window)
Successful vs failed verifications

### 7️⃣ **Task Success Rate** (Gauge)
Overall APO success percentage

### 8️⃣ **Performance by Strategy** (Table)
Breakdown by APO strategy

## 🔄 Auto-Refresh

Dashboard auto-refreshes every **5 seconds** by default.

Change refresh rate:
- Top-right dropdown: `5s`, `10s`, `30s`, `1m`, `5m`

## 📈 Time Range

Default: **Last 15 minutes**

Change time range:
- Top-right clock icon
- Select: `5m`, `15m`, `1h`, `6h`, `24h`, `7d`, etc.

## 💾 Save Changes

After customizing panels:
1. Click **💾 Save dashboard** (top-right)
2. Add optional description
3. Click **Save**

## 🔗 Quick Links

- **Grafana**: http://localhost:3001
- **Prometheus**: http://localhost:9091
- **Alertmanager**: http://localhost:9093
- **Metrics Endpoint**: http://localhost:9090/metrics

## 📞 Support

If you encounter issues:
1. Check Docker logs: `docker-compose logs grafana`
2. Verify Prometheus is scraping: http://localhost:9091/targets
3. Check metrics are exported: `curl http://localhost:9090/metrics`
4. Restart monitoring stack: `docker-compose restart`

---

**Next**: Start Deep Research Agent (`dotnet run`) and watch real-time metrics! 🎉
