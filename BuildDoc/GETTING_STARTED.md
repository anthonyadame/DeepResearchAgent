# 🚀 Getting Started with Deep Research Agent

## Project Overview

**Deep Research Agent** is a sophisticated research automation system combining:
- **.NET 8 API** (`DeepResearchAgent.Api`) - Backend with research workflows
- **React + TypeScript UI** (`DeepResearchAgent.UI`) - Modern web interface
- **Docker Compose** - Complete containerized stack

---

## 📋 Prerequisites

### For Local Development

- **Node.js** 18+ (for UI development)
- **.NET 8 SDK** (for API development)
- **Docker** & **Docker Compose** (optional, for containerized deployment)
- **Git** (for repository management)

### For Docker Deployment Only

- **Docker** & **Docker Compose**
- Minimum 8GB RAM
- 4 CPU cores recommended

---

## 🏃 Quick Start (5 minutes)

### Option 1: Docker Compose (Recommended)

```bash
# Clone repository
git clone https://github.com/anthonyadame/DeepResearchAgent.git
cd deep-research-code/DeepResearchTTD

# Start all services
docker-compose up

# Access the application
# UI:  http://localhost:5173
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### Option 2: Local Development

#### Start the API (.NET)
```bash
cd DeepResearchAgent.Api
dotnet restore
dotnet run
# API available at http://localhost:5000
```

#### Start the UI (React)
```bash
cd DeepResearchAgent.UI
npm install
npm run dev
# UI available at http://localhost:5173
```

---

## 📁 Project Structure

```
deep-research-code/DeepResearchTTD/
├── DeepResearchAgent/              # Core business logic
│   ├── Agents/                     # Research agents
│   ├── Workflows/                  # Orchestration
│   ├── Services/                   # Services (Ollama, SearXNG, etc.)
│   ├── Tools/                      # Research tools
│   └── Program.cs
│
├── DeepResearchAgent.Api/          # ASP.NET Core API
│   ├── Controllers/                # API endpoints
│   ├── Dockerfile                  # API Docker image
│   └── Program.cs                  # API configuration
│
├── DeepResearchAgent.UI/           # React + TypeScript UI
│   ├── src/
│   │   ├── components/             # React components
│   │   ├── services/               # API client
│   │   ├── hooks/                  # Custom hooks
│   │   └── types/                  # TypeScript types
│   ├── Dockerfile                  # UI Docker image
│   ├── package.json
│   └── vite.config.ts
│
├── docker-compose.yml              # Multi-service orchestration
└── README.md
```

---

## 🔧 Configuration

### API Configuration

**File:** `DeepResearchAgent.Api/appsettings.json`

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "gpt-oss:20b"
  },
  "SearXNG": {
    "BaseUrl": "http://localhost:8080"
  },
  "Crawl4AI": {
    "BaseUrl": "http://localhost:11235"
  },
  "Lightning": {
    "ServerUrl": "http://localhost:8090"
  }
}
```

### UI Configuration

**File:** `DeepResearchAgent.UI/.env.local`

```bash
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=Deep Research Agent
VITE_LOG_LEVEL=info
```

---

## 🎯 Core Features

### 1. Chat Interface
- Real-time research queries
- Message history
- Loading indicators
- Error handling

### 2. Research Configuration
- Select language
- Choose LLM models
- Include/exclude websites
- Define research topics

### 3. File Management
- Upload documents
- Attach webpages
- Knowledge base integration

### 4. Navigation
- Sidebar with chat history
- New chat creation
- Settings & configuration
- Theme selection

---

## 🔗 API Endpoints

### Chat Management
```
POST   /api/chat/sessions              Create new session
GET    /api/chat/sessions              List all sessions
DELETE /api/chat/sessions/{id}         Delete session
```

### Message Exchange
```
POST   /api/chat/{sessionId}/query     Submit research query
GET    /api/chat/{sessionId}/history   Get chat history
```

### File Operations
```
POST   /api/chat/{sessionId}/files     Upload file
```

### Configuration
```
GET    /api/config/models              List available models
GET    /api/config/search-tools        List search tools
POST   /api/config/save                Save configuration
```

---

## 📦 Docker Compose Services

| Service | Port | Purpose |
|---------|------|---------|
| **ui** | 5173 | React web interface |
| **api** | 5000 | .NET Core API |
| **ollama** | 11434 | Local LLM inference |
| **searxng** | 8080 | Meta-search engine |
| **redis** | 6379 | Caching & state |
| **qdrant** | 6333 | Vector database |

---

## 🧪 Development Workflow

### 1. Making Changes to UI

```bash
cd DeepResearchAgent.UI

# Development mode with hot reload
npm run dev

# Lint code
npm run lint

# Type check
npm run type-check

# Build for production
npm run build
```

