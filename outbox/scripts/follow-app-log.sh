#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/edu-samples/outbox"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi

# Exit if the data directory does not exist
if [ ! -d "$root_path/data" ]; then
    echo "Error: Data directory $root_path/data does not exist. Exiting."
    exit 1
fi

docker compose --profile app -f "$root_path/docker-compose.yml" logs -f