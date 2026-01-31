#!/bin/bash

# Monitoring Stack Verification Script
# Run this after setup to verify everything works

echo ""
echo "=== Deep Research Agent - Monitoring Verification ==="
echo ""

allGood=true

# Test 1: Docker running
echo "1. Checking Docker..."
if docker ps > /dev/null 2>&1; then
    echo "   ✓ Docker is running"
else
    echo "   ✗ Docker is not running"
    allGood=false
fi

# Test 2: Containers running
echo "2. Checking containers..."
requiredContainers=("deep-research-grafana" "deep-research-prometheus" "deep-research-alertmanager")

for container in "${requiredContainers[@]}"; do
    if docker ps --format "{{.Names}}" | grep -q "^${container}$"; then
        echo "   ✓ $container is running"
    else
        echo "   ✗ $container is NOT running"
        allGood=false
    fi
done

# Test 3: Grafana health
echo "3. Checking Grafana health..."
if curl -s -f http://localhost:3001/api/health > /dev/null 2>&1; then
    echo "   ✓ Grafana API is responding"
else
    echo "   ✗ Grafana API is not responding"
    echo "   Try: docker-compose restart grafana"
    allGood=false
fi

# Test 4: Prometheus health
echo "4. Checking Prometheus health..."
if curl -s -f http://localhost:9091/-/healthy > /dev/null 2>&1; then
    echo "   ✓ Prometheus is healthy"
else
    echo "   ✗ Prometheus is not responding"
    echo "   Try: docker-compose restart prometheus"
    allGood=false
fi

# Test 5: Dashboard provisioned
echo "5. Checking dashboard provisioning..."
dashboards=$(curl -s http://localhost:3001/api/search?type=dash-db 2>/dev/null || echo "[]")

if echo "$dashboards" | grep -q "APO Performance"; then
    echo "   ✓ APO Performance dashboard found"
else
    echo "   ✗ APO Performance dashboard not found"
    echo "   Found dashboards:"
    echo "$dashboards" | grep -o '"title":"[^"]*"' | cut -d'"' -f4 | sed 's/^/   - /'
    allGood=false
fi

# Test 6: Datasource configured
echo "6. Checking Prometheus datasource..."
datasources=$(curl -s http://localhost:3001/api/datasources 2>/dev/null || echo "[]")

if echo "$datasources" | grep -q '"type":"prometheus"'; then
    echo "   ✓ Prometheus datasource configured"
else
    echo "   ✗ Prometheus datasource not found"
    allGood=false
fi

# Test 7: Agent metrics endpoint (optional)
echo "7. Checking agent metrics endpoint (optional)..."
if curl -s http://localhost:9090/metrics 2>/dev/null | grep -q "dra_"; then
    echo "   ✓ Agent is exporting metrics"
    metricCount=$(curl -s http://localhost:9090/metrics 2>/dev/null | grep -c "^dra_")
    echo "   Found $metricCount DRA metrics"
else
    echo "   ⚠️  Agent metrics endpoint not accessible (agent may not be running)"
    echo "   This is OK - start agent when ready: cd DeepResearchAgent && dotnet run"
fi

# Test 8: Prometheus targets
echo "8. Checking Prometheus targets..."
targets=$(curl -s http://localhost:9091/api/v1/targets 2>/dev/null || echo "{}")

if echo "$targets" | grep -q '"job":"deep-research-agent"'; then
    if echo "$targets" | grep -q '"health":"up"'; then
        echo "   ✓ Agent target is UP"
    else
        echo "   ⚠️  Agent target is down"
        echo "   This is OK if agent is not running yet"
    fi
else
    echo "   ⚠️  Agent target not found in Prometheus"
    echo "   Check prometheus.yml configuration"
fi

# Summary
echo ""
echo "=== Verification Summary ==="
echo ""

if [ "$allGood" = true ]; then
    echo "🎉 All critical checks passed!"
    echo ""
    echo "Next steps:"
    echo "1. Open Grafana: http://localhost:3001"
    echo "2. Login: admin / admin"
    echo "3. Navigate: Dashboards → Deep Research Agent → APO Performance"
    echo "4. Start agent: cd DeepResearchAgent && dotnet run"
    echo "5. Execute a workflow and watch metrics appear!"
    echo ""
else
    echo "⚠️  Some checks failed. See errors above."
    echo ""
    echo "Troubleshooting:"
    echo "1. Restart services: docker-compose restart"
    echo "2. Check logs: docker-compose logs -f grafana"
    echo "3. See: monitoring/TROUBLESHOOTING_DASHBOARD.md"
    echo ""
fi

echo "Quick commands:"
echo "  View logs:      docker-compose logs -f"
echo "  Restart:        docker-compose restart"
echo "  Stop:           docker-compose down"
echo "  Grafana:        http://localhost:3001"
echo "  Prometheus:     http://localhost:9091"
echo ""
