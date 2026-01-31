# Deep Research Agent - Monitoring Stack Setup (Windows)
# This script sets up Prometheus + Grafana + Alertmanager for APO monitoring

Write-Host "=== Deep Research Agent - Monitoring Stack Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
try {
    docker --version | Out-Null
    Write-Host "✓ Docker detected" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not installed. Please install Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if Docker is running
try {
    docker ps | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Navigate to monitoring directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host ""

# Start the monitoring stack
Write-Host "Starting monitoring stack..." -ForegroundColor Yellow
docker-compose down 2>&1 | Out-Null
docker-compose up -d

Write-Host ""
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check service health
Write-Host ""
Write-Host "Checking service health..." -ForegroundColor Yellow

# Check Prometheus
try {
    $prometheusResponse = Invoke-WebRequest -Uri "http://localhost:9091/-/healthy" -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ Prometheus is healthy" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Prometheus health check failed" -ForegroundColor Yellow
}

# Check Grafana
$grafanaReady = $false
try {
    $grafanaResponse = Invoke-WebRequest -Uri "http://localhost:3001/api/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ Grafana is healthy" -ForegroundColor Green
    $grafanaReady = $true
} catch {
    Write-Host "⚠️  Grafana health check failed" -ForegroundColor Yellow
}

# Check Alertmanager
try {
    $alertmanagerResponse = Invoke-WebRequest -Uri "http://localhost:9093/-/healthy" -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ Alertmanager is healthy" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Alertmanager health check failed" -ForegroundColor Yellow
}

if (-not $grafanaReady) {
    Write-Host ""
    Write-Host "⚠️  Grafana is not responding. Waiting additional 10 seconds..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

# Wait for dashboard auto-provisioning
Write-Host ""
Write-Host "⏳ Waiting for dashboard auto-provisioning..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Verify dashboard was auto-provisioned
Write-Host ""
Write-Host "Verifying dashboard provisioning..." -ForegroundColor Yellow
$dashboardFound = $false

try {
    $dashboardsResponse = Invoke-WebRequest -Uri "http://localhost:3001/api/search?type=dash-db" -UseBasicParsing -TimeoutSec 5
    $dashboards = $dashboardsResponse.Content | ConvertFrom-Json
    
    if ($dashboards.Count -gt 0) {
        $apoDashboard = $dashboards | Where-Object { $_.title -like "*APO Performance*" }
        
        if ($apoDashboard) {
            Write-Host "✓ Dashboard auto-provisioned successfully!" -ForegroundColor Green
            Write-Host "  • $($apoDashboard.title)" -ForegroundColor Cyan
            $dashboardFound = $true
        }
    }
} catch {
    Write-Host "⚠️  Could not verify auto-provisioning" -ForegroundColor Yellow
}

# If auto-provisioning failed, create dashboard via API
if (-not $dashboardFound) {
    Write-Host ""
    Write-Host "⚠️  Auto-provisioning failed. Creating dashboard via API..." -ForegroundColor Yellow
    
    try {
        # Read dashboard JSON file
        $dashboardJsonPath = Join-Path $scriptDir "grafana\dashboards\apo-performance.json"
        
        if (Test-Path $dashboardJsonPath) {
            $dashboardJson = Get-Content $dashboardJsonPath -Raw | ConvertFrom-Json
            
            # Prepare API payload
            $payload = @{
                dashboard = $dashboardJson
                overwrite = $true
                message = "Imported by setup script"
                folderId = 0
            } | ConvertTo-Json -Depth 100
            
            # Create dashboard via API (default credentials admin/admin)
            $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
            $headers = @{
                "Authorization" = "Basic $base64Auth"
                "Content-Type" = "application/json"
            }
            
            $createResponse = Invoke-RestMethod -Uri "http://localhost:3001/api/dashboards/db" `
                -Method Post `
                -Headers $headers `
                -Body $payload `
                -TimeoutSec 10
            
            if ($createResponse.status -eq "success") {
                Write-Host "✓ Dashboard created successfully via API!" -ForegroundColor Green
                Write-Host "  URL: http://localhost:3001$($createResponse.url)" -ForegroundColor Cyan
                $dashboardFound = $true
            } else {
                Write-Host "⚠️  Dashboard creation returned unexpected status: $($createResponse.status)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "✗ Dashboard JSON file not found: $dashboardJsonPath" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to create dashboard via API" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Manual Import Instructions:" -ForegroundColor Yellow
        Write-Host "  1. Open: http://localhost:3001" -ForegroundColor White
        Write-Host "  2. Login: admin / admin" -ForegroundColor White
        Write-Host "  3. Click '+' → 'Import dashboard'" -ForegroundColor White
        Write-Host "  4. Upload: monitoring\grafana\dashboards\apo-performance.json" -ForegroundColor White
        Write-Host "  5. Select datasource: Prometheus" -ForegroundColor White
        Write-Host "  6. Click 'Import'" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Monitoring Stack Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Access URLs:" -ForegroundColor Cyan
Write-Host "  Grafana:      http://localhost:3001 (admin/admin)" -ForegroundColor White
Write-Host "  Prometheus:   http://localhost:9091" -ForegroundColor White
Write-Host "  Alertmanager: http://localhost:9093" -ForegroundColor White
Write-Host ""

if ($dashboardFound) {
    Write-Host "📊 Dashboard Ready!" -ForegroundColor Green
    Write-Host "  1. Open: http://localhost:3001" -ForegroundColor White
    Write-Host "  2. Login with admin/admin (change password when prompted)" -ForegroundColor White
    Write-Host "  3. Navigate: Dashboards → Deep Research Agent → APO Performance" -ForegroundColor White
    Write-Host ""
    Write-Host "  Or go directly to:" -ForegroundColor White
    Write-Host "  http://localhost:3001/d/apo-performance/deep-research-agent-apo-performance" -ForegroundColor Cyan
} else {
    Write-Host "⚠️  Dashboard Setup Incomplete" -ForegroundColor Yellow
    Write-Host "  Follow manual import instructions above" -ForegroundColor White
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Start your Deep Research Agent: cd DeepResearchAgent && dotnet run" -ForegroundColor White
Write-Host "  2. Execute a workflow to generate metrics" -ForegroundColor White
Write-Host "  3. Watch real-time data appear in the dashboard!" -ForegroundColor White
Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Cyan
Write-Host "  View logs:    docker-compose logs -f grafana" -ForegroundColor White
Write-Host "  Restart:      docker-compose restart grafana" -ForegroundColor White
Write-Host "  Verify:       .\verify.ps1" -ForegroundColor White
Write-Host "  Stop:         docker-compose down" -ForegroundColor White
Write-Host ""
Write-Host "📖 For detailed help, see: monitoring/QUICK_START.md" -ForegroundColor Gray
Write-Host ""
