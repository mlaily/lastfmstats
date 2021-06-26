#!/usr/bin/env bash

# Goal: to be able to just do a docker-compose -d up
# to start my app on my server, and be done with it (no need to fiddle with -f or whatever)

${LOCAL_SRC_DIR:?} 2> /dev/null # exit if null or not set
${LOCAL_DOCKER_COMPOSE_PATH:?} 2> /dev/null # exit if null or not set
${TAG:?} 2> /dev/null # exit if null or not set

# We don't use docker config to generate a compiled config, because it always expands paths (./... because an absolute path)
# So instead, we rely on the default override file:

echo -e "\nGenerating docker-compose release configuration ..."
cp "$LOCAL_SRC_DIR/docker-compose.yml" "$LOCAL_DOCKER_COMPOSE_PATH/docker-compose.yml"
cp "$LOCAL_SRC_DIR/docker-compose.prod.yml" "$LOCAL_DOCKER_COMPOSE_PATH/docker-compose.override.yml"
cp "$LOCAL_SRC_DIR/.prodenv" "$LOCAL_DOCKER_COMPOSE_PATH/.env"

echo "Appending tag ($TAG) to $LOCAL_DOCKER_COMPOSE_PATH/.env"
echo "TAG=$TAG" >> "$LOCAL_DOCKER_COMPOSE_PATH/.env"
