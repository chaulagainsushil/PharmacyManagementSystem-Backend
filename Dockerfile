# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and all project files first (better layer caching)
COPY PMS.sln ./
COPY PMS/PMS.csproj                                 PMS/
COPY PMS.Application/PMS.Application.csproj         PMS.Application/
COPY PMS.Repository/PMS.Repository.csproj           PMS.Repository/
COPY PMS.Models/PMS.Models.csproj                   PMS.Models/
COPY PMS.Common/PMS.Common.csproj                   PMS.Common/
COPY PMS.Tests/PMS.Tests.csproj                     PMS.Tests/

# Restore dependencies
RUN dotnet restore PMS.sln

# Copy all source code
COPY . .

# Publish the web project in Release mode
RUN dotnet publish PMS/PMS.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used by the compose healthcheck (not shipped in the aspnet image)
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd --system --gid 1001 appgroup && \
    useradd  --system --uid 1001 --gid appgroup appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app
USER appuser

# Expose HTTP port
EXPOSE 8080

# Override default URL to listen on 0.0.0.0:8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PMS.dll"]
