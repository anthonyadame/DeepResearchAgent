# Deep Research Agent UI - Development Guide

## 🎯 Overview

This document provides detailed guidance for developing the React + TypeScript UI for the Deep Research Agent.

---

## 🛠️ Development Environment Setup

### 1. Install Node.js

**Required:** Node.js 18+

```bash
# Check version
node --version
npm --version

# If not installed, download from https://nodejs.org/
```

### 2. Project Setup

```bash
cd DeepResearchAgent.UI

# Install dependencies
npm install

# Verify installation
npm list
```

### 3. Start Development Server

```bash
npm run dev

# Output:
# ➜  Local:   http://localhost:5173/
# ➜  press h to show help
```

Open `http://localhost:5173` in your browser.

---

## 📁 Project Structure Guide

```
src/
├── App.tsx                    # Root component
├── main.tsx                   # React entry point
├── index.css                  # Global Tailwind styles
│
├── components/                # React UI Components
│   ├── ChatDialog.tsx         # Main chat interface
│   ├── Sidebar.tsx            # Navigation sidebar
│   ├── MessageList.tsx        # Message container
│   ├── MessageBubble.tsx      # Individual message
│   ├── InputBar.tsx           # Input with send button
│   ├── FileUploadModal.tsx    # File upload dialog
│   └── ConfigurationDialog.tsx # Settings dialog
│
├── services/                  # API & External Services
│   └── api.ts                 # Axios HTTP client
│
├── hooks/                     # Custom React Hooks
│   └── useChat.ts             # Chat state logic
│
├── types/                     # TypeScript Definitions
│   └── index.ts               # All type interfaces
│
└── pages/                     # Full-page components (future)
```

---

## 🧩 Component Development

### Creating a New Component

**Convention:** PascalCase, `.tsx` extension

```typescript
// src/components/MyNewComponent.tsx
import React from 'react'
import { SomeIcon } from 'lucide-react'

interface MyNewComponentProps {
  title: string
  onAction?: () => void
}

export default function MyNewComponent({ title, onAction }: MyNewComponentProps) {
  return (
    <div className="p-4 bg-white rounded-lg">
      <h2 className="text-lg font-semibold">{title}</h2>
      {onAction && (
        <button onClick={onAction} className="mt-2 px-4 py-2 bg-blue-500 text-white rounded">
          Action
        </button>
      )}
    </div>
  )
}
```

### Component Guidelines

1. **Props Typing**
   ```typescript
   interface ComponentProps {
     required: string           // Required prop
     optional?: boolean         // Optional prop
     children?: React.ReactNode // For layout components
     onClick?: () => void       // Callbacks
   }
   ```

2. **Hooks Usage**
   ```typescript
   const [state, setState] = useState<string>('')
   const [loading, setLoading] = useState(false)
   const [error, setError] = useState<string | null>(null)
   ```

3. **Effects**
   ```typescript
   useEffect(() => {
     // Side effects
     return () => {
       // Cleanup
     }
   }, [dependency])
   ```

---

## 🔌 API Integration

### Using the API Service

```typescript
import { apiService } from '@services/api'

// Create a session
const session = await apiService.createSession('My Research')

// Submit a query
const message = await apiService.submitQuery(sessionId, 'Research question')

// Upload a file
const result = await apiService.uploadFile(sessionId, file)

// Error handling
try {
  await apiService.createSession()
} catch (error) {
  console.error('API Error:', error)
}
```

### Adding New API Endpoints

**File:** `src/services/api.ts`

```typescript
// Add to ApiService class
async getNewData(param: string): Promise<DataType> {
  const response = await this.client.get(`/endpoint/${param}`)
  return response.data
}
```

---

## 🎨 Styling with Tailwind CSS

### Common Patterns

```typescript
// Container with padding and rounded corners
<div className="p-4 bg-white rounded-lg shadow-md">

// Flex layout
<div className="flex gap-2 items-center justify-between">

// Responsive grid
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">

// Text styling
<h1 className="text-xl font-semibold text-gray-800">
<p className="text-sm text-gray-600">

// Buttons
<button className="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg">

// Responsive padding
<div className="p-4 md:p-6 lg:p-8">

// Hover effects
<div className="hover:bg-gray-100 cursor-pointer transition-colors">
```

### Color Scheme

Defined in `tailwind.config.ts`:

