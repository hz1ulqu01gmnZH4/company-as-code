# Event-Driven Architecture

## Overview

Domain events enable loose coupling between bounded contexts and aggregates, facilitate audit trails, and support eventual consistency in the Japanese company model.

## Event Design Principles

### 1. Events Are Immutable Facts

**Rule**: Events represent something that has already happened and cannot be changed.

```haskell
-- ✅ Past tense naming
data CompanyIncorporated = CompanyIncorporated { ... }
data EmployeeHired = EmployeeHired { ... }
data InvoiceIssued = InvoiceIssued { ... }

-- ❌ Present/future tense (commands, not events)
data IncorporateCompany = ...  -- This is a command
data HireEmployee = ...         -- This is a command
```

### 2. Events Contain Sufficient Information

**Rule**: Event consumers should not need to query for additional data.

```haskell
-- ❌ BAD: Insufficient information
data OrderConfirmed = OrderConfirmed
  { orderId :: OrderId
  }

-- ✅ GOOD: Complete information
data OrderConfirmed = OrderConfirmed
  { orderId :: OrderId
  , orderNumber :: OrderNumber
  , customerId :: CustomerId
  , customerName :: Text
  , lineItems :: NonEmpty OrderLineItem
  , total :: Money
  , confirmedAt :: Timestamp
  , confirmedBy :: EmployeeId
  }
```

### 3. Events Are Self-Describing

**Rule**: Events should be understandable without external context.

```haskell
data EmployeeHired = EmployeeHired
  { employeeId :: EmployeeId
  , companyId :: CompanyId
  , employeeNumber :: EmployeeNumber
  , personalInfo :: PersonalInfo
  , position :: Position
  , department :: DepartmentId
  , departmentName :: Text          -- ✅ Include denormalized data
  , salary :: BaseSalary
  , hireDate :: Date
  , employmentType :: EmploymentType
  , occurredAt :: Timestamp
  , recordedBy :: EmployeeId
  }
```

## Event Categories

### 1. Entity Lifecycle Events

**Purpose**: Track creation, modification, and deletion of entities.

```haskell
-- Company lifecycle
data CompanyIncorporated = CompanyIncorporated { ... }
data CompanyDissolved = CompanyDissolved { ... }

-- Employee lifecycle
data EmployeeHired = EmployeeHired { ... }
data EmploymentTerminated = EmploymentTerminated { ... }

-- Product lifecycle
data ProductAdded = ProductAdded { ... }
data ProductDiscontinued = ProductDiscontinued { ... }
```

### 2. State Transition Events

**Purpose**: Capture significant state changes within aggregates.

```haskell
-- Order state transitions
data OrderConfirmed = OrderConfirmed { ... }
data OrderShipped = OrderShipped { ... }
data OrderDelivered = OrderDelivered { ... }
data OrderCancelled = OrderCancelled { ... }

-- Employment status transitions
data EmployeeSuspended = EmployeeSuspended { ... }
data EmployeeReactivated = EmployeeReactivated { ... }
data LeaveGranted = LeaveGranted { ... }

-- Fiscal period transitions
data PeriodOpened = PeriodOpened { ... }
data PeriodClosed = PeriodClosed { ... }
```

### 3. Business Process Events

**Purpose**: Represent completion of significant business processes.

```haskell
-- Financial processes
data FiscalYearClosed = FiscalYearClosed { ... }
data TaxReturnFiled = TaxReturnFiled { ... }
data PaymentReceived = PaymentReceived { ... }

-- HR processes
data SalaryCalculated = SalaryCalculated { ... }
data BenefitsEnrolled = BenefitsEnrolled { ... }
data PerformanceReviewed = PerformanceReviewed { ... }
```

### 4. Integration Events

**Purpose**: Notify external systems or contexts of changes.

```haskell
-- Published to external systems
data InvoiceIssued = InvoiceIssued { ... }
data PaymentDue = PaymentDue { ... }
data RegulatoryDeadlineApproaching = RegulatoryDeadlineApproaching { ... }
```

### 5. Audit Events

**Purpose**: Track actions for compliance and audit trails.

```haskell
data UserActionRecorded = UserActionRecorded
  { actionId :: ActionId
  , userId :: UserId
  , actionType :: ActionType
  , entityType :: EntityType
  , entityId :: EntityId
  , timestamp :: Timestamp
  , ipAddress :: Maybe IPAddress
  , changes :: StateChanges
  }

data CorporateSealUsed = CorporateSealUsed
  { sealId :: SealId
  , usedBy :: EmployeeId
  , documentId :: DocumentId
  , purpose :: SealUsagePurpose
  , timestamp :: Timestamp
  }
```

