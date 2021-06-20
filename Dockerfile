FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /build

# copy csproj and restore as distinct layers
COPY *.sln .
COPY src/WebHookCatcher/*.csproj ./src/WebHookCatcher/
COPY test/EgoCatcher.Tests/*.csproj ./test/EgoCatcher.Tests/

WORKDIR /build/src/WebHookCatcher
RUN dotnet restore

# copy everything else and build app
COPY src/. ./src/
WORKDIR /build/src/WebHookCatcher
RUN dotnet publish -c Release -o /build/output

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /build

WORKDIR /app
COPY --from=build /build/output /app
run ls

ENTRYPOINT ["dotnet", "Ego.WebHookCatcher.dll"]