#!/usr/bin/env bash

export SERVICE_NAME="lastfmstats"
#export TAG="latest" # Should be set by the release script

# Directory paths should NEVER end with a / (so we can consistently concatenate it if needed).
# Local paths are relative to the root of the repo.

export LOCAL_SRC_DIR="src"
export LOCAL_DOCKER_COMPOSE_PATH="release"
export REMOTE_APP_PATH="/srv/www/lastfmstats.200d.net"

export SSH_USER_AT_HOST="yaurthek@0.x2a.yt"
export SSH_PORT=22
export SSH_CONTROL_PATH="~/.ssh/ssh-mux/%L-%r@%h:%p"