## Event Schema Design

### Standard Event Structure

```haskell
-- All events follow this pattern
data DomainEvent = DomainEvent
  { eventId :: EventId          -- Unique event identifier
  , eventType :: EventType      -- Type discriminator
  , aggregateId :: AggregateId  -- Which aggregate produced this
  , aggregateType :: AggregateType
  , version :: Version          -- Aggregate version
  , occurredAt :: Timestamp     -- When it happened
  , recordedBy :: Maybe UserId  -- Who caused it
  , correlationId :: Maybe CorrelationId  -- Request tracking
  , causationId :: Maybe EventId          -- Which event caused this
  , metadata :: EventMetadata   -- Additional context
  , payload :: Value            -- Event-specific data
  }

data EventMetadata = EventMetadata
  { source :: Source            -- Which service/context
  , environment :: Environment  -- Production/staging/dev
  , version :: SchemaVersion    -- Event schema version
  , tags :: Map Text Text       -- Custom tags
  }
```

### Versioned Event Schemas

```haskell
-- Support schema evolution
data CompanyIncorporatedV1 = CompanyIncorporatedV1
  { companyId :: CompanyId
  , legalName :: CompanyLegalName
  , establishmentDate :: Date
  }

data CompanyIncorporatedV2 = CompanyIncorporatedV2
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber  -- ✅ Added in V2
  , legalName :: CompanyLegalName
  , legalNameKana :: CompanyLegalNameKana  -- ✅ Added in V2
  , establishmentDate :: Date
  , fiscalYearEnd :: FiscalYearEnd  -- ✅ Added in V2
  }

-- Upcasting from V1 to V2
upcastCompanyIncorporated :: CompanyIncorporatedV1 -> CompanyIncorporatedV2
upcastCompanyIncorporated v1 = CompanyIncorporatedV2
  { companyId = companyId v1
  , corporateNumber = generateCorporateNumber (companyId v1)
  , legalName = legalName v1
  , legalNameKana = inferKana (legalName v1)
  , establishmentDate = establishmentDate v1
  , fiscalYearEnd = defaultFiscalYearEnd  -- March 31
  }
```

## Event Publishing Patterns

### Pattern 1: Outbox Pattern (Reliable Publishing)

**Problem**: Ensure events are published exactly once, even if system crashes.

**Solution**: Store events in database as part of aggregate transaction.

```haskell
-- Save aggregate and events atomically
saveWithEvents :: Aggregate a => a -> [DomainEvent] -> Transaction ()
saveWithEvents aggregate events = do
  -- Save aggregate state
  saveAggregate aggregate
  -- Save events to outbox table
  forM_ events $ \event -> do
    insertOutboxEvent event { status = Pending }
  commit

-- Background process publishes events
publishOutboxEvents :: IO ()
publishOutboxEvents = forever $ do
  events <- queryPendingEvents
  forM_ events $ \event -> do
    publishToEventBus event
    markEventAsPublished event
  threadDelay (seconds 1)

-- Event bus (Kafka, RabbitMQ, etc.)
publishToEventBus :: DomainEvent -> IO ()
publishToEventBus event = do
  let topic = eventTypeTopic (eventType event)
  let key = aggregateId event
  let value = encodeJSON event
  produceMessage topic key value
```

### Pattern 2: In-Memory Event Bus (Simple)

**Use Case**: Development, testing, or simple deployments.

```haskell
-- In-memory event bus
data EventBus = EventBus
  { subscribers :: TVar (Map EventType [EventHandler])
  }

newEventBus :: IO EventBus
newEventBus = do
  subs <- newTVarIO Map.empty
  pure $ EventBus subs

-- Subscribe to events
subscribe :: EventBus -> EventType -> EventHandler -> IO ()
subscribe bus eventType handler = atomically $ do
  subs <- readTVar (subscribers bus)
  let handlers = Map.findWithDefault [] eventType subs
  writeTVar (subscribers bus) (Map.insert eventType (handler : handlers) subs)

-- Publish event
publish :: EventBus -> DomainEvent -> IO ()
publish bus event = do
  handlers <- atomically $ do
    subs <- readTVar (subscribers bus)
    pure $ Map.findWithDefault [] (eventType event) subs
  -- Execute handlers asynchronously
  forM_ handlers $ \handler -> async (handler event)
```

### Pattern 3: Event Sourcing

**Use Case**: Complete audit trail, temporal queries.

**Pattern**: Store all events, derive current state by replaying.

