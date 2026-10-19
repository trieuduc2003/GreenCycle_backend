# ==========================================
# Multi-stage Dockerfile cho GreenCycle API
# ==========================================

# Giai đoạn 1: Build source code (.NET 8 SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj của từng tầng để cache Docker layer (tối ưu tốc độ build)
COPY ["GreenCycle.Domain/GreenCycle.Domain.csproj", "GreenCycle.Domain/"]
COPY ["GreenCycle.Application/GreenCycle.Application.csproj", "GreenCycle.Application/"]
COPY ["GreenCycle.Infrastructure/GreenCycle.Infrastructure.csproj", "GreenCycle.Infrastructure/"]
COPY ["GreenCycle.API/GreenCycle.API.csproj", "GreenCycle.API/"]

# Restore packages
RUN dotnet restore "GreenCycle.API/GreenCycle.API.csproj"

# Copy toàn bộ mã nguồn còn lại
COPY . .

# Build dự án
WORKDIR "/src/GreenCycle.API"
RUN dotnet build "GreenCycle.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish ứng dụng
FROM build AS publish
RUN dotnet publish "GreenCycle.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Runtime image (.NET 8 ASP.NET Core Runtime siêu nhẹ)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render tự động gán biến PORT (thường là 10000 hoặc 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GreenCycle.API.dll"]
