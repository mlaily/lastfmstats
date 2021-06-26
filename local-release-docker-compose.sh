#!/usr/bin/env bash

# Use this script like you would use docker-compose.
# It's just a shortcut to use the docker-compose file and env in /release ...

. "scripts/set-env.sh"

${LOCAL_DOCKER_COMPOSE_PATH:?} 2> /dev/null # exit if null or not set
${LOCAL_SRC_DIR:?} 2> /dev/null # exit if null or not set

docker-compose \
    -f $LOCAL_DOCKER_COMPOSE_PATH/docker-compose.yml \
    -f $LOCAL_DOCKER_COMPOSE_PATH/docker-compose.override.yml \
    -f $LOCAL_SRC_DIR/docker-compose.local-release-paths.yml \
    --env-file $LOCAL_DOCKER_COMPOSE_PATH/.env $@
