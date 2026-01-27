# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS builder

ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_VERSION=8.0

RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /build

COPY DeepResearchAgent.Api/ ./DeepResearchAgent.Api/
COPY DeepResearchAgent/ ./DeepResearchAgent/

RUN dotnet restore DeepResearchAgent.Api/DeepResearchAgent.Api.csproj

RUN dotnet publish DeepResearchAgent.Api/DeepResearchAgent.Api.csproj \
    -c Release \
    -o /app/out

# Production stage
FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_VERSION=8.0 \
    ASPNETCORE_URLS=http://+:5000

RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y aspnetcore-runtime-8.0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=builder /app/out .

# Create non-root user for security
RUN if ! id -u dotnetuser >/dev/null 2>&1; then useradd -m dotnetuser; fi && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000

ENTRYPOINT ["dotnet", "DeepResearchAgent.Api.dll"]
