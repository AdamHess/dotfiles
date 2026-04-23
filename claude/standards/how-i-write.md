# How I Write Code

Extracted from my review patterns. These are my personal implementation guidelines, not just review checklists.

---

## Names Are Your Primary Tool

### Variables and Parameters

```python
# Don't do this
def score_case(s, w, t):
    return s * w > t

# Do this
def score_case(confidence_score: float, weight_multiplier: float, approval_threshold: float) -> bool:
    return confidence_score * weight_multiplier > approval_threshold
```

Names should answer:
- **What is this?** (not `s`, but `confidence_score`)
- **What's its range?** (0-1? 0-100? unbounded?)
- **What's the domain?** (it's a score in what context?)

### Functions

```python
# Don't do this
def process_doc(doc):
    ...

# Do this
def fetch_and_validate_docket_entry(entry_id: str, dynamodb_table: Table) -> DocketEntry:
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
def process_and_cache_document(doc_id, bucket, table):
    doc = s3.get_object(Bucket=bucket, Key=doc_id)
    parsed = parse_pdf(doc['Body'].read())
    item = {'id': doc_id, 'text': parsed['text']}
    table.put_item(Item=item)
    notify_downstream(doc_id, item)
    return item

# Do this
def fetch_from_s3(doc_id: str, bucket: str) -> bytes:
    response = s3.get_object(Bucket=bucket, Key=doc_id)
    return response['Body'].read()

def parse_document(raw_bytes: bytes) -> ParsedDocument:
    return ParsedDocument.model_validate(parse_pdf(raw_bytes))

def save_to_cache(doc: ParsedDocument, table: Table) -> None:
    table.put_item(Item={'id': doc.id, 'text': doc.text})

# Caller orchestrates
doc_bytes = fetch_from_s3(doc_id, bucket)
parsed = parse_document(doc_bytes)
save_to_cache(parsed, table)
notify_downstream(parsed.id, parsed)
```

Each function has one reason to change. When you can't name it simply, it's doing too much.

---

## No Boolean Parameters

```python
# Don't do this
def save_document(doc, table, archive=False):
    if archive:
        doc['status'] = 'archived'
    table.put_item(Item=doc)

# Do this
def save_active_document(doc: Document, table: Table) -> None:
    doc_item = {'id': doc.id, 'status': 'active', 'data': doc.data}
    table.put_item(Item=doc_item)

def save_archived_document(doc: Document, table: Table) -> None:
    doc_item = {'id': doc.id, 'status': 'archived', 'data': doc.data}
    table.put_item(Item=doc_item)
```

Boolean parameters hide branching. Split into two clear functions instead. The caller decides which one to call.

---

## Types Everywhere

```python
# Don't do this
def get_case_prediction(case_data, model):
    if not case_data.get('total_damages'):
        return None
    features = extract_features(case_data)
    return model.predict(features)[0]

# Do this
class CaseFeatures(BaseModel):
    total_damages: float
    liability_assigned: bool
    treatment_recommended: bool

class Prediction(BaseModel):
    value: float
    confidence: float

def get_case_prediction(case: CaseFeatures, model: Model) -> Prediction | None:
    if case.total_damages <= 0:
        return None
    features = extract_features(case)
    result = model.predict(features)
    return Prediction.model_validate(result)
```

- Use Pydantic models for structured data, never `dict`.
- Validate at boundaries (API input, external JSON), trust types internally.
- Every function parameter and return must have an explicit type.

---

## Guard Clauses for Readability

```python
# Don't do this
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

# Do this
def calculate_settlement(case: Case) -> float | None:
    if case.total_damages is None:
        return None
    if not case.liability_assigned:
        return 0.0
    if not case.treatment_recommended:
        return 0.0
    return case.total_damages * LIABILITY_FACTOR
```

Early returns flatten nesting. Code flows top-to-bottom: invalid states first, success path last.

---

## Minimal Public API

```python
# Don't do this
class DocumentCache:
    def get(self, doc_id):
        if doc_id in self._cache:
            return self._cache[doc_id]
        item = self._table.get_item(Key={'id': doc_id})
        self._cache[doc_id] = item
        return item
    
    def set(self, doc_id, value):
        self._cache[doc_id] = value
        self._table.put_item(Item={'id': doc_id, 'data': value})

# Caller knows about two layers, inconsistent behavior

# Do this
class DocumentCache:
    def fetch(self, doc_id: str) -> Document | None:
        """Fetch document with transparent two-tier caching."""
        if cached := self._local_cache.get(doc_id):
            return cached
        item = self._table.get_item(Key={'id': doc_id})
        if item:
            self._local_cache[doc_id] = item
        return item
    
    def invalidate(self, doc_id: str) -> None:
        """Clear document from all cache layers."""
        self._local_cache.pop(doc_id, None)
        # DynamoDB cache invalidation handled separately by TTL

# Caller doesn't care about layers
```

