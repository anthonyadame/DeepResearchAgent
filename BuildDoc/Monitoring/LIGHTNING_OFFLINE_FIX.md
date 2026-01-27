# Quick Fix: Lightning Server "Offline" Warning

## What You're Seeing

In your Grafana dashboard, you see this warning:

```
┌─────────────────────────────────────┐
│  Server connection                  │
│  Offline                            │
│  http://localhost:8090              │
└─────────────────────────────────────┘
```

---

## Why This Happens

The monitoring stack is configured to scrape metrics from two sources:

1. ✅ **Deep Research Agent** (port 9090) - **REQUIRED** - This is working!
2. ⚠️  **Lightning Server** (port 8090) - **OPTIONAL** - This is offline!

**Good News:** You don't need the Lightning Server! The Deep Research Agent has built-in APO and all metrics work without it.

---

## 30-Second Fix

### Windows PowerShell
```powershell
cd monitoring
.\disable-lightning-server.ps1
```

### Linux/macOS
```bash
cd monitoring
chmod +x disable-lightning-server.sh
./disable-lightning-server.sh
```

### What It Does
- Comments out the Lightning Server scrape config
- Restarts Prometheus
- Removes the "Offline" warning

---

## Manual Fix (If Scripts Don't Work)

### Step 1: Edit prometheus.yml

Open `monitoring/prometheus/prometheus.yml` and find this section:

```yaml
  # Lightning Server metrics (if exposed)
  - job_name: 'lightning-server'
    metrics_path: '/metrics'
    static_configs:
      - targets: ['host.docker.internal:8090']
        labels:
          instance: 'lightning-server-1'
          environment: 'development'
```

### Step 2: Comment It Out

Add `#` at the beginning of each line:

```yaml
  # Lightning Server metrics (if exposed)
  # - job_name: 'lightning-server'
  #   metrics_path: '/metrics'
  #   static_configs:
  #     - targets: ['host.docker.internal:8090']
  #       labels:
  #         instance: 'lightning-server-1'
  #         environment: 'development'
```

### Step 3: Restart Prometheus

```bash
docker-compose restart prometheus
```

### Step 4: Wait & Refresh

Wait 30 seconds, then refresh your Grafana dashboard. The warning should be gone!

---

## Verification

### Check Prometheus Targets

Visit: http://localhost:9091/targets

You should see:

✅ **deep-research-agent** - State: UP  
~~❌ lightning-server - State: DOWN~~ (removed)

### Check Grafana

Visit: http://localhost:3000

No more "Offline" warnings!

---

## Do I Need Lightning Server?

### Short Answer: NO

The Deep Research Agent works perfectly without it.

### Long Answer

**Lightning Server** is an optional external orchestration service for advanced scenarios like:
- Multi-agent distributed systems
- Cross-machine workload distribution
- Centralized agent management

**For most users:** You're running a single instance of Deep Research Agent with built-in APO. Lightning Server adds no value and isn't running by default.

---

## What If I Want to Use Lightning Server Later?

No problem! Just:

1. Restore the backup config:
   ```bash
   cp prometheus/prometheus.yml.bak prometheus/prometheus.yml
   ```

2. Start Lightning Server:
   ```bash
   cd lightning-server
   docker-compose up -d
   ```

3. Restart Prometheus:
   ```bash
   cd monitoring
   docker-compose restart prometheus
   ```

---

## Still Having Issues?

See the comprehensive guide:
- **[TROUBLESHOOTING_LIGHTNING.md](TROUBLESHOOTING_LIGHTNING.md)** - Detailed troubleshooting
- **[README.md](README.md)** - Full monitoring documentation
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Quick reference card

---

## Summary

✅ The "Offline" warning is **harmless** - your APO monitoring works fine  
✅ Lightning Server is **optional** - not needed for Deep Research Agent  
✅ **Quick fix:** Run `disable-lightning-server.ps1` or `.sh`  
✅ All your APO metrics come from the Deep Research Agent itself  

Don't worry, your monitoring is working perfectly! 🎉
