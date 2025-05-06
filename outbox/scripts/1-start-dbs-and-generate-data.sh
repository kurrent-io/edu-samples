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

"$root_path/scripts/start-kurrentdb.sh"

"$root_path/scripts/start-postgres.sh"

"$root_path/scripts/seed-data.sh" "$root_path/data/data-two-orders-1.json" # Seed the data using the seed-data.sh script with the provided JSON file

echo ""
echo -e "🚀 \e[32mKurrentDB Server has started!!\e[0m 🚀" 

"$root_path/scripts/get-kurrentdb-ui-url.sh"