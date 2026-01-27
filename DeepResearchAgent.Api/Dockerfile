# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS builder

WORKDIR /build

# Copy project files
COPY DeepResearchAgent.Api/ ./DeepResearchAgent.Api/
COPY DeepResearchAgent/ ./DeepResearchAgent/

# Restore and publish
RUN dotnet restore DeepResearchAgent.Api/DeepResearchAgent.Api.csproj

RUN dotnet publish DeepResearchAgent.Api/DeepResearchAgent.Api.csproj \
    -c Release \
    -o /app/out \
    --no-restore

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

ENV ASPNETCORE_URLS=http://+:5000

WORKDIR /app

COPY --from=builder /app/out .

# Create non-root user for security
RUN useradd -m dotnetuser && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000

ENTRYPOINT ["dotnet", "DeepResearchAgent.Api.dll"]