### 2. Making Changes to API

```bash
cd DeepResearchAgent.Api

# Restore & build
dotnet restore
dotnet build

# Run with watch mode
dotnet watch run

# Run tests
dotnet test
```

### 3. Docker Debugging

```bash
# View container logs
docker-compose logs api
docker-compose logs ui

# Exec into container
docker-compose exec api bash
docker-compose exec ui sh

# Rebuild specific service
docker-compose build --no-cache api
docker-compose build --no-cache ui
```

---

## 🚀 Deployment

### Local Deployment with Docker Compose

```bash
# Start all services
docker-compose up -d

# Check service health
docker-compose ps

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Production Considerations

1. **Environment Variables**
   - Store secrets in `.env` file (not in `.env.local`)
   - Use Docker secrets for sensitive data

2. **Scaling**
   - API is stateless, can scale horizontally
   - Redis handles distributed caching

3. **Monitoring**
   - InfluxDB for metrics storage
   - Prometheus for scraping
   - Consider APM solutions

4. **Security**
   - Enable HTTPS in production
   - Implement authentication
   - API rate limiting
   - CORS configuration review

---

## 🔍 Troubleshooting

### UI Won't Connect to API

**Error:** "Failed to fetch from http://localhost:5000/api"

**Solution:**
```bash
# 1. Verify API is running
curl http://localhost:5000/swagger

# 2. Check VITE_API_BASE_URL in .env.local
cat DeepResearchAgent.UI/.env.local

# 3. Verify CORS is enabled in API
# Check DeepResearchAgent.Api/Program.cs
```

### Port Already in Use

```bash
# Find process using port 5000 (API)
lsof -i :5000
kill -9 <PID>

# Find process using port 5173 (UI)
lsof -i :5173
kill -9 <PID>
```

### Docker Container Logs

```bash
# API logs
docker logs research-api

# UI logs
docker logs research-ui

# Docker compose logs
docker-compose logs --tail=100
```

### Build Failures

```bash
# Clear Docker cache and rebuild
docker-compose build --no-cache

# Clear npm cache
npm cache clean --force
cd DeepResearchAgent.UI && npm install

# Clear .NET cache
cd DeepResearchAgent.Api && rm -rf bin obj && dotnet restore
```

---

## 📚 Additional Resources

### API Documentation
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI spec: `http://localhost:5000/swagger/v1/swagger.json`

### Component Documentation
- UI README: `DeepResearchAgent.UI/README.md`
- Build Docs: `BuildDoc/` directory

### Architecture Docs
- Phase Overview: `BuildDoc/PHASE1_SUMMARY_AND_ROADMAP.md`
- Component Inventory: `BuildDoc/COMPONENT_INVENTORY.md`

---

## 🤝 Contributing

### Code Style
- Follow existing patterns in codebase
- Use TypeScript strict mode
- C# with nullable reference types

### Pull Request Process
1. Create feature branch
2. Make changes
3. Test locally (local dev + Docker)
4. Commit with clear messages
5. Push and create PR

---

## 📋 Checklist for First Run

- [ ] Clone repository
- [ ] Install Node.js 18+ & .NET 8 SDK
- [ ] Install Docker (optional)
- [ ] Configure `.env.local` files
- [ ] Run `docker-compose up` OR start services locally
- [ ] Open http://localhost:5173
- [ ] Create new chat and test API connection
- [ ] Review logs for any errors

---

## 🎓 Learning Path

1. **Understand Architecture**
   - Read `BuildDoc/README.md`
   - Review service diagrams

2. **Explore Components**
   - Start with `DeepResearchAgent.UI/src/components/ChatDialog.tsx`
   - Follow the flow to understand data binding

3. **Examine Workflows**
   - Look at `DeepResearchAgent/Workflows/` directory
   - Understand research orchestration

4. **Test Locally**
   - Make small UI changes
   - Test API endpoints with Swagger
   - Verify Docker builds work

5. **Deploy**
   - Use Docker Compose for full stack
   - Monitor services with logs

---

## 💡 Tips & Tricks

```bash
# Hot reload both services while developing
# Terminal 1
cd DeepResearchAgent.Api && dotnet watch run

# Terminal 2
cd DeepResearchAgent.UI && npm run dev

# View API logs in real-time
docker-compose logs -f api

# Stop and remove all volumes (clean slate)
docker-compose down -v

# Rebuild UI without changing API
docker-compose up --build ui
```

---

## 📞 Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Review README files in each project
3. Check GitHub issues
4. Open a new issue with details

---

**Version:** 0.6.5-beta  
**Last Updated:** 2024  
**Status:** Production Ready with Ongoing Development

Happy coding! 🚀
