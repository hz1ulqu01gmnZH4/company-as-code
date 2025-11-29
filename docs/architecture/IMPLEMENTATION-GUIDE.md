# Implementation Guide

## Technology Stack Recommendations

### Functional Object-Oriented Languages

#### Option 1: Scala with Cats/ZIO

**Strengths**:
- Strong type system with algebraic data types
- Excellent functional programming support
- JVM ecosystem access
- Good tooling (IntelliJ, sbt)
- Production-ready effect systems (ZIO, Cats Effect)

**Example**:
```scala
// Domain model with ADTs
sealed trait EntityType
case object KabushikiKaisha extends EntityType
case object GodoKaisha extends EntityType
case object GomeiKaisha extends EntityType
case object GoshiKaisha extends EntityType

// Value object with validation
case class CorporateNumber private (value: String)

object CorporateNumber {
  def make(value: String): Either[ValidationError, CorporateNumber] =
    if (value.length == 13 && value.forall(_.isDigit) && validCheckDigit(value))
      Right(new CorporateNumber(value))
    else
      Left(InvalidCorporateNumber(value))
}

// Aggregate with invariants
case class Company private (
  companyId: CompanyId,
  corporateNumber: CorporateNumber,
  legalName: CompanyLegalName,
  entityType: EntityType,
  representativeDirector: DirectorId,
  status: CompanyStatus
)

object Company {
  def incorporate(
    legalName: CompanyLegalName,
    entityType: EntityType,
    director: DirectorId
  ): Either[CompanyError, Company] = {
    for {
      _ <- validateCompanyName(legalName)
      companyId <- generateCompanyId
      corpNum <- generateCorporateNumber
    } yield Company(
      companyId,
      corpNum,
      legalName,
      entityType,
      director,
      Active
    )
  }
}

// Effects with ZIO
trait CompanyRepository {
  def save(company: Company): Task[Unit]
  def findById(id: CompanyId): Task[Option[Company]]
}

// Service with dependency injection
class CompanyService(repository: CompanyRepository) {
  def incorporateCompany(
    legalName: CompanyLegalName,
    entityType: EntityType,
    director: DirectorId
  ): ZIO[Any, CompanyError, Company] = {
    for {
      company <- ZIO.fromEither(Company.incorporate(legalName, entityType, director))
      _ <- repository.save(company)
      _ <- publishEvent(CompanyIncorporated(company))
    } yield company
  }
}
```

#### Option 2: F# (.NET)

**Strengths**:
- Functional-first language
- Excellent type inference
- Strong pattern matching
- .NET ecosystem access
- Good tooling (Visual Studio, Rider)

**Example**:
```fsharp
// Domain model with discriminated unions
type EntityType =
    | KabushikiKaisha
    | GodoKaisha
    | GomeiKaisha
    | GoshiKaisha

// Value object with validation
type CorporateNumber = private CorporateNumber of string

module CorporateNumber =
    let create (value: string) : Result<CorporateNumber, ValidationError> =
        if value.Length = 13 && value |> Seq.forall Char.IsDigit && validCheckDigit value then
            Ok (CorporateNumber value)
        else
            Error (InvalidCorporateNumber value)

// Aggregate
type Company = private {
    CompanyId: CompanyId
    CorporateNumber: CorporateNumber
    LegalName: CompanyLegalName
    EntityType: EntityType
    RepresentativeDirector: DirectorId
    Status: CompanyStatus
}

module Company =
    let incorporate (legalName: CompanyLegalName) (entityType: EntityType) (director: DirectorId) =
        result {
            let! _ = validateCompanyName legalName
            let! companyId = generateCompanyId()
            let! corpNum = generateCorporateNumber()
            return {
                CompanyId = companyId
                CorporateNumber = corpNum
                LegalName = legalName
                EntityType = entityType
                RepresentativeDirector = director
                Status = Active
            }
        }

// Repository interface
type ICompanyRepository =
    abstract member Save: Company -> Async<unit>
    abstract member FindById: CompanyId -> Async<Company option>

// Service
type CompanyService(repository: ICompanyRepository, eventBus: IEventBus) =
    member this.IncorporateCompany(legalName, entityType, director) =
        async {
            match Company.incorporate legalName entityType director with
            | Ok company ->
                do! repository.Save company
                do! eventBus.Publish (CompanyIncorporated company)
                return Ok company
            | Error err ->
                return Error err
        }
```

#### Option 3: Haskell

**Strengths**:
- Pure functional programming
- Most advanced type system
- Excellent abstractions (Monads, Applicatives, etc.)
- Strong correctness guarantees
- Pattern matching

