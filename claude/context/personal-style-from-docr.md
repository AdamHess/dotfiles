# Personal Style Profile (from docr)

This profile is based on `docr` commits authored by Adam Hess.

## Snapshot

- Approximate commits reviewed: 350
- Dominant patterns in commit subjects: fix-forward, refactor, typing/test cleanup
- Typical change size labels in subjects: XS/S/M/L
- Active domains: `docket_sync`, `case_worth`, `ltl`, `sf_sync`, and recent `arize`/tracing work

## How Your Work Reads

- You optimize for speed and iteration, then tighten correctness quickly.
- You are comfortable making surgical, production-facing changes.
- You use explicit commit messages that map to intent (`Fix`, `Revert`, `Deploy`, `refactor`, `tests`, `typing`).
- You frequently do cleanup passes after functional changes (naming, tests, typing, API surface).

## Strengths

- Fast operational feedback loops
- Pragmatic fixes under real deployment pressure
- Willingness to revert and re-shape when needed
- Consistent movement toward clearer naming and safer APIs

## Risks To Watch

- Rapid iteration can leave mixed abstraction levels in touched files.
- Follow-up cleanup can drift if not captured as explicit rule checks.
- Commit granularity may become narrative-heavy if many quick fix commits stack.

## Style Signature

A practical, delivery-first engineer with strong fix-forward instincts, who follows with cleanup to improve readability and correctness.
