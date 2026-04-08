FROM docker.io/library/node:lts-alpine AS web-build

ARG NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS=
ARG NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY=

ENV NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS=${NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS}
ENV NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY=${NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY}

WORKDIR /src/Temasek.WebApp

# Prepare pnpm https://pnpm.io/installation#using-corepack
# workaround for npm registry key change
# ref. `pnpm@10.1.0` / `pnpm@9.15.4` cannot be installed due to key id mismatch · Issue #612 · nodejs/corepack
# - https://github.com/nodejs/corepack/issues/612#issuecomment-2629496091
RUN npm i -g corepack@latest && corepack enable

# Prepare deps
RUN apk update
RUN apk add git --no-cache

# Prepare build deps ( ignore postinstall scripts for now )
COPY package.json ./
COPY pnpm-lock.yaml ./
RUN pnpm i --frozen-lockfile --ignore-scripts

# Copy all source files
COPY Temasek.WebApp/. ./
RUN pnpm nuxt prepare

# Run full install with every postinstall script ( This needs project file )
RUN pnpm i --frozen-lockfile

RUN pnpm generate

FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS api-build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY Temasek.WebApi/Temasek.WebApi.csproj Temasek.WebApi/
COPY Temasek.ServiceDefaults/Temasek.ServiceDefaults.csproj Temasek.ServiceDefaults/

RUN dotnet restore "./Temasek.WebApi/Temasek.WebApi.csproj"

COPY . .

WORKDIR /src/Temasek.WebApi
RUN dotnet publish "./Temasek.WebApi.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS final

WORKDIR /app

COPY --from=api-build /app/publish .
COPY --from=web-build /src/Temasek.WebApp/.output ./wwwroot/.output/

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Temasek.WebApi.dll"]
