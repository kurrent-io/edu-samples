#!/bin/bash

if [ -n "$CODESPACES" ]; then
    # In Codespace environment, use the preset project root path
    root_path="/workspaces/edu-samples/outbox"
else
    # Otherwise, assume you are in the project root directory
    root_path="./"
fi

"$root_path/scripts/start-kurrentdb.sh" # Start the database containers

# Create the persistence subscription for the fulfillment group
curl -i -X PUT -d $'{ "minCheckPointCount": 0, "maxCheckPointCount": 0, "resolveLinktos": true, "maxRetryCount": 100 }' \
    http://localhost:2113/subscriptions/%24ce-order/fulfillment \
    -H "Content-Type: application/json"