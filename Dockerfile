# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj và restore
COPY TaskManager/*.csproj ./TaskManager/
RUN dotnet restore TaskManager/TaskManager.csproj

# Copy toàn bộ code và build release
COPY . ./TaskManager/
WORKDIR /app/TaskManager
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Render sẽ cung cấp biến môi trường PORT
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}
EXPOSE ${PORT:-10000}

# Start app
ENTRYPOINT ["dotnet", "TaskManager.dll"]
