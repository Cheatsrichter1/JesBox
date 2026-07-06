#!/usr/bin/env bash
# Run this from your own machine to push the latest server + phone-ui to your
# VPS and restart it. Edit SERVER_HOST / SERVER_PATH once, then just run:
#   ./scripts/deploy.sh
set -euo pipefail

SERVER_HOST="root@46.62.163.219"
SERVER_PATH="/root/jesbox"

ssh "$SERVER_HOST" bash -s <<REMOTE
set -euo pipefail
cd "$SERVER_PATH"
git pull
cd server && npm install --omit=dev
cd ../phone-ui && npm install && npm run build
cd ../server
if pm2 describe jesbox > /dev/null 2>&1; then
  pm2 restart jesbox
else
  pm2 start ecosystem.config.js
fi
REMOTE

echo "Deployed."
