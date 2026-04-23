# Code Review Examples: How I Review

These are made-up code examples showing common patterns I flag during review. Not tied to any specific codebase.

---

## 1. Function Doing Too Many Things

**Bad:**
```python
def fetch_and_store_record(record_id, bucket, table):
    doc = s3.get_object(Bucket=bucket, Key=record_id)
    parsed = parse_content(doc['Body'].read())
    item = {
      'id': record_id,
      'content': parsed['content'],
      'page_count': len(parsed['pages']),
      'cached': False,
    }
    table.put_item(Item=item)
    alert_subscribers(record_id, item)
    return item
```

**Review Comment:**
> This function owns retrieval, parsing, persistence, and notifications. Split into:
> - `fetch_from_s3(record_id, bucket)` → bytes
> - `parse_record(raw_bytes)` → ParsedRecord (Pydantic model)
> - `save_to_table(parsed_record, table)` → None
> - Leave notification to the caller.

Each function should have one reason to change.

---

## 2. Unclear Variable Names Hiding Intent

**Bad:**
```python
def check_approval(s, w, t):
    result = s * w
    if result > t:
        return True
    return False
```

**Review Comment:**
> Rename for clarity:
> - `s` → `approval_score` (0-1 range? 0-100?)
> - `w` → `weight_factor` (is this domain-specific?)
> - `t` → `threshold` (why is >threshold approval?)
> - `result` → intermediate name; see if it disappears after refactor
>
> What is this actually doing? "Is score high enough after weighting?"

---

## 3. Missing Type Annotations

**Bad:**
```python
def calculate_bonus(employee_data, rate):
    if not employee_data.get('salary'):
        return None
    base = extract_salary(employee_data)
    bonus = base * rate
    return bonus
```

**Review Comment:**
> Add explicit type hints:
>
> ```python
> def calculate_bonus(employee_data: dict, rate: float) -> float | None:
> ```
>
> But first: `employee_data` should be a Pydantic model, not `dict`. Then the type is `EmployeeRecord`. Same for the return—is it a float, a BonusCalculation object, or a structured result?

---

## 4. Boolean Parameter That Hides Branching

**Bad:**
```python
def save_item(item, table, is_archived=False):
    record = model_validate(item)
    if is_archived:
        record['archived_at'] = utcnow()
        record['state'] = 'archived'
    table.put_item(Item=record)
```

**Review Comment:**
> The boolean `is_archived` is a code smell. This function has two distinct behaviors.
>
> Split into:
> ```python
> def save_active_item(item: Item, table: Table) -> None: ...
> def save_archived_item(item: Item, table: Table) -> None: ...
> ```
>
> Or restructure so the caller decides what `Item` looks like before calling `save_item`.
> The function name should say what it does, not what it conditionally might do.

---

## 5. Test Names That Don't Describe Behavior

**Bad:**
```python
def test_calculate():
    result = calculate({'amount': 1000})
    assert result is not None

def test_calculate_boundary():
    result = calculate({'amount': 0})
    assert result == 0
```

**Review Comment:**
> Rename to describe the scenario and expected outcome:
>
> ```python
> def test_calculate_when_amount_provided_should_return_float():
>     result = calculate({'amount': 1000})
>     assert isinstance(result, float)
>
> def test_calculate_when_amount_zero_should_return_none():
>     result = calculate({'amount': 0})
>     assert result is None
> ```
>
> This way, a failing test name tells you exactly which scenario broke.

---

## 6. Comments Explaining Confusing Code (Refactor Instead)

**Bad:**
```python
# Check if the rating is in the accepted range for scores
if 1 <= score <= 5:
    ratings.append(score)
```

**Review Comment:**
> The comment restates what the code does. Instead, extract intent:
>
> ```python
> VALID_RATING_RANGE = range(1, 6)
> 
> if score in VALID_RATING_RANGE:
>     ratings.append(score)
> ```
>
> Or better, a named function:
> ```python
> def is_valid_rating(score):
>     """Rating must be 1-5 inclusive."""
>     return 1 <= score <= 5
>
> if is_valid_rating(score):
>     ratings.append(score)
> ```

---

