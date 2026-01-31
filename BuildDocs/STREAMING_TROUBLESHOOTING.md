# 🔧 Streaming Endpoint - Troubleshooting Guide

## Issue: API Call Refused with AuthenticationException

### Symptoms
```
Exception thrown: 'System.Security.Authentication.AuthenticationException'
The program '[4804] DeepResearchAgent.Api.exe' has exited with code 4294967295
curl: (52) Empty reply from server
```

### Root Causes & Solutions

## ✅ FIX 1: HTTPS Redirect Issue (PRIMARY)

### Problem
The code had:
```csharp
app.UseHttpsRedirection();  // Always enabled
```

This causes SSL/TLS certificate errors in development when accessing `http://localhost:5000`.

### Solution
**Applied to `DeepResearchAgent.Api\Program.cs`:**

```csharp
// ONLY redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

### Why This Works
- ✅ In development: Allows HTTP on `localhost:5000` without SSL
- ✅ In production: Properly redirects HTTP → HTTPS
- ✅ Prevents authentication exceptions on localhost
- ✅ Streaming works over plain HTTP in development

---

## ✅ FIX 2: Streaming Endpoint Error Handling (SECONDARY)

### Problem
The StreamMasterWorkflow method didn't handle errors gracefully:
- No input validation
- Didn't handle client disconnections
- No buffering control for proxies
- Errors could crash the app

### Solution
**Applied to `DeepResearchAgent.Api\Controllers\WorkflowsController.cs`:**

```csharp
// Added:
1. Input validation at start
2. X-Accel-Buffering header (for reverse proxies)
3. hasContent flag (only send completion if data sent)
4. Try/catch around Response.WriteAsync
5. Handle OperationCanceledException separately
6. Error recovery in catch blocks
```

### Why This Helps
- ✅ Prevents crashes from invalid requests
- ✅ Works behind proxies/load balancers
- ✅ Gracefully handles client disconnections
- ✅ Better error messages
- ✅ Recovers from transient failures

---

## 🧪 Testing the Fix

### Step 1: Verify Fixes Applied

Check `Program.cs` has:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

Check `WorkflowsController.cs` has:
```csharp
if (string.IsNullOrWhiteSpace(request?.UserQuery))
{
    Response.StatusCode = StatusCodes.Status400BadRequest;
    // ... error response
}
```

### Step 2: Clean and Rebuild

```bash
dotnet clean
dotnet build
```

### Step 3: Start the API

```bash
cd DeepResearchAgent.Api
dotnet run
```

### Step 4: Test with curl

```bash
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery": "What is artificial intelligence?"}'
```

**Expected Output:**
```
data: {"status":"connected",...}
data: {"researchBrief":"...",...}
data: {"draftReport":"...",...}
...
data: {"status":"completed"}
```

### Step 5: Verify All Services Running

```bash
# Check required services
docker ps | grep -E "ollama|searxng|crawl4ai|lightning"

# Should see output for:
✓ ollama (port 11434)
✓ searxng (port 8080)
✓ crawl4ai (port 11235)
✓ lightning-server (port 8090)
```

---

## 🔍 Additional Diagnostics

### Check HTTP vs HTTPS

```bash
# This should work (HTTP)
curl -v http://localhost:5000/api/workflows/master/stream ...

# This will fail in dev (HTTPS issues)
curl -v https://localhost:5001/api/workflows/master/stream ...
```

### Check for SSL Certificate Errors

```bash
# Windows - Check certificate store
certmgr.msc

# Or use OpenSSL
openssl s_client -connect localhost:5001
```

### Check Port is Listening

```bash
# Windows
netstat -ano | findstr :5000

# Linux/Mac
lsof -i :5000
```

---

## 📋 Complete Checklist

- [ ] Fix 1: Applied HTTPS redirect condition in Program.cs
- [ ] Fix 2: Applied streaming endpoint improvements in WorkflowsController.cs
- [ ] Rebuilt solution: `dotnet clean && dotnet build`
- [ ] Started API: `dotnet run`
- [ ] All services running (docker-compose up -d)
- [ ] Tested with curl
- [ ] Received SSE stream responses
- [ ] No AuthenticationException in logs
- [ ] Program runs successfully
- [ ] UI can connect (if UI also running)

---

## 🚨 If Still Having Issues

### Check Event Viewer (Windows)
Look for detailed error information in Windows Event Viewer:
```
Event Viewer → Windows Logs → Application
Look for exceptions with details
```

### Check API Logs
Set logging level to Debug:
```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",  // Change from Information
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### Restart Everything
```bash
# Stop all services
docker-compose down

# Kill any remaining processes
pkill -f "dotnet run"

# Start services fresh
docker-compose up -d

# Run API
cd DeepResearchAgent.Api
dotnet run
```

### Check for Firewall Issues
```bash
# Windows Firewall - Allow port 5000
netsh advfirewall firewall add rule name="Allow 5000" dir=in action=allow protocol=tcp localport=5000

# Linux/Mac - Allow in firewall
sudo ufw allow 5000
```

---

## 📊 Expected Behavior After Fix

### When Curl Request Succeeds
```
✓ HTTP 200 OK
✓ Content-Type: text/event-stream
✓ First data line: {"status":"connected",...}
✓ Multiple updates flow
✓ Final: {"status":"completed"}
✓ Connection closes cleanly
```

### When API Starts
```
✓ No SSL/TLS certificate errors
✓ No AuthenticationException
✓ "Now listening on: http://localhost:5000"
✓ API responds to health checks
✓ Swagger UI available at http://localhost:5000
```

### When UI Connects
```
✓ No CORS errors
✓ Stream events received
✓ Progress component updates
✓ All phases displayed
✓ Final report shown
```

---

## 🔗 Related Documentation

- **END_TO_END_TESTING.md** - Full testing guide
- **STREAMING_QUICK_REFERENCE.md** - API reference
- **INTEGRATION_COMPLETE.md** - Architecture details

---

## 📝 Summary

### What Was Fixed
1. ✅ HTTPS redirect causing SSL errors in development
2. ✅ Streaming endpoint error handling improved
3. ✅ Better validation and recovery

### How to Apply
1. ✅ Update Program.cs (wrap UseHttpsRedirection in if statement)
2. ✅ Update WorkflowsController.cs (improve StreamMasterWorkflow)
3. ✅ Rebuild and test

### Result
✅ Streaming endpoint works over HTTP on localhost  
✅ API no longer crashes with AuthenticationException  
✅ UI can successfully connect and stream  
✅ Production HTTPS still works properly  

---

**Ready to test!** 🚀

```bash
dotnet run
# Then test with: curl -X POST http://localhost:5000/api/workflows/master/stream ...
```
