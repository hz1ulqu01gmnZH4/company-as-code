# Repository and Factory Patterns

## Repository Pattern

### Purpose

Repositories abstract persistence mechanisms and provide collection-like interfaces for accessing aggregates.

### Design Principles

1. **One Repository Per Aggregate Root**: Not per entity
2. **Collection-Oriented Interface**: Mimic collections (add, remove, find)
3. **Persistence Ignorance**: Domain layer doesn't know about database
4. **Query by Identity**: Primary access pattern
5. **Encapsulate Queries**: Hide query complexity

## Repository Interfaces

### Generic Repository Interface

```haskell
-- Generic repository interface
class Repository m a where
  type Id a :: *
  save :: a -> m ()
  findById :: Id a -> m (Maybe a)
  exists :: Id a -> m Bool
  delete :: Id a -> m ()

-- Specific repository interfaces extend this
class Repository m Company => CompanyRepository m where
  findByCorporateNumber :: CorporateNumber -> m (Maybe Company)
  findByLegalName :: CompanyLegalName -> m [Company]
  findActiveCompanies :: m [Company]
```

### Company Repository

```haskell
class CompanyRepository m where
  -- Core operations
  save :: Company -> m ()
  findById :: CompanyId -> m (Maybe Company)
  findByCorporateNumber :: CorporateNumber -> m (Maybe Company)
  exists :: CompanyId -> m Bool

  -- Query operations
  findByLegalName :: CompanyLegalName -> m [Company]
  findByEntityType :: EntityType -> m [Company]
  findActiveCompanies :: m [Company]
  findByFiscalYearEnd :: FiscalYearEnd -> m [Company]

  -- Special queries
  findCompaniesByDirector :: DirectorId -> m [Company]
  findCompaniesByAddress :: Prefecture -> m [Company]
```

### Employee Repository

```haskell
class EmployeeRepository m where
  -- Core operations
  save :: Employee -> m ()
  findById :: EmployeeId -> m (Maybe Employee)
  findByEmployeeNumber :: EmployeeNumber -> m (Maybe Employee)

  -- Query operations
  findByCompany :: CompanyId -> m [Employee]
  findByDepartment :: DepartmentId -> m [Employee]
  findByManager :: EmployeeId -> m [Employee]
  findActiveEmployees :: CompanyId -> m [Employee]
  findByEmploymentType :: EmploymentType -> m [Employee]

  -- Search operations
  searchByName :: PersonName -> m [Employee]
  findByHireDate :: DateRange -> m [Employee]
```

### General Ledger Repository

```haskell
class GeneralLedgerRepository m where
  save :: GeneralLedger -> m ()
  findByCompany :: CompanyId -> FiscalYear -> m (Maybe GeneralLedger)

  -- Journal entry queries
  findEntriesByPeriod :: PeriodId -> m [JournalEntry]
  findEntriesByAccount :: AccountCode -> DateRange -> m [JournalEntry]
  findEntriesByType :: EntryType -> DateRange -> m [JournalEntry]

  -- Aggregations
  calculateAccountBalance :: AccountCode -> Date -> m Money
  generateTrialBalance :: PeriodId -> m TrialBalance
  findUnbalancedEntries :: PeriodId -> m [JournalEntry]
```

### Invoice Repository

```haskell
class InvoiceRepository m where
  save :: Invoice -> m ()
  findById :: InvoiceId -> m (Maybe Invoice)
  findByNumber :: InvoiceNumber -> m (Maybe Invoice)

  -- Query operations
  findByCustomer :: CustomerId -> m [Invoice]
  findByCustomerAndDateRange :: CustomerId -> DateRange -> m [Invoice]
  findByStatus :: InvoiceStatus -> m [Invoice]
  findOverdueInvoices :: Date -> m [Invoice]
  findUnpaidInvoices :: CustomerId -> m [Invoice]

  -- Aggregations
  calculateOutstandingBalance :: CustomerId -> m Money
  calculateAging :: CustomerId -> m AgingReport
```

