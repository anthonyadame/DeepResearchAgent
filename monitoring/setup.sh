#!/bin/bash

# Deep Research Agent - Monitoring Stack Setup
# This script sets up Prometheus + Grafana + Alertmanager for APO monitoring

set -e

echo "=== Deep Research Agent - Monitoring Stack Setup ==="
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Navigate to monitoring directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "✓ Docker and Docker Compose detected"
echo ""

# Create necessary directories
echo "Creating directory structure..."
mkdir -p prometheus/alerts
mkdir -p grafana/dashboards
mkdir -p grafana/datasources
mkdir -p alertmanager

echo "✓ Directory structure created"
echo ""

# Start the monitoring stack
echo "Starting monitoring stack..."
docker-compose down 2>&1 > /dev/null
docker-compose up -d

echo ""
echo "⏳ Waiting for services to start..."
sleep 15

# Check service health
echo ""
echo "Checking service health..."

# Check Prometheus
if curl -s http://localhost:9091/-/healthy > /dev/null 2>&1; then
    echo "✓ Prometheus is healthy"
else
    echo "⚠️  Prometheus health check failed"
fi

# Check Grafana
grafanaReady=false
if curl -s http://localhost:3001/api/health > /dev/null 2>&1; then
    echo "✓ Grafana is healthy"
    grafanaReady=true
else
    echo "⚠️  Grafana health check failed"
fi

# Check Alertmanager
if curl -s http://localhost:9093/-/healthy > /dev/null 2>&1; then
    echo "✓ Alertmanager is healthy"
else
    echo "⚠️  Alertmanager health check failed"
fi

if [ "$grafanaReady" = false ]; then
    echo ""
    echo "⚠️  Grafana is not responding. Waiting additional 10 seconds..."
    sleep 10
fi

# Wait for dashboard auto-provisioning
echo ""
echo "⏳ Waiting for dashboard auto-provisioning..."
sleep 8

# Verify dashboard was auto-provisioned
echo ""
echo "Verifying dashboard provisioning..."
dashboardFound=false

dashboards=$(curl -s http://localhost:3001/api/search?type=dash-db 2>/dev/null || echo "[]")

if echo "$dashboards" | grep -q "APO Performance"; then
    echo "✓ Dashboard auto-provisioned successfully!"
    echo "$dashboards" | grep -o '"title":"[^"]*APO Performance[^"]*"' | cut -d'"' -f4 | sed 's/^/  • /'
    dashboardFound=true
fi

# If auto-provisioning failed, create dashboard via API
if [ "$dashboardFound" = false ]; then
    echo ""
    echo "⚠️  Auto-provisioning failed. Creating dashboard via API..."
    
    dashboardJsonPath="$SCRIPT_DIR/grafana/dashboards/apo-performance.json"
    
    if [ -f "$dashboardJsonPath" ]; then
        # Prepare API payload
        payload=$(jq -n --slurpfile dash "$dashboardJsonPath" '{
            dashboard: $dash[0],
            overwrite: true,
            message: "Imported by setup script",
            folderId: 0
        }')
        
        # Create dashboard via API (default credentials admin/admin)
        response=$(curl -s -X POST \
            -H "Content-Type: application/json" \
            -u admin:admin \
            -d "$payload" \
            http://localhost:3001/api/dashboards/db 2>/dev/null || echo '{"status":"error"}')
        
        if echo "$response" | grep -q '"status":"success"'; then
            echo "✓ Dashboard created successfully via API!"
            url=$(echo "$response" | grep -o '"url":"[^"]*"' | cut -d'"' -f4)
            echo "  URL: http://localhost:3001$url"
            dashboardFound=true
        else
            echo "⚠️  Dashboard creation via API failed"
        fi
    else
        echo "✗ Dashboard JSON file not found: $dashboardJsonPath"
    fi
fi

# If still not found, show manual instructions
if [ "$dashboardFound" = false ]; then
    echo ""
    echo "Manual Import Instructions:"
    echo "  1. Open: http://localhost:3001"
    echo "  2. Login: admin / admin"
    echo "  3. Click '+' → 'Import dashboard'"
    echo "  4. Upload: monitoring/grafana/dashboards/apo-performance.json"
    echo "  5. Select datasource: Prometheus"
    echo "  6. Click 'Import'"
fi

echo ""
echo "=== Monitoring Stack Setup Complete ==="
echo ""
echo "Access URLs:"
echo "  Grafana:      http://localhost:3001 (admin/admin)"
echo "  Prometheus:   http://localhost:9091"
echo "  Alertmanager: http://localhost:9093"
echo ""

if [ "$dashboardFound" = true ]; then
    echo "📊 Dashboard Ready!"
    echo "  1. Open: http://localhost:3001"
    echo "  2. Login with admin/admin (change password when prompted)"
    echo "  3. Navigate: Dashboards → Deep Research Agent → APO Performance"
    echo ""
    echo "  Or go directly to:"
    echo "  http://localhost:3001/d/apo-performance/deep-research-agent-apo-performance"
else
    echo "⚠️  Dashboard Setup Incomplete"
    echo "  Follow manual import instructions above"
fi

echo ""
echo "Next steps:"
echo "  1. Start your Deep Research Agent: cd DeepResearchAgent && dotnet run"
echo "  2. Execute a workflow to generate metrics"
echo "  3. Watch real-time data appear in the dashboard!"
echo ""
echo "Troubleshooting:"
echo "  View logs:    docker-compose logs -f grafana"
echo "  Restart:      docker-compose restart grafana"
echo "  Verify:       ./verify.sh"
echo "  Stop:         docker-compose down"
echo ""
echo "📖 For detailed help, see: monitoring/QUICK_START.md"
echo ""
