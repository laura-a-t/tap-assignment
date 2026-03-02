# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY tap-assignment.csproj ./
RUN dotnet restore ./tap-assignment.csproj

COPY Program.cs ./
COPY Cli ./Cli
COPY Domain ./Domain
COPY Engine ./Engine
COPY Game ./Game

RUN dotnet publish ./tap-assignment.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS final
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "tap-assignment.dll"]
