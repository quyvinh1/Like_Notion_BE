# Giai đoạn 1: Build và Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Bước 1: Copy TẤT CẢ MỌI THỨ từ repo vào /src
# Bây giờ, /src sẽ có thư mục /src/TaskManager/
COPY . .

# Bước 2: Chạy restore và chỉ chính xác file .sln
# (Nó nằm trong thư mục TaskManager)
RUN dotnet restore "TaskManager/TaskManager.sln"

# Bước 3: Chạy publish và chỉ chính xác file .csproj
# (Nó nằm trong TaskManager/TaskManager)
# Lệnh publish sẽ tự động build, chúng ta không cần build riêng
RUN dotnet publish "TaskManager/TaskManager/TaskManager.csproj" -c Release -o /app/publish

# Giai đoạn 2: Runtime (Chạy thật)
# Sử dụng image aspnet nhỏ hơn
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TaskManager.dll"]