# Visual Reference Guide: Dual-Window Chat Interface

## Layout Overview

```
┌─────────────────────────────────────────────────────────┐
│  Header: "Deep Research Agent"                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│         ┌──────────┐                                    │
│         │ 🤖 Bot   │ Assistant Message                  │
│         └──────────┘ (white bg, left-aligned)           │
│                                                          │
│                            ┌──────────┐                 │
│               User Message │ 👤 User  │                 │
│       (blue gradient, right)└──────────┘                │
│                                                          │
│                     [↓] Scroll to Bottom                │
├─────────────────────────────────────────────────────────┤
│  Input Bar: [+ Globe ⚙️] [Send]                        │
│  "Deep Research Agent can make mistakes..."             │
├═════════════════════════════════════════════════════════┤ ← Draggable
│  Debug Console Header [Messages|State|API] [🗑️]        │
├─────────────────────────────────────────────────────────┤
│  ▶ message  09:45:12  Copy                              │
│  ▶ state    09:45:15  Copy                              │
│  ▶ api_call 09:45:18  200  Copy                         │
└─────────────────────────────────────────────────────────┘
                                                    [⚙️] ← Toggle
```

## Component Breakdown

### 1. Message Bubbles

#### User Message (Right Side)
```
                           ┌─────────────┐
                           │  👤         │
                           │ ┌─────────┐ │
                           │ │ Hello!  │ │
                           │ └─────────┘ │
                           │  2m ago     │
                           └─────────────┘
```
- **Background**: `bg-gradient-to-br from-blue-500 to-blue-600`
- **Text**: White
- **Alignment**: Right
- **Avatar**: User icon in circle

#### Assistant Message (Left Side)
```
┌─────────────┐
│  🤖         │
│ ┌─────────┐ │
│ │ Hi there│ │
│ └─────────┘ │
│  Just now   │
└─────────────┘
```
- **Background**: `bg-white border border-gray-200`
- **Text**: Gray-800
- **Alignment**: Left
- **Avatar**: Bot icon in circle

### 2. Debug Console Tabs

```
┌──────────────────────────────────────────┐
│ Debug Console  [Messages][State][API] [🗑️]│
├──────────────────────────────────────────┤
│                                           │
│  Active Tab Content                       │
│                                           │
└──────────────────────────────────────────┘
```

**Tab States:**
- **Active**: `bg-gray-900 text-white`
- **Inactive**: `text-gray-300 hover:bg-gray-600`
- **Badge**: Shows count (`bg-blue-500` when active)

### 3. Debug Message Item

#### Collapsed State
```
┌─────────────────────────────────────────┐
│ ▶ message  09:45:12  Copy               │
└─────────────────────────────────────────┘
```

#### Expanded State
```
┌─────────────────────────────────────────┐
│ ▼ message  09:45:12  Copy               │
├─────────────────────────────────────────┤
│ {                                        │
│   "role": "user",                        │
│   "content": "Hello world",              │
│   "timestamp": "2024-01-15T09:45:12Z"    │
│ }                                        │
└─────────────────────────────────────────┘
```

### 4. Resize Behavior

```
Initial State (30% debug):
┌─────────┐
│ Chat    │ 70%
│ Area    │
├═════════┤ ← Draggable
│ Debug   │ 30%
└─────────┘

Dragged Up (50% debug):
┌─────────┐
│ Chat    │ 50%
├═════════┤
│ Debug   │ 50%
│ Console │
└─────────┘

Maximum (70% debug):
┌─────────┐ 30%
├═════════┤
│ Debug   │
│ Console │ 70%
│         │
└─────────┘
```

## Color Reference

### Message Colors
```css
/* User */
.user-message {
  background: linear-gradient(to-br, #3B82F6, #8B5CF6);
  color: white;
}

/* Assistant */
.assistant-message {
  background: white;
  border: 1px solid #E5E7EB;
  color: #1F2937;
}

/* Avatar Backgrounds */
.user-avatar {
  background: linear-gradient(to-br, #3B82F6, #7C3AED);
}

.bot-avatar {
  background: linear-gradient(to-br, #374151, #111827);
}
```

### Debug Badge Colors
```css
.badge-message  { background: #DBEAFE; color: #1E40AF; }
.badge-state    { background: #EDE9FE; color: #6B21A8; }
.badge-error    { background: #FEE2E2; color: #991B1B; }
.badge-api-call { background: #D1FAE5; color: #065F46; }
```

### Debug Console
```css
.debug-header {
  background: #1F2937;
  color: white;
}

.debug-content {
  background: white;
}

.debug-tab-active {
  background: #111827;
  color: white;
}

.debug-tab-inactive {
  color: #9CA3AF;
}
```

## Icon Reference

| Component         | Icon        | Library      |
|-------------------|-------------|--------------|
| User Avatar       | `User`      | lucide-react |
| Bot Avatar        | `Bot`       | lucide-react |
| Scroll Down       | `ChevronDown` | lucide-react |
| Copy              | `Copy`      | lucide-react |
| Copy Success      | `Check`     | lucide-react |
| Expand            | `ChevronRight` | lucide-react |
| Collapse          | `ChevronDown` | lucide-react |
| Debug Toggle      | `Code`      | lucide-react |
| Clear Console     | `Trash2`    | lucide-react |
| Resize Handle     | `GripHorizontal` | lucide-react |

## Spacing & Sizing

```css
/* Message Bubble */
.message-bubble {
  max-width: 70%;
  padding: 0.75rem 1rem; /* px-4 py-3 */
  border-radius: 1rem; /* rounded-2xl */
  margin-bottom: 1rem; /* mb-4 */
}

/* Avatar */
.avatar {
  width: 2rem; /* w-8 */
  height: 2rem; /* h-8 */
  border-radius: 9999px; /* rounded-full */
}

/* Debug Console */
.debug-header {
  padding: 0.5rem 1rem; /* px-4 py-2 */
}

.debug-entry {
  padding: 0.5rem 0.75rem; /* px-3 py-2 */
}

/* Gear Icon Button */
.debug-toggle {
  position: fixed;
  bottom: 1rem; /* bottom-4 */
  right: 1rem; /* right-4 */
  padding: 0.75rem; /* p-3 */
  border-radius: 9999px; /* rounded-full */
}
```

## Animations

```css
/* Fade In (Messages) */
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Duration: 0.3s */
/* Easing: ease-in */

/* Resize Transition */
transition: height 0.2s ease;

/* Disabled during drag */
transition: none;
```

## Responsive Breakpoints

```css
/* Mobile: < 640px */
.message-bubble { max-width: 85%; }

/* Tablet: 640px - 1024px */
.message-bubble { max-width: 75%; }

/* Desktop: > 1024px */
.message-bubble { max-width: 70%; }
```

## Accessibility

- **ARIA Labels**: All interactive elements have titles
- **Keyboard Navigation**: Tab through elements
- **Screen Reader**: Proper semantic HTML
- **Color Contrast**: WCAG AA compliant
- **Focus Indicators**: Visible focus states

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Performance Metrics

- **Initial Load**: < 100ms
- **Message Render**: < 16ms (60fps)
- **Resize**: Smooth 60fps during drag
- **Debug Entry Expand**: < 50ms
