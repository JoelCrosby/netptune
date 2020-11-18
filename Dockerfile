#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /
EXPOSE 80
EXPOSE 443

ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /
COPY ["server/Netptune.App/Netptune.App.csproj", "Netptune.App/"]
COPY ["server/Netptune.Services/Netptune.Services.csproj", "Netptune.Services/"]
COPY ["server/Netptune.Core/Netptune.Core.csproj", "Netptune.Core/"]
COPY ["server/Netptune.Repositories/Netptune.Repositories.csproj", "Netptune.Repositories/"]
COPY ["server/Netptune.Entities/Netptune.Entities.csproj", "Netptune.Entities/"]
COPY ["server/Netptune.Messaging/Netptune.Messaging.csproj", "Netptune.Messaging/"]
COPY ["server/Netptune.Storage/Netptune.Storage.csproj", "Netptune.Storage/"]
RUN dotnet restore "Netptune.App/Netptune.App.csproj"
COPY /server .
WORKDIR "/Netptune.App"
RUN dotnet build "Netptune.App.csproj" -c Release -o /app/build /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}"

FROM node:14 AS client-build
WORKDIR /client
COPY /client/package*.json ./
RUN npm install
COPY /client .
RUN npm run build


FROM build AS publish
RUN dotnet publish "Netptune.App.csproj" -c Release -o /app/publish /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=client-build /client/dist ./wwwroot/dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
