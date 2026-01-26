# Deep Research Agent - Monitoring Stack Setup
# This script sets up Prometheus + Grafana + Alertmanager for APO monitoring

Write-Host "=== Deep Research Agent - Monitoring Stack Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker detected: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not installed. Please install Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is available
try {
    $composeVersion = docker-compose --version
    Write-Host "✓ Docker Compose detected: $composeVersion" -ForegroundColor Green
} catch {
    try {
        $composeVersion = docker compose version
        Write-Host "✓ Docker Compose detected: $composeVersion" -ForegroundColor Green
    } catch {
        Write-Host "❌ Docker Compose is not available." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Navigate to monitoring directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Create necessary directories
Write-Host "Creating directory structure..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "prometheus\alerts" | Out-Null
New-Item -ItemType Directory -Force -Path "grafana\dashboards" | Out-Null
New-Item -ItemType Directory -Force -Path "grafana\datasources" | Out-Null
New-Item -ItemType Directory -Force -Path "alertmanager" | Out-Null

Write-Host "✓ Directory structure created" -ForegroundColor Green
Write-Host ""

# Start the monitoring stack
Write-Host "Starting monitoring stack..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check service health
Write-Host ""
Write-Host "Checking service health..." -ForegroundColor Yellow

# Check Prometheus
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9091/-/healthy" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Prometheus is healthy" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Prometheus health check failed" -ForegroundColor Yellow
}

# Check Grafana
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Grafana is healthy" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Grafana health check failed" -ForegroundColor Yellow
}

# Check Alertmanager
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9093/-/healthy" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Alertmanager is healthy" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Alertmanager health check failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Monitoring Stack Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Access URLs:" -ForegroundColor White
Write-Host "  Grafana:      http://localhost:3000 (admin/admin)" -ForegroundColor White
Write-Host "  Prometheus:   http://localhost:9091" -ForegroundColor White
Write-Host "  Alertmanager: http://localhost:9093" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Navigate to Grafana: http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login with admin/admin (change password when prompted)" -ForegroundColor White
Write-Host "  3. Navigate to Dashboards > Deep Research Agent > APO Performance" -ForegroundColor White
Write-Host "  4. Start your Deep Research Agent with APO enabled" -ForegroundColor White
Write-Host "  5. Watch metrics flow into the dashboard!" -ForegroundColor White
Write-Host ""
Write-Host "To stop the monitoring stack:" -ForegroundColor Yellow
Write-Host "  docker-compose down" -ForegroundColor White
Write-Host ""
Write-Host "To view logs:" -ForegroundColor Yellow
Write-Host "  docker-compose logs -f" -ForegroundColor White
Write-Host ""