Hide implementation details (`_local_cache`, tier logic). Public methods should be high-level operations.

---

## Specific Error Handling

```python
# Don't do this
def process_case_data(case_json):
    try:
        data = CaseData.model_validate(case_json)
        result = predict(data)
        table.put_item(Item=result.model_dump())
    except Exception:
        log.error("failed")
        return None

# Don't do this
def process_case_data(case_json):
    try:
        data = CaseData.model_validate(case_json)
    except ValidationError as e:
        log.warning("invalid case data", extra={"error": str(e)})
        return None
    except Exception as e:
        log.error("unexpected error", extra={"error": str(e)})
        raise
    
    result = predict(data)  # let failures bubble up
    table.put_item(Item=result.model_dump())
    return result

# Do this
def process_case_data(case_json: dict) -> Prediction | None:
    """Process case data with graceful validation failure handling."""
    try:
        data = CaseData.model_validate(case_json)
    except ValidationError as e:
        log.warning("Invalid case data format", extra={"error": str(e), "input": case_json})
        return None
    
    # These should fail loudly if broken; don't swallow them
    result = predict(data)
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
def get_docket_entry(matter_id, entry_id, table):
    response = table.get_item(Key={'pk': f'MATTER#{matter_id}', 'sk': f'ENTRY#{entry_id}'})
    return response.get('Item')

def get_document(matter_id, doc_id, table):
    response = table.get_item(Key={'pk': f'MATTER#{matter_id}', 'sk': f'DOC#{doc_id}'})
    return response.get('Item')

def get_audit_log(matter_id, log_id, table):
    response = table.get_item(Key={'pk': f'MATTER#{matter_id}', 'sk': f'LOG#{log_id}'})
    return response.get('Item')

# Do this
def _build_dynamodb_key(matter_id: str, entity_type: str, entity_id: str) -> dict[str, str]:
    return {'pk': f'MATTER#{matter_id}', 'sk': f'{entity_type}#{entity_id}'}

def get_docket_entry(matter_id: str, entry_id: str, table: Table) -> dict | None:
    response = table.get_item(Key=_build_dynamodb_key(matter_id, 'ENTRY', entry_id))
    return response.get('Item')

def get_document(matter_id: str, doc_id: str, table: Table) -> dict | None:
    response = table.get_item(Key=_build_dynamodb_key(matter_id, 'DOC', doc_id))
    return response.get('Item')
```

When you copy code, you've found a missing abstraction. Extract it before the duplication spreads.

---

## Comments Are A Failure

```python
# Don't do this
# Check if treatment severity is in valid range (0-5)
if 0 <= score <= 5:
    treatments.append(score)

# Do this
VALID_TREATMENT_SEVERITY_RANGE = range(0, 6)  # 0-5 inclusive

if score in VALID_TREATMENT_SEVERITY_RANGE:
    treatments.append(score)

# Or better, extract:
def is_valid_treatment_severity(score: int) -> bool:
    """Treatment severity must be 0-5 inclusive."""
    return 0 <= score <= 5

if is_valid_treatment_severity(score):
    treatments.append(score)
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
def test_prediction():
    result = get_prediction({'total_damages': 100000})
    assert result is not None

def test_prediction_edge_case():
    result = get_prediction({'total_damages': 0})
    assert result == 0

# Do this
def test_get_prediction_when_damages_provided_should_return_float():
    result = get_prediction(CaseFeatures(total_damages=100000, ...))
    assert isinstance(result, float)
    assert result > 0

def test_get_prediction_when_damages_zero_should_return_none():
    result = get_prediction(CaseFeatures(total_damages=0, ...))
    assert result is None
```

Test names are documentation. When a test fails, the name should tell you the broken scenario immediately.

Pattern: `test_<function>_when_<condition>_should_<outcome>`

---

## DRY on Error Handling Too

```python
# Don't do this
def process_case_1(data):
    try:
        return internal_service_1(data)
    except ServiceUnavailableError:
        log.error("service_1 down")
        return None

def process_case_2(data):
    try:
        return internal_service_2(data)
    except ServiceUnavailableError:
        log.error("service_2 down")
        return None

# Do this
def _handle_service_failure(service_name: str, error: ServiceUnavailableError) -> None:
    log.error(f"Service unavailable", extra={"service": service_name, "error": str(error)})

def process_case_1(data: CaseFeatures) -> Result | None:
    try:
        return internal_service_1(data)
    except ServiceUnavailableError as e:
        _handle_service_failure("internal_service_1", e)
        return None

def process_case_2(data: CaseFeatures) -> Result | None:
    try:
        return internal_service_2(data)
    except ServiceUnavailableError as e:
        _handle_service_failure("internal_service_2", e)
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
