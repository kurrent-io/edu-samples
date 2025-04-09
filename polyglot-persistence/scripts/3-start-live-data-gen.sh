#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/developer-bootcamp/polyglot-persistence"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi

# Exit if the data directory does not exist
if [ ! -d "$root_path/data" ]; then
    echo "Error: Data directory $root_path/data does not exist. Exiting."
    exit 1
fi

 "$root_path/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce" live-data-set --configuration ./data/datagen.live.config