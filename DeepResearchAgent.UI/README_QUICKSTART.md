# Deep Research Agent UI - Quick Start Guide

## Prerequisites

- Node.js 18+ installed
- npm or yarn package manager
- Running backend API (optional for full functionality)

## Installation

```bash
cd DeepResearchAgent.UI
npm install
```

## Development Server

```bash
npm run dev
```

The application will start on `http://localhost:5173`

## Environment Configuration

Create a `.env` file in the `DeepResearchAgent.UI` directory:

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

## Features Overview

### 1. Chat Interface
- **New Chat**: Click "+ New Chat" in sidebar or welcome screen
- **Send Message**: Type and press Enter (Shift+Enter for new line)
- **View History**: Messages display in chat area

### 2. Sidebar Navigation
- **Toggle**: Click menu icon (mobile) or always visible (desktop)
- **New Chat**: Create new conversation
- **Chat History**: Browse and search past sessions
- **Settings**: Configuration options (coming soon)
- **Themes**: Switch between Light/Dark/System themes

### 3. Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| Enter | Send message |
| Shift+Enter | New line in message |
| Escape | Close modal/dropdown |
| Ctrl+/ (Cmd+/) | Open configuration |

### 4. Action Buttons (Bottom Bar)

#### Left Group:
- **+ Button**: Add items dropdown
  - Upload Files
  - Attach Webpage (placeholder)
  - Attach Knowledge (placeholder)
- **Globe Icon**: Select web search provider
  - SearXNG (recommended)
  - Google
  - Bing
  - DuckDuckGo
- **Settings Icon**: Research configuration
  - Language
  - LLM Models
  - Website filters
  - Topics

#### Right Side:
- **Send Button**: Submit message (also triggered by Enter key)

### 5. Theme Switching
1. Click **Palette** icon in sidebar
2. Select theme:
   - **Light**: Always light theme
   - **Dark**: Always dark theme
   - **System**: Follow OS preference
3. Preference saved automatically

### 6. Chat History
1. Click **Chat History** in sidebar
2. Search conversations using search box
3. Click session to load it
4. Hover over session to see delete button

## Component Structure

```
App (ThemeProvider wrapper)
├── Sidebar
│   ├── New Chat Button
│   ├── Navigation (History, Settings, Themes)
│   └── ChatHistoryPanel (when in history view)
└── ChatDialog
    ├── Header
    ├── MessageList
    └── Action Bar
        ├── Add Items Dropdown
        ├── Web Search Dialog
        ├── Configuration Dialog
        └── Send Button
```

## API Integration

### Required Backend Endpoints

```typescript
// Sessions
POST   /api/chat/sessions          // Create new session
GET    /api/chat/sessions          // List all sessions
DELETE /api/chat/sessions/:id      // Delete session

// Messages
POST   /api/chat/:sessionId/query  // Send message
GET    /api/chat/:sessionId/history // Get history

// Files
POST   /api/chat/:sessionId/files  // Upload file

// Configuration
GET    /api/config/models          // Get available models
GET    /api/config/search-tools    // Get search providers
POST   /api/config/save            // Save configuration
```

### API Service Configuration

Edit `src/services/api.ts` to change base URL:

```typescript
const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'
```

## Build for Production

```bash
npm run build
```

Output will be in `dist/` directory.

## Serve Production Build

```bash
npm run preview
```

## Troubleshooting

### Issue: API calls failing
**Solution**: Check that backend is running and `VITE_API_BASE_URL` is correct

### Issue: Theme not persisting
**Solution**: Check browser's localStorage is enabled

### Issue: Sidebar not showing on mobile
**Solution**: Click the menu icon in top-left corner

### Issue: TypeScript errors in IDE
**Solution**: These are mostly warnings about JSX imports. Run `npm run dev` to verify actual functionality.

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Mobile Support

- Responsive design works on all screen sizes
- Touch-optimized buttons
- Collapsible sidebar
- Overlay navigation on mobile

## Known Limitations

1. **File Upload**: UI ready, needs backend testing
2. **Message Streaming**: Not yet implemented
3. **Webpage Attachment**: Placeholder only
4. **Knowledge Base**: Placeholder only

## Development Tips

### Hot Reload
Vite provides instant hot module replacement. Changes to components will reflect immediately.

### Component Development
All components are in `src/components/`. Each is self-contained with props interface.

### Adding New Features
1. Create component in `src/components/`
2. Add types in `src/types/index.ts`
3. Create hook if needed in `src/hooks/`
4. Import and use in parent component

### Styling
- Tailwind CSS classes used throughout
- Dark mode: Add `dark:` prefix to classes
- Custom animations in `tailwind.config.ts`

## File Structure

```
DeepResearchAgent.UI/
├── src/
│   ├── components/          # React components
│   │   ├── ChatDialog.tsx
│   │   ├── ChatHistoryPanel.tsx
│   │   ├── ConfigurationDialog.tsx
│   │   ├── DropdownMenu.tsx
│   │   ├── FileUploadModal.tsx
│   │   ├── InputBar.tsx
│   │   ├── MessageBubble.tsx
│   │   ├── MessageList.tsx
│   │   ├── Sidebar.tsx
│   │   ├── ThemeDialog.tsx
│   │   └── WebSearchDialog.tsx
│   ├── contexts/            # React contexts
│   │   └── ThemeContext.tsx
│   ├── hooks/               # Custom hooks
│   │   ├── useChat.ts
│   │   └── useChatHistory.ts
│   ├── services/            # API services
│   │   └── api.ts
│   ├── types/               # TypeScript types
│   │   └── index.ts
│   ├── App.tsx              # Main app component
│   ├── main.tsx             # Entry point
│   └── index.css            # Global styles
├── public/                  # Static assets
├── index.html               # HTML template
├── package.json             # Dependencies
├── tailwind.config.ts       # Tailwind configuration
├── tsconfig.json            # TypeScript configuration
└── vite.config.ts           # Vite configuration
```

## Support

For issues or questions:
1. Check this guide
2. Review `BuildDoc/UI_IMPLEMENTATION_PHASE1.md` for detailed documentation
3. Check `BuildDoc/NEXT_STEPS_COMPLETE.md` for implementation status
4. Review component source code (well-commented)

## What's Next?

See `BuildDoc/NEXT_STEPS_COMPLETE.md` for:
- Phase 2: Backend integration testing
- Phase 3: Advanced features
- Phase 4: Polish and optimization

Happy coding! 🚀
