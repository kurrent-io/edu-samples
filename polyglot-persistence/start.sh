max_attempts=30
attempt=0
while ! docker ps > /dev/null 2>&1; do                                # While docker fails to run (e.g. Docker daemon is not running)
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempt exceeds the max_attempts then we exit
              echo "Docker daemon is not available. Exiting"
              exit 1
       fi
       echo "Waiting for Docker daemon to start... (attempt $attempt)"
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for few seconds before we check again
done

echo "Docker daemon is running."

docker compose -f /workspaces/developer-bootcamp/polyglot-persistence/docker-compose.yml up -d

max_attempts=30
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

max_attempts=30
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