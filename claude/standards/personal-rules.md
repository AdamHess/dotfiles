# Adam Personal Rules

Use these as default implementation rules. For code examples, see `claude/review/code-review-examples.md`.

## On Names and Intent

1. **Variable names reveal their range and meaning.** Not `s`, `w`, `t`; instead `confidence_score`, `weight_multiplier`, `approval_threshold`.
2. **Function names describe what the caller gets back, not implementation.** Not `do_work()` or `process()`; instead `fetch_case_metadata()`, `calculate_settlement()`.
3. **Boolean parameters are a code smell.** If `save(doc, archive=True/False)` has two paths, split into `save_active_document()` and `save_archived_document()`.
4. **If you write a comment to explain control flow, refactor instead.** "Check if value is valid" becomes `is_valid_treatment_severity(score)` with the logic clear in the name and implementation.

## On Function Design

5. **One function, one reason to change.** A function that fetches, parses, validates, caches, and notifies has 5 reasons to change. Break it into 5 functions.
6. **Guard clauses flatten nesting.** Return early for invalid states, leaving the success path readable top-to-bottom.
7. **Public API surface is intentional and minimal.** Hide internal state (`_cache`), intermediate methods. Expose only what callers need to know.

## On Types

8. **Types are contracts.** Every function parameter and return must have an explicit type annotation. Use Pydantic models for structured data, not dicts.
9. **Missing type = incomplete interface.** If you can't write the type, the design isn't clear yet.
10. **`dict` is not a type.** Use Pydantic models. If it's coming from external JSON, validate and transform at the boundary.

## On Error Handling

11. **Catch what you expect and can recover from.** Don't `except Exception`; be specific (`ValidationError`, `DatabaseConnectionError`). Let programming errors (TypeError, AttributeError) bubble up.
12. **Errors should not swallow context.** Include the input and the reason in error context, not just "failed".

## On Testing

13. **Test names describe the scenario and expected behavior.** Not `test_prediction()`; instead `test_get_prediction_when_damages_provided_should_return_float()`. When the test fails, the name tells you the broken scenario.
14. **Tests verify outcomes, not mock call counts.** Assert return values, state changes, or exceptions. Don't assert `mock.assert_called_once()`.
15. **One behavior per test.** If a test checks three paths, it's three tests.

## On DRY

16. **Repeated logic across functions = missing abstraction.** Extract the pattern into a helper. Don't copy-paste key builders, validators, or formatters.
17. **If you see the same error handling in three places, extract it.** The caller shouldn't need to handle the same exception three different ways.

## Commit Message Rules

1. Start with intent: `Fix:`, `Refactor:`, `Tests:`, `Typing:`, `Deploy:`.
2. Include scope early (`prompt_api`, `arize`, `case_worth`, etc.).
3. Keep subject line focused on one change.
4. Prefer multiple small commits over one mixed commit.

## Comment Policy

Default: do not add comments for code that can be made clearer.

Comments are allowed only for:

- Non-obvious constraints
- Contract boundaries
- Legal/compliance notes

## When Reviewing Code

See `code-review-examples.md` for 10 concrete patterns. Quick checklist:

- [ ] Function doing one thing?
- [ ] Names reveal intent without comments?
- [ ] Types explicit everywhere?
- [ ] Error handling specific (not `Exception`)?
- [ ] Test names describe the broken scenario?
- [ ] Guard clauses flatten deep nesting?
- [ ] Public API surface intentional?
- [ ] Duplication hiding a missing function?