## Repository Implementations

### In-Memory Repository (Testing)

```haskell
-- In-memory implementation for testing
data InMemoryCompanyRepo = InMemoryCompanyRepo
  { companies :: TVar (Map CompanyId Company)
  }

newInMemoryCompanyRepo :: IO InMemoryCompanyRepo
newInMemoryCompanyRepo = do
  companies <- newTVarIO Map.empty
  pure $ InMemoryCompanyRepo companies

instance CompanyRepository IO InMemoryCompanyRepo where
  save repo company = atomically $ do
    companies <- readTVar (companies repo)
    writeTVar (companies repo) $
      Map.insert (companyId company) company companies

  findById repo id = atomically $ do
    companies <- readTVar (companies repo)
    pure $ Map.lookup id companies

  findByCorporateNumber repo corpNum = atomically $ do
    companies <- readTVar (companies repo)
    pure $ find (\c -> corporateNumber c == corpNum) (Map.elems companies)

  findActiveCompanies repo = atomically $ do
    companies <- readTVar (companies repo)
    pure $ filter (\c -> status c == Active) (Map.elems companies)
```

### PostgreSQL Repository

```haskell
-- PostgreSQL implementation
instance CompanyRepository (ReaderT Connection IO) where
  save company = do
    conn <- ask
    liftIO $ execute conn
      "INSERT INTO companies (id, corporate_number, legal_name, entity_type, ...) \
      \VALUES (?, ?, ?, ?, ...) \
      \ON CONFLICT (id) DO UPDATE SET \
      \  corporate_number = EXCLUDED.corporate_number, \
      \  legal_name = EXCLUDED.legal_name, \
      \  ..."
      ( companyId company
      , corporateNumber company
      , legalName company
      , entityType company
      , ...
      )

  findById companyId = do
    conn <- ask
    result <- liftIO $ query conn
      "SELECT id, corporate_number, legal_name, entity_type, ... \
      \FROM companies WHERE id = ?"
      (Only companyId)
    case result of
      [row] -> pure $ Just (fromRow row)
      _ -> pure Nothing

  findByCorporateNumber corpNum = do
    conn <- ask
    result <- liftIO $ query conn
      "SELECT id, corporate_number, legal_name, entity_type, ... \
      \FROM companies WHERE corporate_number = ?"
      (Only corpNum)
    case result of
      [row] -> pure $ Just (fromRow row)
      _ -> pure Nothing
```

### Event-Sourced Repository

```haskell
-- Event-sourced repository
class EventStore m where
  appendEvents :: StreamId -> [DomainEvent] -> m ()
  loadEvents :: StreamId -> m [DomainEvent]
  loadEventsSince :: StreamId -> Version -> m [DomainEvent]

-- Repository using event store
data EventSourcedCompanyRepo = EventSourcedCompanyRepo
  { eventStore :: EventStore
  , snapshotStore :: SnapshotStore
  }

instance CompanyRepository IO EventSourcedCompanyRepo where
  save repo company = do
    let streamId = companyStreamId (companyId company)
    let events = getPendingEvents company
    appendEvents (eventStore repo) streamId events
    -- Optionally save snapshot every N events
    when (shouldSnapshot company) $
      saveSnapshot (snapshotStore repo) streamId company

  findById repo id = do
    let streamId = companyStreamId id
    -- Try to load from snapshot
    snapshot <- loadSnapshot (snapshotStore repo) streamId
    case snapshot of
      Just (version, company) -> do
        -- Load events since snapshot
        events <- loadEventsSince (eventStore repo) streamId version
        pure $ Just (applyEvents company events)
      Nothing -> do
        -- Rebuild from all events
        events <- loadEvents (eventStore repo) streamId
        pure $ if null events
                then Nothing
                else Just (applyEvents emptyCompany events)

-- Apply events to rebuild state
applyEvents :: Company -> [DomainEvent] -> Company
applyEvents = foldl applyEvent

applyEvent :: Company -> DomainEvent -> Company
applyEvent company (CompanyIncorporated e) =
  company
    { companyId = companyId e
    , corporateNumber = corporateNumber e
    , legalName = legalName e
    , ...
    }
applyEvent company (RepresentativeDirectorChanged e) =
  company { representativeDirector = newDirectorId e }
applyEvent company _ = company
```

