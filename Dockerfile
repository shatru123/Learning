# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/LearningOS/LearningOS.csproj", "src/LearningOS/"]
RUN dotnet restore "src/LearningOS/LearningOS.csproj"

COPY . .
WORKDIR "/src/src/LearningOS"
RUN dotnet publish "LearningOS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "LearningOS.dll"]
