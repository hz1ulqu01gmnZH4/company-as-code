# Aggregate Design Patterns

## Overview

This document defines patterns for designing aggregates in the Japanese company model, ensuring consistency boundaries, transaction scopes, and proper encapsulation of business rules.

## Aggregate Design Principles

### 1. Consistency Boundaries

**Rule**: An aggregate is a cluster of entities and value objects that must remain consistent as a unit.

**Application**:
- **Company Aggregate**: Company, CorporateSeals, RegisteredAddress must be consistent
- **Employee Aggregate**: Employee, EmploymentContract, BenefitEnrollments must be consistent
- **Sales Order Aggregate**: Order, LineItems, Inventory Reservations must be consistent

### 2. Small Aggregates

**Rule**: Design small aggregates with a single root entity and minimal child entities.

**Rationale**:
- Reduces contention in concurrent scenarios
- Improves performance (smaller transactions)
- Easier to understand and maintain

**Example**:
```haskell
-- ❌ BAD: Large aggregate with too many responsibilities
data Company = Company
  { ... company fields
  , employees :: [Employee]           -- Don't embed!
  , invoices :: [Invoice]             -- Don't embed!
  , products :: [Product]             -- Don't embed!
  }

-- ✅ GOOD: Small focused aggregate
data Company = Company
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , entityType :: EntityType
  , representativeDirector :: DirectorId  -- Reference by ID
  , corporateSeals :: CorporateSeals      -- Embedded value object
  }
```

### 3. Reference by Identity

**Rule**: Use IDs to reference other aggregates, not object references.

**Benefits**:
- Clear aggregate boundaries
- Enables eventual consistency
- Prevents unintended coupling

**Example**:
```haskell
-- Employee aggregate references Company by ID
data Employee = Employee
  { employeeId :: EmployeeId
  , companyId :: CompanyId           -- ✅ Reference by ID
  , employeeNumber :: EmployeeNumber
  , ...
  }

-- Not embedded reference
-- company :: Company  -- ❌ Don't do this
```

### 4. Eventual Consistency Between Aggregates

**Rule**: Use domain events for cross-aggregate consistency.

**Pattern**:
```haskell
-- 1. Modify first aggregate
confirmOrder :: SalesOrder -> Either OrderError (SalesOrder, [DomainEvent])
confirmOrder order = do
  validated <- validateOrder order
  let confirmed = order { orderStatus = Confirmed }
  let event = OrderConfirmed
        { orderId = orderId order
        , lineItems = lineItems order
        , ...
        }
  pure (confirmed, [event])

-- 2. Event handler updates related aggregate
handleOrderConfirmed :: OrderConfirmed -> IO ()
handleOrderConfirmed event = do
  -- Reserve inventory in separate transaction
  reserveInventory (orderId event) (lineItems event)
  -- Generate invoice in separate transaction
  generateInvoice (orderId event)
```

### 5. Invariants Within Aggregates

**Rule**: All business invariants must be enforced within the aggregate.

**Examples**:

```haskell
-- Company invariant: Must have representative director
data Company = Company
  { representativeDirector :: DirectorId  -- Not Maybe!
  , ...
  }

-- Constructor enforces invariant
createCompany
  :: CompanyLegalName
  -> DirectorId  -- Required
  -> ...
  -> Either CompanyError Company

-- Journal Entry invariant: Debits must equal credits
postJournalEntry :: [JournalLine] -> Either EntryError JournalEntry
postJournalEntry lines = do
  let debits = sum [amount | JournalLine _ _ (Just amount) _ _ _ _ <- lines]
  let credits = sum [amount | JournalLine _ _ _ (Just amount) _ _ _ <- lines]
  unless (debits == credits) $
    Left DebitCreditMismatch
  ...
```

## Aggregate Patterns

### Pattern 1: Root Entity with Value Objects

**Use Case**: Simple aggregate with no child entities.

**Structure**:
```haskell
data Product = Product
  { productId :: ProductId              -- Identity
  , productCode :: ProductCode          -- Value object
  , productName :: ProductName          -- Value object
  , price :: Money                      -- Value object
  , specifications :: Specifications    -- Value object
  , status :: ProductStatus             -- Enum
  }
```

**Characteristics**:
- Single entity (root)
- All other components are value objects
- No collections of entities
- Simple transaction scope

### Pattern 2: Root Entity with Child Entities

**Use Case**: Aggregate with parent-child relationships that must be consistent.

**Structure**:
```haskell
data Board = Board
  { boardId :: BoardId
  , companyId :: CompanyId
  , directors :: NonEmpty Director        -- Child entities
  , representativeDirectors :: NonEmpty DirectorId
  , structure :: BoardStructure
  }

data Director = Director
  { directorId :: DirectorId
  , personId :: PersonId
  , appointmentDate :: AppointmentDate
  , termExpiry :: TermExpiry
  , position :: DirectorPosition
  }
```

