# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy the rest of the source
COPY ./ArchipeLemmeGo .

RUN dotnet restore ./ArchipeLemmeGo.csproj

RUN dotnet publish ./ArchipeLemmeGo.csproj -c Release -o /app --no-restore

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

# if your app assembly is ArchipeLemmeGo.dll; otherwise replace accordingly
ENTRYPOINT ["dotnet", "ArchipeLemmeGo.dll"]