## 7. Repeated Logic Across Functions

**Bad:**
```python
def get_user_record(tenant_id, user_id, dynamodb_table):
    response = dynamodb_table.get_item(
        Key={'pk': f'TENANT#{tenant_id}', 'sk': f'USER#{user_id}'}
    )
    return response.get('Item')

def get_settings_record(tenant_id, setting_id, dynamodb_table):
    response = dynamodb_table.get_item(
        Key={'pk': f'TENANT#{tenant_id}', 'sk': f'SETTING#{setting_id}'}
    )
    return response.get('Item')

def get_audit_record(tenant_id, audit_id, dynamodb_table):
    response = dynamodb_table.get_item(
        Key={'pk': f'TENANT#{tenant_id}', 'sk': f'AUDIT#{audit_id}'}
    )
    return response.get('Item')
```

**Review Comment:**
> DRY violation. Extract the key-building pattern:
>
> ```python
> def _build_dynamodb_key(tenant_id: str, entity_type: str, entity_id: str) -> dict:
>     return {
>         'pk': f'TENANT#{tenant_id}',
>         'sk': f'{entity_type}#{entity_id}'
>     }
>
> def get_user_record(tenant_id: str, user_id: str, table: Table) -> dict | None:
>     response = table.get_item(Key=_build_dynamodb_key(tenant_id, 'USER', user_id))
>     return response.get('Item')
> ```

---

## 8. Error Handling That Swallows Signals

**Bad:**
```python
def process_request(json_input):
    try:
        data = RequestData.model_validate(json_input)
        result = compute(data)
        table.put_item(Item=result.model_dump())
    except Exception:
        log.error("failed")
        return None
```

**Review Comment:**
> `except Exception` is too broad; it catches programming errors (AttributeError, TypeError) alongside real failures.
>
> ```python
> def process_request(json_input: dict) -> Result | None:
>     try:
>         data = RequestData.model_validate(json_input)
>     except ValidationError as e:
>         log.warning("invalid input", extra={"error": str(e)})
>         return None
>     
>     result = compute(data)  # let this fail loudly if broken
>     table.put_item(Item=result.model_dump())  # same here
>     return result
> ```
>
> Catch only what you expect and can recover from. Let bugs bubble up.

---

## 9. Guard Clauses Over Nested Blocks

**Bad:**
```python
def calculate_payout(order):
    if order.total is not None:
        if order.is_valid:
            if order.has_discount:
                return order.total * DISCOUNT_RATE
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
> def calculate_payout(order: Order) -> float | None:
>     if order.total is None:
>         return None
>     if not order.is_valid:
>         return 0.0
>     if not order.has_discount:
>         return 0.0
>     return order.total * DISCOUNT_RATE
> ```
>
> Read top-to-bottom: early exits for invalid states, then the success path.

---

## 10. Mixing Public API Design with Implementation

**Bad:**
```python
class DataStore:
    def __init__(self, backend):
        self._backend = backend
        self._local_cache = {}
    
    def get(self, key):
        if key in self._local_cache:
            return self._local_cache[key]
        item = self._backend.fetch(key)
        if item:
            self._local_cache[key] = item
        return item
    
    def set(self, key, value):
        self._local_cache[key] = value
        self._backend.store(key, value)
```

**Review Comment:**
> API surface is too low-level and exposes caching internals. Caller shouldn't know about dual-layer lookup.
>
> Rethink:
> ```python
> class DataStore:
>     def retrieve(self, key: str) -> Data | None:
>         """Retrieve data with transparent multi-tier caching."""
>         ...
>     
>     def clear(self, key: str) -> None:
>         """Clear data from all cache layers."""
>         ...
> ```
>
> Hide `get`/`set` and `_local_cache` from callers. They call `retrieve()` and don't care about tiers.

---

## When To Stop

If you catch yourself thinking "this is just style," it's probably worth flagging. Your goal as a reviewer is to make the next change easier by raising the clarity floor. But don't nitpick; focus on:

1. Is the next person's intent clear from names and structure?
2. Does error handling make sense?
3. Is duplication hiding a missing abstraction?
4. Are types as explicit as they should be?
