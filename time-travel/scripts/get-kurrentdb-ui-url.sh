KURRENTDB_URL=http://localhost:2113                                                            # Set default URL to localhost (for KurrentDB started locally, not in Codespaces)
if [ "$CODESPACES" == "true" ]; then                                                      # If this environment is Codespaces 
       KURRENTDB_URL=https://"$CODESPACE_NAME"-2113.$GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN  # Build the URL to forwarded github codespaces domain       
fi

echo ""
echo ""
echo -e "URL to KurrentDB Admin UI 👉 \e[0m \e[34m$KURRENTDB_URL/web/index.html\e[0m"                      # Print URL to KurrentDB Admin UI
echo ""
echo ""