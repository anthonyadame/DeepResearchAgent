# Troubleshooting: Lightning Server Connection Issues

## Symptom

Grafana dashboard shows:
```
Server connection
Offline
http://localhost:8090
```

Or Prometheus Targets page shows `lightning-server` as **DOWN**.

---

## Root Cause

The Lightning Server is either:
1. Not running
2. Not exposing metrics on `/metrics` endpoint
3. Running but not accessible from Docker container
4. Blocked by firewall

---

## Quick Diagnosis

### Step 1: Check if Lightning Server is Running

**From your host machine:**
```bash
# Check if port 8090 is listening
netstat -an | grep 8090   # Linux/macOS
netstat -an | findstr 8090   # Windows

# Or use curl
curl http://localhost:8090/health
```

**Expected:** Connection successful or HTTP response  
**If fails:** Lightning Server is not running → See "Solution 1"

### Step 2: Check if Metrics Endpoint Exists

```bash
curl http://localhost:8090/metrics
```

**Expected:** Prometheus-formatted metrics  
**If fails:** Metrics not exposed → See "Solution 2"

### Step 3: Check Docker Networking

**From inside Prometheus container:**
```bash
docker exec deep-research-prometheus wget -qO- http://host.docker.internal:8090/metrics
```

**Expected:** Metrics output  
**If fails:** Docker networking issue → See "Solution 3"

---

## Solutions

### Solution 1: Lightning Server Not Running

The Lightning Server is **optional** for the Deep Research Agent APO monitoring. If you don't have a Lightning Server:

**Option A: Disable Lightning Server Monitoring**

Edit `monitoring/prometheus/prometheus.yml`:

```yaml
# Comment out or remove the lightning-server job
# scrape_configs:
#   - job_name: 'lightning-server'
#     ...
```

Restart Prometheus:
```bash
docker-compose restart prometheus
```

**Option B: Start Lightning Server**

If you have the Lightning Server code:

```bash
cd lightning-server
docker-compose up -d
```

Or if running locally:
```bash
cd lightning-server
dotnet run --urls="http://localhost:8090"
```

### Solution 2: Metrics Endpoint Not Configured

The Lightning Server needs to expose Prometheus metrics.

**Check if Lightning Server has OpenTelemetry configured:**

`lightning-server/Program.cs` should have:
```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("LightningServer.Metrics")
    .AddPrometheusExporter(options =>
    {
        options.StartHttpListener = true;
        options.HttpListenerPrefixes = new[] { "http://localhost:8090/" };
    })
    .Build();
```

**If missing:** Lightning Server doesn't expose metrics → Use Solution 1A (disable monitoring)

### Solution 3: Docker Networking Issues

#### Windows

**Issue:** `host.docker.internal` not resolving

**Fix:**
1. Update Docker Desktop to latest version
2. Enable "Use the WSL 2 based engine" in Docker Desktop settings
3. Restart Docker Desktop

**Alternative:** Use host IP instead of `host.docker.internal`

Find your host IP:
```powershell
ipconfig | findstr IPv4
```

Edit `monitoring/prometheus/prometheus.yml`:
```yaml
- targets: ['YOUR_HOST_IP:8090']  # e.g., 192.168.1.100:8090
```

#### Linux

**Issue:** `host.docker.internal` not available

**Fix:** Docker Compose already adds `extra_hosts` mapping. If still failing:

```bash
# Find host IP on Docker bridge
ip addr show docker0

# Use that IP in prometheus.yml
- targets: ['172.17.0.1:8090']  # Common Docker bridge IP
```

#### macOS

Should work out of the box. If failing:

```bash
# Test connectivity
docker run --rm --add-host=host.docker.internal:host-gateway curlimages/curl \
  curl http://host.docker.internal:8090/metrics
```

### Solution 4: Firewall Blocking Connection

**Windows Firewall:**
```powershell
# Allow port 8090 inbound
New-NetFirewallRule -DisplayName "Lightning Server" -Direction Inbound -LocalPort 8090 -Protocol TCP -Action Allow
```

**Linux Firewall (ufw):**
```bash
sudo ufw allow 8090/tcp
```

