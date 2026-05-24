# --- web build stage ---
FROM node:22-alpine AS web-build
WORKDIR /web
COPY ./web/package*.json ./
RUN npm ci
COPY ./web .
RUN npm run build

# --- dotnet build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./ArchipeLemmeGo .
RUN dotnet restore ./ArchipeLemmeGo.csproj
RUN dotnet publish ./ArchipeLemmeGo.csproj -c Release -o /app --no-restore

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./
COPY --from=web-build /web/dist ./wwwroot/

ENTRYPOINT ["dotnet", "ArchipeLemmeGo.dll"]
