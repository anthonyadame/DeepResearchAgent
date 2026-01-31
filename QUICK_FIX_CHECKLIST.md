# 🔧 QUICK FIX SUMMARY

## Two Critical Issues Fixed

### 1. 🔴 CRITICAL: Port Mismatch
```
❌ WRONG:  http://localhost:8080/api/chat/step (SearXNG uses this port)
✅ RIGHT:  http://localhost:5000/api/chat/step (API listens here)
```

### 2. 📦 Package Version Mismatch
```
❌ WRONG:  AutoMapper 13.0.1 with AutoMapper.Extensions 12.0.1 (incompatible)
✅ RIGHT:  AutoMapper 12.0.1 with AutoMapper.Extensions 12.0.1 (matched)
```

---

## 🚀 What You Need to Do Right Now

### Step 1: Stop the Running API
```powershell
# PowerShell
taskkill /F /IM dotnet.exe

# OR find by port
Get-NetTCPConnection -LocalPort 5000 | Select-Object -ExpandProperty OwningProcess | 
  ForEach-Object { Get-Process -Id $_ | Stop-Process -Force }
```

```bash
# Bash/Linux
lsof -i :5000 | grep LISTEN | awk '{print $2}' | xargs kill -9
```

### Step 2: Clean and Rebuild

**PowerShell:**
```powershell
cd C:\RepoEx\DeepResearchAgent
dotnet clean
dotnet restore
dotnet build
```

**Bash/Linux:**
```sh
cd ~/DeepResearchAgent
dotnet clean
dotnet restore
dotnet build
```

### Step 3: Update Frontend Code

In `src/services/chatService.ts` or wherever you have the API URL:

```typescript
// CHANGE THIS
const API_BASE = 'http://localhost:8080/api/chat';

// TO THIS
const API_BASE = 'http://localhost:5000/api/chat';
```

### Step 4: Start API Again

**PowerShell:**
```powershell
dotnet run --project DeepResearchAgent.Api
```

**Bash/Linux:**
```sh
dotnet run --project DeepResearchAgent.Api
```

**Expected Output:**
```
Now listening on: http://localhost:5000
```

---

## ✅ Verification

### Quick Test

**PowerShell:**
```powershell
Invoke-WebRequest http://localhost:5000/api/chat/step -Method OPTIONS
```

**Bash/Linux:**
```sh
curl -I http://localhost:5000/api/chat/step
```

Should return **HTTP 200** or **HTTP 204**.

---

## 📝 Files Changed

- ✅ `DeepResearchAgent\DeepResearchAgent.csproj` - Package versions fixed
- ✅ `DeepResearchAgent.Api\DeepResearchAgent.Api.csproj` - AutoMapper versions fixed
- ✅ `INTEGRATION_GUIDE.md` - API base URL updated
- ✅ `QUICK_REFERENCE.md` - cURL examples updated

---

## 🎯 Key Points to Remember

| Item | Value |
|------|-------|
| API Port | **5000** |
| SearXNG Port | 8080 |
| API Base URL | `http://localhost:5000/api/chat` |
| AutoMapper Version | 12.0.1 |
| Build Command | `dotnet build` |
| Run Command | `dotnet run --project DeepResearchAgent.Api` |

---

**⏱️ Time to implement fixes: 5-10 minutes**

**✅ Status: Ready to proceed with frontend development**
