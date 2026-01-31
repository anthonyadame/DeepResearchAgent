# Monitoring Stack Verification Script
# Run this after setup to verify everything works

Write-Host ""
Write-Host "=== Deep Research Agent - Monitoring Verification ===" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# Test 1: Docker running
Write-Host "1. Checking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "   ✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Docker is not running" -ForegroundColor Red
    $allGood = $false
}

# Test 2: Containers running
Write-Host "2. Checking containers..." -ForegroundColor Yellow
$containers = docker ps --format "{{.Names}}"
$requiredContainers = @("deep-research-grafana", "deep-research-prometheus", "deep-research-alertmanager")

foreach ($container in $requiredContainers) {
    if ($containers -contains $container) {
        Write-Host "   ✓ $container is running" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $container is NOT running" -ForegroundColor Red
        $allGood = $false
    }
}

# Test 3: Grafana health
Write-Host "3. Checking Grafana health..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3001/api/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "   ✓ Grafana API is responding" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Grafana API is not responding" -ForegroundColor Red
    Write-Host "   Try: docker-compose restart grafana" -ForegroundColor Gray
    $allGood = $false
}

# Test 4: Prometheus health
Write-Host "4. Checking Prometheus health..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9091/-/healthy" -UseBasicParsing -TimeoutSec 5
    Write-Host "   ✓ Prometheus is healthy" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Prometheus is not responding" -ForegroundColor Red
    Write-Host "   Try: docker-compose restart prometheus" -ForegroundColor Gray
    $allGood = $false
}

# Test 5: Dashboard provisioned
Write-Host "5. Checking dashboard provisioning..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3001/api/search?type=dash-db" -UseBasicParsing -TimeoutSec 5
    $dashboards = $response.Content | ConvertFrom-Json
    
    $apoDashboard = $dashboards | Where-Object { $_.title -like "*APO Performance*" }
    
    if ($apoDashboard) {
        Write-Host "   ✓ APO Performance dashboard found" -ForegroundColor Green
        Write-Host "   Dashboard UID: $($apoDashboard.uid)" -ForegroundColor Gray
    } else {
        Write-Host "   ✗ APO Performance dashboard not found" -ForegroundColor Red
        Write-Host "   Found $($dashboards.Count) dashboard(s):" -ForegroundColor Gray
        foreach ($dash in $dashboards) {
            Write-Host "   - $($dash.title)" -ForegroundColor Gray
        }
        $allGood = $false
    }
} catch {
    Write-Host "   ✗ Could not query dashboards" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    $allGood = $false
}

# Test 6: Datasource configured
Write-Host "6. Checking Prometheus datasource..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3001/api/datasources" -UseBasicParsing -TimeoutSec 5
    $datasources = $response.Content | ConvertFrom-Json
    
    $promDs = $datasources | Where-Object { $_.type -eq "prometheus" }
    
    if ($promDs) {
        Write-Host "   ✓ Prometheus datasource configured" -ForegroundColor Green
        Write-Host "   URL: $($promDs.url)" -ForegroundColor Gray
    } else {
        Write-Host "   ✗ Prometheus datasource not found" -ForegroundColor Red
        $allGood = $false
    }
} catch {
    Write-Host "   ⚠️  Could not verify datasource (may need authentication)" -ForegroundColor Yellow
}

# Test 7: Agent metrics endpoint (optional)
Write-Host "7. Checking agent metrics endpoint (optional)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090/metrics" -UseBasicParsing -TimeoutSec 3
    if ($response.Content -match "dra_") {
        Write-Host "   ✓ Agent is exporting metrics" -ForegroundColor Green
        
        # Count metrics
        $metricCount = ([regex]::Matches($response.Content, "dra_\w+")).Count
        Write-Host "   Found $metricCount DRA metrics" -ForegroundColor Gray
    } else {
        Write-Host "   ⚠️  Metrics endpoint responding but no DRA metrics yet" -ForegroundColor Yellow
        Write-Host "   Start the agent and execute a workflow to generate metrics" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ⚠️  Agent metrics endpoint not accessible (agent may not be running)" -ForegroundColor Yellow
    Write-Host "   This is OK - start agent when ready: cd DeepResearchAgent && dotnet run" -ForegroundColor Gray
}

# Test 8: Prometheus targets
Write-Host "8. Checking Prometheus targets..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9091/api/v1/targets" -UseBasicParsing -TimeoutSec 5
    $targets = ($response.Content | ConvertFrom-Json).data.activeTargets
    
    $agentTarget = $targets | Where-Object { $_.labels.job -eq "deep-research-agent" }
    
    if ($agentTarget) {
        if ($agentTarget.health -eq "up") {
            Write-Host "   ✓ Agent target is UP" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Agent target is $($agentTarget.health)" -ForegroundColor Yellow
            Write-Host "   This is OK if agent is not running yet" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️  Agent target not found in Prometheus" -ForegroundColor Yellow
        Write-Host "   Check prometheus.yml configuration" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ⚠️  Could not check Prometheus targets" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "=== Verification Summary ===" -ForegroundColor Cyan
Write-Host ""

if ($allGood) {
    Write-Host "🎉 All critical checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Open Grafana: http://localhost:3001" -ForegroundColor White
    Write-Host "2. Login: admin / admin" -ForegroundColor White
    Write-Host "3. Navigate: Dashboards → Deep Research Agent → APO Performance" -ForegroundColor White
    Write-Host "4. Start agent: cd DeepResearchAgent && dotnet run" -ForegroundColor White
    Write-Host "5. Execute a workflow and watch metrics appear!" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "⚠️  Some checks failed. See errors above." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Cyan
    Write-Host "1. Restart services: docker-compose restart" -ForegroundColor White
    Write-Host "2. Check logs: docker-compose logs -f grafana" -ForegroundColor White
    Write-Host "3. See: monitoring/TROUBLESHOOTING_DASHBOARD.md" -ForegroundColor White
    Write-Host ""
}

Write-Host "Quick commands:" -ForegroundColor Cyan
Write-Host "  View logs:      docker-compose logs -f" -ForegroundColor Gray
Write-Host "  Restart:        docker-compose restart" -ForegroundColor Gray
Write-Host "  Stop:           docker-compose down" -ForegroundColor Gray
Write-Host "  Grafana:        http://localhost:3001" -ForegroundColor Gray
Write-Host "  Prometheus:     http://localhost:9091" -ForegroundColor Gray
Write-Host ""
