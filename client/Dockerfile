FROM node:26-alpine AS build
ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID
RUN apk upgrade --no-cache && npm install -g pnpm

WORKDIR /client
COPY package*.json ./
COPY pnpm-workspace.yaml ./
COPY pnpm-lock.yaml ./
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
COPY . .
ARG NG_CONFIGURATION=production
RUN if [ "$NG_CONFIGURATION" = "production" ]; then node scripts/check-catalogues.js; fi
RUN pnpm ng build --configuration "$NG_CONFIGURATION"
RUN node scripts/embed-build-meta.js

FROM nginx:stable-alpine AS final
RUN apk upgrade --no-cache
COPY --from=build /client/dist/netptune/browser /usr/share/nginx/html
ENV NGINX_PORT=80
COPY nginx.conf /etc/nginx/templates/default.conf.template
EXPOSE 80
