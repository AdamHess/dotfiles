# How I Write Code

Extracted from my review patterns. These are my personal implementation guidelines, not just review checklists.

---

## Names Are Your Primary Tool

### Variables and Parameters

```python
# Don't do this
def score_user(s, w, t):
    return s * w > t

# Do this
def score_user(engagement_score: float, weight_multiplier: float, approval_threshold: float) -> bool:
    return engagement_score * weight_multiplier > approval_threshold
```

Names should answer:
- **What is this?** (not `s`, but `engagement_score`)
- **What's its range?** (0-1? 0-100? unbounded?)
- **What's the domain?** (it's a score in what context?)

### Functions

```python
# Don't do this
def process_data(obj):
    ...

# Do this
def fetch_and_validate_user_profile(user_id: str, database: Database) -> UserProfile:
    ...
```

Function names describe the contract, not the implementation. Callers should understand from the name:
- What they're getting back
- What they're passing in
- What side effects to expect (if any)

---

## One Function, One Responsibility

```python
# Don't do this
def process_and_store_order(order_id, bucket, table):
    data = s3.get_object(Bucket=bucket, Key=order_id)
    parsed = parse_json(data['Body'].read())
    item = {'id': order_id, 'content': parsed['content']}
    table.put_item(Item=item)
    notify_downstream(order_id, item)
    return item

# Do this
def fetch_from_storage(order_id: str, bucket: str) -> bytes:
    response = s3.get_object(Bucket=bucket, Key=order_id)
    return response['Body'].read()

def parse_order(raw_bytes: bytes) -> ParsedOrder:
    return ParsedOrder.model_validate(parse_json(raw_bytes))

def save_to_database(order: ParsedOrder, table: Table) -> None:
    table.put_item(Item={'id': order.id, 'content': order.content})

# Caller orchestrates
order_bytes = fetch_from_storage(order_id, bucket)
parsed = parse_order(order_bytes)
save_to_database(parsed, table)
notify_downstream(parsed.id, parsed)
```

Each function has one reason to change. When you can't name it simply, it's doing too much.

---

## No Boolean Parameters

```python
# Don't do this
def save_record(record, table, is_deleted=False):
    if is_deleted:
        record['status'] = 'deleted'
    table.put_item(Item=record)

# Do this
def save_active_record(record: Record, table: Table) -> None:
    record_item = {'id': record.id, 'status': 'active', 'data': record.data}
    table.put_item(Item=record_item)

def save_deleted_record(record: Record, table: Table) -> None:
    record_item = {'id': record.id, 'status': 'deleted', 'data': record.data}
    table.put_item(Item=record_item)
```

Boolean parameters hide branching. Split into two clear functions instead. The caller decides which one to call.

---

## Types Everywhere

```python
# Don't do this
def get_score(data, model):
    if not data.get('amount'):
        return None
    features = extract_features(data)
    return model.predict(features)[0]

# Do this
class DataInput(BaseModel):
    amount: float
    is_active: bool
    has_flag: bool

class Score(BaseModel):
    value: float
    confidence: float

def get_score(data: DataInput, model: Model) -> Score | None:
    if data.amount <= 0:
        return None
    features = extract_features(data)
    result = model.predict(features)
    return Score.model_validate(result)
```

- Use Pydantic models for structured data, never `dict`.
- Validate at boundaries (API input, external JSON), trust types internally.
- Every function parameter and return must have an explicit type.

---

## Guard Clauses for Readability

```python
# Don't do this
def calculate_result(obj):
    if obj.value is not None:
        if obj.is_active:
            if obj.has_flag:
                return obj.value * MULTIPLIER
            else:
                return 0
        else:
            return 0
    else:
        return None

# Do this
def calculate_result(obj: DataObject) -> float | None:
    if obj.value is None:
        return None
    if not obj.is_active:
        return 0.0
    if not obj.has_flag:
        return 0.0
    return obj.value * MULTIPLIER
```

Early returns flatten nesting. Code flows top-to-bottom: invalid states first, success path last.

---

## Minimal Public API

```python
# Don't do this
class Cache:
    def get(self, key):
        if key in self._cache:
            return self._cache[key]
        item = self._store.get(key)
        self._cache[key] = item
        return item
    
    def set(self, key, value):
        self._cache[key] = value
        self._store.put(key, value)

# Caller knows about two layers, inconsistent behavior

# Do this
class Cache:
    def fetch(self, key: str) -> Item | None:
        """Fetch item with transparent two-tier caching."""
        if cached := self._local_cache.get(key):
            return cached
        item = self._store.get(key)
        if item:
            self._local_cache[key] = item
        return item
    
    def invalidate(self, key: str) -> None:
        """Clear item from all cache layers."""
        self._local_cache.pop(key, None)
        # Backend invalidation handled by TTL

# Caller doesn't care about layers
```

Hide implementation details (`_local_cache`, tier logic). Public methods should be high-level operations.

---

## Specific Error Handling

