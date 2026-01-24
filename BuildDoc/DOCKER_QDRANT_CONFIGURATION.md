# Docker Configuration - Qdrant Vector Database Setup

**Version**: 0.6.5-beta  
**Status**: ✅ COMPLETE  

---

## 📋 Overview

Qdrant vector database has been added to the Docker configuration for both development and testing environments.

---

## 🐳 Docker Files Updated

### 1. docker-compose.yml (Production/Development)

**Qdrant Service Added**:
```yaml
qdrant:
  image: qdrant/qdrant:latest
  container_name: deep-research-qdrant
  ports:
    - "6333:6333"
  volumes:
    - qdrant_storage:/qdrant/storage
  environment:
    QDRANT_API_KEY: ${QDRANT_API_KEY:-}
    LOG_LEVEL: info
  networks:
    - deep-research-network
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
    interval: 30s
    timeout: 10s
    retries: 3
```

**Volume Added**:
```yaml
volumes:
  qdrant_storage:
    driver: local
```

### 2. docker-compose.test.yml (Testing)

**Qdrant Test Service Added**:
```yaml
qdrant-test:
  image: qdrant/qdrant:latest
  container_name: qdrant-test
  ports:
    - "6334:6333"
  volumes:
    - qdrant_storage_test:/qdrant/storage
  environment:
    LOG_LEVEL: info
  networks:
    - test-network
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
    interval: 5s
    timeout: 3s
    retries: 5
```

**Test Runner Updated**:
- Added `qdrant-test` as dependency
- Added `QDRANT_URL: http://qdrant-test:6333` environment variable

**Volume Added**:
```yaml
volumes:
  qdrant_storage_test:
    driver: local
```

---

## 🚀 Getting Started

### Start All Services (Production)
```bash
docker-compose up -d
```

This will start:
- ✅ Ollama (LLM)
- ✅ SearXNG (Search)
- ✅ Crawl4AI (Web scraping)
- ✅ Lightning Server (Orchestration)
- ✅ Redis (Caching)
- ✅ **Qdrant (Vector Database)** ← NEW

### Start All Services (Testing)
```bash
docker-compose -f docker-compose.test.yml up -d
```

This will start:
- ✅ Lightning Server (Test)
- ✅ **Qdrant Test (Vector Database)** ← NEW
- ✅ Test Runner

---

## 📊 Service Details

### Qdrant Production Configuration

| Setting | Value | Purpose |
|---------|-------|---------|
| **Image** | qdrant/qdrant:latest | Latest Qdrant version |
| **Port** | 6333 | REST API port |
| **Volume** | qdrant_storage | Persistent data storage |
| **API Key** | ${QDRANT_API_KEY:-} | Optional authentication |
| **Network** | deep-research-network | Connected to other services |
| **Health Check** | Every 30 seconds | Service monitoring |

### Qdrant Test Configuration

| Setting | Value | Purpose |
|---------|-------|---------|
| **Image** | qdrant/qdrant:latest | Latest Qdrant version |
| **Port** | 6334 | Test environment port |
| **Volume** | qdrant_storage_test | Isolated test data |
| **Network** | test-network | Test network isolation |
| **Health Check** | Every 5 seconds | Quick test feedback |

---

## 📁 Volume Configuration

### Production Volume
```yaml
qdrant_storage:
  driver: local
```
- **Location**: Docker managed storage
- **Purpose**: Persistent vector database storage
- **Data Persistence**: Yes ✅
- **Cleanup**: `docker volume rm qdrant_storage`

### Test Volume
```yaml
qdrant_storage_test:
  driver: local
```
- **Location**: Docker managed storage (test)
- **Purpose**: Isolated test data storage
- **Data Persistence**: Yes ✅
- **Cleanup**: `docker volume rm qdrant_storage_test`

---

## 🔧 Configuration

### Enable API Key (Optional)

Set environment variable before starting:
```bash
export QDRANT_API_KEY=your-secret-key
docker-compose up -d
```

Or in `.env` file:
```env
QDRANT_API_KEY=your-secret-key
```

### Custom Port Mapping

Edit docker-compose.yml:
```yaml
ports:
  - "6333:6333"  # Change first number to use different host port
```

### Increase Storage

Add size limit (optional):
```yaml
volumes:
  qdrant_storage:
    driver: local
    driver_opts:
      type: tmpfs
      device: tmpfs
      o: size=4g
```

---

## 🏥 Health Checks

### Check Qdrant Health (Production)

**Using Docker**:
```bash
docker-compose ps
```
Output should show `qdrant` with status `healthy`.

**Using curl**:
```bash
curl http://localhost:6333/health
```

