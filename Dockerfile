ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /
COPY server/*/*.csproj ./
RUN for file in $(ls *.csproj); do \
      mkdir -p ${file%.*}/ && mv $file ${file%.*}/; \
    done

FROM node:14 AS client-build
WORKDIR /client
COPY /client/package*.json ./
COPY /client/yarn.lock ./
RUN yarn
COPY /client .
RUN yarn build


FROM build AS publish
ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID
COPY /server .
WORKDIR "/Netptune.App"
RUN dotnet publish "Netptune.App.csproj" -c Release -o /app/publish /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

FROM base AS final
WORKDIR /app
COPY nginx.conf.sigil .
COPY --from=publish /app/publish .
COPY --from=client-build /client/dist ./wwwroot/dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
