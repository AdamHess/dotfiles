# Code Review Examples: Adam's Style

Use these patterns when reviewing code. Each example shows a common issue and how to flag it explicitly.

---

## 1. Function Doing Too Many Things

**Bad:**
```python
def process_and_cache_document(doc_id, bucket, table):
    doc = s3.get_object(Bucket=bucket, Key=doc_id)
    parsed = parse_pdf(doc['Body'].read())
    item = {
      'id': doc_id,
      'text': parsed['text'],
      'pages': len(parsed['pages']),
      'cache_hit': False,
    }
    table.put_item(Item=item)
    notify_downstream(doc_id, item)
    return item
```

**Review Comment:**
> This function owns retrieval, parsing, persistence, and downstream notifications. Split into:
> - `fetch_from_s3(doc_id, bucket)` → bytes
> - `parse_document(raw_bytes)` → ParsedDoc (Pydantic model)
> - `save_to_cache(parsed_doc, table)` → None
> - Leave notification to the caller.

Each function should have one reason to change.

---

## 2. Unclear Variable Names Hiding Intent

**Bad:**
```python
def evaluate_score(s, w, t):
    result = s * w
    if result > t:
        return True
    return False
```

**Review Comment:**
> Rename for clarity:
> - `s` → `confidence_score` (0-1 range? 0-100?)
> - `w` → `weight_multiplier` (is this domain-specific?)
> - `t` → `approval_threshold` (why is >threshold approval?)
> - `result` → intermediate name; see if it disappears after refactor
>
> What is this actually doing? "Is confidence high enough after weighting?"

---

## 3. Missing Type Annotations

**Bad:**
```python
def get_case_worth_prediction(case_data, model):
    if not case_data.get('total_damages'):
        return None
    features = extract_features(case_data)
    prediction = model.predict(features)
    return prediction[0]
```

**Review Comment:**
> Add explicit type hints:
>
> ```python
> def get_case_worth_prediction(case_data: dict, model: Model) -> float | None:
> ```
>
> But first: `case_data` should be a Pydantic model, not `dict`. Then the type is `CaseData`. Same for the return—is it a float, a Prediction object, or a range?

---

## 4. Boolean Parameter That Hides Branching

**Bad:**
```python
def save_document(doc, table, archive=False):
    item = model_validate(doc)
    if archive:
        item['archived_at'] = utcnow()
        item['status'] = 'archived'
    table.put_item(Item=item)
```

**Review Comment:**
> The boolean `archive` is a code smell. This function has two distinct behaviors.
>
> Split into:
> ```python
> def save_active_document(doc: Document, table: Table) -> None: ...
> def save_archived_document(doc: Document, table: Table) -> None: ...
> ```
>
> Or restructure so the caller decides what `Document` looks like before calling `save_document`.
> The function name should say what it does, not what it conditionally might do.

---

## 5. Test Names That Don't Describe Behavior

**Bad:**
```python
def test_prediction():
    result = get_prediction({'total_damages': 100000})
    assert result is not None

def test_prediction_boundary():
    result = get_prediction({'total_damages': 0})
    assert result == 0
```

**Review Comment:**
> Rename to describe the scenario and expected outcome:
>
> ```python
> def test_get_prediction_when_damages_provided_should_return_float():
>     result = get_prediction({'total_damages': 100000})
>     assert isinstance(result, float)
>
> def test_get_prediction_when_damages_zero_should_return_none():
>     result = get_prediction({'total_damages': 0})
>     assert result is None
> ```
>
> This way, a failing test name tells you exactly which scenario broke.

---

## 6. Comments Explaining Confusing Code (Refactor Instead)

**Bad:**
```python
# Check if the value is in the valid range for treatment severity
if 0 <= score <= 5:
    treatments.append(score)
```

**Review Comment:**
> The comment restates what the code does. Instead, extract intent:
>
> ```python
> VALID_TREATMENT_SEVERITY_RANGE = range(0, 6)
> 
> if score in VALID_TREATMENT_SEVERITY_RANGE:
>     treatments.append(score)
> ```
>
> Or better, a named function:
> ```python
> def is_valid_treatment_severity(score):
>     """Treatment severity scores must be 0-5 inclusive."""
>     return 0 <= score <= 5
>
> if is_valid_treatment_severity(score):
>     treatments.append(score)
> ```