## Specification Pattern

### Purpose

Encapsulate query criteria in reusable, composable specifications.

### Implementation

```haskell
-- Specification interface
data Specification a = Specification
  { isSatisfiedBy :: a -> Bool
  , toSqlWhere :: Text
  , toSqlParams :: [SqlValue]
  }

-- Combinators
(.&&.) :: Specification a -> Specification a -> Specification a
spec1 .&&. spec2 = Specification
  { isSatisfiedBy = \x -> isSatisfiedBy spec1 x && isSatisfiedBy spec2 x
  , toSqlWhere = "(" <> toSqlWhere spec1 <> ") AND (" <> toSqlWhere spec2 <> ")"
  , toSqlParams = toSqlParams spec1 ++ toSqlParams spec2
  }

(.||.) :: Specification a -> Specification a -> Specification a
spec1 .||. spec2 = Specification
  { isSatisfiedBy = \x -> isSatisfiedBy spec1 x || isSatisfiedBy spec2 x
  , toSqlWhere = "(" <> toSqlWhere spec1 <> ") OR (" <> toSqlWhere spec2 <> ")"
  , toSqlParams = toSqlParams spec1 ++ toSqlParams spec2
  }

notSpec :: Specification a -> Specification a
notSpec spec = Specification
  { isSatisfiedBy = not . isSatisfiedBy spec
  , toSqlWhere = "NOT (" <> toSqlWhere spec <> ")"
  , toSqlParams = toSqlParams spec
  }

-- Example specifications for Employee
activeEmployeeSpec :: Specification Employee
activeEmployeeSpec = Specification
  { isSatisfiedBy = \e -> case employmentStatus e of
      Active _ -> True
      _ -> False
  , toSqlWhere = "status = ?"
  , toSqlParams = [toSql ("Active" :: Text)]
  }

regularEmployeeSpec :: Specification Employee
regularEmployeeSpec = Specification
  { isSatisfiedBy = \e -> case employmentStatus e of
      Active (ActiveEmployment RegularEmployee _ _ _ _) -> True
      _ -> False
  , toSqlWhere = "employment_type = ?"
  , toSqlParams = [toSql ("Regular" :: Text)]
  }

hiredAfterSpec :: Date -> Specification Employee
hiredAfterSpec date = Specification
  { isSatisfiedBy = \e -> case employmentStatus e of
      Active (ActiveEmployment _ hireDate _ _ _) -> hireDate > date
      _ -> False
  , toSqlWhere = "hire_date > ?"
  , toSqlParams = [toSql date]
  }

-- Compose specifications
activeRegularEmployees :: Specification Employee
activeRegularEmployees = activeEmployeeSpec .&&. regularEmployeeSpec

recentHires :: Specification Employee
recentHires = activeEmployeeSpec .&&. hiredAfterSpec (Date.addDays (-90) today)

-- Use in repository
findBySpecification :: Specification Employee -> m [Employee]
findBySpecification spec = do
  conn <- ask
  query conn
    ("SELECT * FROM employees WHERE " <> toSqlWhere spec)
    (toSqlParams spec)
```

## Factory Pattern

### Purpose

Encapsulate complex aggregate creation logic and ensure invariants.

### Company Factory

