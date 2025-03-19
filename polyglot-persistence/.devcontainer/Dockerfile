FROM mcr.microsoft.com/vscode/devcontainers/javascript-node:0-14

# Install any additional tools or dependencies needed for development
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Set the working directory
WORKDIR /workspace

# Copy any necessary files (if needed)
# COPY . .