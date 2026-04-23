# dotfiles

Simple, symlink-friendly dotfiles with a small AI guidance knowledge base.

## Layout

- `config/`: source of truth for dotfiles
- `scripts/bootstrap/`: setup and one-time machine scripts
- `claude/`: vendor-agnostic AI coding guidance
- `CLAUDE.md`: entry point for AI coding behavior

## Compatibility

Root-level `.bash_aliases`, `.gitconfig`, `.vimrc`, `setup.sh`, and `run_once.sh` are symlinks to their structured locations so existing workflows keep working.
