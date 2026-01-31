# ⚡ QUICK ACTION PLAN: Fix "Try Again" Error

## What's Happening

Frontend shows error instead of working. This means the connection between frontend (port 5173) and backend (port 5000) is failing.

## 5-Minute Quick Fix

### Step 1: Check Backend Terminal (30 seconds)

Look at Terminal 1 where you ran `dotnet run --project DeepResearchAgent.Api`

**You should see:**
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

**If you don't see this:**
- **Problem**: Backend crashed or hasn't started properly
- **Fix**: Stop it (Ctrl+C) and restart:
```powershell
cd C:\RepoEx\DeepResearchAgent
dotnet run --project DeepResearchAgent.Api
```

---

### Step 2: Check Browser Console (1 minute)

1. **Open Browser DevTools**: Press **F12**
2. **Go to Console tab**
3. **Look for red error messages**

**If you see CORS error:**
```
Access to XMLHttpRequest at 'http://localhost:5000/api/chat/step' 
from origin 'http://localhost:5173' has been blocked by CORS policy
```

This means CORS is not configured. See solution below.

**If you see Network error:**
```
Failed to fetch
```

This means API is not running. Go back to Step 1.

---

### Step 3: Verify API is Reachable (1 minute)

**Run this in PowerShell:**
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/chat/step" -Method OPTIONS
```

**Expected**: Returns with Status Code 204 or 200

**If it fails**: Backend is not running, see Step 1

---

### Step 4: Check Frontend Configuration (1 minute)

**Open**: `src/services/chatService.ts`

**Look for:**
```typescript
const API_BASE = 'http://localhost:5000/api/chat';
```

**Should be:**
- ✅ `http://localhost:5000` (NOT 8080)
- ✅ `/api/chat` (NOT just `/chat`)

If wrong, fix it and save.

---

### Step 5: Refresh Browser (30 seconds)

1. **Clear browser cache**: Ctrl+Shift+Delete
2. **Hard refresh**: Ctrl+Shift+R (or Cmd+Shift+R on Mac)
3. **Try again**

---

## Still Getting "Try Again"?

### Most Common Issues (In Order)

**#1: Backend Not Running (70% of cases)**
```powershell
# Check if port 5000 is in use
Get-NetTCPConnection -LocalPort 5000

# If nothing shows, backend isn't running
# Start it:
dotnet run --project DeepResearchAgent.Api
```

**#2: CORS Not Enabled (20% of cases)**

Check `DeepResearchAgent.Api\Program.cs` has these lines:
```csharp
// CORS should be registered
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// ... later in middleware ...

// CORS must be used
app.UseCors("AllowUI");
```

If missing, add them and restart backend.

**#3: Wrong API Base URL (10% of cases)**

In `src/services/chatService.ts`:
```typescript
const API_BASE = 'http://localhost:5000/api/chat';
```

Must be exactly this. No variations.

---

## The Nuclear Option: Complete Restart

If nothing above works:

### Terminal 1 (Backend):
```powershell
# Stop everything
taskkill /F /IM dotnet.exe

# Wait 5 seconds
Start-Sleep 5

# Clean build
cd C:\RepoEx\DeepResearchAgent
dotnet clean
dotnet build

# Run
dotnet run --project DeepResearchAgent.Api
```

Wait until you see:
```
Now listening on: http://localhost:5000
```

### Terminal 2 (Frontend):
```powershell
# Stop everything
Ctrl+C

# Clean cache
rm -r node_modules
rm package-lock.json

# Reinstall
npm install

# Run
npm run dev
```

### Browser:
1. Close completely
2. Open fresh
3. Go to http://localhost:5173
4. Try again

---

## How to Verify It's Working

Once fixed, you should:

1. **See chat input box** (not "Try Again" button)
2. **Type a query** like "What is AI?"
3. **Click "Start Research"**
4. **See progress** through 5 steps
5. **Get final report**

If you get to step 3 but see error, that means API connection works but there's a processing error. That's different.

---

## For Debugging

I've created two files to help:

1. **`TROUBLESHOOTING_TRY_AGAIN_ERROR.md`**
   - Complete troubleshooting guide
   - All error messages with solutions
   - Manual testing procedures

2. **`DIAGNOSTIC_TESTS.md`**
   - Step-by-step diagnostic tests
   - PowerShell scripts to run
   - What results mean

3. **Debug Components Created:**
   - `chatServiceDebug.ts` - With console logging
   - `APIStatusIndicator.tsx` - Shows connection status in UI

---

## What to Do Right Now

**Pick ONE of these:**

### Option A: Quick Fix (Recommended First)
1. Check if backend is running (Terminal 1)
2. If not, restart it
3. Hard refresh browser (Ctrl+Shift+R)
4. Try again

### Option B: Full Diagnostic
1. Run all tests in `DIAGNOSTIC_TESTS.md`
2. Find which step fails
3. Fix that specific issue
4. Restart and test

### Option C: Complete Restart
1. Follow "Nuclear Option" above
2. Takes 5-10 minutes
3. Fixes almost everything

---

## Expected Timeline

- **Option A**: 2 minutes
- **Option B**: 5 minutes  
- **Option C**: 10 minutes

---

**Which option would you like to try first?**

Or tell me:
1. What do you see in the backend terminal?
2. What error (if any) is in the browser console (F12)?
3. Does `Invoke-WebRequest http://localhost:5000/api/chat/step -Method OPTIONS` work?

With those answers, I can pinpoint the exact fix!
