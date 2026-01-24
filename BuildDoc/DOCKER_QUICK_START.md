# Quick Start - Docker with Qdrant Vector Database

**Get the full stack running in minutes**

---

## 🚀 Start Everything (One Command)

```bash
# Production/Development Environment
docker-compose up -d

# Test Environment
docker-compose -f docker-compose.test.yml up
```

---

## ✅ Verify Services are Running

```bash
# Check all containers
docker-compose ps

# Expected output:
# NAME                           STATUS
# deep-research-ollama          healthy
# deep-research-searxng         healthy
# deep-research-crawl4ai        healthy
# deep-research-lightning-server healthy
# deep-research-qdrant          healthy  ← NEW
# redis                         healthy
# caddy                         up
```

---

## 🔍 Quick Health Checks

### Qdrant Vector Database
```bash
# Check Qdrant is running
curl http://localhost:6333/health

# Expected response:
# {"title":"Qdrant","version":"v1.x.x"}
```

### All Services
```bash
# Ollama (LLM)
curl http://localhost:11434/api/health

# SearXNG (Search)
curl http://localhost:8080/healthz

# Crawl4AI (Web Scraping)
curl http://localhost:11235/health

# Lightning Server
curl http://localhost:9090/health

# Qdrant (Vector DB)
curl http://localhost:6333/health
```

---

## 🔌 Connect Your Application

### Update appsettings.json
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

### Run Application
```bash
dotnet run
```

---

## 📊 Service Ports Reference

| Service | Port | URL |
|---------|------|-----|
| Ollama | 11434 | http://localhost:11434 |
| SearXNG | 8080 | http://localhost:8080 |
| Crawl4AI | 11235 | http://localhost:11235 |
| Lightning Server | 9090 | http://localhost:9090 |
| **Qdrant** | **6333** | **http://localhost:6333** |
| Redis | 6379 | localhost:6379 |

---

## 🗂️ Data Persistence

### Volumes Created
```bash
# View all volumes
docker volume ls

# Qdrant storage (your vector data)
qdrant_storage        # Production data
qdrant_storage_test   # Test data

# Other services
ollama_data           # LLM models
searxng-data          # Search cache
valkey-data2          # Redis data
```

### Backup Vector Database
```bash
# Backup Qdrant data
docker run --rm -v qdrant_storage:/data \
  -v $(pwd):/backup \
  busybox tar czf /backup/qdrant_backup.tar.gz /data

# Restore from backup
docker run --rm -v qdrant_storage:/data \
  -v $(pwd):/backup \
  busybox tar xzf /backup/qdrant_backup.tar.gz -C /
```

---

## 🛑 Stop Services

```bash
# Stop all services (keep data)
docker-compose down

# Stop and remove volumes (delete data)
docker-compose down -v

# Test environment
docker-compose -f docker-compose.test.yml down
```

---

## 🔄 Restart Services

```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart deep-research-qdrant

# Force recreate containers
docker-compose up -d --force-recreate
```

---

## 📝 Logs

### View All Logs
```bash
# Follow all service logs
docker-compose logs -f

# View specific service
docker-compose logs -f deep-research-qdrant

# View last 100 lines
docker-compose logs --tail=100 deep-research-qdrant
```

---

## 🔐 Enable API Key (Optional)

### Set Environment Variable
```bash
# Linux/Mac
export QDRANT_API_KEY=your-secret-key
docker-compose up -d

# Windows (PowerShell)
$env:QDRANT_API_KEY="your-secret-key"
docker-compose up -d
```

### Or Create .env File
```bash
# Create .env file
echo "QDRANT_API_KEY=your-secret-key" > .env

# Start services (will read .env)
docker-compose up -d
```

### Update Application Config
```json
{
  "VectorDatabase": {
    "Enabled": true,
    "Qdrant": {
      "BaseUrl": "http://localhost:6333",
      "ApiKey": "your-secret-key"
    }
  }
}
```

---

## 🧹 Cleanup

### Remove Everything
```bash
# Stop and remove all (keep volumes)
docker-compose down

# Stop and remove all (delete volumes)
docker-compose down -v

# Remove dangling volumes
docker volume prune

# Remove unused images
docker image prune
```

### Clean Specific Service
```bash
# Remove Qdrant container only
docker rm deep-research-qdrant

# Remove Qdrant volume
docker volume rm qdrant_storage

# Remove test Qdrant
docker rm qdrant-test
docker volume rm qdrant_storage_test
```

---

## 🐛 Troubleshooting

### Qdrant Won't Start

**Check logs**:
```bash
docker logs deep-research-qdrant
```

**Port already in use**:
```bash
# Find what's using port 6333
lsof -i :6333  # Mac/Linux
netstat -ano | findstr :6333  # Windows

# Change port in docker-compose.yml
# ports:
#   - "6335:6333"  # Use different host port
```

### Can't Connect to Qdrant

**From host machine**:
```bash
curl http://localhost:6333/health
```

**From other container** (use service name):
```bash
docker exec deep-research-agent \
  curl http://qdrant:6333/health
```

### Storage Full

```bash
# Check volume size
docker volume inspect qdrant_storage

# Clean up old data
docker exec deep-research-qdrant \
  qdrant-cli --url http://localhost:6333 cleanup
```

---

## 📊 Monitor Services

### Real-time Monitoring
```bash
# Docker Stats
docker stats

# Watch specific container
docker stats deep-research-qdrant
```

### Container Information
```bash
# Inspect container
docker inspect deep-research-qdrant

# View port mappings
docker port deep-research-qdrant

# View volumes
docker inspect deep-research-qdrant | grep Mounts -A 10
```

---

## 🎯 Common Commands

```bash
# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f qdrant

# Stop all services
docker-compose stop

# Restart all services
docker-compose restart

# Remove everything
docker-compose down -v

# Build and start
docker-compose up -d --build

# Scale service (if applicable)
docker-compose up -d --scale qdrant=1
```

---

## 📚 Next Steps

1. **Start Services**
   ```bash
   docker-compose up -d
   ```

2. **Verify Qdrant**
   ```bash
   curl http://localhost:6333/health
   ```

3. **Update Configuration**
   - Edit appsettings.json
   - Add Qdrant connection settings

4. **Run Application**
   ```bash
   dotnet run
   ```

5. **Test Vector Search**
   - Use ResearchAsync to gather facts
   - Search similar facts with SearchSimilarFactsAsync

---

## 💡 Tips

- **Keep services running**: Use `docker-compose up -d` (background)
- **Real-time development**: Use `docker-compose up` (foreground with logs)
- **Data persistence**: Volumes keep data even after container removal
- **API Security**: Use QDRANT_API_KEY for production
- **Port conflicts**: Change port mapping if ports already in use
- **Resource limits**: Add `mem_limit` for memory constraints

---

## 📞 Support

### Check Documentation
- Full Setup Guide: `DOCKER_QDRANT_CONFIGURATION.md`
- Vector DB Guide: `VECTOR_DATABASE.md`
- Implementation Guide: `VECTOR_DATABASE_SEARCH_INTEGRATION.md`

### Debug Commands
```bash
# See all services
docker-compose config

# Validate syntax
docker-compose config --quiet

# Show specific service
docker-compose config | grep -A 20 "qdrant:"
```

---

**Version**: 0.6.5-beta  
**Date**: 2024  
**Status**: ✅ Ready to Use