**Example**:
```haskell
-- Domain model with ADTs
data EntityType
  = KabushikiKaisha
  | GodoKaisha
  | GomeiKaisha
  | GoshiKaisha
  deriving (Eq, Show, Enum)

-- Value object with validation
newtype CorporateNumber = CorporateNumber Text
  deriving (Eq, Ord)

mkCorporateNumber :: Text -> Either ValidationError CorporateNumber
mkCorporateNumber value
  | Text.length value == 13 &&
    Text.all isDigit value &&
    validCheckDigit value =
      Right (CorporateNumber value)
  | otherwise =
      Left (InvalidCorporateNumber value)

-- Aggregate
data Company = Company
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , entityType :: EntityType
  , representativeDirector :: DirectorId
  , status :: CompanyStatus
  }
  deriving (Eq, Show)

-- Smart constructor
incorporateCompany
  :: CompanyLegalName
  -> EntityType
  -> DirectorId
  -> Either CompanyError Company
incorporateCompany legalName entityType director = do
  validateCompanyName legalName
  companyId <- generateCompanyId
  corpNum <- generateCorporateNumber
  pure $ Company
    { companyId = companyId
    , corporateNumber = corpNum
    , legalName = legalName
    , entityType = entityType
    , representativeDirector = director
    , status = Active
    }

-- Repository type class
class Monad m => CompanyRepository m where
  save :: Company -> m ()
  findById :: CompanyId -> m (Maybe Company)

-- Service
class CompanyService m where
  incorporateCompanyService
    :: CompanyLegalName
    -> EntityType
    -> DirectorId
    -> m (Either CompanyError Company)

instance (CompanyRepository m, EventBus m) => CompanyService m where
  incorporateCompanyService legalName entityType director =
    case incorporateCompany legalName entityType director of
      Right company -> do
        save company
        publishEvent (CompanyIncorporated company)
        pure (Right company)
      Left err ->
        pure (Left err)
```

## Project Structure

### Recommended Directory Layout

```
company-as-code/
├── src/
│   ├── SharedKernel/           # Shared value objects and primitives
│   │   ├── Money.hs
│   │   ├── PersonName.hs
│   │   ├── Address.hs
│   │   └── Common.hs
│   │
│   ├── Legal/                  # Legal & Corporate Governance Context
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── Company.hs
│   │   │   │   ├── Board.hs
│   │   │   │   └── ShareholderRegister.hs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CorporateNumber.hs
│   │   │   │   ├── CompanyLegalName.hs
│   │   │   │   └── CorporateSeals.hs
│   │   │   ├── Events/
│   │   │   │   └── LegalEvents.hs
│   │   │   └── Services/
│   │   │       └── CompanyIncorporationService.hs
│   │   ├── Application/
│   │   │   └── CompanyService.hs
│   │   └── Infrastructure/
│   │       ├── Repositories/
│   │       │   └── CompanyRepository.hs
│   │       └── Persistence/
│   │           └── PostgreSQL.hs
│   │
│   ├── Financial/              # Financial & Accounting Context
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── GeneralLedger.hs
│   │   │   │   ├── FiscalYear.hs
│   │   │   │   └── TaxPeriod.hs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── AccountCode.hs
│   │   │   │   ├── JournalEntry.hs
│   │   │   │   └── TaxCategory.hs
│   │   │   ├── Events/
│   │   │   │   └── FinancialEvents.hs
│   │   │   └── Services/
│   │   │       ├── TaxCalculationService.hs
│   │   │       └── FinancialClosingService.hs
│   │   ├── Application/
│   │   │   └── AccountingService.hs
│   │   └── Infrastructure/
│   │       ├── Repositories/
│   │       └── ACL/
│   │           └── BankingACL.hs
│   │
│   ├── HR/                     # HR & Employment Context
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── Employee.hs
│   │   │   │   ├── EmploymentContract.hs
│   │   │   │   └── LeaveEntitlement.hs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── EmployeeNumber.hs
│   │   │   │   ├── EmploymentType.hs
│   │   │   │   └── Salary.hs
│   │   │   ├── Events/
│   │   │   │   └── HREvents.hs
│   │   │   └── Services/
│   │   │       ├── EmployeeOnboardingService.hs
│   │   │       └── LeaveAccrualService.hs
│   │   ├── Application/
│   │   │   └── EmployeeService.hs
│   │   └── Infrastructure/
│   │       └── Repositories/
│   │
│   ├── Operations/             # Operations & Business Context
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── Customer.hs
│   │   │   │   ├── SalesOrder.hs
│   │   │   │   ├── Product.hs
│   │   │   │   └── Inventory.hs
│   │   │   ├── ValueObjects/
│   │   │   ├── Events/
│   │   │   └── Services/
│   │   ├── Application/
│   │   └── Infrastructure/
│   │
│   ├── Compliance/             # Compliance & Regulatory Context
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   ├── RegulatoryRequirement.hs
│   │   │   │   ├── FilingSchedule.hs
│   │   │   │   └── AuditTrail.hs
│   │   │   ├── ValueObjects/
│   │   │   ├── Events/
│   │   │   └── Services/
│   │   ├── Application/
│   │   └── Infrastructure/
│   │       └── ACL/
│   │           └── GovernmentAPI.hs
│   │
│   └── Infrastructure/         # Shared infrastructure
│       ├── EventBus/
│       │   ├── EventBus.hs
│       │   ├── EventStore.hs
│       │   └── Outbox.hs
│       ├── Database/
│       │   ├── Migrations/
│       │   └── Connection.hs
│       └── Config/
│           └── Configuration.hs
│
├── tests/
│   ├── Legal/
│   │   ├── Domain/
│   │   │   └── CompanySpec.hs
│   │   └── Integration/
│   │       └── CompanyRepositorySpec.hs
│   ├── Financial/
│   ├── HR/
│   ├── Operations/
│   ├── Compliance/
│   └── Integration/
│       └── CrossContextSpec.hs
│
├── docs/
│   └── architecture/           # This documentation
│
└── config/
    ├── development.yaml
    ├── production.yaml
    └── test.yaml
```

