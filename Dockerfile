# Giai đoạn 1: Build và Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TaskManager/TaskManager.sln"
RUN dotnet publish "TaskManager/TaskManager/TaskManager.csproj" -c Release -o /app/publish

# Giai đoạn 2: Runtime (Chạy thật)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# === THÊM DÒNG NÀY VÀO ===
# Cài đặt thư viện ICU mà .NET cần
RUN apk add --no-cache icu-libs
# =========================

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TaskManager.dll"]