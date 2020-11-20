#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /
COPY server/*/*.csproj ./
RUN for file in $(ls *.csproj); do \
      mkdir -p ${file%.*}/ && mv $file ${file%.*}/; \
    done

RUN dotnet restore "Netptune.App/Netptune.App.csproj"
COPY /server .
WORKDIR "/Netptune.App"
RUN dotnet build "Netptune.App.csproj" -c Release -o /app/build /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

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
RUN dotnet publish "Netptune.App.csproj" -c Release -o /app/publish /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=client-build /client/dist ./wwwroot/dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
