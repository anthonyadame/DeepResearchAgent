# 🚨 Quick Troubleshooting Card

## ❓ Dashboard Not Showing Up

```powershell
# Try these in order:

# 1. Wait a bit longer (provisioning can take 20-30 seconds)
Start-Sleep -Seconds 20
# Then check: http://localhost:3001

# 2. Restart Grafana
docker-compose restart grafana
Start-Sleep -Seconds 15

# 3. Re-run setup (will create via API)
.\setup.ps1

# 4. Check logs
docker-compose logs grafana | Select-String "dashboard"
```

---

## ❓ No Data in Panels

```bash
# Dashboard shows "No data"

# Cause: Agent not running or not executing workflows

# Fix:
cd DeepResearchAgent
dotnet run
# Execute a workflow
# Wait 30 seconds
# Refresh dashboard
```

---

## ❓ Can't Access Grafana

```powershell
# Error: Connection refused to http://localhost:3001

# Check if container is running:
docker ps | Select-String "grafana"

# If not running:
cd monitoring
docker-compose up -d grafana

# If running but still can't access:
docker-compose restart grafana
```

---

## ❓ Prometheus Not Scraping

```powershell
# Dashboard shows no metrics

# 1. Check Prometheus targets
# Open: http://localhost:9091/targets
# deep-research-agent should be UP

# 2. Check agent metrics endpoint
curl http://localhost:9090/metrics

# 3. Restart Prometheus
docker-compose restart prometheus
```

---

## ❓ "Error loading dashboard"

```powershell
# Grafana UI shows error

# Cause: Dashboard JSON corrupted or datasource missing

# Fix 1: Recreate via API
.\setup.ps1

# Fix 2: Manual import
# 1. Open Grafana: http://localhost:3001
# 2. Click + → Import
# 3. Upload: monitoring\grafana\dashboards\apo-performance.json
# 4. Select datasource: Prometheus
# 5. Click Import
```

---

## ❓ Port Already in Use

```powershell
# Error: Port 3001 already in use

# Find what's using port 3001:
netstat -ano | findstr :3001

# Option 1: Stop the other service
# Option 2: Change Grafana port in docker-compose.yml:
#   ports:
#     - "3002:3000"  # Change 3001 to 3002
```

---

## ❓ Docker Not Starting

```powershell
# Error: Docker daemon not running

# Windows:
# 1. Open Docker Desktop
# 2. Wait for it to fully start
# 3. Run setup again

# Linux:
sudo systemctl start docker

# macOS:
# Open Docker Desktop from Applications
```

---

## 🆘 Nuclear Option (Start Fresh)

```powershell
# If nothing else works, reset everything:

cd monitoring

# 1. Stop and remove everything
docker-compose down -v

# 2. Remove volumes
docker volume rm monitoring_grafana-data
docker volume rm monitoring_prometheus-data
docker volume rm monitoring_alertmanager-data

# 3. Start fresh
.\setup.ps1

# 4. Wait 30 seconds
Start-Sleep -Seconds 30

# 5. Open dashboard
start http://localhost:3001
```

---

## ✅ Verify Everything is Working

```powershell
# Run verification script
.\verify.ps1

# Should see:
# ✓ Docker is running
# ✓ All containers running
# ✓ Grafana is healthy
# ✓ Prometheus is healthy
# ✓ Dashboard found
# ✓ Datasource configured
```

---

## 📞 Get Help

| Issue | Command | Expected Output |
|-------|---------|----------------|
| Check Docker | `docker ps` | 3 containers running |
| Check Grafana | `curl http://localhost:3001/api/health` | `{"database":"ok"}` |
| Check Prometheus | `curl http://localhost:9091/-/healthy` | `Prometheus is Healthy.` |
| Check Metrics | `curl http://localhost:9090/metrics` | `dra_*` metrics listed |
| Check Dashboard | `curl http://localhost:3001/api/search` | JSON with dashboards |

---

## 🎯 Quick Health Check

```powershell
# One-liner to check everything:
docker ps; `
curl -s http://localhost:3001/api/health; `
curl -s http://localhost:9091/-/healthy; `
curl -s http://localhost:9090/metrics | Select-String "dra_" | Measure-Object
```

**Expected:**
- 3 containers running
- Grafana API returns health status
- Prometheus returns "Healthy"
- Multiple `dra_` metrics found

---

## 📖 Full Documentation

For detailed troubleshooting, see:
- `monitoring/TROUBLESHOOTING_DASHBOARD.md`
- `monitoring/QUICK_START.md`
- `monitoring/README.md`

---

## 🔄 Common Solutions Summary

| Problem | Quick Fix |
|---------|-----------|
| Dashboard missing | Re-run `.\setup.ps1` |
| No data | Start agent, execute workflow |
| Can't access Grafana | `docker-compose restart grafana` |
| Prometheus not scraping | Check `http://localhost:9091/targets` |
| Port conflict | Change port in docker-compose.yml |
| Everything broken | Nuclear option above ☢️ |

---

**Keep this card handy!** 📌

**Most issues solved with:** `.\setup.ps1` or `docker-compose restart`
