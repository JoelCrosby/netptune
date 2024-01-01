ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
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

# build app
WORKDIR "/Netptune.App"
RUN dotnet publish "Netptune.App.csproj" \
    -c Release \
    -o /app/publish \
    /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

# client app
FROM node:20-slim AS client-build
WORKDIR /client
COPY /client/package*.json ./
COPY /client/pnpm-lock.yaml ./
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
COPY /client .
RUN pnpm build

FROM base AS final
EXPOSE 4800/tcp
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=client-build /client/dist/browser ./wwwroot/dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
