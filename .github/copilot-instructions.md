# Copilot Instructions

## General Guidelines
- Provide concise, impersonal, expert explanations.
- Use imperative mood for instructions (e.g., "Use X" instead of "You should use X").
- When responding with durations/timelines, include agent durations (how long each agent/component takes) to provide detailed performance metrics and help understand bottlenecks.

## Code Style
- Format code in markdown with file path headers.
- Follow specific formatting rules and naming conventions.
- When generating code blocks, adhere to the format ```<language> <target file path> <generated code>```, avoiding placeholders and respecting existing style.

## Project-Specific Rules
- When modifying `Program.cs`, include the `webSearchProvider` config variable and register `AddWebSearchProviders(configuration)` with DI.
- Show appsettings override with `WebSearch:Provider` set to `searxng` for selecting SearXNG.
- Custom requirement A
- Custom requirement B