#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/edu-samples/outbox"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi


docker compose --profile db -f "$root_path/docker-compose.yml" start postgres