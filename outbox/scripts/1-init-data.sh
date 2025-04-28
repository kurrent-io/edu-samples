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

# Set variables based on input arguments
data_dir="$root_path/data"                                     # Directory that contains the configuration file
datagen_init_config_path="$data_dir/datagen.init.config" # Path to the initialization configuration file
data_init_zip="$data_dir/data.init.zip"           # Path for the output zip file containing generated data
data_init_path="$data_dir/data.init.json"                # Path to the extracted JSON file for seeding
edbcommerce="$root_path/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce"                                  # Path to the edb-commerce executable

"$root_path/scripts/start-db.sh" # Start the database containers

# Generate the data set using the updated configuration via edb-commerce
"$edbcommerce" generate-data-set --configuration "$datagen_init_config_path" --output "$data_dir/data.init.zip"

# Unzip the generated data set into the data directory
unzip "$data_init_zip" -d "$data_dir"
# Rename the extracted data file to match the expected name
mv "$data_dir/data.json" "$data_init_path"

# Seed the data using the edb-commerce tool with the updated initialization JSON
"$edbcommerce" seed-data-set "$data_init_path"

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
echo "Appended sample data to KurrentDB"
