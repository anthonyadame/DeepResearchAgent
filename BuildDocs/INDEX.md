# BuildDocs Index

This folder centralizes internal engineering documentation for the Deep Research Agent project.

## 📂 Structure

### Architecture
`Architecture/` - System diagrams, components, and interfaces.

### Decisions (ADRs)
`Decisions/` - Architectural Decision Records documenting key technical choices.

### Design
`Design/` - Design documents and UI/UX planning.

### Troubleshooting
`Troubleshooting/` - Common issues, fixes, and workarounds.
- Sidebar issues and fixes
- DI and configuration errors
- Build error resolutions

### Verification & Reports
`Verification/` - Benchmarks, validation runs, and verification reports.

### Migration & Modernization
`Migration/` - Framework upgrades, cloud moves, and tech transitions.

### UI Development
`UI/` - Internal development guides for the React/TypeScript UI.
- [UI Development Guide](UI/DEVELOPMENT.md)

## 📋 Implementation Docs

### Phase 1: Foundation
- [API Configuration Alignment](DeepResearchAgent.Api-Configuration-Alignment.md)
- [API Fixed Quick Start](API_FIXED_QUICK_START.md)
- [Build Errors Fixed](BUILD_ERRORS_FIXED.md)
- [DI Error Fixed](DI_ERROR_FIXED.md)

### Phase 2: Backend Integration & UI
- [Phase 2 Backend Integration Plan](PHASE2_BACKEND_INTEGRATION_PLAN.md)
- [Phase 2 Implementation Complete](PHASE2_IMPLEMENTATION_COMPLETE.md)
- [Phase 2 Ready to Test](PHASE2_READY_TO_TEST.md)
- [Quick Start Phase 2](QUICK_START_PHASE2.md)
- [UI Implementation Phase 1](UI_IMPLEMENTATION_PHASE1.md)
- [Sidebar Fix Applied](SIDEBAR_FIX_APPLIED.md)
- [Sidebar Troubleshooting](SIDEBAR_TROUBLESHOOTING.md)

### Iterative Agent Implementation
- [Clarify Iterative Agent README](CLARIFY_ITERATIVE_AGENT_README.md)
- [Clarify Iterative Implementation Summary](CLARIFY_ITERATIVE_IMPLEMENTATION_SUMMARY.md)
- [Clarify Iterative Quick Start](CLARIFY_ITERATIVE_QUICK_START.md)

### Testing
- [Testing Guide](TESTING_GUIDE.md)

### Final Steps
- [Implementation Summary Final](IMPLEMENTATION_SUMMARY_FINAL.md)
- [Next Steps Complete](NEXT_STEPS_COMPLETE.md)

## 🎯 Conventions

- **Consumer-facing docs** (README.md, setup guides) live at project roots (e.g., `DeepResearchAgent.Api/README.md`, `DeepResearchAgent.UI/README.md`).
- **Internal process notes**, experiments, validation, and operational runbooks live here under `BuildDocs/`.
- Cross-link from root READMEs to this index for deeper details.

## 🔗 Links

- [Root README](../README.md) - Project overview and quick start
- [API README](../DeepResearchAgent.Api/README.md) - API usage and endpoints
- [UI README](../DeepResearchAgent.UI/README.md) - UI setup and features
- [DLL README](../DeepResearchAgent/README.md) - Core library documentation
