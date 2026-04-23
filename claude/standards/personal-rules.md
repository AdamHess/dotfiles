# Adam Personal Rules

Quick reference. Details and examples:
- Implementation: `how-i-write.md`
- Code review: `code-review-examples.md`

## Writing Code: Priority Order

1. **Clear naming.** Not `s`; `confidence_score`. Not `process()`; `fetch_case_data()`.
2. **One job per function.** Fetch, parse, cache, notify = 4 functions, not 1.
3. **Guard clauses.** Return early for invalid states; success path at bottom.
4. **Explicit types.** Use Pydantic models for structured data, never `dict`.
5. **Specific error handling.** Catch ValidationError, not Exception.
6. **Extract duplication.** See it twice → make it once.
7. **No flow comments.** Refactor instead of explaining control flow.
8. **Descriptive test names.** `test_<fn>_when_<condition>_should_<outcome>`.

## No

- Boolean parameters (use two functions)
- `except Exception` (catch specific exceptions)
- Comments explaining code flow (refactor instead)
- Dicts for structured data (use Pydantic models)
- Functions doing multiple jobs
- Repeated error handling logic
- Generic names (`data`, `util`, `process`)

## Commits

- Intent first: `Fix:`, `Refactor:`, `Tests:`, `Typing:`
- Scope next: `case_worth`, `arize`, `prompt_api`
- One idea per commit
- Prefer 3 focused commits over 1 mixed commit

## Comments Are Allowed For

- Non-obvious constraints
- Contract/API boundaries
- Legal/compliance requirements