```typescript
colors: {
  primary: '#3B82F6',      // Blue
  secondary: '#1F2937',    // Dark gray
  accent: '#10B981',       // Green
}
```

### Custom Utilities

Add to `src/index.css`:

```css
@layer components {
  .btn-primary {
    @apply px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors;
  }
}

/* Then use in components */
<button className="btn-primary">Click me</button>
```

---

## 🪝 Custom Hooks

### useChat Hook

```typescript
import { useChat } from '@hooks/useChat'

export function MyComponent() {
  const { messages, isLoading, error, sendMessage, loadHistory } = useChat(sessionId)
  
  const handleSend = async () => {
    try {
      await sendMessage('Your message')
    } catch (err) {
      console.error(err)
    }
  }

  return (
    // Component JSX
  )
}
```

### Creating Custom Hooks

**File:** `src/hooks/useMyHook.ts`

```typescript
import { useState, useCallback } from 'react'

export const useMyHook = (param: string) => {
  const [state, setState] = useState<string>('')
  const [loading, setLoading] = useState(false)

  const action = useCallback(async () => {
    setLoading(true)
    try {
      // Do something
    } finally {
      setLoading(false)
    }
  }, [param])

  return { state, loading, action }
}
```

---

## 🔄 State Management

### Current Approach: React Hooks

For simple state within components:

```typescript
const [value, setValue] = useState<string>('')
const [list, setList] = useState<Item[]>([])
```

### For Complex Global State: Zustand (Pre-installed)

Create a store:

```typescript
// src/stores/chatStore.ts
import { create } from 'zustand'

interface ChatStore {
  sessions: Session[]
  currentSession: Session | null
  addSession: (session: Session) => void
  setCurrentSession: (session: Session) => void
}

export const useChatStore = create<ChatStore>((set) => ({
  sessions: [],
  currentSession: null,
  addSession: (session) => set((state) => ({
    sessions: [...state.sessions, session]
  })),
  setCurrentSession: (session) => set({ currentSession: session })
}))
```

Use in component:

```typescript
const { sessions, currentSession } = useChatStore()
```

---

## 🧪 Testing

### Type Checking

```bash
npm run type-check
```

### Linting

```bash
npm run lint
```

### Manual Testing Checklist

- [ ] Run locally: `npm run dev`
- [ ] Test all components load
- [ ] Test API calls (check browser console)
- [ ] Test responsive design (DevTools - F12)
- [ ] Test on mobile viewport
- [ ] Test error states
- [ ] Check for TypeScript errors

---

## 🔍 Debugging

### Browser DevTools

1. Open DevTools: `F12` or `Right-click > Inspect`
2. **Console Tab**: View logs and errors
3. **Network Tab**: Check API calls
4. **React DevTools**: Inspect component tree
5. **Redux DevTools**: If using Zustand

### Console Logging

```typescript
// Development only
if (import.meta.env.DEV) {
  console.log('Debug info:', data)
}

// Use custom logger
const logLevel = import.meta.env.VITE_LOG_LEVEL || 'info'
if (logLevel === 'debug') {
  console.log(...)
}
```

### API Debugging

```typescript
// Add to api.ts before making requests
this.client.interceptors.request.use(config => {
  console.log('Request:', config)
  return config
})

this.client.interceptors.response.use(response => {
  console.log('Response:', response)
  return response
})
```

---

## 🏗️ Building for Production

### Build Process

```bash
npm run build
```

This:
1. Runs TypeScript compiler (`tsc`)
2. Bundles with Vite
3. Minifies and optimizes
4. Output in `dist/` directory

### Preview Production Build

```bash
npm run preview
```

Visit `http://localhost:4173` to test production build locally.

### Docker Build

```bash
docker build -f DeepResearchAgent.UI/Dockerfile -t research-ui:latest .
docker run -p 5173:5173 research-ui:latest
```

---

## 📦 Dependencies Management

### Adding Dependencies

```bash
# Add production dependency
npm install package-name

# Add dev dependency
npm install --save-dev package-name

# Update all packages
npm update

# Check for outdated packages
npm outdated
```

### Key Dependencies

| Package | Purpose |
|---------|---------|
| `react` | UI framework |
| `react-dom` | React rendering |
| `axios` | HTTP client |
| `lucide-react` | Icons |
| `tailwindcss` | Styling |
| `zustand` | State management |