**macOS Firewall:**
System Preferences → Security & Privacy → Firewall → Firewall Options → Allow Lightning Server

---

## Verification

After applying fixes, verify:

### 1. Check Prometheus Targets
Visit: http://localhost:9091/targets

**lightning-server** should show:
- State: **UP**
- Labels: `instance="lightning-server-1"`
- Last Scrape: Recent timestamp

### 2. Check Grafana Dashboard
Visit: http://localhost:3000

The "Server connection Offline" warning should disappear.

### 3. Query Metrics Directly

In Prometheus (http://localhost:9091):

Execute query:
```promql
up{job="lightning-server"}
```

**Expected result:** `1` (server is up)

---

## Making Lightning Server Optional (Recommended)

Since the Deep Research Agent works fine without Lightning Server, you can configure Prometheus to treat it as optional:

### Edit `monitoring/prometheus/prometheus.yml`

```yaml
  # Lightning Server metrics (OPTIONAL - won't cause alerts if down)
  - job_name: 'lightning-server'
    metrics_path: '/metrics'
    scrape_interval: 30s
    scrape_timeout: 10s
    static_configs:
      - targets: ['host.docker.internal:8090']
        labels:
          instance: 'lightning-server-1'
          environment: 'development'
          optional: 'true'  # Mark as optional
```

### Disable Lightning Server Down Alert

Edit `monitoring/prometheus/alerts/apo-alerts.yml`:

Comment out or remove:
```yaml
# - alert: LightningServerDown
#   expr: up{job="lightning-server"} == 0
#   for: 1m
#   ...
```

Or add a condition:
```yaml
  - alert: LightningServerDown
    expr: up{job="lightning-server",optional!="true"} == 0
    for: 1m
```

### Restart Prometheus

```bash
docker-compose restart prometheus
```

---

## Architecture Clarification

```
┌─────────────────────────────────────┐
│  Deep Research Agent (Required)     │
│  - Exposes metrics: :9090/metrics   │ ──► Prometheus scrapes this
│  - APO functionality built-in       │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Lightning Server (OPTIONAL)        │
│  - External orchestration service   │ ──► Optional scrape
│  - Not required for APO to work     │
└─────────────────────────────────────┘
```

**Key Point:** The Deep Research Agent has **built-in APO capabilities** and doesn't require an external Lightning Server for core functionality.

---

## Quick Fix Commands

### Disable Lightning Server Monitoring Entirely

```bash
cd monitoring

# Backup original config
cp prometheus/prometheus.yml prometheus/prometheus.yml.bak

# Remove lightning-server scrape config
sed -i '/job_name: .lightning-server/,/environment: .development/d' prometheus/prometheus.yml

# Restart
docker-compose restart prometheus
```

### Verify Fix

```bash
# Check Prometheus targets
curl http://localhost:9091/api/v1/targets | jq '.data.activeTargets[] | select(.job=="lightning-server")'

# Should return empty if successfully removed
```

---

## FAQ

**Q: Do I need Lightning Server for APO to work?**  
A: No. The Deep Research Agent has APO built-in. Lightning Server is for advanced distributed orchestration scenarios.

**Q: Will the dashboard still work without Lightning Server?**  
A: Yes! All APO metrics come from the Deep Research Agent itself. The dashboard will work perfectly.

**Q: Should I remove the lightning-server scrape config?**  
A: Recommended if you don't plan to use Lightning Server. Prevents unnecessary "DOWN" warnings.

**Q: Can I add Lightning Server later?**  
A: Absolutely! Just uncomment the scrape config and start the server. Prometheus will auto-detect it.

---

## Summary Checklist

- [ ] Confirmed Deep Research Agent is running (port 9090)
- [ ] Decided: Do you need Lightning Server? (Usually NO)
- [ ] If NO: Removed/commented lightning-server from prometheus.yml
- [ ] If YES: Started Lightning Server and verified port 8090
- [ ] Restarted Prometheus: `docker-compose restart prometheus`
- [ ] Verified targets: http://localhost:9091/targets
- [ ] Checked dashboard: http://localhost:3000
- [ ] No more "Offline" warnings

---

**For most users:** The recommended solution is to **disable Lightning Server monitoring** since it's optional and not running by default.