```haskell
-- Event store interface
class EventStore m where
  appendEvent :: StreamId -> DomainEvent -> m ()
  loadEvents :: StreamId -> m [DomainEvent]
  loadEventsSince :: StreamId -> Version -> m [DomainEvent]

-- Rebuild aggregate from events
loadAggregate :: EventStore m => AggregateId -> m SalesOrder
loadAggregate aggId = do
  events <- loadEvents (streamId aggId)
  pure $ foldl apply emptyOrder events

-- Apply event to aggregate
apply :: SalesOrder -> DomainEvent -> SalesOrder
apply order (OrderConfirmed confirmed) =
  order { orderStatus = Confirmed (confirmedDate confirmed) }
apply order (OrderShipped shipment) =
  order { orderStatus = Shipped (shipmentInfo shipment) }
apply order _ = order

-- Handle command and generate events
confirmOrder :: SalesOrder -> Either OrderError [DomainEvent]
confirmOrder order = case orderStatus order of
  Draft -> Right [OrderConfirmed ...]
  _ -> Left InvalidStateTransition
```

## Event Handlers

### Handler Registration

```haskell
-- Financial context subscribes to HR events
setupFinancialHandlers :: EventBus -> IO ()
setupFinancialHandlers bus = do
  subscribe bus "EmployeeHired" handleEmployeeHired
  subscribe bus "EmploymentTerminated" handleEmploymentTerminated
  subscribe bus "SalaryAdjusted" handleSalaryAdjusted

-- Handler implementation
handleEmployeeHired :: EventHandler
handleEmployeeHired event = do
  let EmployeeHired{..} = decodeEvent event
  -- Create payroll account
  createPayrollAccount employeeId salary
  -- Set up withholding tax
  setupWithholdingTax employeeId
  -- Record in general ledger
  recordPayrollExpenseAccount employeeId department
```

### Idempotent Handlers

**Rule**: Handlers must be idempotent (safe to execute multiple times).

```haskell
-- ✅ GOOD: Idempotent handler
handleOrderConfirmed :: EventHandler
handleOrderConfirmed event = do
  let OrderConfirmed{..} = decodeEvent event
  -- Check if already processed
  exists <- invoiceExists orderId
  unless exists $ do
    -- Only create if not already created
    createInvoice orderId orderNumber total

-- Track processed events
data ProcessedEvent = ProcessedEvent
  { eventId :: EventId
  , handlerName :: Text
  , processedAt :: Timestamp
  }

markEventProcessed :: EventId -> Text -> IO ()
markEventProcessed eventId handler = do
  insertProcessedEvent $ ProcessedEvent eventId handler now
```

### Error Handling in Handlers

```haskell
-- Retry with exponential backoff
handleEventWithRetry :: EventHandler -> DomainEvent -> IO ()
handleEventWithRetry handler event = do
  result <- try (handler event)
  case result of
    Right _ -> pure ()
    Left (err :: SomeException) -> do
      -- Log error
      logError $ "Event handler failed: " <> show err
      -- Retry with backoff
      retryWithBackoff (handler event) maxRetries
      -- Or move to dead letter queue
      moveToDeadLetterQueue event err

-- Dead letter queue for failed events
data DeadLetterEvent = DeadLetterEvent
  { originalEvent :: DomainEvent
  , error :: Text
  , failedAt :: Timestamp
  , retryCount :: Int
  }
```

## Event Flows

### Example: Order Processing Flow

```mermaid
OrderConfirmed (Operations Context)
  ↓
  ├─→ InventoryReserved (Operations Context)
  │     ↓
  │     └─→ InvoiceGenerated (Financial Context)
  │           ↓
  │           └─→ PaymentDue (Financial Context)
  │
  ├─→ ShipmentScheduled (Operations Context)
  │     ↓
  │     └─→ OrderShipped (Operations Context)
  │           ↓
  │           ├─→ RevenueRecognized (Financial Context)
  │           └─→ DeliveryNotificationSent (Operations Context)
  │
  └─→ OrderConfirmationSent (Operations Context)
```

### Example: Employee Onboarding Flow

```mermaid
EmployeeHired (HR Context)
  ↓
  ├─→ PayrollAccountCreated (Financial Context)
  │     ↓
  │     └─→ WithholdingTaxSetup (Financial Context)
  │
  ├─→ SocialInsuranceEnrollmentInitiated (HR Context)
  │     ↓
  │     └─→ InsurancePremiumCalculated (Financial Context)
  │
  ├─→ EmployeeNumberAssigned (HR Context)
  │
  └─→ ComplianceFilingRequired (Compliance Context)
        ↓
        └─→ FilingScheduled (Compliance Context)
```

## Event Schemas (Complete Examples)

### Company Events

