# ADR-002: Event-Driven Integration Between Contexts

## Status
Accepted

## Context

Bounded contexts need to communicate to support end-to-end business processes:
- Legal context incorporates company → Financial context creates chart of accounts
- HR context hires employee → Financial context creates payroll account
- Operations context confirms order → Financial context recognizes revenue
- Financial context closes period → Compliance context schedules tax filings

We need a mechanism for cross-context communication that maintains loose coupling.

## Decision

We will use **event-driven integration** as the primary communication mechanism between bounded contexts.

### Pattern: Domain Events

1. **Aggregates publish events** when state changes occur
2. **Events stored in outbox table** (transactional outbox pattern)
3. **Background process publishes** events to event bus
4. **Contexts subscribe** to events they care about
5. **Handlers are idempotent** (safe to process multiple times)

### Event Structure

```haskell
data DomainEvent = DomainEvent
  { eventId :: EventId
  , eventType :: EventType
  , aggregateId :: AggregateId
  , occurredAt :: Timestamp
  , payload :: Value
  }
```

### Key Events

#### Legal Context
- `CompanyIncorporated`
- `DirectorAppointed`
- `CorporateSealRegistered`

#### Financial Context
- `JournalEntryPosted`
- `FiscalPeriodClosed`
- `InvoiceIssued`

#### HR Context
- `EmployeeHired`
- `EmploymentTerminated`
- `SalaryAdjusted`

#### Operations Context
- `OrderConfirmed`
- `OrderShipped`
- `InventoryReceived`

#### Compliance Context
- `RegulatoryDeadlineApproaching`
- `FilingCompleted`

## Consequences

### Positive

- **Loose coupling**: Contexts don't directly depend on each other
- **Scalability**: Async processing enables scale
- **Auditability**: Complete event log for compliance
- **Flexibility**: Easy to add new event subscribers
- **Temporal queries**: Can reconstruct state at any point in time

### Negative

- **Eventual consistency**: Cross-context operations not immediately consistent
- **Complexity**: More moving parts than synchronous calls
- **Debugging**: Harder to trace across async boundaries
- **Event schema evolution**: Need versioning strategy
- **Duplicate events**: Must handle idempotency

### Mitigation

- **Outbox pattern**: Ensures exactly-once publishing
- **Correlation IDs**: Track requests across contexts
- **Event versioning**: Support schema evolution
- **Idempotent handlers**: Safe to replay events
- **Monitoring**: Track event lag and failures

## Alternatives Considered

### 1. Synchronous API Calls
- **Rejected**: Creates tight coupling between contexts
- **Issue**: Changes in one context break others
- **Issue**: Cannot scale independently

### 2. Shared Database
- **Rejected**: Breaks encapsulation of aggregates
- **Issue**: Schema changes affect multiple contexts
- **Issue**: Cannot use different database technologies

### 3. Message Queue with RPC Pattern
- **Rejected**: Still creates coupling via request/response
- **Issue**: Caller must know about callee
- **Issue**: Synchronous semantics over async infrastructure

## Implementation Plan

### Phase 1: In-Process Event Bus
- Simple in-memory event bus
- Synchronous event processing
- Good for development/testing

### Phase 2: Outbox Pattern
- Store events in database table
- Background process publishes events
- Ensures exactly-once semantics

### Phase 3: External Event Bus (Optional)
- Use Kafka/RabbitMQ for production
- Supports distributed deployment
- Better scalability and reliability

## Event Versioning Strategy

### Upcasting Pattern
```haskell
data CompanyIncorporatedV1 = ...
data CompanyIncorporatedV2 = ...

upcast :: CompanyIncorporatedV1 -> CompanyIncorporatedV2
```

### Versioned Topics
- Topic: `company.incorporated.v1`
- Topic: `company.incorporated.v2`
- Consumers subscribe to versions they support

## Monitoring

### Metrics to Track
- Event publishing lag (time from creation to publication)
- Event processing time (time from publication to handling)
- Failed event count
- Dead letter queue size
- Event throughput (events/second)

### Alerts
- Event lag > 1 minute
- Failed events > threshold
- Dead letter queue growing
- Event bus unavailable

## References

- Hohpe, Gregor. "Enterprise Integration Patterns"
- Richardson, Chris. "Microservices Patterns" (Transactional Outbox)
- Kleppmann, Martin. "Designing Data-Intensive Applications"
