#!/bin/bash
# Quick Commands for Deep Research Agent UI

echo "🚀 Deep Research Agent - Quick Commands"
echo "========================================"
echo ""

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}📖 First Time Setup${NC}"
echo "  cd DeepResearchAgent.UI"
echo "  npm install"
echo ""

echo -e "${BLUE}💻 Development${NC}"
echo "  npm run dev                # Start dev server (http://localhost:5173)"
echo "  npm run build              # Build for production"
echo "  npm run preview            # Preview production build"
echo "  npm run type-check         # Check TypeScript errors"
echo "  npm run lint               # Run linter"
echo ""

echo -e "${BLUE}🐳 Docker${NC}"
echo "  docker-compose up          # Start all services"
echo "  docker-compose up -d       # Start in background"
echo "  docker-compose down        # Stop all services"
echo "  docker-compose logs -f     # View logs"
echo ""

echo -e "${BLUE}📦 Build & Deploy${NC}"
echo "  npm run build                                                              # Build"
echo "  docker build -f DeepResearchAgent.UI/Dockerfile -t research-ui:latest .  # Build image"
echo "  docker run -p 5173:5173 research-ui:latest                               # Run container"
echo ""

echo -e "${BLUE}📚 Documentation${NC}"
echo "  Read: GETTING_STARTED.md      # Complete setup guide"
echo "  Read: DEVELOPMENT.md          # Development workflow"
echo "  Read: README.md               # Project overview"
echo "  Read: INDEX.md                # Navigation guide"
echo ""

echo -e "${BLUE}🔍 Debugging${NC}"
echo "  Browser: Press F12                     # Open DevTools"
echo "  Logs: docker-compose logs -f ui        # View UI logs"
echo "  Logs: docker-compose logs -f api       # View API logs"
echo "  Check: http://localhost:5173           # UI running?"
echo "  Check: http://localhost:5000/swagger   # API running?"
echo ""

echo -e "${BLUE}✅ Verification${NC}"
echo "  npm run type-check              # Verify TypeScript"
echo "  docker-compose up               # Verify Docker"
echo "  http://localhost:5173           # Verify UI loads"
echo ""

echo -e "${GREEN}Ready to start! Follow one of the paths above.${NC}"
echo ""
echo "Quick Start:"
echo "  npm install && npm run dev"
echo ""
echo "Or with Docker:"
echo "  docker-compose up"
echo ""
