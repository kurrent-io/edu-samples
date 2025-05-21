FROM node:20-slim AS client-build

ARG CODESPACES
ARG CODESPACE_NAME
ARG GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN

ENV VITE_CODESPACES=${CODESPACES}
ENV VITE_CODESPACE_NAME=${CODESPACE_NAME}
ENV VITE_GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN=${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}

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