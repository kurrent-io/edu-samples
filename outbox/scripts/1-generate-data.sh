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

"$root_path/scripts/seed-data.sh" "$root_path/data/data-two-orders-1.json" # Seed the data using the seed-data.sh script with the provided JSON file

KURRENTDB_URL=http://localhost:2113                                                            # Set default URL to localhost (for KurrentDB started locally, not in Codespaces)
if [ "$CODESPACES" == "true" ]; then                                                      # If this environment is Codespaces 
       KURRENTDB_URL=https://"$CODESPACE_NAME"-2113.$GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN  # Build the URL to forwarded github codespaces domain       
fi

echo ""
echo ""
echo -e "🚀 \e[32mKurrentDB Server has started!!\e[0m 🚀" 
echo ""
echo -e "URL to KurrentDB Admin UI 👉 \e[0m \e[34m$KURRENTDB_URL/web/index.html\e[0m"                      # Print URL to KurrentDB Admin UI
echo ""
echo ""