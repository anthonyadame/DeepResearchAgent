# Quick Start Guide: Testing the Dual-Window Chat Interface

## Prerequisites

✅ Node.js 18+ installed  
✅ npm installed  
✅ All dependencies installed (`npm install`)  

## Step 1: Start the Development Server

```bash
cd DeepResearchAgent.UI
npm run dev
```

Expected output:
```
VITE v5.0.0  ready in 450 ms

➜  Local:   http://localhost:5173/
➜  Network: use --host to expose
```

## Step 2: Open Browser

Navigate to: `http://localhost:5173`

## Step 3: Test Basic Chat

### Send User Message
1. Type "Hello, can you help me research quantum computing?" in the input box
2. Click **Send** button
3. **Expected**: Message appears on the RIGHT side with BLUE background and user avatar

### Receive Assistant Response
1. Wait for response (or mock it if backend not running)
2. **Expected**: Response appears on LEFT side with WHITE background and bot avatar

### Verify Visual Elements
- ✅ User message has blue gradient (`#3B82F6 → #8B5CF6`)
- ✅ Assistant message has white background with gray border
- ✅ Avatars are circular with icons (User/Bot)
- ✅ Timestamps show relative time ("Just now", "2m ago")
- ✅ Messages fade in smoothly

## Step 4: Test Auto-Scroll

### Scenario 1: New Message Arrives
1. Send multiple messages (5+)
2. **Expected**: Chat automatically scrolls to latest message

### Scenario 2: Scroll Up
1. Scroll up in chat history
2. **Expected**: "Scroll to bottom" button (↓) appears in bottom-right of chat area
3. Click the button
4. **Expected**: Smoothly scrolls back to latest message

## Step 5: Open Debug Console

### Toggle Console
1. Look for **gear icon (⚙️)** in bottom-right corner
2. Click the gear icon
3. **Expected**: 
   - Debug console slides up from bottom
   - Chat area shrinks to accommodate
   - Gear icon turns BLUE (active state)

### Close Console
1. Click gear icon again
2. **Expected**:
   - Debug console slides down
   - Chat area expands back to full height
   - Gear icon turns GRAY (inactive state)

## Step 6: Test Debug Console Tabs

### Messages Tab
1. Open debug console
2. Ensure "Messages" tab is selected (should be default)
3. **Expected**:
   - See all user/assistant messages logged
   - Each entry shows: `▶ message  HH:MM:SS  Copy`
   - Badge shows count (e.g., "3" in blue circle)

### State Tab
1. Click "State" tab
2. **Expected**:
   - Shows state objects (if any streamed)
   - Purple badges: `▶ state  HH:MM:SS  Copy`
   - Count badge updates

### API Calls Tab
1. Click "API Calls" tab
2. **Expected**:
   - Shows HTTP requests/responses
   - Green badges: `▶ api_call  HH:MM:SS  200  Copy`
   - Status codes visible (200, 404, etc.)
   - Error entries show in red

## Step 7: Test Debug Entry Expansion

### Expand Entry
1. Click on any collapsed debug entry (e.g., `▶ message  09:45:12`)
2. **Expected**:
   - Arrow changes to `▼` (down)
   - JSON content expands below
   - Dark background (`bg-gray-900`) with syntax highlighting
   - Content is scrollable if large

### Collapse Entry
1. Click on an expanded entry
2. **Expected**:
   - Arrow changes back to `▶` (right)
   - JSON content collapses/hides

## Step 8: Test Copy-to-Clipboard

### Copy Debug Data
1. Expand a debug entry (or leave collapsed)
2. Click the **Copy** icon on the right
3. **Expected**:
   - Icon changes to ✓ (checkmark) in green
   - After 2 seconds, reverts back to copy icon
4. Paste (Ctrl+V) in a text editor
5. **Expected**: Valid JSON pasted

## Step 9: Test Resize Functionality

### Drag Divider
1. Ensure debug console is open
2. Locate the horizontal divider between chat and debug console
3. Hover over divider
4. **Expected**: 
   - Cursor changes to `ns-resize` (vertical resize arrows)
   - Grip icon (⋮⋮⋮) appears on hover
   - Divider highlights blue

