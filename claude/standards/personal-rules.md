# Adam Personal Rules

Use these as default implementation rules.

## Core Rules

1. Keep changes small and obvious.
2. Prefer explicit names over explanatory comments.
3. If a comment explains control flow, refactor the code first.
4. One function should do one job; extract when intent blurs.
5. Use guard clauses to reduce nesting.
6. Keep public API surface minimal.
7. Restore or add tests when behavior changes.
8. Strengthen typing as part of normal cleanup.
9. Fix forward quickly, then do a clarity pass.
10. If a fix is uncertain, revert early and re-approach cleanly.

## Commit Message Rules

1. Start with intent: `Fix:`, `Refactor:`, `Tests:`, `Typing:`, `Deploy:`.
2. Include scope early (`prompt_api`, `arize`, `case_worth`, etc.).
3. Keep subject line focused on one change.
4. Prefer multiple small commits over one mixed commit.

## Review Rules (Robert Martin Lean)

1. Is the intent clear without comments?
2. Is the function doing one thing?
3. Are names specific and domain-meaningful?
4. Is duplication removed?
5. Is error handling explicit and unsurprising?

## Comment Policy

Default: do not add comments for code that can be made clearer.

Comments are allowed only for:

- Non-obvious constraints
- Contract boundaries
- Legal/compliance notes
