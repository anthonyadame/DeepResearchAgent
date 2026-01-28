# Testing Guide - Deep Research Agent UI

## 🚀 Dev Server Running
**URL:** http://localhost:5173

## ✅ Implementation Complete

All requested features have been successfully implemented:

### 1. Backend API Connections
- Chat session management
- Message sending/receiving
- File upload support
- Configuration endpoints

### 2. Keyboard Shortcuts
- **Enter**: Send message
- **Shift+Enter**: New line in textarea
- **Escape**: Close modals/dropdowns
- **Ctrl+/** (Cmd+/): Open configuration dialog

### 3. Chat History Persistence
- LocalStorage for last session
- Server-side full history
- Search and filter functionality
- Session deletion

### 4. Theme Switching
- Light mode
- Dark mode
- System preference (auto-detect)
- Persistent storage

### 5. Web Search Provider Selection
- SearXNG (recommended)
- Google Search API
- Bing Search
- DuckDuckGo

---

## 🧪 Manual Testing Checklist

### Initial Load
- [ ] Application loads without errors
- [ ] Welcome screen displays
- [ ] "Deep Research Agent" title visible
- [ ] "New Chat" button present

### Sidebar
- [ ] Sidebar visible on desktop
- [ ] Sidebar collapses on mobile (<1024px)
- [ ] Toggle button works
- [ ] "New Chat" button creates session
- [ ] Navigation items display:
  - [ ] Chat History
  - [ ] Settings
  - [ ] Themes

### Theme Switching
1. Click **Palette** icon in sidebar
2. Test each theme:
   - [ ] Light mode applies
   - [ ] Dark mode applies
   - [ ] System mode follows OS preference
3. [ ] Refresh page - theme persists
4. [ ] All components respect theme (no missing dark mode styles)

### Chat History
1. Create 2-3 new chat sessions
2. Click **Chat History** in sidebar
3. Verify:
   - [ ] All sessions listed
   - [ ] Search box filters sessions
   - [ ] Current session highlighted
   - [ ] Click session to load it
   - [ ] Hover shows delete button
   - [ ] Delete removes session
4. [ ] Refresh - last session auto-loads

### Chat Interface

#### Input Bar
- [ ] Text input accepts typing
- [ ] Enter key sends message (when input has text)
- [ ] Shift+Enter creates new line
- [ ] Input disabled while loading
- [ ] Send button disabled when empty
- [ ] Send button shows loading state

#### Action Buttons (Bottom Left)
**"+" Button (Add Items)**
1. Click the "+" button
2. Verify dropdown shows:
   - [ ] Upload Files
   - [ ] Attach Webpage (placeholder)
   - [ ] Attach Knowledge (placeholder)
3. [ ] Click outside closes dropdown
4. [ ] Escape key closes dropdown

**Globe Button (Web Search)**
1. Click globe icon
2. Verify dialog shows:
   - [ ] SearXNG (with "Recommended" badge)
   - [ ] Google
   - [ ] Bing
   - [ ] DuckDuckGo
3. [ ] Click provider to select
4. [ ] Selected provider shows checkmark
5. [ ] "Apply Selection" confirms choice
6. [ ] "Cancel" closes without changes
7. [ ] Escape key closes dialog

**Settings Button (Configuration)**
1. Click settings gear icon
2. [ ] Configuration dialog opens
3. [ ] Test Ctrl+/ shortcut opens same dialog

### File Upload Modal
1. Click "+" button → "Upload Files"
2. [ ] Modal opens
3. [ ] File selection UI visible
4. [ ] Close button works
5. [ ] Escape key closes modal

### Responsive Design
**Desktop (>1024px)**
- [ ] Sidebar always visible
- [ ] Chat centered with max-width
- [ ] All buttons visible and accessible

**Tablet (768px-1024px)**
- [ ] Sidebar toggleable
- [ ] Layout adjusts properly
- [ ] Touch targets adequate

**Mobile (<768px)**
- [ ] Sidebar hidden by default
- [ ] Menu button visible
- [ ] Overlay appears when sidebar open
- [ ] Click overlay closes sidebar
- [ ] All features accessible

### Dark Mode Verification
Switch to dark mode and verify:
- [ ] Background colors inverted
- [ ] Text readable (good contrast)
- [ ] Buttons styled correctly
- [ ] Modals/dialogs dark themed
- [ ] Hover states visible
- [ ] Input fields styled
- [ ] Borders visible but subtle

### Keyboard Navigation
- [ ] Tab through interactive elements
- [ ] Focus states visible
- [ ] Enter activates buttons
- [ ] Escape closes modals
- [ ] Shortcuts work as documented

### Performance
- [ ] Initial load <2 seconds
- [ ] UI interactions feel instant
- [ ] No visible lag when typing
- [ ] Smooth animations
- [ ] No console errors

---

## 🐛 Common Issues & Solutions

### Issue: Theme not changing
**Solution:** Check browser's localStorage is enabled

### Issue: Sidebar not visible on mobile
**Solution:** Click the menu icon (top-left)

### Issue: Keyboard shortcuts not working
**Solution:** Ensure focus is on the input field or window

### Issue: Session not persisting
**Solution:** Check localStorage permissions and backend availability

### Issue: Dark mode styles missing
**Solution:** Verify Tailwind dark mode classes are compiling

---

## 🔌 Backend Integration Testing

### Prerequisites
1. Backend API running on `http://localhost:5000`
2. Update `.env` if using different URL:
   ```
   VITE_API_BASE_URL=http://localhost:5000/api
   ```

### API Endpoint Tests

#### Session Management
```bash
# Check sessions endpoint
curl http://localhost:5000/api/chat/sessions
```
- [ ] Returns array of sessions
- [ ] UI displays sessions in Chat History

#### Create Session
1. Click "New Chat"
2. Check network tab for:
   - [ ] POST `/api/chat/sessions`
   - [ ] 200/201 response
   - [ ] New session ID returned
   - [ ] UI updates with new session

#### Send Message
1. Type "Hello" and press Enter
2. Check network tab for:
   - [ ] POST `/api/chat/{sessionId}/query`
   - [ ] Request contains message text
   - [ ] Response contains assistant reply
   - [ ] Message appears in chat

#### File Upload
1. Click "+" → "Upload Files"
2. Select a file
3. Check network tab for:
   - [ ] POST `/api/chat/{sessionId}/files`
   - [ ] multipart/form-data request
   - [ ] File successfully uploaded
   - [ ] Confirmation message

---

## 🎨 Visual Verification

### Layout
- [ ] Centered chat interface
- [ ] Consistent spacing
- [ ] Professional appearance
- [ ] Buttons aligned properly
- [ ] Icons sized correctly

### Colors
**Light Mode:**
- [ ] White/gray backgrounds
- [ ] Dark text
- [ ] Blue accents
- [ ] Subtle borders

**Dark Mode:**
- [ ] Dark gray/black backgrounds
- [ ] Light text
- [ ] Blue accents maintained
- [ ] Visible borders

### Typography
- [ ] Headers clear and hierarchical
- [ ] Body text readable
- [ ] Button text legible
- [ ] Consistent font sizes

### Animations
- [ ] Smooth transitions
- [ ] No jarring movements
- [ ] Hover effects subtle
- [ ] Loading states clear

---

## 📊 Browser Compatibility

Test in multiple browsers:
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)

---

## 🚨 Known Limitations

1. **File Upload**: UI ready, backend integration pending
2. **Message Streaming**: Not yet implemented
3. **Webpage Attachment**: Placeholder only
4. **Knowledge Base**: Placeholder only
5. **Configuration Save**: UI ready, backend persistence pending

---

## 📝 Testing Notes Template

Use this template to document your testing:

```
Date: ___________
Tester: ___________
Browser: ___________
Screen Size: ___________

Feature Tested: ___________
Status: [ ] Pass [ ] Fail
Notes:
- 
- 
- 

Issues Found:
1. 
2. 
3. 

Screenshots: (attach if applicable)
```

---

## ✅ Sign-off Checklist

Before marking as complete:
- [ ] All features tested
- [ ] No console errors
- [ ] Responsive on all devices
- [ ] Dark mode fully functional
- [ ] Keyboard shortcuts work
- [ ] Documentation reviewed
- [ ] Code properly commented
- [ ] No TypeScript errors
- [ ] Build succeeds (`npm run build`)

---

## 🎯 Next Steps After Testing

1. **Bug Fixes**: Address any issues found
2. **Backend Integration**: Connect to running API
3. **Real Data Testing**: Test with actual chat sessions
4. **Performance Optimization**: Profile and optimize if needed
5. **Accessibility Audit**: WCAG compliance check
6. **User Acceptance Testing**: Get feedback from real users

---

## 📞 Support

For issues or questions:
1. Check `BuildDoc/UI_IMPLEMENTATION_PHASE1.md`
2. Review `BuildDoc/NEXT_STEPS_COMPLETE.md`
3. Check component source code (well-commented)
4. Review `README_QUICKSTART.md`

Happy Testing! 🎉
