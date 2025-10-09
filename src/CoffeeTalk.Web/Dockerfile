FROM node:22-alpine AS base

# Next.js collects completely anonymous telemetry data about general usage.
# Learn more here: https://nextjs.org/telemetry
# Uncomment the following line in case you want to disable telemetry during the build.
# ENV NEXT_TELEMETRY_DISABLED=1

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

RUN apk add --no-cache libc6-compat

COPY package.json next.config.mjs tsconfig.json .eslintrc.json ./
RUN npm i

COPY app/ ./app/

RUN npm run build

EXPOSE 3000

ENV PORT=3000

# server.js is created by next build from the standalone output
# https://nextjs.org/docs/pages/api-reference/config/next-config-js/output
ENV HOSTNAME="0.0.0.0"
CMD ["npm", "run", "start"]