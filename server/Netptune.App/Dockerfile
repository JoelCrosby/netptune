ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
# Alpine ships no timezone database, and without it every TimeZoneInfo lookup silently
# falls back to UTC - automations and reports would ignore the workspace time zone.
# It also ships no ICU, and the base image runs invariant: creating any culture throws,
# so the localized resources (and the assistant's reply language) could never load.
RUN apk add --no-cache tzdata icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
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

# build app
WORKDIR "/Netptune.App"
RUN dotnet publish "Netptune.App.csproj" \
    -c Release \
    -o /app/publish \
    /p:SourceRevisionId="${COMMIT}+${GITHUB_REF}+${BUILD_NUMBER}+${RUN_ID}"

FROM base AS final
EXPOSE 7400/tcp
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Netptune.App.dll"]
