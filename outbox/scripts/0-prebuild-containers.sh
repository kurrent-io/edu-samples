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

sudo apt-get update 
sudo apt-get install -y git-lfs 
git lfs install 
git lfs pull

find "$root_path/tools/" -type f -name "edb-commerce*" -exec chmod +x {} \;
# Ensure all scripts in the /scripts directory have executable permission
chmod +x "$root_path/scripts"/*.sh

"$root_path/scripts/start-db.sh" # Start the database containers
"$root_path/scripts/start-app.sh" # Start the application containers

# Create the persistence subscription for the fulfillment group
curl -i -X PUT -d $'{"startFrom": 0,"resolveLinktos": true}' \
    http://localhost:2113/subscriptions/%24et-order-placed/fulfillment-group \
    -u admin:changeit -H "Content-Type: application/json"