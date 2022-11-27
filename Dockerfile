ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID
WORKDIR /

# copy csproj and restore as distinct layers
COPY server/*/*.csproj ./
RUN for file in $(ls *.csproj); do \
      mkdir -p ${file%.*}/ && mv $file ${file%.*}/; \
    done
COPY server/*.sln .
RUN dotnet restore

# copy everything else
COPY /server .

# run unit tests
WORKDIR "/Netptune.UnitTests"
RUN dotnet test -c Release

# run integration tests
WORKDIR "/Netptune.IntegrationTests"
RUN dotnet test -c Release

# build app
WORKDIR "/Netptune.App"
RUN dotnet publish "Netptune.App.csproj" \
    -c Release \
    -o /app/publish \
    /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

# client app
FROM node:18 AS client-build
WORKDIR /client
COPY /client/package*.json ./
COPY /client/yarn.lock ./
RUN yarn install --immutable --immutable-cache --check-cache
COPY /client .
RUN yarn build

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=client-build /client/dist ./wwwroot/dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