```haskell
class CompanyFactory m where
  createKabushikiKaisha
    :: CompanyLegalName
    -> RegisteredAddress
    -> RegisteredCapital
    -> FiscalYearEnd
    -> DirectorId
    -> m (Either CompanyError Company)

  createGodoKaisha
    :: CompanyLegalName
    -> RegisteredAddress
    -> RegisteredCapital
    -> FiscalYearEnd
    -> DirectorId
    -> m (Either CompanyError Company)

-- Implementation
instance CompanyFactory IO where
  createKabushikiKaisha legalName address capital fyEnd director = do
    -- Generate IDs
    companyId <- generateCompanyId
    corpNum <- generateCorporateNumber

    -- Validate inputs
    validateCompanyName legalName >>= \case
      Left err -> pure $ Left err
      Right _ -> do

        -- Check minimum capital (symbolic for KK after 2006)
        when (amount (unRegisteredCapital capital) < 1) $
          pure $ Left InsufficientCapital

        -- Create company
        let company = Company
              { companyId = companyId
              , corporateNumber = corpNum
              , legalName = legalName
              , legalNameKana = inferKana legalName
              , entityType = KabushikiKaisha
              , establishmentDate = today
              , fiscalYearEnd = fyEnd
              , registeredCapital = capital
              , headquarters = address
              , representativeDirector = director
              , corporateSeals = emptyCorporateSeals
              , status = Active
              }

        pure $ Right company

-- Generate corporate number
generateCorporateNumber :: IO CorporateNumber
generateCorporateNumber = do
  -- In practice, this would come from government API
  -- For now, generate random valid number
  payload <- replicateM 12 (randomRIO (0, 9))
  let checkDigit = calculateCheckDigit payload
  let digits = checkDigit : payload
  pure $ CorporateNumber (Text.pack (concatMap show digits))
```

### Employee Factory

```haskell
class EmployeeFactory m where
  createEmployee
    :: CompanyId
    -> PersonalInfo
    -> EmploymentType
    :: Position
    -> BaseSalary
    -> DepartmentId
    -> m (Either EmployeeError Employee)

instance EmployeeFactory IO where
  createEmployee companyId personalInfo empType position salary dept = do
    -- Generate IDs
    employeeId <- generateEmployeeId
    employeeNumber <- generateEmployeeNumber companyId

    -- Validate salary meets minimum wage
    validateMinimumWage salary >>= \case
      Left err -> pure $ Left err
      Right _ -> do

        -- Create employment contract
        contract <- createEmploymentContract
          empType
          position
          salary
          standardWorkingConditions

        -- Create employee
        let employee = Employee
              { employeeId = employeeId
              , companyId = companyId
              , employeeNumber = employeeNumber
              , personalInfo = personalInfo
              , employmentStatus = Active ActiveEmployment
                  { employmentType = empType
                  , hireDate = today
                  , contract = contract
                  , workLocation = headquarters
                  , department = dept
                  , manager = Nothing
                  }
              , currentPosition = position
              , employmentHistory = []
              , compensation = CompensationPackage salary [] Nothing
              , benefits = emptyBenefitEnrollments
              , taxInfo = emptyTaxInfo
              }

        pure $ Right employee

-- Generate employee number with year prefix
generateEmployeeNumber :: CompanyId -> IO EmployeeNumber
generateEmployeeNumber companyId = do
  year <- getCurrentYear
  sequence <- getNextSequence companyId year
  pure $ mkEmployeeNumber year sequence
```

### Invoice Factory

```haskell
class InvoiceFactory m where
  createInvoiceFromOrder
    :: SalesOrder
    -> m Invoice

  createManualInvoice
    :: CustomerId
    -> [InvoiceLineItem]
    -> Maybe ShippingFee
    -> m Invoice

instance InvoiceFactory IO where
  createInvoiceFromOrder order = do
    invoiceId <- generateInvoiceId
    invoiceNumber <- generateInvoiceNumber (companyId order)

    -- Convert order line items to invoice line items
    let lineItems = map orderLineToInvoiceLine (lineItems order)

    -- Calculate totals
    let subtotal = sum $ map amount lineItems
    let tax = calculateConsumptionTax subtotal
    let total = subtotal + tax + fromMaybe 0 (shippingFee order)

    pure $ Invoice
      { invoiceId = invoiceId
      , invoiceNumber = invoiceNumber
      , invoiceDate = today
      , dueDate = calculateDueDate (paymentTerms order)
      , lineItems = lineItems
      , subtotal = subtotal
      , consumptionTax = tax
      , total = total
      , status = Issued
      , payments = []
      }

  orderLineToInvoiceLine :: OrderLineItem -> InvoiceLineItem
  orderLineToInvoiceLine orderLine = InvoiceLineItem
    { lineItemId = generateId
    , productId = productId orderLine
    , description = description orderLine
    , quantity = quantity orderLine
    , unitPrice = unitPrice orderLine
    , amount = lineTotal orderLine
    , taxCategory = taxCategory orderLine
    , account = determineRevenueAccount (productId orderLine)
    }
```

