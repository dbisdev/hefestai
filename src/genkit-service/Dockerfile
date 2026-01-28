FROM node:18-alpine AS base
WORKDIR /app

FROM base AS deps
COPY package.json package-lock.json* ./
RUN npm ci --omit=dev

FROM base AS builder
COPY package.json package-lock.json* ./
RUN npm ci
COPY . .
RUN npm run build

FROM base AS runner
ENV NODE_ENV=production

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 genkit

COPY --from=deps /app/node_modules ./node_modules
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/package.json ./

USER genkit

EXPOSE 3000

CMD ["node", "dist/index.js"]
