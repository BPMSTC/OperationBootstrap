# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first for restore caching
COPY ["A New Hope/A New Hope.csproj", "A New Hope/"]

RUN dotnet restore "A New Hope/A New Hope.csproj"

# Copy the rest of the source
COPY . .

WORKDIR "/src/A New Hope"
RUN dotnet publish "A New Hope.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "A New Hope.dll"]