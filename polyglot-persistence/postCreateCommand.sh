max_attempts=10
attempt=0
while ! docker ps > /dev/null 2>&1; do                                # While docker fails to run (e.g. Docker daemon is not running)
       if [ "$attempt" -ge "$max_attempts" ]; then                    # If number of attempt exceeds the max_attempts then we exit
              echo "Docker daemon is still not available. Exiting"
              exit 1
       fi
       attempt=$((attempt+1))                                         # Increment the attempt count
       sleep 2                                                        # Wait for few seconds before we check again
done

docker compose -f /workspaces/developer-bootcamp/polyglot-persistence/docker-compose.yml up -d

# dotnet run --project /workspaces/developer-bootcamp/polyglot-persistence/DemoWeb &

# dotnet run --project /workspaces/developer-bootcamp/polyglot-persistence/PostgresProjection &

# dotnet run --project /workspaces/developer-bootcamp/polyglot-persistence/RedisProjection &

# dotnet run --project /workspaces/developer-bootcamp/polyglot-persistence/MongoProjection &