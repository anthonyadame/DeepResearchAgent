# Quick Fix: Sidebar Not Showing

## Issue
You're only seeing the welcome screen without the sidebar.

## Solution Steps

### 1. **Hard Refresh the Browser**
- **Windows/Linux**: Press `Ctrl + Shift + R` or `Ctrl + F5`
- **Mac**: Press `Cmd + Shift + R`

This clears the cached JavaScript and loads the new components.

### 2. **Check Browser Console**
1. Open browser DevTools (`F12`)
2. Check the Console tab for errors
3. Look for:
   - Import errors
   - Component errors
   - 404 errors for missing files

### 3. **Verify Dev Server Restart**
```bash
# Stop the current server (Ctrl+C)
# Then restart
cd DeepResearchAgent.UI
npm run dev
```

### 4. **Check the Network Tab**
1. Open DevTools → Network tab
2. Refresh the page
3. Verify these files load:
   - `Sidebar.tsx`
   - `ChatHistoryPanel.tsx`
   - `ThemeContext.tsx`
   - `ThemeDialog.tsx`

### 5. **Check Responsive View**
The sidebar might be hidden on mobile view:
1. Press `F12` to open DevTools
2. Click the device icon (Toggle device toolbar)
3. Change to "Desktop" view
4. Or resize browser window wider than 1024px

### 6. **Clear localStorage**
```javascript
// In browser console (F12):
localStorage.clear()
location.reload()
```

## Expected Behavior

### Desktop (>1024px)
- Sidebar visible on the left (dark gray/black)
- Width: 256px (w-64)
- Contains:
  - "Deep Research" header
  - Blue "New Chat" button
  - Chat History menu item
  - Settings menu item
  - Themes menu item

### Mobile (<1024px)
- Sidebar hidden by default
- Menu button (hamburger icon) in top-left corner
- Click menu to show sidebar as overlay

## Quick Verification

Open browser console and run:
```javascript
// Check if Sidebar component is in DOM
document.querySelector('aside')

// Should return: <aside class="fixed left-0...">
```

If it returns `null`, the component isn't rendering.

## Common Causes

### 1. Tailwind Not Compiling `lg:` Classes
**Fix:** Check `tailwind.config.ts` has correct content paths

### 2. Import Path Issues
**Check:** Browser console for "Cannot find module" errors

### 3. React Component Errors
**Check:** Console for React error boundaries

### 4. Cached Old Version
**Fix:** Hard refresh (Ctrl+Shift+R)

## Manual Check - App Structure

The App should render like this:
```
<ThemeProvider>
  <div class="flex h-screen">
    <Sidebar /> ← Should be here!
    <main class="flex-1">
      ... content ...
    </main>
  </div>
</ThemeProvider>
```

## Debugging in Browser Console

```javascript
// 1. Check if ThemeProvider rendered
document.querySelector('html').classList
// Should show 'light' or 'dark'

// 2. Check if sidebar exists in DOM
document.querySelector('aside')

// 3. Check sidebar classes
document.querySelector('aside').className
// Should include 'fixed left-0 top-0 h-screen w-64'

// 4. Check if sidebar is visible (not translated off-screen)
getComputedStyle(document.querySelector('aside')).transform
// Should be 'none' or 'matrix(1, 0, 0, 1, 0, 0)' on desktop
```

## Still Not Working?

### Check File Contents
```bash
# In terminal, check if Sidebar has the new code
cd DeepResearchAgent.UI/src/components
cat Sidebar.tsx | head -20
```

Should show:
```typescript
import { useState } from 'react'
import { Menu, Plus, MessageSquare, X, History, ChevronLeft, Settings, Palette } from 'lucide-react'
import ChatHistoryPanel from './ChatHistoryPanel'
import ThemeDialog from './ThemeDialog'
```

If not, the file didn't save correctly.

### Nuclear Option - Full Rebuild
```bash
# Stop dev server
# Delete node_modules and dist
rm -rf node_modules dist .vite

# Reinstall and rebuild
npm install
npm run dev
```

## Contact Points

If still not working, check:
1. Console errors (F12)
2. Network tab (F12 → Network)
3. Verify Vite is serving latest code (check file timestamps)

## Expected Visual

You should see:
```
┌─────────────────────────────────────┐
│ [Menu]        Deep Research  [←]    │ ← Sidebar header
├─────────────────────────────────────┤
│  ┌───────────────────────────────┐  │
│  │     + New Chat                │  │ ← Blue button
│  └───────────────────────────────┘  │
│                                     │
│  📜 Chat History                     │
│  ⚙️  Settings                        │
│  🎨 Themes                           │
└─────────────────────────────────────┘
```

Width: 256px (fixed on desktop)
Color: Dark gray/black (`bg-gray-900`)
