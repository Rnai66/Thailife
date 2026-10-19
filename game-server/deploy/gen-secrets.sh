#!/bin/sh
# Writes .env.prod with fresh random secrets (run once on the server).
#   ./gen-secrets.sh game.yourdomain.com
set -eu
[ $# -eq 1 ] || { echo "usage: $0 <domain>"; exit 1; }
[ -e .env.prod ] && { echo ".env.prod already exists — not overwriting"; exit 1; }
r() { openssl rand -hex "$1"; }
umask 077
cat > .env.prod <<ENV
DOMAIN=$1
POSTGRES_PASSWORD=$(r 24)
NAKAMA_SERVER_KEY=$(r 16)
NAKAMA_CONSOLE_USER=admin
NAKAMA_CONSOLE_PASSWORD=$(r 16)
NAKAMA_SESSION_KEY=$(r 24)
NAKAMA_REFRESH_KEY=$(r 24)
NAKAMA_HTTP_KEY=$(r 24)
NAKAMA_CONSOLE_SIGNING_KEY=$(r 24)
ENV
echo "Wrote .env.prod (keep it private). Put NAKAMA_SERVER_KEY into the game's NakamaConfig."
