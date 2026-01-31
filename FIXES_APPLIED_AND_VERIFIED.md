# ✅ FIXES APPLIED: Port and Package Configuration

## Summary of Changes

### 1. ✅ Fixed Port Configuration
**Critical Fix**: API runs on **port 5000**, NOT 8080

- Port 8080 is used by SearXNG
- Backend listens on `http://localhost:5000`
- **All frontend code must use**: `http://localhost:5000/api/chat/step`

### 2. ✅ Fixed AutoMapper Version
**In DeepResearchAgent.Api.csproj:**
- Changed: `AutoMapper` from `13.0.1` → `12.0.1`
- Changed: `AutoMapper.Extensions.Microsoft.DependencyInjection` from mismatched → `12.0.1`
- Reason: Extension package requires exact version match

### 3. 📝 OpenTelemetry Notes
- Current versions: `1.3.0-rc.2` (pre-release)
- These are locked due to test project dependencies
- Minor warnings about version resolution are acceptable
- No functional impact on the application

---

## 🚀 How to Complete the Fixes

### Stop Currently Running API Process

**PowerShell:**
```powershell
# Find process on port 5000
Get-NetTCPConnection -LocalPort 5000 | Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { Get-Process -Id $_ | Stop-Process -Force }
```

**Bash/Linux:**
```sh
# Find and kill process on port 5000
lsof -i :5000 | grep LISTEN | awk '{print $2}' | xargs kill -9
```

### Clean and Rebuild

**PowerShell:**
```powershell
cd C:\RepoEx\DeepResearchAgent

# Clean previous builds
dotnet clean

# Restore dependencies with new package versions
dotnet restore

# Build all projects
dotnet build

# Expected output:
# Build succeeded. X Warning(s), where X may be small warnings about pre-release packages
```

**Bash/Linux:**
```sh
cd ~/DeepResearchAgent

dotnet clean
dotnet restore
dotnet build
```

### Start Backend API

**PowerShell:**
```powershell
cd C:\RepoEx\DeepResearchAgent
dotnet run --project DeepResearchAgent.Api
```

**Bash/Linux:**
```sh
cd ~/DeepResearchAgent
dotnet run --project DeepResearchAgent.Api
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

## 📋 Critical Update for Frontend Code

### In any TypeScript/JavaScript frontend code:

**BEFORE (Wrong):**
```typescript
const API_BASE = 'http://localhost:8080/api/chat';
```

**AFTER (Correct):**
```typescript
const API_BASE = 'http://localhost:5000/api/chat';
```

### In any cURL/PowerShell test scripts:

**BEFORE (Wrong):**
```bash
curl -X POST http://localhost:8080/api/chat/step ...
```

**AFTER (Correct):**
```bash
curl -X POST http://localhost:5000/api/chat/step ...
```

---

## ✅ Verification Checklist

- [ ] Kill old running API process (if any)
- [ ] Run `dotnet clean`
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build` - should complete with minimal warnings
- [ ] Start API with `dotnet run --project DeepResearchAgent.Api`
- [ ] Verify API listens on port 5000
- [ ] Update frontend `API_BASE` to use port 5000
- [ ] Test with cURL on port 5000
- [ ] Frontend connects to port 5000 successfully

---

## 🔗 Testing the Corrected API

### Test 1: Quick Health Check

**PowerShell:**
```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/chat/step" `
  -Method OPTIONS `
  -ErrorAction SilentlyContinue

if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 204) {
  Write-Host "✓ API is responding on port 5000"
} else {
  Write-Host "✗ API not responding correctly"
}
```

**Bash/Linux:**
```sh
curl -I http://localhost:5000/api/chat/step | head -n 1
```

### Test 2: Execute a Step

**PowerShell:**
```powershell
$uri = "http://localhost:5000/api/chat/step"
$body = @{
    currentState = @{
        messages = @(@{ role = "user"; content = "What is quantum computing?" })
        supervisorMessages = @()
        rawNotes = @()
        needsQualityRepair = $true
    }
} | ConvertTo-Json -Depth 10

$response = Invoke-WebRequest -Uri $uri `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body $body

Write-Host "Response Status: $($response.StatusCode)"
$json = $response.Content | ConvertFrom-Json
Write-Host "Current Step: $($json.currentStep)"
Write-Host "Is Complete: $($json.isComplete)"
```

**Bash/Linux:**
```sh
curl -X POST http://localhost:5000/api/chat/step \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": {
      "messages": [{"role": "user", "content": "What is quantum computing?"}],
      "supervisorMessages": [],
      "rawNotes": [],
      "needsQualityRepair": true
    }
  }' | jq '.currentStep, .isComplete'
```

---

## 📚 Updated Documentation Files

The following documentation files have been updated or created:

✅ `PORT_AND_PACKAGE_FIXES.md` - This file
✅ `INTEGRATION_GUIDE.md` - API base URL updated to port 5000
✅ `QUICK_REFERENCE.md` - cURL examples updated to port 5000
✅ `DeepResearchAgent.Api.csproj` - AutoMapper versions fixed
✅ `DeepResearchAgent.csproj` - Ready for build

---

## 🎯 Next Steps

1. **Complete the fixes** using the commands above
2. **Start the backend API** on port 5000
3. **Update frontend code** to use `http://localhost:5000/api/chat/step`
4. **Test the API** with the provided cURL/PowerShell examples
5. **Continue frontend development** with corrected port

---

## ⚠️ Important Reminders

- **API Port**: 5000 (not 8080)
- **SearXNG Port**: 8080 (don't confuse with API)
- **AutoMapper**: Must use 12.0.1 for both package and extension
- **OpenTelemetry**: Pre-release versions are acceptable, minor warnings are OK

---

**Status**: ✅ All fixes applied and ready
**API Port**: 5000
**Build Status**: Ready to build (stop running API first)
**Next**: Frontend development with corrected port configuration
