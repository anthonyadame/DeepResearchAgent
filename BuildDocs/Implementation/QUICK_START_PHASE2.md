# 🚀 Phase 2 - Quick Start Card

## START BOTH SERVERS

### Terminal 1 - Backend
```bash
cd DeepResearchAgent.Api
dotnet run
```
✅ Ready when you see: `Now listening on: http://localhost:5000`

### Terminal 2 - Frontend  
```bash
cd DeepResearchAgent.UI
npm run dev
```
✅ Ready when you see: `Local: http://localhost:5173/`

---

## VERIFY IT WORKS

### 1. Open Browser
```
http://localhost:5173
```

### 2. You Should See
- ✅ Dark sidebar on left
- ✅ Chat interface in center
- ✅ Input textbox at bottom
- ✅ No welcome screen (chat loads immediately)

### 3. Test It
1. Type: **"What is machine learning?"**
2. Press **Enter**
3. Wait 10-30 seconds for research
4. See response!

---

## QUICK TESTS

### Test 1: New Chat
- Click **"+ New Chat"** in sidebar
- Send a message
- ✅ New session created

### Test 2: History
- Click **"Chat History"** in sidebar
- See all sessions
- Click one to load it
- ✅ History loaded

### Test 3: Theme
- Click **Palette icon** in sidebar
- Select **Dark** mode
- ✅ Theme changes

### Test 4: Refresh
- Press **F5**
- ✅ Same session loads with messages

---

## TROUBLESHOOTING

### Backend won't start
```bash
cd DeepResearchAgent.Api
dotnet build
# Fix any errors
dotnet run
```

### Frontend shows "Unable to connect"
1. Check backend is running
2. Visit http://localhost:5000/swagger
3. If not working, restart backend

### Nothing happens when I send a message
1. Open browser console (F12)
2. Check for errors
3. Look at Network tab
4. See if request reaches backend

---

## FILES CREATED

### Backend
- ✅ `Controllers/ChatController.cs`
- ✅ `Controllers/ConfigurationController.cs`
- ✅ `DTOs/ChatDtos.cs`
- ✅ `Services/ChatSessionService.cs`
- ✅ `Services/ChatIntegrationService.cs`
- ✅ `Program.cs` (updated)

### Frontend
- ✅ `src/App.tsx` (updated - auto-loads chat)
- ✅ All components from Phase 1

---

## API ENDPOINTS

### Sessions
```
POST   /api/chat/sessions          → Create
GET    /api/chat/sessions          → List all
DELETE /api/chat/sessions/{id}     → Delete
```

### Messages
```
POST   /api/chat/{id}/query        → Send message
GET    /api/chat/{id}/history      → Get history
```

### Config
```
GET    /api/config/models          → Available models
GET    /api/config/search-tools    → Search providers
```

---

## SUCCESS = ✅

When everything works you should be able to:
1. ✅ Open app without clicking "New Chat"
2. ✅ Send a research query
3. ✅ Receive AI-generated research report
4. ✅ Create multiple sessions
5. ✅ Switch between sessions
6. ✅ Delete sessions
7. ✅ Refresh page and see same session

---

## KEYBOARD SHORTCUTS

| Key | Action |
|-----|--------|
| Enter | Send message |
| Shift+Enter | New line |
| Escape | Close modal |
| Ctrl+/ | Settings |

---

## DOCUMENTATION

📚 Full guides in `BuildDoc/`:
- `PHASE2_IMPLEMENTATION_COMPLETE.md` - Full summary
- `PHASE2_READY_TO_TEST.md` - Testing guide
- `PHASE2_BACKEND_INTEGRATION_PLAN.md` - Implementation plan
- `TESTING_GUIDE.md` - UI testing checklist

---

## READY TO GO! 🎉

Start both servers and test the integration!

Questions? Check the documentation or review the browser console.

**Happy testing!** 🚀
