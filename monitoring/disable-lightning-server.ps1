# Deep Research Agent - Disable Lightning Server Monitoring
# Run this script if you see "Lightning Server Offline" warnings

Write-Host "=== Disable Lightning Server Monitoring ===" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Check if prometheus.yml exists
if (-not (Test-Path "prometheus\prometheus.yml")) {
    Write-Host "❌ prometheus\prometheus.yml not found" -ForegroundColor Red
    exit 1
}

# Backup original config
Write-Host "📦 Backing up prometheus.yml..." -ForegroundColor Yellow
Copy-Item "prometheus\prometheus.yml" "prometheus\prometheus.yml.bak" -Force
Write-Host "✓ Backup created: prometheus\prometheus.yml.bak" -ForegroundColor Green
Write-Host ""

# Read the config file
Write-Host "🔧 Disabling lightning-server scrape config..." -ForegroundColor Yellow
$content = Get-Content "prometheus\prometheus.yml" -Raw

# Comment out the lightning-server section
$pattern = '(?s)(  # Lightning Server metrics.*?environment: ''development'')'
$content = $content -replace $pattern, {
    param($match)
    $match.Value -split "`n" | ForEach-Object {
        if ($_ -match '^\s{2}[^#]') {
            "  #$_"
        } else {
            $_
        }
    }
} -join "`n"

# Save the modified config
$content | Set-Content "prometheus\prometheus.yml" -NoNewline

Write-Host "✓ Lightning Server monitoring disabled" -ForegroundColor Green
Write-Host ""

# Restart Prometheus if running
$prometheusRunning = docker ps --filter "name=deep-research-prometheus" --format "{{.Names}}"
if ($prometheusRunning) {
    Write-Host "🔄 Restarting Prometheus..." -ForegroundColor Yellow
    docker-compose restart prometheus
    Write-Host "✓ Prometheus restarted" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Prometheus is not running. Start with: docker-compose up -d" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Lightning Server Monitoring Disabled ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Changes made:" -ForegroundColor White
Write-Host "  - Commented out lightning-server scrape config" -ForegroundColor White
Write-Host "  - Backup saved: prometheus\prometheus.yml.bak" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Wait ~30 seconds for Prometheus to reload" -ForegroundColor White
Write-Host "  2. Visit Grafana: http://localhost:3000" -ForegroundColor White
Write-Host "  3. The 'Offline' warning should disappear" -ForegroundColor White
Write-Host ""
Write-Host "To re-enable later:" -ForegroundColor Yellow
Write-Host "  Copy-Item prometheus\prometheus.yml.bak prometheus\prometheus.yml" -ForegroundColor White
Write-Host "  docker-compose restart prometheus" -ForegroundColor White
Write-Host ""
