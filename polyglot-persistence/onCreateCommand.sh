docker compose --profile db -f /workspaces/developer-bootcamp/polyglot-persistence/docker-compose.yml up -d

unzip -o /workspaces/developer-bootcamp/polyglot-persistence/data/init-data.zip -d /workspaces/developer-bootcamp/polyglot-persistence/

chmod +x /workspaces/developer-bootcamp/polyglot-persistence/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce

# Wait for EventStoreDB to be ready
echo "Waiting for EventStoreDB to be ready..."
max_attempts=30
attempt=0
while ! curl -s http://localhost:2113/ > /dev/null; do
    attempt=$((attempt+1))
    if [ $attempt -eq $max_attempts ]; then
        echo "EventStoreDB failed to start after $max_attempts attempts. Exiting."
        exit 1
    fi
    echo "Waiting for EventStoreDB to be ready... (attempt $attempt/$max_attempts)"
    sleep 2
done
echo "EventStoreDB is ready."

/workspaces/developer-bootcamp/polyglot-persistence/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce seed-data-set /workspaces/developer-bootcamp/polyglot-persistence/data.json

dotnet build /workspaces/developer-bootcamp/polyglot-persistence/polyglot-persistence.sln