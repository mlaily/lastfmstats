#!/usr/bin/env bash

. "scripts/set-env.sh"

DB_RELATIVE_PATH="data/main.db"

read -p "Rsync local main.db to $SSH_USER_AT_HOST:$REMOTE_APP_PATH/$DB_RELATIVE_PATH? (y/n): " confirm
if [[ $confirm == [yY] || $confirm == [yY][eE][sS] ]]; then
    echo "rsyncing..."
    rsync -vah --progress --chmod=D755,F664 -e "ssh -p $SSH_PORT" "$@" "$DB_RELATIVE_PATH" "$SSH_USER_AT_HOST:$REMOTE_APP_PATH/$DB_RELATIVE_PATH"
fi
