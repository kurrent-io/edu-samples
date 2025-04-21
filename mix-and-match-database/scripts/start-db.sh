#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/developer-bootcamp/mix-and-match-database"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi

# Exit if the data directory does not exist
if [ ! -d "$root_path/data" ]; then
    echo "Error: Data directory $root_path/data does not exist. Exiting."
    exit 1
fi

docker compose -f "$root_path/docker-compose.yml" up -d

max_attempts=60
attempt=0
while ! curl -s -o /dev/null -w "%{http_code}" http://localhost:2113/web/index.html | grep -q "200"; do
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempts exceeds max_attempts then we exit
              echo "KurrentDB is not available. Exiting"
              exit 1
       fi
       echo "Waiting for databases to start... (attempt $attempt)"
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for a few seconds before checking again
done

echo "Databases are running."