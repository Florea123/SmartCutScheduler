FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SmartCutScheduler.Api/SmartCutScheduler.Api.csproj", "SmartCutScheduler.Api/"]
COPY ["SmartCutScheduler.Application/SmartCutScheduler.Application.csproj", "SmartCutScheduler.Application/"]
COPY ["SmartCutScheduler.Domain/SmartCutScheduler.Domain.csproj", "SmartCutScheduler.Domain/"]
COPY ["SmartCutScheduler.Infrastructure/SmartCutScheduler.Infrastructure.csproj", "SmartCutScheduler.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "SmartCutScheduler.Api/SmartCutScheduler.Api.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/SmartCutScheduler.Api"
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SmartCutScheduler.Api.dll"]