**Invariants**:
- Representative directors must be in directors list
- At least one director required (NonEmpty)
- All directors accessed through Board aggregate

**Operations**:
```haskell
-- Add director to board
appointDirector :: Board -> Director -> Either BoardError Board
appointDirector board director = do
  -- Validate director doesn't already exist
  when (director `elem` directors board) $
    Left DirectorAlreadyAppointed
  -- Add to directors list
  let newDirectors = director :| (NE.toList (directors board))
  pure board { directors = newDirectors }

-- Designate representative
designateRepresentative :: Board -> DirectorId -> Either BoardError Board
designateRepresentative board dirId = do
  -- Validate director exists
  unless (dirId `elem` map directorId (NE.toList (directors board))) $
    Left DirectorNotFound
  -- Add to representative directors
  let newReps = dirId :| (NE.toList (representativeDirectors board))
  pure board { representativeDirectors = newReps }
```

### Pattern 3: Root Entity with Collection

**Use Case**: Aggregate with unbounded collection of child entities.

**Structure**:
```haskell
data Invoice = Invoice
  { invoiceId :: InvoiceId
  , invoiceNumber :: InvoiceNumber
  , customerId :: CustomerId
  , lineItems :: NonEmpty InvoiceLineItem  -- Must have at least one
  , subtotal :: Money
  , consumptionTax :: Money
  , total :: Money
  }

data InvoiceLineItem = InvoiceLineItem
  { lineItemId :: LineItemId
  , productId :: ProductId
  , quantity :: Quantity
  , unitPrice :: Money
  , amount :: Money
  , taxCategory :: TaxCategory
  }
```

**Invariants**:
- Total = Subtotal + ConsumptionTax
- Subtotal = Sum of line item amounts
- Line item amount = Quantity × UnitPrice

**Smart Constructors**:
```haskell
-- Add line item and recalculate totals
addLineItem :: Invoice -> InvoiceLineItem -> Invoice
addLineItem invoice item =
  let newItems = item :| (NE.toList (lineItems invoice))
      newSubtotal = calculateSubtotal newItems
      newTax = calculateTax newSubtotal
      newTotal = newSubtotal + newTax
  in invoice
       { lineItems = newItems
       , subtotal = newSubtotal
       , consumptionTax = newTax
       , total = newTotal
       }

calculateSubtotal :: NonEmpty InvoiceLineItem -> Money
calculateSubtotal items = sum (map amount (NE.toList items))

calculateTax :: Money -> Money
calculateTax subtotal = subtotal * 0.10  -- 10% consumption tax
```

### Pattern 4: Aggregate with State Transitions

**Use Case**: Aggregate with complex lifecycle and state-dependent behavior.

**Structure**:
```haskell
data SalesOrder = SalesOrder
  { orderId :: OrderId
  , orderNumber :: OrderNumber
  , customerId :: CustomerId
  , lineItems :: NonEmpty OrderLineItem
  , orderStatus :: OrderStatus    -- State machine
  , paymentStatus :: PaymentStatus
  , fulfillmentStatus :: FulfillmentStatus
  , ...
  }

data OrderStatus
  = Draft
  | Quoted QuotationInfo
  | Confirmed ConfirmationDate
  | InProduction ProductionInfo
  | ReadyToShip
  | Shipped ShipmentInfo
  | Delivered DeliveryConfirmation
  | Completed
  | Cancelled CancellationReason
```

**State Transitions**:
```haskell
-- Type-safe state transitions
confirmOrder :: SalesOrder -> Either OrderError SalesOrder
confirmOrder order = case orderStatus order of
  Draft -> Left CannotConfirmDraft
  Quoted _ -> Right order { orderStatus = Confirmed (Date.today) }
  Confirmed _ -> Left AlreadyConfirmed
  _ -> Left InvalidStateTransition

shipOrder :: SalesOrder -> ShipmentInfo -> Either OrderError SalesOrder
shipOrder order shipmentInfo = case orderStatus order of
  ReadyToShip -> Right order { orderStatus = Shipped shipmentInfo }
  Confirmed _ -> Left NotReadyToShip
  _ -> Left InvalidStateTransition

-- Phantom types for compile-time state safety (advanced)
data OrderState = Draft | Confirmed | Shipped

newtype TypedOrder (s :: OrderState) = TypedOrder SalesOrder

confirmOrder' :: TypedOrder 'Draft -> TypedOrder 'Confirmed
shipOrder' :: TypedOrder 'Confirmed -> ShipmentInfo -> TypedOrder 'Shipped
```

