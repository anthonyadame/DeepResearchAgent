# Dual-Window Chat Interface with Debug Console

## Overview

The chat interface now features a dual-window layout with a collapsible debug console for development and debugging purposes.

## Features

### 1. **Enhanced Message Display**
- **User Messages**: Right-aligned with blue/purple gradient background and user avatar
- **Assistant Messages**: Left-aligned with gray background and bot avatar
- **System Messages**: Centered with pill-shaped badges
- **Smooth Animations**: Fade-in effects for new messages
- **Auto-scroll**: Automatically scrolls to latest messages
- **Scroll-to-bottom Button**: Appears when not at bottom of chat

### 2. **Debug Console**
- **Three Tabs**:
  - **Messages**: All user/assistant/system messages
  - **State**: State objects and streaming updates
  - **API Calls**: HTTP requests/responses and errors
- **Features**:
  - Collapsible/expandable entries
  - Copy-to-clipboard for each debug entry
  - Syntax-highlighted JSON
  - Real-time streaming updates
  - Filterable by type
  - Clear console button
  - Message count badges

### 3. **Resizable Panels**
- **Draggable Divider**: Drag to resize chat vs debug console
- **Height Limits**: 10% - 70% for debug console
- **Smooth Transitions**: Animated resize when toggling
- **Persistent State**: Size preference saved across sessions

### 4. **Debug Toggle**
- **Gear Icon Button**: Fixed bottom-right corner
- **Visual Feedback**: Changes color when console is active
- **Keyboard Shortcut**: (Future enhancement)

## Architecture

### Component Structure
```
ChatDialog
├── MessageList
│   └── MessageBubble (enhanced)
├── DebugConsole
│   ├── TabButtons (Messages | State | API Calls)
│   └── DebugMessageItem (collapsible entries)
└── ResizableDivider
```

### State Management
- **Zustand Store** (`debugStore.ts`): Centralized debug message storage
- **Local State**: UI interactions (resize, tabs, visibility)

### Hooks
- **useDebugLogger**: Captures messages, states, API calls, errors
- **useResizable**: Handles drag-to-resize functionality
- **useChat**: Enhanced with debug logging integration

### Data Flow
```
User Action → useChat → API Call → Debug Logger → Debug Store → Debug Console
                                                              ↓
                                                        Chat Messages
```

## Usage

### For Users
1. **Open Debug Console**: Click the gear icon (⚙️) in bottom-right corner
2. **Switch Tabs**: Click "Messages", "State", or "API Calls" tabs
3. **Expand Entries**: Click on any debug entry to see full JSON
4. **Copy Data**: Click copy icon to copy JSON to clipboard
5. **Resize Panels**: Drag the horizontal divider to adjust heights
6. **Clear Console**: Click trash icon to clear all debug entries

### For Developers
```typescript
import { useDebugLogger } from '@hooks/useDebugLogger'

const { logMessage, logState, logApiCall, logError } = useDebugLogger()

// Log a message
logMessage('Hello world', 'user', 'sent')

// Log state change
logState({ step: 2, briefGenerated: true }, 'ResearchBrief', 'received')

// Log API call
logApiCall('/api/chat/123/query', 'POST', { message: 'test' }, 'sent')

// Log error
logError(new Error('Something failed'), 'sendMessage')
```

## File Structure
```
src/
├── components/
│   ├── ChatDialog.tsx              (main container)
│   ├── MessageList.tsx             (auto-scroll, messages)
│   ├── MessageBubble.tsx           (enhanced styling)
│   ├── DebugConsole.tsx            (tabbed debug panel)
│   ├── DebugMessageItem.tsx        (collapsible debug entry)
│   └── ResizableDivider.tsx        (drag handle)
├── hooks/
│   ├── useChat.ts                  (enhanced with logging)
│   ├── useDebugLogger.ts           (debug utilities)
│   └── useResizable.ts             (resize logic)
├── stores/
│   └── debugStore.ts               (Zustand store)
└── styles/
    └── animations.css              (fadeIn, scrolling)
```

## Implementation Details

### Message Bubble Styling
- User messages: `bg-gradient-to-br from-blue-500 to-blue-600`
- Assistant messages: `bg-white border border-gray-200`
- Avatars: Circular with lucide-react icons (User/Bot)
- Timestamps: Relative time formatting ("2m ago", "Just now")

### Debug Console Colors
- Messages: Blue badge (`bg-blue-100 text-blue-700`)
- State: Purple badge (`bg-purple-100 text-purple-700`)
- Errors: Red badge (`bg-red-100 text-red-700`)
- API Calls: Green badge (`bg-green-100 text-green-700`)

### Performance Optimizations
- **useMemo**: Filtered messages computed only when needed
- **Virtualization**: (Future) For large debug logs
- **Debouncing**: Resize events throttled during drag

## Future Enhancements
- [ ] Search/filter within debug console
- [ ] Export debug logs to JSON file
- [ ] Keyboard shortcuts (Ctrl+` to toggle)
- [ ] Virtualized scrolling for large message lists
- [ ] Syntax highlighting for code blocks in messages
- [ ] Network waterfall visualization for API calls
- [ ] Performance metrics dashboard
- [ ] Theme customization (dark mode for debug console)

## Troubleshooting

### Console Not Appearing
- Verify gear icon is clickable
- Check `isConsoleVisible` state in React DevTools
- Ensure `debugStore` is properly initialized

### Messages Not Logging
- Verify `useDebugLogger` hook is called in `useChat`
- Check that log methods are being invoked
- Inspect `debugStore.messages` in React DevTools

### Resize Not Working
- Ensure mouse events are not blocked
- Check `useResizable` hook is receiving events
- Verify height constraints (10% - 70%)

## Credits
Built with:
- React 18
- TypeScript 5
- Tailwind CSS 3
- Zustand 4
- Lucide React (icons)
- Vite (build tool)
