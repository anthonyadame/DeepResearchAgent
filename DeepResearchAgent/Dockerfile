FROM ubuntu:24.04

# Set environment variables
ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_VERSION=8.0 \
    ASPNETCORE_URLS=http://+:5000 \
    LIGHTNING_SERVER_URL=http://localhost:9090

# Install dependencies
RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Install .NET 8 SDK
RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy project files
COPY DeepResearchAgent.csproj ./
RUN dotnet restore

# Copy application code
COPY . ./

# Build application
RUN dotnet publish -c Release -o out

# Create non-root user for security
RUN useradd -m -u 1000 dotnetuser && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose ports
EXPOSE 5000

# Set entrypoint
ENTRYPOINT ["dotnet", "out/DeepResearchAgent.dll"]
