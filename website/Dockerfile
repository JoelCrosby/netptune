FROM node:22-alpine AS build
RUN apk upgrade --no-cache && npm install -g pnpm
WORKDIR /src
COPY package*.json ./
COPY pnpm-lock.yaml ./
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
COPY . .
RUN pnpm build

FROM node:22-alpine AS final
RUN apk upgrade --no-cache
COPY --from=build /src/.output /usr/share/nginx/html

WORKDIR /usr/share/nginx/html
ENV PORT="80"
EXPOSE 80

CMD ["node", "server/index.mjs"]
