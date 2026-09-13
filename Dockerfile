FROM node:20-alpine AS frontend
WORKDIR /app/client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /app
COPY . .
RUN dotnet restore src/Tansekak.Api/Tansekak.Api.csproj
RUN dotnet publish src/Tansekak.Api/Tansekak.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app/publish .
COPY --from=frontend /app/client/dist/client/browser ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tansekak.Api.dll"]
