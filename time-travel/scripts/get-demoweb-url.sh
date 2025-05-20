DEMOWEB_URL=http://localhost:3000                                                            # Set default URL to localhost (for KurrentDB started locally, not in Codespaces)
if [ "$CODESPACES" == "true" ]; then                                                      # If this environment is Codespaces 
       DEMOWEB_URL=https://"$CODESPACE_NAME"-3000.$GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN  # Build the URL to forwarded github codespaces domain       
fi

echo ""
echo ""
echo -e "URL to Demo Web Application 👉 \e[0m \e[34m$DEMOWEB_URL\e[0m"                      # Print URL to KurrentDB Admin UI
echo ""
echo ""