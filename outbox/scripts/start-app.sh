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

docker_compose_file="$root_path/docker-compose.yml"

docker compose --profile app -f "$docker_compose_file" up -d

# Wait for the required messages to be detected
max_attempts=60
attempt=0
while true; do
    logs=$(docker compose --profile app -f "$docker_compose_file" logs 2>&1)
    if echo "$logs" | grep -q "PersistentSubscriptionOrderProcessor started"; then
        echo "All apps are running."
        break
    fi
    attempt=$((attempt+1))
    if [ $attempt -ge $max_attempts ]; then
        echo "Required apps did not start after $max_attempts attempts. Exiting."
        exit 1
    fi
    echo "Waiting apps to start... (attempt $attempt)"
    sleep 2
done