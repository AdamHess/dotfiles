# Comments And Intent

Default rule: avoid comments when better code structure can express intent.

## Practical Rule

Before adding a comment, try one of these first:

1. Rename variables to reveal purpose
2. Rename functions to reveal behavior
3. Extract a small function with a clear name
4. Split conditionals into intention-revealing predicates

## Keep Comments For

- Legal or compliance requirements
- Public API contracts that are not obvious
- Non-obvious tradeoffs and constraints

## Avoid Comments For

- Restating what code already says
- Explaining confusing code that should be refactored
- Dead code or TODOs without owner/context
