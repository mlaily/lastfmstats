#!/usr/bin/env bash

${LOCAL_SRC_DIR:?} 2> /dev/null # exit if null or not set
${SERVICE_NAME:?} 2> /dev/null # exit if null or not set
${TAG:?} 2> /dev/null # exit if null or not set

newline=$'\n'

echo -e "\nBuilding image...\n"

docker-compose -f "$LOCAL_SRC_DIR/docker-compose.yml" -f "$LOCAL_SRC_DIR/docker-compose.build.yml" build --pull

read -p "${newline}Continue pushing the image? (y/n): " confirm && [[ $confirm == [yY] || $confirm == [yY][eE][sS] ]] || exit 1 # https://stackoverflow.com/questions/18544359/how-to-read-user-input-into-a-variable-in-bash

echo -e "\nPushing image...\n"

# For the push to work, docker-compose.build.yml should set "image" to an appropriate value (yaurthek/zerowidthjoiner:${TAG:-latest})
docker-compose -f "$LOCAL_SRC_DIR/docker-compose.yml" -f "$LOCAL_SRC_DIR/docker-compose.build.yml" push $SERVICE_NAME