### Pattern 5: Aggregate with Temporal Aspects

**Use Case**: Time-sensitive data with effective dates and history.

**Structure**:
```haskell
data EmploymentContract = EmploymentContract
  { contractId :: ContractId
  , employeeId :: EmployeeId
  , currentTerms :: ContractTerms
  , history :: [HistoricalTerms]
  , amendments :: [ContractAmendment]
  }

data ContractTerms = ContractTerms
  { effectiveDate :: Date
  , expiryDate :: Maybe Date
  , salary :: BaseSalary
  , workingConditions :: WorkingConditions
  , benefits :: BenefitTerms
  }

data HistoricalTerms = HistoricalTerms
  { effectiveFrom :: Date
  , effectiveTo :: Date
  , terms :: ContractTerms
  , supersededBy :: Maybe ContractId
  }
```

**Operations**:
```haskell
-- Amend contract (creates new version)
amendContract
  :: EmploymentContract
  -> ContractTerms
  -> Date  -- Effective date
  -> EmploymentContract
amendContract contract newTerms effectiveDate =
  let amendment = ContractAmendment
        { amendmentDate = Date.today
        , effectiveDate = effectiveDate
        , previousTerms = currentTerms contract
        , newTerms = newTerms
        }
      historicalTerms = HistoricalTerms
        { effectiveFrom = effectiveDate (currentTerms contract)
        , effectiveTo = effectiveDate
        , terms = currentTerms contract
        , supersededBy = Nothing
        }
  in contract
       { currentTerms = newTerms
       , history = historicalTerms : history contract
       , amendments = amendment : amendments contract
       }

-- Get terms effective on specific date
getTermsAt :: EmploymentContract -> Date -> Maybe ContractTerms
getTermsAt contract date
  | date >= effectiveDate (currentTerms contract) = Just (currentTerms contract)
  | otherwise = find (inEffect date) (history contract)
  where
    inEffect d historical =
      d >= effectiveFrom historical && d < effectiveTo historical
```

## Aggregate Identification Strategy

### Step 1: Identify Transactional Boundaries

**Question**: What data must be immediately consistent?

**Examples**:
- **Invoice** + **Line Items**: Must be consistent (total = sum of lines)
- **Employee** + **Contract**: Must be consistent (active employee has valid contract)
- **Company** + **Employees**: NOT immediately consistent (use eventual consistency)

### Step 2: Identify Invariants

**Question**: What business rules must always hold true?

**Examples**:
- Company must have at least one representative director
- Journal entry debits must equal credits
- Invoice total must equal subtotal + tax
- Shareholder percentages must sum to 100%

### Step 3: Identify Lifecycle Boundaries

**Question**: What entities are created, modified, and deleted together?

**Examples**:
- **Sales Order** creates **Line Items** (together)
- **Company** appoints **Directors** (separately)
- **Fiscal Year** creates **Fiscal Periods** (together)

### Step 4: Consider Access Patterns

**Question**: How is this data typically queried and modified?

**Examples**:
- Orders queried by customer → Customer is separate aggregate
- Invoice line items only accessed through invoice → Part of Invoice aggregate
- Directors queried independently → Separate from Company aggregate (or sub-aggregate)

## Common Mistakes to Avoid

### Mistake 1: God Aggregates

**Problem**: Single aggregate containing everything.

```haskell
-- ❌ DON'T DO THIS
data Company = Company
  { companyInfo :: ...
  , employees :: [Employee]
  , invoices :: [Invoice]
  , products :: [Product]
  , orders :: [Order]
  , financialRecords :: [JournalEntry]
  }
```

**Solution**: Break into separate aggregates, reference by ID.

### Mistake 2: Anemic Aggregates

**Problem**: Aggregates with no behavior, just getters/setters.

```haskell
-- ❌ ANEMIC
data Invoice = Invoice
  { getInvoiceId :: InvoiceId
  , setInvoiceId :: InvoiceId -> Invoice
  , getTotal :: Money
  , setTotal :: Money -> Invoice
  }
```

**Solution**: Encapsulate behavior and invariants.

```haskell
-- ✅ RICH DOMAIN MODEL
data Invoice = Invoice
  { invoiceId :: InvoiceId
  , lineItems :: NonEmpty InvoiceLineItem
  , total :: Money  -- Calculated, not set directly
  }

addLineItem :: Invoice -> InvoiceLineItem -> Invoice
calculateTotal :: Invoice -> Money
applyDiscount :: Invoice -> Percentage -> Invoice
```

### Mistake 3: Ignoring Consistency Boundaries

**Problem**: Modifying multiple aggregates in one transaction.

