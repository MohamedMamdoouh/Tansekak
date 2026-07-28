# Build Angular frontend
FROM node:20-alpine AS frontend
WORKDIR /src/client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build -- --configuration production

# Build and publish API
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS publish
WORKDIR /src
COPY Tansekak.sln ./
COPY src/ src/
RUN dotnet restore src/Tansekak.Api/Tansekak.Api.csproj
RUN dotnet publish src/Tansekak.Api/Tansekak.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish ./
COPY --from=frontend /src/client/dist/client/browser ./wwwroot/
COPY SeededData/ ./Data/

ENTRYPOINT ["dotnet", "Tansekak.Api.dll"]
