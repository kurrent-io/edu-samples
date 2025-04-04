docker compose -f /workspaces/developer-bootcamp/polyglot-persistence/docker-compose.yml up -d

unzip -o /workspaces/developer-bootcamp/polyglot-persistence/data/init-data.zip -d /workspaces/developer-bootcamp/polyglot-persistence/

chmod +x /workspaces/developer-bootcamp/polyglot-persistence/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce


max_attempts=60
attempt=0
while ! curl -s -o /dev/null -w "%{http_code}" http://localhost:2113/web/index.html | grep -q "200"; do
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempts exceeds max_attempts then we exit
              echo "EventStoreDB is not available. Exiting"
              exit 1
       fi
       echo "Waiting for EventStoreDB to start... (attempt $attempt)"
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for a few seconds before checking again
done

echo "EventStoreDB is running."

max_attempts=60
attempt=0
while ! curl -s -o /dev/null -w "%{http_code}" http://localhost:5108/carts | grep -q "200"; do
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempts exceeds max_attempts then we exit
              echo "DemoWeb is not available. Exiting"
              exit 1
       fi
       echo "Waiting for DemoWeb to start... (attempt $attempt)"
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for a few seconds before checking again
done

echo "DemoWeb is running."

/workspaces/developer-bootcamp/polyglot-persistence/tools/Kurrent.Extensions.Commerce/linux-x64/edb-commerce seed-data-set /workspaces/developer-bootcamp/polyglot-persistence/data.json

echo "Updated EventStoreDB with initial data."

# Wait for the required projection messages to be detected
echo "Waiting for 'MongoProjection started', 'RedisProjection started' and 'DemoWeb started' messages..."
max_attempts=60
attempt=0
while true; do
    logs=$(docker compose -f /workspaces/developer-bootcamp/polyglot-persistence/docker-compose.yml logs 2>&1)
    if echo "$logs" | grep -q "MongoProjection started" && \
       echo "$logs" | grep -q "RedisProjection started" && \
       echo "$logs" | grep -q "PostgresProjection started"; then
        echo "All required messages detected."
        break
    fi
    attempt=$((attempt+1))
    if [ $attempt -ge $max_attempts ]; then
        echo "Required projections did not start after $max_attempts attempts. Exiting."
        exit 1
    fi
    echo "Waiting for required projections... (attempt $attempt/$max_attempts)"
    sleep 2
done