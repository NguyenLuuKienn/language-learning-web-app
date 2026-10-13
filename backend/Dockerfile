# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/LanguageLearning.Domain/LanguageLearning.Domain.csproj", "src/LanguageLearning.Domain/"]
COPY ["src/LanguageLearning.Application/LanguageLearning.Application.csproj", "src/LanguageLearning.Application/"]
COPY ["src/LanguageLearning.Infrastructure/LanguageLearning.Infrastructure.csproj", "src/LanguageLearning.Infrastructure/"]
COPY ["src/LanguageLearning.Api/LanguageLearning.Api.csproj", "src/LanguageLearning.Api/"]
COPY ["tests/LanguageLearning.Tests/LanguageLearning.Tests.csproj", "tests/LanguageLearning.Tests/"]
COPY ["LanguageLearning.slnx", "./"]

RUN dotnet restore "src/LanguageLearning.Api/LanguageLearning.Api.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/src/LanguageLearning.Api"
RUN dotnet build "LanguageLearning.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LanguageLearning.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LanguageLearning.Api.dll"]
