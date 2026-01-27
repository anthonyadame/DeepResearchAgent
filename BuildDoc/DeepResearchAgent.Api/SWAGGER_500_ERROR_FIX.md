# Swagger 500 Error Fix - Duplicate Schema IDs

## Problem
Swagger was failing with a 500 error when trying to generate the OpenAPI specification:

```
Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException: 
Can't use schemaId "$ChatMessageDto" for type "$DeepResearchAgent.Api.DTOs.Requests.Services.ChatMessageDto". 
The same schemaId is already used for type "$DeepResearchAgent.Api.DTOs.Requests.Agents.ChatMessageDto"
```

## Root Cause
The API had **multiple duplicate DTO classes** with the same names in different namespaces causing Swagger schema ID collisions:

1. `ChatMessageDto` - in Services and Agents
2. `FindingDto` - in multiple locations  
3. `ThemeDto` - in multiple locations
4. `ContradictionDto` - in multiple locations
5. `InsightDto` - in multiple locations
6. `PatternDto` - in multiple locations
7. `FactDto` - in multiple locations
8. `AsyncOperationResponse` - in Common and Workflows

Swagger uses the simple class name as the schema ID by default, causing collisions.

## Solution Implemented

### 1. Created Shared DTOs in Common Folder

**File**: `DeepResearchAgent.Api/DTOs/Common/ChatMessageDto.cs`
```csharp
public class ChatMessageDto
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**File**: `DeepResearchAgent.Api/DTOs/Common/AnalysisModels.cs`
- `FindingDto` - Research findings/facts
- `FactDto` - Alias for findings in workflows
- `ThemeDto` - Identified themes
- `ContradictionDto` - Detected contradictions
- `InsightDto` - Synthesized insights
- `PatternDto` - Identified patterns

### 2. Removed ALL Duplicate Definitions

Updated files to use shared versions from Common:

- ✅ `LlmInvokeRequest.cs` - Removed `ChatMessageDto`
- ✅ `ClarifyAgentRequest.cs` - Removed `ChatMessageDto`
- ✅ `AnalystAgentRequest.cs` - Removed `FindingDto`
- ✅ `AnalystAgentResponse.cs` - Removed `ThemeDto`, `ContradictionDto`, `InsightDto`, `PatternDto`
- ✅ `WorkflowResponses.cs` - Removed `FactDto`, `AsyncOperationResponse`

All now import: `using DeepResearchAgent.Api.DTOs.Common;`

### 3. Enhanced Swagger Configuration with Custom Schema IDs

**File**: `DeepResearchAgent.Api/Extensions/ServiceCollectionExtensions.cs`

```csharp
options.CustomSchemaIds(type => 
{
    if (type.FullName != null && type.FullName.Contains("DeepResearchAgent.Api.DTOs"))
    {
        // For API DTOs, use simple name if in Common, otherwise include parent folder
        if (type.FullName.Contains(".Common."))
        {
            return type.Name;
        }
        else
        {
            // Extract parent folder name (e.g., "Services", "Agents", "Workflows")
            var parts = type.FullName.Split('.');
            var dtoIndex = Array.IndexOf(parts, "DTOs");
            if (dtoIndex >= 0 && dtoIndex + 2 < parts.Length)
            {
                var category = parts[dtoIndex + 1]; // Requests/Responses
                var subcategory = parts[dtoIndex + 2]; // Services/Agents/Workflows
                return $"{subcategory}{category}{type.Name}";
            }
        }
    }
    return type.FullName?.Replace("+", ".") ?? type.Name;
});
```

This ensures:
- DTOs in `Common` use simple names (e.g., `ChatMessageDto`, `FindingDto`)
- DTOs in specific folders get prefixed to avoid conflicts
- No more schema ID collisions

## Benefits

✅ **Zero Duplication** - Single source of truth for all shared DTOs  
✅ **No Schema Conflicts** - Custom schema IDs ensure uniqueness  
✅ **Better Organization** - Common DTOs centralized and reusable  
✅ **Improved Maintainability** - Update once, use everywhere  
✅ **Swagger Working** - OpenAPI specification generates successfully  
✅ **Type Safety** - Consistent models across all endpoints  

## Shared DTOs Catalog

### Common Folder (`DeepResearchAgent.Api/DTOs/Common/`)

| DTO | Purpose | Used By |
|-----|---------|---------|
| `ChatMessageDto` | LLM conversation messages | Services, Agents |
| `FindingDto` | Research findings/facts | Agents, Workflows |
| `FactDto` | Workflow fact (extends FindingDto) | Workflows |
| `ThemeDto` | Identified research themes | Analyst responses |
| `ContradictionDto` | Detected contradictions | Analyst responses |
| `InsightDto` | Synthesized insights | Analyst responses |
| `PatternDto` | Identified patterns | Analyst responses |
| `SessionContextDto` | Session tracking | All requests |
| `ApiResponse<T>` | Standard response wrapper | All endpoints |
| `ApiError` | Error details | All endpoints |
| `AsyncOperationResponse` | Background job tracking | Workflows |
| `ApiMetadata` | Operation metadata | All responses |

## Files Modified

### Created:
1. ✅ `DeepResearchAgent.Api/DTOs/Common/ChatMessageDto.cs` - **NEW**
2. ✅ `DeepResearchAgent.Api/DTOs/Common/AnalysisModels.cs` - **NEW**

### Modified:
3. ✅ `DeepResearchAgent.Api/DTOs/Requests/Services/LlmInvokeRequest.cs`
4. ✅ `DeepResearchAgent.Api/DTOs/Requests/Agents/ClarifyAgentRequest.cs`
5. ✅ `DeepResearchAgent.Api/DTOs/Requests/Agents/AnalystAgentRequest.cs`
6. ✅ `DeepResearchAgent.Api/DTOs/Responses/Agents/AnalystAgentResponse.cs`
7. ✅ `DeepResearchAgent.Api/DTOs/Responses/Workflows/WorkflowResponses.cs`
8. ✅ `DeepResearchAgent.Api/Extensions/ServiceCollectionExtensions.cs`

## Best Practices Going Forward

### 1. Always Check for Duplicates Before Creating DTOs
```bash
# PowerShell command to find duplicates:
Get-ChildItem -Path "DeepResearchAgent.Api\DTOs" -Recurse -Filter "*.cs" | 
  Select-String -Pattern "^public class " | 
  Group-Object -Property Line | 
  Where-Object { $_.Count -gt 1 }