### Resize Up (Enlarge Debug Console)
1. Click and hold on divider
2. Drag **upward**
3. **Expected**:
   - Debug console grows
   - Chat area shrinks
   - Resize is smooth (no lag)
   - Stops at 70% maximum

### Resize Down (Enlarge Chat Area)
1. Click and hold on divider
2. Drag **downward**
3. **Expected**:
   - Debug console shrinks
   - Chat area grows
   - Stops at 10% minimum

### Release Mouse
1. Release mouse button
2. **Expected**:
   - Resize stops at current position
   - Height is persisted (survives refresh)

## Step 10: Test Clear Console

### Clear All Entries
1. Open debug console
2. Click **trash icon (🗑️)** in top-right of console header
3. **Expected**:
   - All debug entries disappear
   - Empty state message appears: "No [tab] logged yet"
   - Count badges reset to 0

## Step 11: Test Real-Time Logging

### Send Message While Console Open
1. Open debug console
2. Switch to "Messages" tab
3. Send a new message
4. **Expected**:
   - New entry appears in real-time
   - Count badge increments
   - Entry is automatically visible (no scroll needed)

### API Call Logging
1. Switch to "API Calls" tab
2. Send a message
3. **Expected**:
   - See `POST /chat/{sessionId}/query` entry
   - Direction: ↑ (sent)
   - See response entry
   - Direction: ↓ (received)
   - Status code: 200 (green badge)

## Step 12: Edge Cases

### Very Long Message
1. Send a message with 500+ characters
2. **Expected**:
   - Message bubble wraps text (no horizontal scroll)
   - Max width: 70% of container
   - Readable on mobile/tablet

### Empty Message
1. Try to send empty message
2. **Expected**: Send button is disabled (grayed out)

### Rapid Messages
1. Send 10 messages quickly
2. **Expected**:
   - All messages appear
   - Auto-scroll keeps up
   - No visual glitches

### Resize During Drag
1. Open debug console
2. Start dragging divider
3. **Expected**:
   - Smooth, immediate visual feedback
   - No transition animation during drag
   - Transition resumes after release

## Troubleshooting

### Console Not Appearing
- **Check**: Gear icon is clickable (not disabled)
- **Verify**: Browser console has no errors
- **Try**: Hard refresh (Ctrl+Shift+R)

### Messages Not Logging
- **Check**: Browser console for errors
- **Verify**: `useDebugLogger` hook is imported
- **Try**: Send a test message and check React DevTools

### Resize Not Working
- **Check**: Mouse cursor changes on hover
- **Verify**: No CSS conflicts (check browser DevTools)
- **Try**: Refresh and test again

### TypeScript Errors
- **Run**: `npm run type-check`
- **Fix**: Any type errors shown
- **Verify**: All imports resolve correctly

## Success Checklist

- [ ] User messages appear right-aligned, blue gradient
- [ ] Assistant messages appear left-aligned, white background
- [ ] Avatars show (User icon / Bot icon)
- [ ] Timestamps display relative time
- [ ] Auto-scroll works on new messages
- [ ] Scroll-to-bottom button appears/works
- [ ] Gear icon toggles debug console
- [ ] Debug console has 3 tabs (Messages, State, API Calls)
- [ ] Debug entries are collapsible
- [ ] Copy-to-clipboard works
- [ ] Resize divider works (drag up/down)
- [ ] Clear console button works
- [ ] Real-time logging captures all events

## Performance Benchmarks

| Metric | Target | Notes |
|--------|--------|-------|
| Message Render | < 16ms | 60fps |
| Console Toggle | < 200ms | Smooth animation |
| Resize FPS | 60fps | No lag during drag |
| Debug Entry Expand | < 50ms | Instant feedback |
| Copy-to-Clipboard | < 10ms | Native API |

## Next Steps After Testing

1. ✅ Verify all features work as expected
2. ✅ Test on different browsers (Chrome, Firefox, Safari, Edge)
3. ✅ Test on mobile/tablet (responsive design)
4. ✅ Check performance with 50+ messages
5. ✅ Run full build: `npm run build`
6. ✅ Deploy to staging environment
7. ✅ Gather user feedback
8. ✅ Iterate based on feedback

---

**Ready to test?** Run `npm run dev` and follow the steps above! 🚀
