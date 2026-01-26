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
docker-compose up -d

echo ""
echo "⏳ Waiting for services to start..."
sleep 10

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
if curl -s http://localhost:3000/api/health > /dev/null 2>&1; then
    echo "✓ Grafana is healthy"
else
    echo "⚠️  Grafana health check failed"
fi

# Check Alertmanager
if curl -s http://localhost:9093/-/healthy > /dev/null 2>&1; then
    echo "✓ Alertmanager is healthy"
else
    echo "⚠️  Alertmanager health check failed"
fi

echo ""
echo "=== Monitoring Stack Setup Complete ==="
echo ""
echo "Access URLs:"
echo "  Grafana:      http://localhost:3000 (admin/admin)"
echo "  Prometheus:   http://localhost:9091"
echo "  Alertmanager: http://localhost:9093"
echo ""
echo "Next steps:"
echo "  1. Navigate to Grafana: http://localhost:3000"
echo "  2. Login with admin/admin (change password when prompted)"
echo "  3. Navigate to Dashboards > Deep Research Agent > APO Performance"
echo "  4. Start your Deep Research Agent with APO enabled"
echo "  5. Watch metrics flow into the dashboard!"
echo ""
echo "To stop the monitoring stack:"
echo "  docker-compose down"
echo ""
echo "To view logs:"
echo "  docker-compose logs -f"
echo ""
