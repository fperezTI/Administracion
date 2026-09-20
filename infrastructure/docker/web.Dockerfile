# syntax=docker/dockerfile:1
FROM node:24-slim AS build
WORKDIR /src/apps/web

COPY apps/web/package*.json ./
RUN npm ci

COPY apps/web ./
RUN npm run build

FROM node:24-slim AS runtime
WORKDIR /app
RUN useradd --uid 5678 --no-create-home appuser

COPY --from=build /src/apps/web/public ./public
COPY --from=build /src/apps/web/.next/standalone ./
COPY --from=build /src/apps/web/.next/static ./.next/static

USER appuser
ENV PORT=3000
EXPOSE 3000
ENTRYPOINT ["node", "server.js"]
