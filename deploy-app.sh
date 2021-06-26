#!/usr/bin/env bash

. "scripts/set-env.sh"

echo -e "Don't forget to build the client app in release mode first!!!"

read -p "Please enter a tag value: " TAG
${TAG:?} 2> /dev/null # exit if null or not set

export TAG

echo -e "\nCleaning $LOCAL_DOCKER_COMPOSE_PATH/"
rm -f ${LOCAL_DOCKER_COMPOSE_PATH:?}/* # https://pubs.opengroup.org/onlinepubs/009695399/utilities/xcu_chap02.html#tag_02_06_02

. "scripts/release-generate-docker-compose.sh"
. "scripts/release-build-push-image.sh"
. "scripts/release-remote-rsync-restart.sh"