```haskell
-- ❌ BAD: Modifying multiple aggregates
processOrder order = do
  confirmOrder order
  reserveInventory (lineItems order)
  updateCustomerBalance (customerId order)
  createInvoice order
  -- All in one transaction!
```

**Solution**: Use domain events for eventual consistency.

```haskell
-- ✅ GOOD: Single aggregate per transaction
confirmOrder :: SalesOrder -> IO [DomainEvent]
confirmOrder order = do
  let confirmed = order { orderStatus = Confirmed }
  save confirmed
  pure [OrderConfirmed ...]

-- Separate event handler
handleOrderConfirmed :: OrderConfirmed -> IO ()
handleOrderConfirmed event = do
  reserveInventory (orderId event) (lineItems event)

handleInventoryReserved :: InventoryReserved -> IO ()
handleInventoryReserved event = do
  createInvoice (orderId event)
```

### Mistake 4: No Business Logic in Aggregates

**Problem**: Business logic in application services instead of aggregates.

```haskell
-- ❌ BAD: Logic in service
class OrderService m where
  confirmOrder :: OrderId -> m ()
  confirmOrder orderId = do
    order <- findOrder orderId
    customer <- findCustomer (customerId order)
    -- Business logic here
    if creditLimit customer > outstandingBalance customer + total order
      then do
        save (order { orderStatus = Confirmed })
        -- More logic...
```

**Solution**: Put business logic in aggregate.

```haskell
-- ✅ GOOD: Logic in aggregate
data SalesOrder = SalesOrder { ... }

-- Business logic encapsulated
confirm :: SalesOrder -> Customer -> Either OrderError SalesOrder
confirm order customer
  | creditExceeded customer order = Left CreditLimitExceeded
  | not (isActive customer) = Left InactiveCustomer
  | otherwise = Right order { orderStatus = Confirmed }

creditExceeded :: Customer -> SalesOrder -> Bool
creditExceeded customer order =
  creditLimit customer < outstandingBalance customer + total order
```

## Aggregate Testing Strategy

### Unit Tests

Test aggregate invariants and business rules:

```haskell
spec :: Spec
spec = describe "Invoice Aggregate" $ do
  it "calculates total correctly" $ do
    let item1 = InvoiceLineItem ... { amount = 1000 }
    let item2 = InvoiceLineItem ... { amount = 2000 }
    let invoice = createInvoice [item1, item2]
    total invoice `shouldBe` 3300  -- 3000 + 10% tax

  it "enforces at least one line item" $ do
    createInvoice [] `shouldBe` Left NoLineItems

  it "maintains consistency when adding items" $ do
    let invoice = createInvoice [item1]
    let updated = addLineItem invoice item2
    total updated `shouldBe` calculateTotal (lineItems updated)
```

### Property-Based Tests

Test aggregate invariants hold for all inputs:

```haskell
prop_invoice_total_equals_subtotal_plus_tax :: Invoice -> Bool
prop_invoice_total_equals_subtotal_plus_tax invoice =
  total invoice == subtotal invoice + consumptionTax invoice

prop_shareholder_percentages_sum_to_100 :: ShareholderRegister -> Bool
prop_shareholder_percentages_sum_to_100 register =
  let percentages = map shareholderPercentage (shareholders register)
  in sum percentages == 100.0
```

## Performance Considerations

### Lazy Loading Child Entities

For aggregates with large collections:

```haskell
data FiscalYear = FiscalYear
  { fiscalYearId :: FiscalYearId
  , -- ... other fields
  , entriesRef :: Lazy [JournalEntry]  -- Load on demand
  }

-- Load entries only when needed
getEntries :: FiscalYear -> IO [JournalEntry]
getEntries fiscalYear = force (entriesRef fiscalYear)
```

### Snapshots for Event-Sourced Aggregates

Store periodic snapshots to avoid replaying all events:

```haskell
data AggregateState s = AggregateState
  { currentState :: s
  , version :: Version
  , lastSnapshot :: Maybe (Version, s)
  }

-- Load from snapshot + recent events
loadAggregate :: AggregateId -> IO SalesOrder
loadAggregate id = do
  (snapshotVersion, snapshot) <- loadSnapshot id
  events <- loadEventsSince id snapshotVersion
  pure $ foldl apply snapshot events
```

## Summary

**Key Principles**:
1. Design small aggregates focused on consistency boundaries
2. Reference other aggregates by ID
3. Use domain events for cross-aggregate consistency
4. Encapsulate all business logic within aggregates
5. Enforce invariants in aggregate roots
6. Make illegal states unrepresentable with types

**Benefits**:
- Clear transactional boundaries
- Improved concurrency
- Better testability
- Explicit business rules
- Maintainable codebase
