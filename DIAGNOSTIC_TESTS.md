# Diagnostic Script: Check API Connection

## Quick Diagnostics - Run This

### 1. Check if Backend is Running

**PowerShell:**
```powershell
# Check if something is listening on port 5000
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | 
  Select-Object LocalAddress, LocalPort, State, @{Name='Process'; Expression={(Get-Process -Id $_.OwningProcess).Name}}
```

**Expected Output:**
```
LocalAddress LocalPort State  Process
------------ --------- ------ -------
::1          5000      LISTEN dotnet
0.0.0.0      5000      LISTEN dotnet
```

If nothing shows, backend is NOT running.

### 2. Test Backend Responds

**PowerShell:**
```powershell
Write-Host "Testing http://localhost:5000..."

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000" `
        -Method OPTIONS `
        -ErrorAction SilentlyContinue `
        -TimeoutSec 5

    Write-Host "✅ Success! Backend is responding"
    Write-Host "Status Code: $($response.StatusCode)"
} catch {
    Write-Host "❌ Failed to connect"
    Write-Host "Error: $_"
}
```

### 3. Test API Endpoint Specifically

**PowerShell:**
```powershell
Write-Host "Testing http://localhost:5000/api/chat/step..."

$uri = "http://localhost:5000/api/chat/step"

try {
    $response = Invoke-WebRequest -Uri $uri `
        -Method OPTIONS `
        -ErrorAction SilentlyContinue `
        -TimeoutSec 5

    Write-Host "✅ API endpoint is responding"
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host ""
    Write-Host "CORS Headers:"
    $response.Headers | ForEach-Object {
        if ($_ -like "Access-Control*") {
            Write-Host "  $_"
        }
    }
} catch {
    Write-Host "❌ API endpoint not responding"
    Write-Host "Error: $_"
}
```

### 4. Check Browser Console

Open your browser with the frontend running (http://localhost:5173):

1. Press **F12** or **Ctrl+Shift+I**
2. Go to **Console** tab
3. Look for errors in red
4. Check **Network** tab:
   - Click on failed request to `localhost:5000`
   - Check Headers section
   - Look for CORS errors

### 5. Test Full API Call

**PowerShell:**
```powershell
$uri = "http://localhost:5000/api/chat/step"

$body = @{
    currentState = @{
        messages = @(@{ role = "user"; content = "test" })
        supervisorMessages = @()
        rawNotes = @()
        needsQualityRepair = $true
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending POST to: $uri"
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $uri `
        -Method POST `
        -Headers @{"Content-Type"="application/json"} `
        -Body $body `
        -TimeoutSec 30 `
        -ErrorAction Stop

    Write-Host "✅ API Call Successful!"
    Write-Host "Status: $($response.StatusCode)"
    Write-Host ""
    Write-Host "Response:"
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
} catch {
    Write-Host "❌ API Call Failed"
    Write-Host "Error: $_"
    Write-Host ""
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $response = $reader.ReadToEnd()
        Write-Host "Response Body: $response"
    }
}
```

---

## Expected Results by Step

| Step | Command | Expected |
|------|---------|----------|
| 1 | Check port 5000 | Shows `dotnet` process listening |
| 2 | Test backend | Status 200 or 204 |
| 3 | Test endpoint | Status 200 or 204, CORS headers present |
| 4 | Browser console | No red errors, Network shows 200/204 |
| 5 | Full API call | Status 200, JSON response with currentStep |

---

## If Something Fails

### Port 5000 shows nothing
- **Problem**: Backend not running
- **Fix**: Start with `dotnet run --project DeepResearchAgent.Api`

### Test backend fails
- **Problem**: Backend crashed or wrong URL
- **Fix**: Check backend terminal for errors, restart it

### Test endpoint fails
- **Problem**: API not listening on /api/chat/step
- **Fix**: Verify ChatController exists and route is correct

### Browser console has red errors
- **Problem**: Frontend or network issue
- **Fix**: Copy error text and search for solution

### API call fails with 500 error
- **Problem**: Backend error processing request
- **Fix**: Check backend console for exception details

---

## Quick Status Check Script (Bash/Linux)

If using Linux/Mac terminal:

```bash
#!/bin/bash

echo "=== Deep Research Agent Diagnostic ==="
echo ""

# Check if backend is running
echo "1. Checking if port 5000 is open..."
if nc -z localhost 5000 2>/dev/null; then
    echo "✅ Port 5000 is open"
else
    echo "❌ Port 5000 is not open (backend not running?)"
fi

echo ""

# Test backend responds
echo "2. Testing backend response..."
if curl -s -o /dev/null -w "%{http_code}" http://localhost:5000 > /dev/null; then
    echo "✅ Backend is responding"
else
    echo "❌ Backend not responding"
fi

echo ""

# Test API endpoint
echo "3. Testing API endpoint..."
response=$(curl -s -o /dev/null -w "%{http_code}" -X OPTIONS http://localhost:5000/api/chat/step)
if [ "$response" = "204" ] || [ "$response" = "200" ]; then
    echo "✅ API endpoint responding (HTTP $response)"
else
    echo "❌ API endpoint error (HTTP $response)"
fi

echo ""
echo "=== Check Complete ==="
```

---

## Solution Flow Chart

```
Is backend running?
├─ NO → Start it: dotnet run --project DeepResearchAgent.Api
└─ YES ↓
   
Does port 5000 respond?
├─ NO → Restart backend
└─ YES ↓
   
Does API endpoint respond?
├─ NO → Check Program.cs for CORS/route config
└─ YES ↓
   
Does browser console show errors?
├─ CORS error → Check app.UseCors("AllowUI");
├─ Network error → Check API_BASE = 'http://localhost:5000/api/chat'
└─ NO ERRORS ↓
   
Try Again still shows?
└─ Contact support with browser console output
```

---

**Run these tests and tell me which one fails. That will pinpoint the exact issue!**