## Unit of Work Pattern

### Purpose

Maintain list of objects affected by business transaction and coordinate writing changes.

### Implementation

```haskell
data UnitOfWork = UnitOfWork
  { newAggregates :: TVar [SomeAggregate]
  , modifiedAggregates :: TVar [SomeAggregate]
  , deletedAggregates :: TVar [AggregateId]
  , domainEvents :: TVar [DomainEvent]
  }

newUnitOfWork :: IO UnitOfWork
newUnitOfWork = UnitOfWork
  <$> newTVarIO []
  <*> newTVarIO []
  <*> newTVarIO []
  <*> newTVarIO []

-- Register changes
registerNew :: UnitOfWork -> SomeAggregate -> IO ()
registerNew uow agg = atomically $
  modifyTVar (newAggregates uow) (agg :)

registerModified :: UnitOfWork -> SomeAggregate -> IO ()
registerModified uow agg = atomically $
  modifyTVar (modifiedAggregates uow) (agg :)

registerDeleted :: UnitOfWork -> AggregateId -> IO ()
registerDeleted uow aggId = atomically $
  modifyTVar (deletedAggregates uow) (aggId :)

registerEvent :: UnitOfWork -> DomainEvent -> IO ()
registerEvent uow event = atomically $
  modifyTVar (domainEvents uow) (event :)

-- Commit all changes in single transaction
commit :: UnitOfWork -> IO ()
commit uow = withTransaction $ do
  -- Insert new aggregates
  new <- readTVarIO (newAggregates uow)
  forM_ new saveAggregate

  -- Update modified aggregates
  modified <- readTVarIO (modifiedAggregates uow)
  forM_ modified saveAggregate

  -- Delete removed aggregates
  deleted <- readTVarIO (deletedAggregates uow)
  forM_ deleted deleteAggregate

  -- Publish domain events
  events <- readTVarIO (domainEvents uow)
  forM_ events publishEvent

-- Rollback
rollback :: UnitOfWork -> IO ()
rollback uow = atomically $ do
  writeTVar (newAggregates uow) []
  writeTVar (modifiedAggregates uow) []
  writeTVar (deletedAggregates uow) []
  writeTVar (domainEvents uow) []
```

## Database Schema Design

### Aggregate Table Design

