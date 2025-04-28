#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/edu-samples/mix-and-match-database"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi

# Exit if the data directory does not exist
if [ ! -d "$root_path/data" ]; then
    echo "Error: Data directory $root_path/data does not exist. Exiting."
    exit 1
fi

docker compose --profile app -f "$root_path/docker-compose.yml" up -d

max_attempts=60
attempt=0
while ! curl -s -o /dev/null -w "%{http_code}" http://localhost:5108/carts | grep -q "200"; do
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempts exceeds max_attempts then we exit
              echo "DemoWeb is not available. Exiting"
              exit 1
       fi
       echo "Waiting for DemoWeb to start... (attempt $attempt)"
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for a few seconds before checking again
done

echo "DemoWeb is running."

# Wait for the required projection messages to be detected
max_attempts=60
attempt=0
while true; do
    logs=$(docker compose --profile app -f "$root_path/docker-compose.yml" logs 2>&1)
    if echo "$logs" | grep -q "RedisProjection started" && \
       echo "$logs" | grep -q "PostgresProjection started"; then
        echo "All projection apps are running."
        break
    fi
    attempt=$((attempt+1))

    if [ $attempt -ge $max_attempts ]; then
        echo "Required projections did not start after $max_attempts attempts. Exiting."
        exit 1
    fi
    echo "Waiting projection apps to start... (attempt $attempt)"

    sleep 2
done