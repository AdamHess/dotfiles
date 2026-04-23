# CLAUDE.md

This is a vendor-agnostic AI coding guide for this dotfiles repo.

## Purpose

Use this file as the entry point for coding and review behavior. Keep guidance simple, direct, and portable across tools.

## Directory Map

- `claude/principles/`: High-level engineering principles
- `claude/review/`: Code review mindset and checklists
- `claude/standards/`: Coding standards and style preferences
- `claude/workflows/`: Repeatable task workflows
- `claude/context/`: Project or machine context notes

## Default Behaviors

1. Prefer simple and readable code over clever code.
2. Make small, focused changes.
3. Keep functions short and intention-revealing.
4. Add tests for changed behavior when practical.
5. Avoid comments when clear naming and structure can express intent.

## How To Extend

1. Add a new markdown file in the most specific folder.
2. Keep one topic per file.
3. Link related files from this document or `claude/README.md`.

## Personal Defaults

- Style profile: `claude/context/personal-style-from-docr.md`
- Actionable rules: `claude/standards/personal-rules.md`