```haskell
data CompanyIncorporated = CompanyIncorporated
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , legalNameKana :: CompanyLegalNameKana
  , entityType :: EntityType
  , establishmentDate :: EstablishmentDate
  , fiscalYearEnd :: FiscalYearEnd
  , registeredCapital :: RegisteredCapital
  , registeredAddress :: RegisteredAddress
  , representativeDirector :: DirectorId
  , representativeDirectorName :: PersonName
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)

data DirectorAppointed = DirectorAppointed
  { companyId :: CompanyId
  , companyName :: CompanyLegalName
  , directorId :: DirectorId
  , personId :: PersonId
  , directorName :: PersonName
  , position :: DirectorPosition
  , isRepresentative :: Bool
  , appointmentDate :: AppointmentDate
  , termExpiry :: TermExpiry
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)
```

### Financial Events

```haskell
data JournalEntryPosted = JournalEntryPosted
  { entryId :: JournalEntryId
  , companyId :: CompanyId
  , fiscalPeriodId :: PeriodId
  , fiscalPeriod :: Text  -- "2024-03"
  , entryDate :: Date
  , entryType :: EntryType
  , description :: Text
  , lines :: NonEmpty JournalLine
  , totalDebit :: Money
  , totalCredit :: Money
  , sourceDocument :: Maybe DocumentReference
  , postedBy :: UserId
  , postedByName :: Text
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)

data FiscalPeriodClosed = FiscalPeriodClosed
  { periodId :: PeriodId
  , companyId :: CompanyId
  , companyName :: CompanyLegalName
  , fiscalYear :: FiscalYear
  , periodNumber :: PeriodNumber
  , periodStart :: Date
  , periodEnd :: Date
  , trialBalance :: [(AccountCode, AccountName, Money)]
  , totalAssets :: Money
  , totalLiabilities :: Money
  , totalEquity :: Money
  , netIncome :: Money
  , closedAt :: Timestamp
  , closedBy :: UserId
  , closedByName :: Text
  }
  deriving (Eq, Generic, ToJSON, FromJSON)
```

### HR Events

```haskell
data EmployeeHired = EmployeeHired
  { employeeId :: EmployeeId
  , companyId :: CompanyId
  , employeeNumber :: EmployeeNumber
  , personalInfo :: PersonalInfo
  , employmentType :: EmploymentType
  , position :: Position
  , positionTitle :: Text
  , department :: DepartmentId
  , departmentName :: Text
  , manager :: Maybe EmployeeId
  , managerName :: Maybe PersonName
  , salary :: BaseSalary
  , hireDate :: HireDate
  , probationEndDate :: Maybe Date
  , workLocation :: WorkLocation
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)

data LeaveRequested = LeaveRequested
  { employeeId :: EmployeeId
  , employeeName :: PersonName
  , leaveType :: LeaveType
  , leaveTypeName :: Text  -- "有給休暇", "病気休暇", etc.
  , startDate :: Date
  , endDate :: Date
  , totalDays :: Days
  , reason :: LeaveReason
  , currentBalance :: Days
  , balanceAfterLeave :: Days
  , requestedAt :: Timestamp
  , requiresApproval :: Bool
  , approver :: Maybe EmployeeId
  }
  deriving (Eq, Generic, ToJSON, FromJSON)
```

### Compliance Events

```haskell
data RegulatoryDeadlineApproaching = RegulatoryDeadlineApproaching
  { filingId :: FilingId
  , requirementId :: RequirementId
  , requirementName :: Text
  , regulatoryAuthority :: RegulatoryAuthority
  , authorityName :: Text
  , dueDate :: Date
  , daysRemaining :: Days
  , urgency :: UrgencyLevel
  , assignedTo :: Maybe EmployeeId
  , assignedToName :: Maybe Text
  , preparationStatus :: FilingStatus
  , penalties :: [Penalty]
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)

data FilingCompleted = FilingCompleted
  { filingId :: FilingId
  , requirementId :: RequirementId
  , requirementName :: Text
  , companyId :: CompanyId
  , fiscalYear :: Maybe FiscalYear
  , dueDate :: Date
  , completedDate :: Date
  , filedDate :: Date
  , daysBeforeDeadline :: Days
  , filingMethod :: FilingMethod
  , confirmationNumber :: Maybe ConfirmationNumber
  , documents :: [DocumentReference]
  , filedBy :: EmployeeId
  , filedByName :: Text
  , occurredAt :: Timestamp
  }
  deriving (Eq, Generic, ToJSON, FromJSON)
```

## Event Serialization

### JSON Encoding

