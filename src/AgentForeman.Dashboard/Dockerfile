FROM node:22-bookworm-slim AS deps
WORKDIR /app
COPY src/AgentForeman.Dashboard/package.json src/AgentForeman.Dashboard/package-lock.json ./
RUN npm ci

FROM node:22-bookworm-slim AS build
WORKDIR /app
ARG AGENT_FOREMAN_API_BASE_URL=http://backend:8080
ENV AGENT_FOREMAN_API_BASE_URL=$AGENT_FOREMAN_API_BASE_URL
COPY --from=deps /app/node_modules ./node_modules
COPY src/AgentForeman.Dashboard/ ./
RUN npm run build

FROM node:22-bookworm-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=build /app ./
EXPOSE 3000
CMD ["npm", "run", "start"]
