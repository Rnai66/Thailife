#!/bin/sh
# Daily database backup (keeps the last 14). Add to cron on the server:
#   0 4 * * * cd /opt/thailife/game-server/deploy && ./backup.sh >> backups/backup.log 2>&1
# Home server: COMPOSE="-p thailife -f docker-compose.home.yml" ./backup.sh
set -eu
COMPOSE="${COMPOSE:--f docker-compose.prod.yml}"
mkdir -p backups
file="backups/nakama-$(date +%Y%m%d-%H%M).sql.gz"
docker compose $COMPOSE --env-file .env.prod exec -T postgres pg_dump -U postgres nakama | gzip > "$file"
ls -1t backups/nakama-*.sql.gz | tail -n +15 | xargs -r rm --
echo "$(date) backup ok: $file"
