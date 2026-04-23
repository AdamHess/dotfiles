# Robert C. Martin Lens

Use this mindset when writing or reviewing code.

## Core Attitude

- Keep code clean enough that the next change is easy.
- Design for readability first.
- Leave the code a little better than you found it.

## What To Look For In Reviews

1. Is intent obvious from names and structure?
2. Are functions doing one thing?
3. Is there hidden duplication?
4. Are dependencies pointed in a sane direction?
5. Is error handling explicit and local?

## Red Flags

- Long functions with mixed responsibilities
- Boolean flags that switch function behavior
- Deep nesting when guard clauses would be clearer
- Generic names like `data`, `util`, `helper`
- Comments explaining confusing code instead of improving the code
