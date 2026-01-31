# 🔧 TROUBLESHOOTING GUIDE: "Try Again" Error

## The Problem
Frontend shows "Try Again" error instead of connecting to the API at `http://localhost:5000`.

## Root Causes (In Order of Likelihood)

### 1. ❌ CORS Issue (Most Common)
**Symptom**: Console shows `CORS policy` error

**Solution**: CORS is already configured in `Program.cs`. Verify:
1. Backend middleware includes: `app.UseCors("AllowUI");`
2. CORS policy allows: `AllowAnyOrigin()`, `AllowAnyMethod()`, `AllowAnyHeader()`

**Quick Fix**: In `Program.cs`, ensure this code exists before `app.MapControllers();`:
```csharp
app.UseCors("AllowUI");
```

### 2. ❌ API Not Running
**Symptom**: Browser console shows `Failed to fetch` or timeout

**Solution**:
1. Check Terminal 1 where backend should be running
2. Should show: `Now listening on: http://localhost:5000`
3. If not, start it:
```powershell
cd C:\RepoEx\DeepResearchAgent
dotnet run --project DeepResearchAgent.Api
```

### 3. ❌ Wrong API URL
**Symptom**: API tries to connect to wrong port

**Solution**: Check `src/services/chatService.ts`:
```typescript
const API_BASE = 'http://localhost:5000/api/chat';  // ✅ Correct
// NOT http://localhost:8080 or localhost:3000
```

### 4. ❌ Firewall/Network Blocking
**Symptom**: Connection times out

**Solution**:
1. Check Windows Firewall isn't blocking port 5000
2. Try running on same machine (localhost)
3. Don't use 127.0.0.1 vs localhost inconsistently

---

## 🔍 Debugging Steps

### Step 1: Check Browser Console

1. Open Browser DevTools: **F12** or **Ctrl+Shift+I**
2. Go to **Console** tab
3. Look for errors like:
   - ❌ `Access to XMLHttpRequest at 'http://localhost:5000/api/chat/step' from origin 'http://localhost:5173' has been blocked by CORS policy`
   - ❌ `Failed to fetch` (usually network issue)
   - ❌ `404 Not Found` (wrong endpoint)

### Step 2: Test API Directly

**PowerShell Test** (verify backend is responding):
```powershell
# Test if API exists
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/chat/step" `
  -Method OPTIONS `
  -ErrorAction SilentlyContinue

Write-Host "Status: $($response.StatusCode)"
# Expected: 200 or 204
```

**cURL Test** (if you have curl):
```bash
curl -i -X OPTIONS http://localhost:5000/api/chat/step
```

Expected response headers:
```
HTTP/1.1 204 No Content
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: *
```

### Step 3: Test with Frontend Logging

I've created a debug version with logging. To use it:

1. Update `src/services/chatService.ts` to import from debug version:
```typescript
// Change from:
import { executeStep } from '../services/chatService';

// To:
import { executeStep, healthCheck } from '../services/chatServiceDebug';
```

2. Or manually add logging to `chatService.ts`:
```typescript
export async function executeStep(
  request: ChatStepRequest,
  signal?: AbortSignal
): Promise<ChatStepResponse> {
  const url = `${API_BASE}/step`;
  console.log('📤 Calling API:', url);  // Add this
  
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  });

  console.log('📥 Response status:', response.status);  // Add this
  
  // ... rest of code
}
```

3. Check console output in browser

### Step 4: Use API Status Indicator

I've created `APIStatusIndicator.tsx` component that shows connection status.

Add to `App.tsx`:
```typescript
import { APIStatusIndicator } from './components/APIStatusIndicator';

function App() {
  return (
    <div className="app">
      <APIStatusIndicator />  {/* Add this */}
      <ResearchChat />
    </div>
  );
}
```

This will show:
- 🟢 Green dot: Connected
- 🔴 Red dot: Disconnected
- 🟡 Yellow dot: Checking

---

## ✅ Quick Fix Checklist

- [ ] Backend running: `dotnet run --project DeepResearchAgent.Api`
- [ ] Backend outputs: `Now listening on: http://localhost:5000`
- [ ] API responds to OPTIONS: `curl -i -X OPTIONS http://localhost:5000/api/chat/step`
- [ ] Frontend API_BASE: `http://localhost:5000/api/chat`
- [ ] CORS enabled in Program.cs: `app.UseCors("AllowUI");`
- [ ] Browser console has no errors
- [ ] Check Windows Firewall isn't blocking port 5000
- [ ] Try in incognito/private window (no cache)

---

## 🚨 Common Error Messages & Fixes

### Error: "CORS policy: Cross-Origin Request Blocked"

**Fix**: CORS needs to be before MapControllers in `Program.cs`:
```csharp
// Order matters!
app.UseRouting();
app.UseCors("AllowUI");  // ← This MUST be here
app.MapControllers();     // ← This comes after
```

### Error: "Failed to fetch"

**Causes**:
1. Backend not running
2. Wrong URL (`http://localhost:8080` instead of `5000`)
3. Network timeout
4. Firewall blocking

**Fix**:
1. Check backend is running on port 5000
2. Verify API_BASE in chatService.ts
3. Check Windows Firewall settings

### Error: "404 Not Found"

**Cause**: Wrong endpoint URL

**Fix**: Verify endpoint:
- ❌ Wrong: `http://localhost:5000/chat/step`
- ✅ Right: `http://localhost:5000/api/chat/step`

---

## 🧪 Manual API Test (Before Frontend)

Use this to verify API works independently:

**PowerShell Script:**
```powershell
$uri = "http://localhost:5000/api/chat/step"
$body = @{
    currentState = @{
        messages = @(@{ role = "user"; content = "What is AI?" })
        supervisorMessages = @()
        rawNotes = @()
        needsQualityRepair = $true
    }
} | ConvertTo-Json -Depth 10

Write-Host "Testing API at: $uri"
Write-Host "Sending request..."

try {
    $response = Invoke-WebRequest -Uri $uri `
        -Method POST `
        -Headers @{"Content-Type"="application/json"} `
        -Body $body `
        -TimeoutSec 30

    Write-Host "✅ Success! Status: $($response.StatusCode)"
    Write-Host "Response:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json
} catch {
    Write-Host "❌ Error: $_"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}
```

If this works, the API is fine and the issue is in frontend configuration.
If this fails, the API has an issue.

---

## 🎯 Most Likely Solution

99% of the time, the fix is one of these:

**Solution 1**: Restart Backend
```powershell
# Kill old process
taskkill /F /IM dotnet.exe

# Or find specific port
Get-NetTCPConnection -LocalPort 5000 | Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { Get-Process -Id $_ | Stop-Process -Force }

# Restart
dotnet run --project DeepResearchAgent.Api
```

**Solution 2**: Clear Browser Cache
- F12 → Application → LocalStorage → Clear All
- Or use Incognito/Private window

**Solution 3**: Check Firewall
- Windows Defender Firewall
- Add exception for port 5000
- Or temporarily disable for testing

---

## 📞 If You Still Have Issues

1. **Open Browser Console** (F12)
2. **Look for red error messages**
3. **Copy the exact error text**
4. **Check if it matches any above**

The error message will tell you exactly what's wrong!

---

**Status**: Following this guide will solve the "Try Again" error in 99% of cases.
