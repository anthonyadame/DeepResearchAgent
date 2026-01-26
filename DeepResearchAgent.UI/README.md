# Deep Research Agent UI

A modern React + TypeScript web interface for the Deep Research Agent API.

## 🚀 Quick Start

### Prerequisites

- Node.js 18+ and npm
- npm packages installed: `npm install`

### Development

```bash
# Start development server
npm run dev

# Open in browser
http://localhost:5173
```

### Build

```bash
# Build for production
npm run build

# Preview production build
npm run preview
```

## 📁 Project Structure

```
src/
├── components/          # React components
│   ├── ChatDialog.tsx   # Main chat interface
│   ├── Sidebar.tsx      # Navigation sidebar
│   ├── InputBar.tsx     # Message input
│   ├── MessageList.tsx  # Chat messages
│   └── ...
├── services/            # API communication
│   └── api.ts           # API client
├── hooks/               # Custom React hooks
│   └── useChat.ts       # Chat state management
├── types/               # TypeScript types
│   └── index.ts         # Type definitions
├── App.tsx              # Root component
├── main.tsx             # Entry point
└── index.css            # Global styles
```

## 🔌 API Integration

The UI communicates with the Deep Research Agent API through the `apiService` in `src/services/api.ts`.

**Default API URL:** `http://localhost:5000/api`

Configure via environment variable: `VITE_API_BASE_URL`

### Available Endpoints

- `POST /chat/sessions` - Create new chat session
- `GET /chat/sessions` - List all sessions
- `POST /chat/{sessionId}/query` - Submit research query
- `GET /chat/{sessionId}/history` - Get chat history
- `POST /chat/{sessionId}/files` - Upload files
- `GET /config/models` - List available LLM models
- `GET /config/search-tools` - List search tools

## 🎨 Components

### ChatDialog
Main chat interface with message display and input controls.

**Props:**
- `sessionId`: string - Current chat session ID

### Sidebar
Navigation sidebar with chat history and settings.

**Props:**
- `onNewChat`: () => void - Callback for new chat creation

### InputBar
Message input with send button.

**Props:**
- `value`: string - Current input text
- `onChange`: (value: string) => void - Input change handler
- `onSend`: () => void - Send message handler
- `isLoading`: boolean - Loading state

### FileUploadModal
Modal for uploading files or attaching webpages.

**Props:**
- `sessionId`: string - Current session ID
- `onClose`: () => void - Close handler

### ConfigurationDialog
Settings dialog for research configuration.

**Props:**
- `config`: ResearchConfig | undefined - Current configuration
- `onSave`: (config: ResearchConfig) => void - Save handler
- `onClose`: () => void - Close handler

## 🎯 Features

### Chat Interface
- Real-time message exchange
- User/assistant message differentiation
- Typing indicators
- Auto-scroll to latest message

### Sidebar
- New chat creation
- Chat history search
- Configuration management
- Theme selection

### Configuration
- Language selection
- LLM model choice
- Website inclusion/exclusion
- Topic filtering

### File Management
- File upload support
- Webpage attachment
- Knowledge base integration

## 🔧 Development

### Environment Variables

Create `.env.local` with:
```
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=Deep Research Agent
VITE_LOG_LEVEL=info
```

### Type Checking

```bash
npm run type-check
```

### Linting

```bash
npm run lint
```

## 🐳 Docker

### Build Image

```bash
docker build -f DeepResearchAgent.UI/Dockerfile -t research-ui:latest .
```

### Run Container

```bash
docker run -p 5173:5173 -e VITE_API_BASE_URL=http://api:5000/api research-ui:latest
```

### Docker Compose

```bash
docker-compose up ui
```

## 🎨 Styling

Uses **Tailwind CSS** for utility-first styling and **Lucide React** for icons.

### Tailwind Configuration

- `tailwind.config.ts` - Theme customization
- `postcss.config.js` - PostCSS plugin configuration
- `src/index.css` - Global styles

## 📦 Dependencies

### Production
- **react** - UI library
- **react-dom** - React DOM rendering
- **axios** - HTTP client
- **zustand** - State management (for future expansion)
- **lucide-react** - Icon library

### Development
- **vite** - Build tool
- **typescript** - Type safety
- **tailwindcss** - Styling
- **eslint** - Code quality

## 🔄 State Management

Currently uses React hooks (`useState`, `useCallback`) for local state management.

For future expansion:
- **Zustand** is pre-installed for simple global state if needed
- Consider Redux/Redux Toolkit for complex state

## 🚦 Performance

- Code splitting via Vite
- Lazy loading for components
- Optimized builds with production mode
- Tree-shaking enabled

## 🐛 Troubleshooting

### API Connection Issues
- Verify `VITE_API_BASE_URL` points to running API
- Check API health endpoint: `http://localhost:5000/health`
- Review browser console for CORS errors

### Build Errors
- Clear `node_modules` and reinstall: `rm -rf node_modules && npm install`
- Clear Vite cache: `rm -rf .vite`

### Development Server Issues
- Port 5173 already in use? Vite will use next available port
- Check firewall/proxy settings

## 📈 Future Enhancements

- [ ] Real-time chat with WebSockets
- [ ] Chat history persistence
- [ ] User authentication
- [ ] Dark theme
- [ ] Export chat functionality
- [ ] Advanced search filters
- [ ] Integration with more knowledge sources

## 📝 License

Same as Deep Research Agent

## 🤝 Contributing

Contributions welcome! Please:
1. Follow TypeScript strict mode
2. Use existing component patterns
3. Add types for new features
4. Test with API integration
