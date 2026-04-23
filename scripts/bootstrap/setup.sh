#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

link_dotfile() {
  local source_path="$1"
  local destination_path="$2"
  ln -sfn "$source_path" "$destination_path"
}

# Keep canonical dotfile paths in $HOME while storing managed files in config/.
link_dotfile "$REPO_ROOT/config/shell/.bash_aliases" "$HOME/.bash_aliases"
link_dotfile "$REPO_ROOT/config/git/.gitconfig" "$HOME/.gitconfig"
link_dotfile "$REPO_ROOT/config/vim/.vimrc" "$HOME/.vimrc"

git config --global alias.sw -e jump

if [ -f /usr/local/share/nvm/nvm.sh ]; then
  # shellcheck source=/dev/null
  source /usr/local/share/nvm/nvm.sh
  nvm install --lts
  nvm use --lts
  npm install -g diff-so-fancy git-jump
fi

if command -v apt-get >/dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y vim
fi