## Type Design Patterns

### Phantom Types for State Safety

```haskell
-- Compile-time state tracking
{-# LANGUAGE DataKinds #-}
{-# LANGUAGE KindSignatures #-}

data OrderState = Draft | Confirmed | Shipped | Delivered

newtype SalesOrder (s :: OrderState) = SalesOrder
  { unOrder :: OrderData
  }

-- State transitions enforced at compile time
confirmOrder :: SalesOrder 'Draft -> SalesOrder 'Confirmed
shipOrder :: SalesOrder 'Confirmed -> ShipmentInfo -> SalesOrder 'Shipped
deliverOrder :: SalesOrder 'Shipped -> DeliveryConfirmation -> SalesOrder 'Delivered

-- This won't compile:
-- shipOrder draftOrder shipmentInfo  -- Type error!
```

### Smart Constructors

```haskell
-- Private constructor, public smart constructor
module CorporateNumber
  ( CorporateNumber  -- Export type but not constructor
  , mkCorporateNumber
  , unCorporateNumber
  ) where

newtype CorporateNumber = CorporateNumber Text
  deriving (Eq, Ord)

-- Smart constructor with validation
mkCorporateNumber :: Text -> Either ValidationError CorporateNumber
mkCorporateNumber value
  | Text.length value == 13 &&
    Text.all isDigit value &&
    validCheckDigit value =
      Right (CorporateNumber value)
  | otherwise =
      Left (InvalidCorporateNumber value)

-- Unwrap for serialization
unCorporateNumber :: CorporateNumber -> Text
unCorporateNumber (CorporateNumber value) = value
```

### NonEmpty Lists for Invariants

```haskell
import Data.List.NonEmpty (NonEmpty(..))

-- Board must have at least one director
data Board = Board
  { boardId :: BoardId
  , directors :: NonEmpty Director  -- At least one required
  , representativeDirectors :: NonEmpty DirectorId
  }

-- Invoice must have at least one line item
data Invoice = Invoice
  { invoiceId :: InvoiceId
  , lineItems :: NonEmpty InvoiceLineItem
  , total :: Money
  }

-- Construction ensures non-emptiness
createBoard :: Director -> [Director] -> Board
createBoard firstDirector otherDirectors =
  Board
    { boardId = generateId
    , directors = firstDirector :| otherDirectors
    , representativeDirectors = directorId firstDirector :| []
    }
```

### Sum Types for Choices

```haskell
-- Exhaustive pattern matching
data EmploymentType
  = RegularEmployee
  | ContractEmployee
      { contractPeriod :: ContractPeriod
      , renewalCount :: RenewalCount
      }
  | DispatchedWorker
      { dispatchingAgency :: DispatchAgencyId
      , dispatchPeriod :: DispatchPeriod
      }
  | PartTimeWorker
      { hoursPerWeek :: HoursPerWeek
      }
  | TemporaryWorker
      { expectedEndDate :: Maybe EndDate
      }

-- Compiler ensures all cases handled
calculateBenefits :: EmploymentType -> Benefits
calculateBenefits empType = case empType of
  RegularEmployee -> fullBenefits
  ContractEmployee period count -> contractBenefits period
  DispatchedWorker agency period -> limitedBenefits
  PartTimeWorker hours -> partTimeBenefits hours
  TemporaryWorker end -> minimalBenefits
  -- Compiler error if case is missing!
```

