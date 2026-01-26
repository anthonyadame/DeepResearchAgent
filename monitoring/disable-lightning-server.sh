#!/bin/bash

# Deep Research Agent - Disable Lightning Server Monitoring
# Run this script if you see "Lightning Server Offline" warnings

set -e

echo "=== Disable Lightning Server Monitoring ==="
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if prometheus.yml exists
if [ ! -f "prometheus/prometheus.yml" ]; then
    echo "❌ prometheus/prometheus.yml not found"
    exit 1
fi

# Backup original config
echo "📦 Backing up prometheus.yml..."
cp prometheus/prometheus.yml prometheus/prometheus.yml.bak
echo "✓ Backup created: prometheus/prometheus.yml.bak"
echo ""

# Comment out lightning-server scrape config
echo "🔧 Disabling lightning-server scrape config..."
sed -i.tmp '/# Lightning Server metrics/,/environment:.*development/{
    s/^  /  # /
}' prometheus/prometheus.yml

# Clean up temp file
rm -f prometheus/prometheus.yml.tmp

echo "✓ Lightning Server monitoring disabled"
echo ""

# Restart Prometheus if running
if docker ps | grep -q deep-research-prometheus; then
    echo "🔄 Restarting Prometheus..."
    docker-compose restart prometheus
    echo "✓ Prometheus restarted"
else
    echo "ℹ️  Prometheus is not running. Start with: docker-compose up -d"
fi

echo ""
echo "=== Lightning Server Monitoring Disabled ==="
echo ""
echo "Changes made:"
echo "  - Commented out lightning-server scrape config"
echo "  - Backup saved: prometheus/prometheus.yml.bak"
echo ""
echo "Next steps:"
echo "  1. Wait ~30 seconds for Prometheus to reload"
echo "  2. Visit Grafana: http://localhost:3000"
echo "  3. The 'Offline' warning should disappear"
echo ""
echo "To re-enable later:"
echo "  cp prometheus/prometheus.yml.bak prometheus/prometheus.yml"
echo "  docker-compose restart prometheus"
echo ""
