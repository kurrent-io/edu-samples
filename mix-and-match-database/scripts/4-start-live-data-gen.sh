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


 ESDB_URL=http://localhost:2113                                                           # Set default URL to localhost (for KurrentDB started locally, not in Codespaces)
if [ "$CODESPACES" == "true" ]; then                                                      # If this environment is Codespaces 
       ESDB_URL=https://"$CODESPACE_NAME"-2113.$GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN  # Build the URL to forwarded github codespaces domain       
fi

echo ""
echo ""
echo -e "URL to KurrentDB Admin UI 👉 \e[0m \e[34m$ESDB_URL\e[0m"                      # Print URL to KurrentDB Admin UI
echo ""

DEMOWEB_URL=http://localhost:5108                                                            # Set default URL to localhost
if [ "$CODESPACES" == "true" ]; then                                                         # If this environment is Codespaces 
       DEMOWEB_URL=https://"$CODESPACE_NAME"-5108.$GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN  # Build the URL to forwarded github codespaces domain       
fi

echo ""
echo -e "URL to the Demo web Page 👉 \e[0m \e[34m$DEMOWEB_URL\e[0m"                      # Print URL to KurrentDB Admin UI
echo ""
echo ""

 "$root_path/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce" live-data-set --configuration ./data/datagen.live.config