```

### 2. Use Common Folder for Shared DTOs
Place reusable DTOs in `DeepResearchAgent.Api/DTOs/Common/`:
- Conversation models (`ChatMessageDto`)
- Research models (`FindingDto`, `FactDto`)
- Analysis models (`ThemeDto`, `InsightDto`, `PatternDto`, `ContradictionDto`)
- Operation models (`SessionContextDto`, `AsyncOperationResponse`)
- Response wrappers (`ApiResponse<T>`, `ApiError`)

### 3. Keep Domain-Specific DTOs in Their Folders
- Workflow-specific DTOs → `DTOs/Requests/Workflows/` or `DTOs/Responses/Workflows/`
- Agent-specific DTOs → `DTOs/Requests/Agents/` or `DTOs/Responses/Agents/`
- Service-specific DTOs → `DTOs/Requests/Services/` or `DTOs/Responses/Services/`

### 4. Test Swagger After DTO Changes
Always verify Swagger after adding/modifying DTOs:
```
https://localhost:5001/
https://localhost:5001/swagger/v1/swagger.json
```

### 5. Use Inheritance When Appropriate
```csharp
// Good: FactDto extends FindingDto for workflow-specific needs
public class FactDto : FindingDto
{
    public string? Category { get; set; }
}
```

## Verification

✅ Build successful  
✅ No compilation errors  
✅ No duplicate class names  
✅ Swagger JSON generates without errors  
✅ Swagger UI loads correctly  
✅ All endpoints visible and testable  
✅ No schema ID conflicts  

## Prevention Checklist

Before committing new DTOs:

- [ ] Check if a similar DTO already exists in Common
- [ ] Run duplicate detection command
- [ ] Build project successfully
- [ ] Test Swagger UI loads
- [ ] Verify Swagger JSON generates
- [ ] Check for schema ID conflicts in browser console

## Related Documentation

- [Swagger Configuration Guide](./SWAGGER_CONFIGURATION.md)
- [Quick Start Guide](./QUICK_START.md)
- [Swashbuckle Schema IDs](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#customizing-schema-ids)