---

## 🚀 Performance Optimization

### Code Splitting

Vite automatically handles this, but ensure components are lazy:

```typescript
// Lazy load components
const ChatHistory = React.lazy(() => import('./ChatHistory'))

export function App() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <ChatHistory />
    </Suspense>
  )
}
```

### Memoization

```typescript
// Prevent unnecessary re-renders
const MemoizedComponent = React.memo(MyComponent)

// Or use useMemo/useCallback
const memoized = useMemo(() => expensiveComputation(), [deps])
const callback = useCallback(() => { ... }, [deps])
```

### Image Optimization

```typescript
// Use next-gen formats when possible
<img src="image.webp" alt="Description" />
```

---

## 📝 Code Style Guide

### Naming Conventions

```typescript
// Components: PascalCase
function ChatDialog() {}

// Variables: camelCase
const messageCount = 0

// Constants: UPPER_SNAKE_CASE
const API_TIMEOUT = 30000

// Private: underscore prefix (convention)
const _internalValue = null

// Booleans: is/has prefix
const isLoading = false
const hasError = false
```

### File Organization

```typescript
// 1. Imports
import React, { useState } from 'react'
import { CustomIcon } from 'lucide-react'
import { apiService } from '@services/api'
import type { ChatMessage } from '@types/index'

// 2. Types
interface ComponentProps {
  title: string
}

// 3. Component
export default function Component({ title }: ComponentProps) {
  // Component code
}
```

---

## 🐛 Common Issues & Solutions

### Issue: "Cannot find module '@components'"

**Solution:**
```bash
# Check tsconfig.json paths match your imports
# Restart dev server
npm run dev
```

### Issue: Styles not applying

**Solution:**
```bash
# Clear Vite cache
rm -rf .vite

# Ensure Tailwind CSS is imported in index.css
@tailwind base;
@tailwind components;
@tailwind utilities;

# Restart dev server
npm run dev
```

### Issue: API calls fail in production

**Solution:**
```bash
# Check VITE_API_BASE_URL in production environment
# Ensure backend CORS allows your domain
# Use absolute URLs in production
```

---

## 📚 Resources

### Documentation
- [React Docs](https://react.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [Vite Guide](https://vitejs.dev/guide/)
- [Axios Documentation](https://axios-http.com/docs/intro)

### Tools
- [TypeScript Playground](https://www.typescriptlang.org/play)
- [Tailwind CSS IntelliSense](https://marketplace.visualstudio.com/items?itemName=bradlc.vscode-tailwindcss)
- [ES7+ React Snippets](https://marketplace.visualstudio.com/items?itemName=dsznajder.es7-react-js-snippets)

---

## 🎓 Best Practices

1. **Type Everything**
   - Use TypeScript strict mode
   - Avoid `any` type
   - Define interfaces for data structures

2. **Error Handling**
   - Wrap API calls in try-catch
   - Show user-friendly error messages
   - Log errors for debugging

3. **Performance**
   - Avoid unnecessary re-renders
   - Use keys in lists
   - Memoize expensive computations

4. **Accessibility**
   - Use semantic HTML
   - Add alt text to images
   - Ensure keyboard navigation
   - Proper ARIA labels

5. **Security**
   - Sanitize user input
   - Don't store sensitive data in localStorage
   - Use environment variables for secrets
   - Validate API responses

---

## 🔄 Development Workflow

```bash
# 1. Start development server
npm run dev

# 2. Make changes to files
# (auto hot-reload)

# 3. Type check before commit
npm run type-check

# 4. Lint code
npm run lint

# 5. Build for testing
npm run build

# 6. Preview production build
npm run preview

# 7. Push to repository
git add .
git commit -m "Feature: description"
git push
```

---

## 💡 Tips for Success

1. **Use TypeScript Strict Mode** - Catches errors early
2. **Component Composition** - Keep components small and focused
3. **Reusable Hooks** - Extract logic into custom hooks
4. **Consistent Styling** - Use Tailwind classes consistently
5. **Error Boundaries** - Consider for production apps
6. **Testing** - Add tests for critical features
7. **Documentation** - Comment complex logic
8. **Version Control** - Commit frequently with clear messages

---

**Version:** 0.6.5-beta  
**Last Updated:** 2024  
**Status:** Active Development

Happy coding! 🚀
