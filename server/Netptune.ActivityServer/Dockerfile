ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
# Alpine ships no timezone database, and without it every TimeZoneInfo lookup silently
# falls back to UTC - automations and reports would ignore the workspace time zone.
RUN apk add --no-cache tzdata
WORKDIR /

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
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
COPY server/*.slnx .
COPY server/Directory.Packages.props .
COPY server/Directory.Build.props .
RUN dotnet restore

# copy everything else
COPY /server .

# build activity server
WORKDIR "/Netptune.ActivityServer"
RUN dotnet publish "Netptune.ActivityServer.csproj" \
    -c Release \
    -o /app/publish \
    /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

FROM base AS final
EXPOSE 8080/tcp
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Netptune.ActivityServer.dll"]
