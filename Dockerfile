# ============ STAGE 1: build ============
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Users.sln ./
COPY src/Users.Api/Users.Api.csproj                      src/Users.Api/
COPY src/Users.Application/Users.Application.csproj      src/Users.Application/
COPY src/Users.Domain/Users.Domain.csproj                src/Users.Domain/
COPY src/Users.Infrastructure/Users.Infrastructure.csproj src/Users.Infrastructure/
COPY src/Users.Tests/Users.Tests.csproj                  src/Users.Tests/

RUN dotnet restore Users.sln

COPY . .
RUN dotnet publish ./src/Users.Api/Users.Api.csproj -c Release -o /app/publish

# ============ STAGE 2: runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Users.Api.dll", "--urls", "http://0.0.0.0:8080"]