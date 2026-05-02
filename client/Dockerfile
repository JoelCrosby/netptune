FROM node:25-alpine AS build
ARG COMMIT
ARG GITHUB_REF
ARG BUILD_NUMBER
ARG RUN_ID
RUN apk upgrade --no-cache && npm install -g pnpm
WORKDIR /client
COPY package*.json ./
COPY pnpm-lock.yaml ./
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
COPY . .
RUN pnpm build
RUN node scripts/embed-build-meta.js

FROM nginx:stable-alpine AS final
RUN apk upgrade --no-cache
COPY --from=build /client/dist/netptune /usr/share/nginx/html
COPY nginx.conf /etc/nginx/templates/default.conf.template
EXPOSE 80
