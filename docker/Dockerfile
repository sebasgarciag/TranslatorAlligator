FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar archivos de solución y proyectos
COPY *.sln ./
COPY src/ ./src/

# Restaurar dependencias
RUN dotnet restore src/Translator.Api/Translator.Api.csproj

# Construir y publicar la aplicación
RUN dotnet publish "src/Translator.Api/Translator.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Environment variables - configuración para entorno Development
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
# No necesitamos especificar OPENAI_API_KEY ya que está en appsettings.Development.json

EXPOSE 80
ENTRYPOINT ["dotnet", "Translator.Api.dll"] 