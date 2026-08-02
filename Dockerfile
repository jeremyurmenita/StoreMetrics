FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Install native graphics and font libraries required by SkiaSharp
USER root
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["StoreMetrics.csproj", "./"]
RUN dotnet restore "./StoreMetrics.csproj"

# Copy remaining files and build
COPY . .
WORKDIR "/src/"
RUN dotnet build "StoreMetrics.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "StoreMetrics.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r linux-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StoreMetrics.dll"]