## Testing Strategy

### Unit Tests (Aggregate Business Rules)

```haskell
-- Test aggregate invariants
spec :: Spec
spec = describe "Company" $ do
  describe "incorporation" $ do
    it "requires representative director" $ do
      let result = incorporateCompany validName KK Nothing validAddress validCapital
      result `shouldSatisfy` isLeft

    it "generates corporate number" $ do
      let Right company = incorporateCompany validName KK (Just directorId) validAddress validCapital
      corporateNumber company `shouldSatisfy` isJust

    it "sets status to Active" $ do
      let Right company = incorporateCompany validName KK (Just directorId) validAddress validCapital
      status company `shouldBe` Active

  describe "board" $ do
    it "requires at least one director" $ do
      -- NonEmpty type prevents this at compile time
      -- This test is mostly documentation
      True `shouldBe` True

    it "representative director must be board member" $ do
      let board = createBoard director1 [director2]
      let result = designateRepresentative board nonMemberDirectorId
      result `shouldBe` Left DirectorNotFound
```

### Property-Based Tests

```haskell
import Test.QuickCheck

-- Property: Total always equals subtotal + tax
prop_invoice_total :: Invoice -> Bool
prop_invoice_total invoice =
  total invoice == subtotal invoice + consumptionTax invoice

-- Property: Journal entry must balance
prop_journal_entry_balances :: JournalEntry -> Bool
prop_journal_entry_balances entry =
  let debits = sum [amount | JournalLine _ _ (Just amount) _ _ _ _ <- lines entry]
      credits = sum [amount | JournalLine _ _ _ (Just amount) _ _ _ <- lines entry]
  in debits == credits

-- Property: Corporate number checksum valid
prop_corporate_number_checksum :: CorporateNumber -> Bool
prop_corporate_number_checksum corpNum =
  validCheckDigit (unCorporateNumber corpNum)

-- Generate arbitrary instances
instance Arbitrary Invoice where
  arbitrary = do
    lineItems <- listOf1 arbitrary  -- At least one
    let subtotal = sum (map amount lineItems)
    let tax = subtotal * 0.10
    pure $ Invoice
      { invoiceId = generateId
      , lineItems = lineItems
      , subtotal = subtotal
      , consumptionTax = tax
      , total = subtotal + tax
      , status = Issued
      }
```

### Integration Tests (Cross-Aggregate)

```haskell
-- Test event-driven integration
spec :: Spec
spec = describe "Employee Onboarding Integration" $ do
  it "creates payroll account when employee hired" $ do
    -- Setup
    eventBus <- newEventBus
    Financial.subscribe eventBus
    HR.subscribe eventBus

    -- Action: Hire employee
    employee <- HR.hireEmployee personalInfo position salary

    -- Wait for events to process
    threadDelay (milliseconds 100)

    -- Verify: Payroll account created
    payrollAccount <- Financial.findPayrollAccount (employeeId employee)
    payrollAccount `shouldSatisfy` isJust

  it "schedules compliance filing when employee hired" $ do
    eventBus <- newEventBus
    Compliance.subscribe eventBus

    employee <- HR.hireEmployee personalInfo position salary

    threadDelay (milliseconds 100)

    -- Verify: Filing scheduled
    filings <- Compliance.findScheduledFilings (companyId employee)
    filings `shouldContain` [SocialInsuranceEnrollment]
```

### End-to-End Tests

```haskell
-- Test complete business workflows
spec :: Spec
spec = describe "Order to Cash Flow" $ do
  it "completes full order lifecycle" $ do
    -- 1. Customer places order
    order <- Operations.confirmOrder customerId lineItems

    -- 2. Inventory reserved
    reservations <- Operations.findReservations (orderId order)
    reservations `shouldNotBe` []

    -- 3. Invoice generated
    invoice <- Financial.findInvoiceByOrder (orderId order)
    invoice `shouldSatisfy` isJust

    -- 4. Order shipped
    shipment <- Operations.shipOrder order shippingInfo

    -- 5. Revenue recognized
    entries <- Financial.findJournalEntriesByOrder (orderId order)
    let revenueEntries = filter isRevenueEntry entries
    revenueEntries `shouldNotBe` []

    -- 6. Payment received
    payment <- Financial.recordPayment (invoiceId invoice) paymentInfo

    -- 7. Invoice marked paid
    updatedInvoice <- Financial.findInvoice (invoiceId invoice)
    status updatedInvoice `shouldBe` FullyPaid
```

