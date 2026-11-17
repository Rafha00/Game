# ขั้นตอนที่ 1: ใช้ .NET 10 SDK เพื่อ Build โปรเจกต์
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# คัดลอกไฟล์โปรเจกต์และโหลด Package ที่จำเป็น
COPY *.csproj ./
RUN dotnet restore

# คัดลอกโค้ดทั้งหมดและสั่ง Publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ขั้นตอนที่ 2: ใช้ ASP.NET Runtime สำหรับรันเว็บไซต์ (ขนาดไฟล์เล็กและเบา)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# เปิด Port 8080 สำหรับเข้าใช้งานเว็บใน Container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Game.dll"]

ENV ASPNETCORE_ENVIRONMENT=Development