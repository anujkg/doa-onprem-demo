# DOA Application — On-Prem Dockerfile
# ⚠️ Single-stage build, runs as root, no health check

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish DOA.WebApp/DOA.WebApp.csproj -c Release -o /app/publish

# ⚠️ Using SDK image for runtime (much larger than needed)
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY --from=build /app/publish .

# ⚠️ No non-root user
# ⚠️ No HEALTHCHECK instruction
# ⚠️ No .dockerignore (copies unnecessary files)

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DOA.WebApp.dll"]
