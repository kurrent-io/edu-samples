FROM node:20-slim AS client-build
WORKDIR /app/ClientApp
RUN mkdir client
COPY DemoWeb/client/package*.json ./client
WORKDIR /app/ClientApp/client
RUN npm install
COPY DemoWeb/client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app
COPY . .
COPY --from=client-build /app/ClientApp/wwwroot ./DemoWeb/wwwroot

ENV ASPNETCORE_URLS=http://0.0.0.0:3000
ENV SALES_DATA_FILEPATH=/app/data/report-read-model.json

EXPOSE 3000
WORKDIR /app/DemoWeb
CMD ["dotnet", "run", "--project", "."]