Expected response:
```json
{
  "title": "Qdrant",
  "version": "..."
}
```

### Check Qdrant Health (Test)

```bash
curl http://localhost:6334/health
```

---

## 📡 Connection Information

### For Application Code

**Production**:
```csharp
var qdrantUrl = "http://localhost:6333";
// Or use Docker network: "http://qdrant:6333"
```

**Testing**:
```csharp
var qdrantUrl = "http://qdrant-test:6333";
// From test environment variable: Environment.GetEnvironmentVariable("QDRANT_URL")
```

### Configuration File

```json
{
  "VectorDatabase": {
    "Enabled": true,
    "Qdrant": {
      "BaseUrl": "http://localhost:6333"
    }
  }
}
```

---

## 🧹 Cleanup

### Remove Container Only
```bash
docker-compose down
# or for test environment
docker-compose -f docker-compose.test.yml down
```

### Remove Container and Volume
```bash
docker-compose down -v
# Removes container, network, and volumes
```

### Remove Specific Volume
```bash
docker volume rm qdrant_storage
# or
docker volume rm qdrant_storage_test
```

---

## 📋 Network Configuration

### Production Network
```yaml
networks:
  deep-research-network:
    driver: bridge
```

**Connected Services**:
- qdrant (Vector DB)
- ollama (LLM)
- searxng (Search)
- crawl4ai (Web Scraping)
- lightning-server (Orchestration)
- redis (Caching)

### Test Network
```yaml
networks:
  test-network:
    driver: bridge
```

**Connected Services**:
- qdrant-test (Vector DB)
- lightning-server-test (Orchestration)
- test-runner (Tests)

---

## 🔐 Security Considerations

### API Key
- Optional but recommended for production
- Set via environment variable `QDRANT_API_KEY`
- All connections within Docker network (secure by default)

### Network Isolation
- Services only accessible via defined networks
- Not exposed to host except through mapped ports
- Port 6333 exposed for local testing

### Data Security
- Volumes stored in Docker managed storage
- Data persists across container restarts
- Can be backed up via Docker volume backup

---

## 🚨 Troubleshooting

### Container Won't Start

**Check logs**:
```bash
docker logs deep-research-qdrant
```

**Common issues**:
- Port 6333 already in use: Change port mapping
- Insufficient disk space: Check docker volume space
- Permission issues: Run with `sudo` if needed

### Health Check Failing

**Test connection**:
```bash
docker exec deep-research-qdrant curl localhost:6333/health
```

**Check port**:
```bash
docker port deep-research-qdrant
```

### Volume Issues

**List volumes**:
```bash
docker volume ls | grep qdrant
```

**Inspect volume**:
```bash
docker volume inspect qdrant_storage
```

**Clean up dangling volumes**:
```bash
docker volume prune
```

---

## 📊 Performance Tips

### Memory Configuration
Qdrant runs efficiently with default settings. For large datasets:

```yaml
environment:
  QDRANT_RESOURCES: |
    {
      "max_memory_usage": 4gb
    }
```

### Persistence Tuning
```yaml
environment:
  QDRANT_PERSISTENCE: |
    {
      "snapshot_interval_sec": 600,
      "snapshots_to_keep": 3
    }
```

---

## 📚 Documentation Links

- **Qdrant Official**: https://qdrant.tech/
- **Qdrant Docker**: https://hub.docker.com/r/qdrant/qdrant
- **Docker Volumes**: https://docs.docker.com/storage/volumes/
- **Docker Compose**: https://docs.docker.com/compose/

---

## ✅ Verification Checklist

- ✅ docker-compose.yml updated with qdrant service
- ✅ docker-compose.test.yml updated with qdrant-test service
- ✅ Volumes configured with persistence
- ✅ Health checks added
- ✅ Network connectivity configured
- ✅ Documentation complete

---

## 🎯 Next Steps

1. **Start Services**
   ```bash
   docker-compose up -d
   ```

2. **Verify Health**
   ```bash
   curl http://localhost:6333/health
   ```

3. **Configure Application**
   - Update appsettings.json with Qdrant URL
   - Set QDRANT_API_KEY if using authentication

4. **Test Integration**
   ```bash
   dotnet test
   ```

---

## 📝 Summary

Qdrant vector database has been successfully integrated into the Docker infrastructure:

✅ **Production Setup** - Full Qdrant service with persistent storage  
✅ **Test Setup** - Isolated test environment with test volume  
✅ **Networking** - Properly configured for service communication  
✅ **Health Checks** - Automatic health monitoring  
✅ **Documentation** - Complete setup and troubleshooting guide  

---

**Version**: 0.6.5-beta  
**Date**: 2024  
**Status**: ✅ COMPLETE