```haskell
-- Generic JSON encoding with type discriminator
instance ToJSON DomainEvent where
  toJSON event = object
    [ "eventId" .= eventId event
    , "eventType" .= eventType event
    , "aggregateId" .= aggregateId event
    , "aggregateType" .= aggregateType event
    , "version" .= version event
    , "occurredAt" .= occurredAt event
    , "metadata" .= metadata event
    , "payload" .= payload event
    ]

instance FromJSON DomainEvent where
  parseJSON = withObject "DomainEvent" $ \o -> do
    eventType <- o .: "eventType"
    payload <- o .: "payload"
    -- Decode specific event based on type
    specificEvent <- case eventType of
      "CompanyIncorporated" -> parseJSON payload
      "EmployeeHired" -> parseJSON payload
      "OrderConfirmed" -> parseJSON payload
      _ -> fail $ "Unknown event type: " <> eventType
    -- ... construct DomainEvent wrapper
```

### Avro Schema (For Kafka)

```json
{
  "type": "record",
  "name": "CompanyIncorporated",
  "namespace": "com.company.events.legal",
  "fields": [
    {"name": "companyId", "type": "string"},
    {"name": "corporateNumber", "type": "string"},
    {"name": "legalName", "type": "string"},
    {"name": "legalNameKana", "type": "string"},
    {"name": "entityType", "type": {"type": "enum", "symbols": ["KK", "GK", "GMK", "GSK"]}},
    {"name": "establishmentDate", "type": "long"},
    {"name": "occurredAt", "type": "long"}
  ]
}
```

## Event Store Schema (PostgreSQL)

```sql
CREATE TABLE domain_events (
  event_id UUID PRIMARY KEY,
  event_type VARCHAR(255) NOT NULL,
  aggregate_id VARCHAR(255) NOT NULL,
  aggregate_type VARCHAR(255) NOT NULL,
  version INTEGER NOT NULL,
  occurred_at TIMESTAMPTZ NOT NULL,
  recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  recorded_by VARCHAR(255),
  correlation_id UUID,
  causation_id UUID,
  metadata JSONB,
  payload JSONB NOT NULL,

  -- Constraints
  UNIQUE(aggregate_id, version)
);

-- Indexes for querying
CREATE INDEX idx_events_aggregate ON domain_events(aggregate_id);
CREATE INDEX idx_events_type ON domain_events(event_type);
CREATE INDEX idx_events_occurred_at ON domain_events(occurred_at);
CREATE INDEX idx_events_correlation ON domain_events(correlation_id);

-- Outbox table for reliable publishing
CREATE TABLE event_outbox (
  event_id UUID PRIMARY KEY REFERENCES domain_events(event_id),
  status VARCHAR(50) NOT NULL CHECK (status IN ('PENDING', 'PUBLISHED', 'FAILED')),
  published_at TIMESTAMPTZ,
  retry_count INTEGER NOT NULL DEFAULT 0,
  last_error TEXT,

  -- Process only pending events
  CHECK (status = 'PENDING' OR published_at IS NOT NULL)
);

CREATE INDEX idx_outbox_pending ON event_outbox(status) WHERE status = 'PENDING';
```

## Testing Events

```haskell
-- Test event generation
spec :: Spec
spec = describe "Order Events" $ do
  it "generates OrderConfirmed event" $ do
    let order = draftOrder
    let (confirmed, events) = confirmOrder order
    events `shouldContain` [OrderConfirmed {...}]

-- Test event handling
spec :: Spec
spec = describe "Event Handlers" $ do
  it "creates invoice on OrderConfirmed" $ do
    let event = OrderConfirmed {...}
    handleOrderConfirmed event
    invoice <- findInvoiceByOrder (orderId event)
    invoice `shouldSatisfy` isJust

-- Test event sourcing
spec :: Spec
spec = describe "Event Sourcing" $ do
  it "rebuilds aggregate from events" $ do
    let events = [OrderCreated {...}, OrderConfirmed {...}, OrderShipped {...}]
    let order = foldl apply emptyOrder events
    orderStatus order `shouldBe` Shipped
```

## Best Practices

1. **Name events in past tense**: CompanyIncorporated, not IncorporateCompany
2. **Include all necessary data**: Avoid requiring queries in handlers
3. **Version your schemas**: Plan for schema evolution
4. **Make handlers idempotent**: Safe to execute multiple times
5. **Use outbox pattern**: Ensure exactly-once publishing
6. **Include correlation IDs**: Track request flows
7. **Store events immutably**: Never delete or modify events
8. **Publish events asynchronously**: Don't block aggregate operations
9. **Handle failures gracefully**: Use dead letter queues
10. **Monitor event processing**: Track lag and failures