## Deployment Architecture

### Modular Monolith (Recommended Starting Point)

```
┌─────────────────────────────────────────────────┐
│           Application Server (Monolith)         │
│                                                 │
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌──────┐ │
│  │ Legal  │  │Financial│  │   HR   │  │ Ops  │ │
│  │ Context│  │ Context │  │Context │  │Context│ │
│  └───┬────┘  └────┬────┘  └────┬───┘  └───┬──┘ │
│      │            │            │           │    │
│      └────────────┴────────────┴───────────┘    │
│                   │                              │
│            ┌──────▼──────┐                       │
│            │  Event Bus  │                       │
│            └──────┬──────┘                       │
│                   │                              │
│            ┌──────▼──────┐                       │
│            │  Database   │                       │
│            │ (PostgreSQL)│                       │
│            └─────────────┘                       │
└─────────────────────────────────────────────────┘
```

**Benefits**:
- Simpler deployment
- Lower operational complexity
- Easier debugging
- Suitable for small/medium scale

### Microservices (Future Evolution)

```
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│  Legal   │  │Financial │  │    HR    │  │Operations│
│ Service  │  │ Service  │  │ Service  │  │ Service  │
└────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘
     │             │              │             │
     └─────────────┴──────────────┴─────────────┘
                   │
            ┌──────▼──────┐
            │  Event Bus  │
            │   (Kafka)   │
            └─────────────┘
```

**Migration Path**:
1. Start with modular monolith
2. Extract contexts as needed for scale
3. Use shared event bus for communication
4. Independent deployment per context

## Performance Considerations

### Caching Strategy

```haskell
-- Cache frequently accessed aggregates
data CachedRepository = CachedRepository
  { cache :: Cache CompanyId Company
  , underlying :: CompanyRepository
  }

instance CompanyRepository CachedRepository where
  findById repo companyId = do
    cached <- Cache.lookup (cache repo) companyId
    case cached of
      Just company -> pure (Just company)
      Nothing -> do
        company <- findById (underlying repo) companyId
        forM_ company $ \c ->
          Cache.insert (cache repo) companyId c (minutes 5)
        pure company

  save repo company = do
    save (underlying repo) company
    Cache.insert (cache repo) (companyId company) company (minutes 5)
```

### Event Processing Optimization

```haskell
-- Batch event processing
processPendingEvents :: Int -> IO ()
processPendingEvents batchSize = forever $ do
  events <- loadPendingEvents batchSize
  -- Process in parallel
  results <- mapConcurrently processEvent events
  -- Mark as published
  forM_ (zip events results) $ \(event, result) ->
    case result of
      Success -> markPublished event
      Failure err -> markFailed event err
  threadDelay (seconds 1)
```

## Monitoring and Observability

### Metrics to Track

```haskell
-- Domain metrics
data DomainMetrics = DomainMetrics
  { companiesIncorporated :: Counter
  , employeesHired :: Counter
  , ordersProcessed :: Counter
  , invoicesIssued :: Counter
  , fiscalPeriodsClosed :: Counter
  }

-- Technical metrics
data TechnicalMetrics = TechnicalMetrics
  { eventPublishingLag :: Histogram
  , eventProcessingTime :: Histogram
  , databaseQueryTime :: Histogram
  , cacheHitRate :: Gauge
  }

-- Instrument operations
incorporateCompany :: Company -> IO ()
incorporateCompany company = do
  start <- getCurrentTime
  result <- save company
  end <- getCurrentTime
  recordMetric saveTime (diffTime end start)
  incrementCounter companiesIncorporated
  pure result
```

## Best Practices Summary

1. **Type Safety First**: Use types to make illegal states unrepresentable
2. **Small Aggregates**: Design consistency boundaries carefully
3. **Value Objects**: Immutable, validated at construction
4. **Domain Events**: Loose coupling between contexts
5. **Repository per Aggregate**: Not per entity
6. **Smart Constructors**: Enforce invariants
7. **Phantom Types**: Compile-time state tracking
8. **Property Tests**: Verify invariants hold
9. **Integration Tests**: Test cross-context flows
10. **Start Simple**: Modular monolith before microservices

## Migration Checklist

- [ ] Define bounded contexts
- [ ] Identify aggregates and value objects
- [ ] Design domain events schema
- [ ] Implement shared kernel
- [ ] Create repository interfaces
- [ ] Implement factories
- [ ] Setup event bus infrastructure
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Setup database migrations
- [ ] Implement caching layer
- [ ] Setup monitoring
- [ ] Document APIs
- [ ] Deploy modular monolith
- [ ] Plan microservices extraction (if needed)
