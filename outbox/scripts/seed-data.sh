#!/bin/bash

# Check if data_path parameter is provided
if [ -z "$1" ]; then
    echo "Error: data_path parameter is required."
    echo "Usage: $0 <data_path>"
    exit 1
fi

# Set data_path from the provided argument
data_path="$1"

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

# Set variables based on input arguments
edbcommerce="$root_path/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce"                                  # Path to the edb-commerce executable

"$root_path/scripts/start-kurrentdb.sh" # Start kurrentdb container

# Seed the data using the edb-commerce tool with the updated initialization JSON
"$edbcommerce" seed-data-set "$data_path"

echo ""
echo "Appended data to KurrentDB"