#!/usr/bin/env bash
set -euo pipefail

if ! command -v code >/dev/null 2>&1; then
	echo "VS Code CLI (code) not found; skipping extension install."
	exit 0
fi

code --install-extension github.copilot --force
code --install-extension oderwat.indent-rainbow --force
code --install-extension hbenl.vscode-test-explorer --force
code --install-extension samuelcolvin.jinjahtml --force
code --install-extension leighlondon.eml --force
code --install-extension joel-harkes.emlviewer --force
code --install-extension eamodio.gitlens --force


