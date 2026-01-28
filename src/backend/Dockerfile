FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY Loremaster.Domain/Loremaster.Domain.csproj Loremaster.Domain/
COPY Loremaster.Application/Loremaster.Application.csproj Loremaster.Application/
COPY Loremaster.Infrastructure/Loremaster.Infrastructure.csproj Loremaster.Infrastructure/
COPY Loremaster.Api/Loremaster.Api.csproj Loremaster.Api/
RUN dotnet restore Loremaster.Api/Loremaster.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish Loremaster.Api/Loremaster.Api.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Loremaster.Api.dll"]
