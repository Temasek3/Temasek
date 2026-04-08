FROM docker.io/library/node:lts-bookworm-slim AS web-build

WORKDIR /src/Temasek.WebApp

RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g corepack@latest \
    && corepack enable

COPY Temasek.WebApp/package.json Temasek.WebApp/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile --ignore-scripts

COPY Temasek.WebApp/. ./

ARG NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS=
ARG NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY=

ENV NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS=${NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS}
ENV NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY=${NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY}

RUN pnpm nuxt prepare
RUN pnpm install --frozen-lockfile
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
COPY --from=web-build /src/Temasek.WebApp/.output/public ./wwwroot/.output/public

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Temasek.WebApi.dll"]
