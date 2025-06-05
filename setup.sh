#!/usr/bin/env bash

git config alias.sw -e jump 
cp . ~/ -r
chmod 755 ~/*.sh

source /usr/local/share/nvm/nvm.sh
nvm install --lts && \
  nvm use --lts && \
  npm install -g diff-so-fancy git-jump &
  
sudo apt-get update  && sudo apt-get install -y vim


