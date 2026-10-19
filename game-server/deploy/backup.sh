#!/bin/sh
# Daily database backup (keeps the last 14). Add to cron on the server:
#   0 4 * * * cd /opt/thailife/game-server/deploy && ./backup.sh >> backups/backup.log 2>&1
set -eu
mkdir -p backups
file="backups/nakama-$(date +%Y%m%d-%H%M).sql.gz"
docker compose -f docker-compose.prod.yml --env-file .env.prod exec -T postgres pg_dump -U postgres nakama | gzip > "$file"
ls -1t backups/nakama-*.sql.gz | tail -n +15 | xargs -r rm --
echo "$(date) backup ok: $file"
