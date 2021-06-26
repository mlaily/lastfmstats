#!/usr/bin/env bash

${LOCAL_DOCKER_COMPOSE_PATH:?} 2> /dev/null # exit if null or not set
${REMOTE_APP_PATH:?} 2> /dev/null # exit if null or not set

${SSH_USER_AT_HOST:?} 2> /dev/null # exit if null or not set
${SSH_PORT:?} 2> /dev/null # exit if null or not set
${SSH_CONTROL_PATH:?} 2> /dev/null # exit if null or not set

newline=$'\n'

# Make sure the directory where we want to store connection info exists
mkdir -p ~/.ssh/ssh-mux

echo -e "\nStarting master connection to $SSH_USER_AT_HOST -p $SSH_PORT..."

# Start SSH multiplexing session
# https://unix.stackexchange.com/questions/50508/reusing-ssh-session-for-repeated-rsync-commands
# https://en.wikibooks.org/wiki/OpenSSH/Cookbook/Multiplexing
ssh -nNf -M -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT

    echo -e "\nCreating a backup of the remote docker-compose.* and .env configuration..."
    ssh -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT "cd $REMOTE_APP_PATH/ && tail -n +1 docker-compose.* .env > previous-docker-compose-config.bkp"

    echo -e "\nContent of $SSH_USER_AT_HOST:$REMOTE_APP_PATH/:"
    ssh -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT "cd $REMOTE_APP_PATH/ && ls -lA"

    echo -e "\nContent of $LOCAL_DOCKER_COMPOSE_PATH/:"
    ls -lA $LOCAL_DOCKER_COMPOSE_PATH/

    read -p "${newline}Ready to rsync $LOCAL_DOCKER_COMPOSE_PATH/ to $SSH_USER_AT_HOST:$REMOTE_APP_PATH/ \
    ${newline}(NO --delete; be careful if the files to update are not the same as before...)\
    ${newline}Rsync? (y/n): " confirm
    if [[ $confirm == [yY] || $confirm == [yY][eE][sS] ]]; then
        echo "rsyncing..."
        rsync -vah --progress --chmod=D755,F664 -e "ssh -p $SSH_PORT -S $SSH_CONTROL_PATH" "$LOCAL_DOCKER_COMPOSE_PATH/" "$SSH_USER_AT_HOST:$REMOTE_APP_PATH/"
    fi

    echo -e "\nContent of $SSH_USER_AT_HOST:$REMOTE_APP_PATH/ is now:"
    ssh -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT "cd $REMOTE_APP_PATH/ && ls -lA"

    read -p "${newline}Do you want to docker-compose up? (y/n): " confirm
    if [[ $confirm == [yY] || $confirm == [yY][eE][sS] ]]; then
        ssh -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT "cd $REMOTE_APP_PATH/ && docker-compose up -d"
        echo -e "\n"
    fi

    read -p "${newline}Do you want to keep the ssh session open? (y/n): " confirm
    if [[ $confirm == [yY] || $confirm == [yY][eE][sS] ]]; then
        echo "Switching to an interactive login shell..."
        ssh -t -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT "cd $REMOTE_APP_PATH/ && exec \$SHELL --login" # open a login shell. -t is important
    fi

# Finish SSH multiplexing session
ssh -O exit -S $SSH_CONTROL_PATH $SSH_USER_AT_HOST -p $SSH_PORT
