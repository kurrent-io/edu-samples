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

# Set variables based on input arguments
data_dir="$root_path/data"                                     # Directory that contains the configuration file
datagen_init_config_path="$data_dir/datagen.init.config" # Path to the initialization configuration file
data_init_zip="$data_dir/data.init.zip"           # Path for the output zip file containing generated data
data_init_path="$data_dir/data.init.json"                # Path to the extracted JSON file for seeding
edbcommerce="$root_path/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce"                                  # Path to the edb-commerce executable

"$root_path/scripts/start-db.sh" # Start the database containers

# Get current UTC time and UTC time from 15 minutes ago in the required format
shoppingPeriod=$(date -u -d '2 hours ago' +"%Y-%m-%dT%H:%M:%S.0000000Z")

# Update the shopping period in the configuration file using jq
jq --arg from "$shoppingPeriod" --arg to "$shoppingPeriod" \
  '.shopping.shoppingPeriod.from = $from | .shopping.shoppingPeriod.to = $to' \
  "$datagen_init_config_path" > tmp.json && mv tmp.json "$datagen_init_config_path"

echo "Updated datagen config's shopping period to $shoppingPeriod"

# Generate the data set using the updated configuration via edb-commerce
"$edbcommerce" generate-data-set --configuration "$datagen_init_config_path" --output "$data_dir/data.init.zip"

# Unzip the generated data set into the data directory
unzip "$data_init_zip" -d "$data_dir"
# Rename the extracted data file to match the expected name
mv "$data_dir/data.json" "$data_init_path"

# Seed the data using the edb-commerce tool with the updated initialization JSON
"$edbcommerce" seed-data-set "$data_init_path"

# Final message indicating that the data set has been generated and seeded successfully
echo "Generated data set performed with edb-commerce at: $edbcommerce"