---

## 7. Repeated Logic Across Functions

**Bad:**
```python
def get_docket_entry(matter_id, entry_id, dynamodb_table):
    response = dynamodb_table.get_item(
        Key={'pk': f'MATTER#{matter_id}', 'sk': f'ENTRY#{entry_id}'}
    )
    return response.get('Item')

def get_document(matter_id, doc_id, dynamodb_table):
    response = dynamodb_table.get_item(
        Key={'pk': f'MATTER#{matter_id}', 'sk': f'DOC#{doc_id}'}
    )
    return response.get('Item')
```

**Review Comment:**
> DRY violation. Extract the key-building pattern:
>
> ```python
> def _build_dynamodb_key(matter_id: str, entity_type: str, entity_id: str) -> dict:
>     return {
>         'pk': f'MATTER#{matter_id}',
>         'sk': f'{entity_type}#{entity_id}'
>     }
>
> def get_docket_entry(matter_id: str, entry_id: str, table: Table) -> dict | None:
>     response = table.get_item(Key=_build_dynamodb_key(matter_id, 'ENTRY', entry_id))
>     return response.get('Item')
> ```

---

## 8. Error Handling That Swallows Signals

**Bad:**
```python
def process_case_data(case_json):
    try:
        data = CaseData.model_validate(case_json)
        result = predict(data)
        table.put_item(Item=result.model_dump())
    except Exception:
        log.error("failed")
        return None
```

**Review Comment:**
> `except Exception` is too broad; it catches programming errors (AttributeError, TypeError) alongside real failures.
>
> ```python
> def process_case_data(case_json: dict) -> Prediction | None:
>     try:
>         data = CaseData.model_validate(case_json)
>     except ValidationError as e:
>         log.warning("invalid case data", extra={"error": str(e)})
>         return None
>     
>     result = predict(data)  # let this fail loudly if broken
>     table.put_item(Item=result.model_dump())  # same here
>     return result
> ```
>
> Catch only what you expect and can recover from. Let bugs bubble up.

---

## 9. Guard Clauses Over Nested Blocks

**Bad:**
```python
def calculate_settlement(case):
    if case.total_damages is not None:
        if case.liability_assigned:
            if case.treatment_recommended:
                return case.total_damages * LIABILITY_FACTOR
            else:
                return 0
        else:
            return 0
    else:
        return None
```

**Review Comment:**
> Flatten with early returns:
>
> ```python
> def calculate_settlement(case: Case) -> float | None:
>     if case.total_damages is None:
>         return None
>     if not case.liability_assigned:
>         return 0.0
>     if not case.treatment_recommended:
>         return 0.0
>     return case.total_damages * LIABILITY_FACTOR
> ```
>
> Read top-to-bottom: early exits for invalid states, then the success path.

---

## 10. Mixing Public API Design with Implementation

**Bad:**
```python
class DocumentCache:
    def __init__(self, dynamodb_table):
        self._table = dynamodb_table
        self._local_cache = {}
    
    def get(self, doc_id):
        if doc_id in self._local_cache:
            return self._local_cache[doc_id]
        item = self._table.get_item(Key={'id': doc_id})
        if item:
            self._local_cache[doc_id] = item
        return item
    
    def set(self, doc_id, value):
        self._local_cache[doc_id] = value
        self._table.put_item(Item={'id': doc_id, 'data': value})
```

**Review Comment:**
> API surface is too low-level and exposes caching internals. Caller shouldn't know about dual-layer lookup.
>
> Rethink:
> ```python
> class DocumentCache:
>     def fetch(self, doc_id: str) -> Document | None:
>         """Fetch document with transparent two-tier caching."""
>         ...
>     
>     def invalidate(self, doc_id: str) -> None:
>         """Clear both cache layers for this document."""
>         ...
> ```
>
> Hide `get`/`set` and `_local_cache` from callers. They call `fetch()` and don't care about tiers.

---

## When To Stop

If you catch yourself thinking "this is just style," it's probably worth flagging. Your goal as a reviewer is to make the next change easier by raising the clarity floor. But don't nitpick; focus on:

1. Is the next person's intent clear from names and structure?
2. Does error handling make sense?
3. Is duplication hiding a missing abstraction?
4. Are types as explicit as they should be?
