#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /
COPY ["Netptune.App/Netptune.App.csproj", "Netptune.App/"]
COPY ["Netptune.Services/Netptune.Services.csproj", "Netptune.Services/"]
COPY ["Netptune.Core/Netptune.Core.csproj", "Netptune.Core/"]
COPY ["Netptune.Repositories/Netptune.Repositories.csproj", "Netptune.Repositories/"]
COPY ["Netptune.Entities/Netptune.Entities.csproj", "Netptune.Entities/"]
COPY ["Netptune.Messaging/Netptune.Messaging.csproj", "Netptune.Messaging/"]
COPY ["Netptune.Storage/Netptune.Storage.csproj", "Netptune.Storage/"]
RUN dotnet restore "Netptune.App/Netptune.App.csproj"
COPY . .
WORKDIR "/Netptune.App"
RUN dotnet build "Netptune.App.csproj" -c Release -o /app/build

FROM node:14 AS client-build
WORKDIR /client
COPY /Client/package*.json ./
RUN npm install
COPY /Client .
RUN npm run build


FROM build AS publish
RUN dotnet publish "Netptune.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=client-build /client/dist ./dist
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