```sql
-- Company aggregate (root)
CREATE TABLE companies (
  id UUID PRIMARY KEY,
  corporate_number VARCHAR(13) UNIQUE NOT NULL,
  legal_name VARCHAR(255) NOT NULL,
  legal_name_kana VARCHAR(255) NOT NULL,
  entity_type VARCHAR(50) NOT NULL,
  establishment_date DATE NOT NULL,
  fiscal_year_end_month INTEGER NOT NULL,
  fiscal_year_end_day INTEGER NOT NULL,
  registered_capital_amount DECIMAL(19, 2) NOT NULL,
  registered_capital_currency VARCHAR(3) NOT NULL,
  registered_address JSONB NOT NULL,
  representative_director_id UUID NOT NULL,
  corporate_seals JSONB,
  status VARCHAR(50) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  version INTEGER NOT NULL DEFAULT 1
);

-- Employee aggregate (root)
CREATE TABLE employees (
  id UUID PRIMARY KEY,
  company_id UUID NOT NULL REFERENCES companies(id),
  employee_number VARCHAR(50) NOT NULL,
  personal_info JSONB NOT NULL,
  employment_status JSONB NOT NULL,
  current_position JSONB NOT NULL,
  compensation JSONB NOT NULL,
  benefits JSONB NOT NULL,
  tax_info JSONB NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  version INTEGER NOT NULL DEFAULT 1,

  UNIQUE(company_id, employee_number)
);

-- Invoice aggregate (root)
CREATE TABLE invoices (
  id UUID PRIMARY KEY,
  invoice_number VARCHAR(50) NOT NULL,
  customer_id UUID NOT NULL,
  invoice_date DATE NOT NULL,
  due_date DATE NOT NULL,
  line_items JSONB NOT NULL,
  subtotal_amount DECIMAL(19, 2) NOT NULL,
  consumption_tax_amount DECIMAL(19, 2) NOT NULL,
  total_amount DECIMAL(19, 2) NOT NULL,
  status VARCHAR(50) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  version INTEGER NOT NULL DEFAULT 1,

  UNIQUE(invoice_number)
);

-- Indexes for common queries
CREATE INDEX idx_companies_corporate_number ON companies(corporate_number);
CREATE INDEX idx_companies_entity_type ON companies(entity_type);
CREATE INDEX idx_employees_company ON employees(company_id);
CREATE INDEX idx_employees_number ON employees(employee_number);
CREATE INDEX idx_invoices_customer ON invoices(customer_id);
CREATE INDEX idx_invoices_status ON invoices(status);
CREATE INDEX idx_invoices_due_date ON invoices(due_date);
```

### Optimistic Locking

```haskell
-- Update with version check
updateWithOptimisticLock :: Company -> ReaderT Connection IO (Either ConcurrencyError ())
updateWithOptimisticLock company = do
  conn <- ask
  result <- liftIO $ execute conn
    "UPDATE companies SET \
    \  legal_name = ?, \
    \  ... \
    \  version = version + 1, \
    \  updated_at = NOW() \
    \WHERE id = ? AND version = ?"
    ( legalName company
    , ...
    , companyId company
    , version company
    )
  case result of
    1 -> pure $ Right ()
    0 -> pure $ Left ConcurrentModification
    _ -> pure $ Left UnexpectedError
```

## Testing Repositories

```haskell
-- Repository tests
spec :: Spec
spec = describe "CompanyRepository" $ do
  it "saves and retrieves company" $ do
    repo <- newInMemoryCompanyRepo
    let company = testCompany
    save repo company
    found <- findById repo (companyId company)
    found `shouldBe` Just company

  it "finds by corporate number" $ do
    repo <- newInMemoryCompanyRepo
    let company = testCompany
    save repo company
    found <- findByCorporateNumber repo (corporateNumber company)
    found `shouldBe` Just company

  it "returns Nothing for non-existent company" $ do
    repo <- newInMemoryCompanyRepo
    found <- findById repo nonExistentId
    found `shouldBe` Nothing

-- Property tests
prop_save_then_find :: Company -> Property
prop_save_then_find company = monadicIO $ do
  repo <- run newInMemoryCompanyRepo
  run $ save repo company
  found <- run $ findById repo (companyId company)
  assert (found == Just company)
```

## Best Practices

1. **One repository per aggregate root**: Not per entity
2. **Return aggregates, not DTOs**: Keep domain pure
3. **Use specifications for complex queries**: Composable and testable
4. **Implement optimistic locking**: Handle concurrency
5. **Abstract persistence details**: Domain doesn't know about DB
6. **Provide query methods for common use cases**: Avoid exposing query language
7. **Use factories for complex creation**: Ensure invariants
8. **Batch operations when possible**: Performance optimization
9. **Cache frequently accessed aggregates**: Read performance
10. **Use projections for read-heavy queries**: CQRS pattern