```python
# Don't do this
def process_input(json_data):
    try:
        data = Input.model_validate(json_data)
        result = compute(data)
        table.put_item(Item=result.model_dump())
    except Exception:
        log.error("failed")
        return None

# Don't do this
def process_input(json_data):
    try:
        data = Input.model_validate(json_data)
    except ValidationError as e:
        log.warning("invalid input", extra={"error": str(e)})
        return None
    except Exception as e:
        log.error("unexpected error", extra={"error": str(e)})
        raise
    
    result = compute(data)  # let failures bubble up
    table.put_item(Item=result.model_dump())
    return result

# Do this
def process_input(json_data: dict) -> Result | None:
    """Process input with graceful validation failure handling."""
    try:
        data = Input.model_validate(json_data)
    except ValidationError as e:
        log.warning("Invalid input format", extra={"error": str(e), "input": json_data})
        return None
    
    # These should fail loudly if broken; don't swallow them
    result = compute(data)
    table.put_item(Item=result.model_dump())
    return result
```

- Catch only what you expect and can recover from.
- Let programming errors (TypeError, AttributeError) bubble up so you notice them.
- Include context in error logs (what you tried, why it failed).

---

## Extract Duplication Immediately

```python
# Don't do this
def get_user(tenant_id, user_id, table):
    response = table.get_item(Key={'pk': f'TENANT#{tenant_id}', 'sk': f'USER#{user_id}'})
    return response.get('Item')

def get_config(tenant_id, config_id, table):
    response = table.get_item(Key={'pk': f'TENANT#{tenant_id}', 'sk': f'CONFIG#{config_id}'})
    return response.get('Item')

def get_setting(tenant_id, setting_id, table):
    response = table.get_item(Key={'pk': f'TENANT#{tenant_id}', 'sk': f'SETTING#{setting_id}'})
    return response.get('Item')

# Do this
def _build_key(tenant_id: str, entity_type: str, entity_id: str) -> dict[str, str]:
    return {'pk': f'TENANT#{tenant_id}', 'sk': f'{entity_type}#{entity_id}'}

def get_user(tenant_id: str, user_id: str, table: Table) -> dict | None:
    response = table.get_item(Key=_build_key(tenant_id, 'USER', user_id))
    return response.get('Item')

def get_config(tenant_id: str, config_id: str, table: Table) -> dict | None:
    response = table.get_item(Key=_build_key(tenant_id, 'CONFIG', config_id))
    return response.get('Item')
```

When you copy code, you've found a missing abstraction. Extract it before the duplication spreads.

---

## Comments Are A Failure

```python
# Don't do this
# Check if priority is in valid range (1-10)
if 1 <= priority <= 10:
    tasks.append(priority)

# Do this
VALID_PRIORITY_RANGE = range(1, 11)  # 1-10 inclusive

if priority in VALID_PRIORITY_RANGE:
    tasks.append(priority)

# Or better, extract:
def is_valid_priority(priority: int) -> bool:
    """Priority must be 1-10 inclusive."""
    return 1 <= priority <= 10

if is_valid_priority(priority):
    tasks.append(priority)
```

If you're about to write a comment, refactor instead:
- Extract the logic into a named function or constant.
- Rename the variable to be more specific.
- Restructure the control flow so intent is obvious.

The only exception: non-obvious constraints, contract boundaries, legal/compliance notes.

---

## Tests Name the Broken Scenario

```python
# Don't do this
def test_compute():
    result = compute({'amount': 100})
    assert result is not None

def test_compute_edge_case():
    result = compute({'amount': 0})
    assert result == 0

# Do this
def test_compute_when_amount_provided_should_return_float():
    result = compute(DataInput(amount=100, is_active=True))
    assert isinstance(result, float)
    assert result > 0

def test_compute_when_amount_zero_should_return_none():
    result = compute(DataInput(amount=0, is_active=True))
    assert result is None
```

Test names are documentation. When a test fails, the name should tell you the broken scenario immediately.

Pattern: `test_<function>_when_<condition>_should_<outcome>`

---

## DRY on Error Handling Too

```python
# Don't do this
def call_api_1(data):
    try:
        return service_1(data)
    except ConnectionError:
        log.error("service_1 down")
        return None

def call_api_2(data):
    try:
        return service_2(data)
    except ConnectionError:
        log.error("service_2 down")
        return None

# Do this
def _handle_connection_error(service_name: str, error: ConnectionError) -> None:
    log.error(f"Service unavailable", extra={"service": service_name, "error": str(error)})

def call_api_1(data: DataInput) -> Result | None:
    try:
        return service_1(data)
    except ConnectionError as e:
        _handle_connection_error("service_1", e)
        return None

def call_api_2(data: DataInput) -> Result | None:
    try:
        return service_2(data)
    except ConnectionError as e:
        _handle_connection_error("service_2", e)
        return None
```

If you handle the same error the same way in multiple places, extract the handler.

---

## Changelog

When implementing, move through these priorities in order:

1. **Clear naming first.** If you can't name it clearly, the design isn't done.
2. **Single responsibility.** If it does more than one thing, split it.
3. **Guard clauses.** Flatten nesting so the success path is obvious.
4. **Explicit types.** Every parameter and return gets a type.
5. **Specific error handling.** Catch only what you can recover from.
6. **Extract duplication.** Once you see the pattern twice, make it once.
7. **No comments for code flow.** If you need a comment, refactor.
8. **Test names document intent.** A failing test name tells you what broke.

Done in this order, your code is clean before you think about refactoring or "elegance."
