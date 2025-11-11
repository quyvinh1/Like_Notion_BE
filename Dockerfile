# Giai đoạn 1: Build dự án
# Sử dụng image .NET SDK (thay 8.0 bằng phiên bản của bạn nếu khác)
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Sao chép file .sln và .csproj để restore packages
COPY TaskManager/*.sln .
COPY TaskManager/*.csproj ./TaskManager/
# (Nếu bạn có các thư mục dự án khác, hãy COPY .csproj của chúng vào đây)
RUN dotnet restore

# Sao chép toàn bộ code còn lại và build
COPY . .
WORKDIR "/src/TaskManager"
RUN dotnet build "TaskManager.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "TaskManager.csproj" -c Release -o /app/publish

# Giai đoạn 3: Runtime (Chạy thật)
# Sử dụng image runtime nhỏ gọn hơn
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManager.dll"]