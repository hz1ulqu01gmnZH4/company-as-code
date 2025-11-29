# Context Mapping and Integration Patterns

## Overview

Context mapping defines how bounded contexts relate to each other and specifies integration patterns between them.

## Context Relationships

### 1. Shared Kernel

**Pattern**: Two contexts share a common subset of the domain model.

**Application**: All contexts share common value objects and primitives.

```haskell
-- Shared kernel (SharedKernel module)
module SharedKernel where

-- Common value objects
data Money = Money Scientific Currency
data PersonName = PersonName FamilyName GivenName
data Address = Address PostalCode Prefecture City StreetAddress
data Email = Email Text
data PhoneNumber = ...

-- Common types
data CompanyId = CompanyId UUID
data EmployeeId = EmployeeId UUID
data CustomerId = CustomerId UUID

-- Common enumerations
data Prefecture = Tokyo | Osaka | ...
data Currency = JPY | USD | EUR
```

**Usage**:
```haskell
-- Legal context imports shared kernel
import SharedKernel (Money, CompanyId, Address, Prefecture)

-- Financial context imports shared kernel
import SharedKernel (Money, Currency)

-- HR context imports shared kernel
import SharedKernel (EmployeeId, PersonName, Address)
```

**Benefits**:
- Consistent representation across contexts
- Reduced duplication
- Type safety across boundaries

**Risks**:
- Creates coupling between contexts
- Changes affect multiple contexts
- Requires coordination

**Mitigation**:
- Keep shared kernel minimal
- Version shared types carefully
- Use semantic versioning
- Make breaking changes rare

### 2. Customer-Supplier

**Pattern**: Upstream context (supplier) provides services to downstream context (customer).

**Application**: Legal Context → HR Context

**Supplier (Legal Context):**
```haskell
-- Legal context provides company information
module Legal.CompanyService where

data CompanyInfo = CompanyInfo
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , fiscalYearEnd :: FiscalYearEnd
  , headquarters :: Address
  }

-- Public API for other contexts
getCompanyInfo :: CompanyId -> IO (Maybe CompanyInfo)
getCompanyInfo companyId = do
  company <- CompanyRepository.findById companyId
  pure $ fmap toCompanyInfo company

toCompanyInfo :: Company -> CompanyInfo
toCompanyInfo company = CompanyInfo
  { companyId = companyId company
  , corporateNumber = corporateNumber company
  , legalName = legalName company
  , fiscalYearEnd = fiscalYearEnd company
  , headquarters = headquarters company
  }
```

**Customer (HR Context):**
```haskell
-- HR context consumes company information
module HR.EmployeeService where

import Legal.CompanyService (CompanyInfo, getCompanyInfo)

hireEmployee :: CompanyId -> PersonalInfo -> IO Employee
hireEmployee companyId personalInfo = do
  -- Get company info from Legal context
  companyInfo <- getCompanyInfo companyId
  case companyInfo of
    Nothing -> throwError CompanyNotFound
    Just info -> do
      -- Use fiscal year end for employee benefits calculation
      let fyEnd = fiscalYearEnd info
      -- Create employee
      createEmployee companyId personalInfo fyEnd
```

**Coordination**:
- Supplier (Legal) defines API
- Customer (HR) requests features
- Supplier prioritizes customer needs
- Version API carefully
- Maintain backwards compatibility

### 3. Conformist

**Pattern**: Downstream context conforms to upstream context's model.

**Application**: Compliance Context → All Contexts (conforms to government regulations)

**Upstream (Government Regulations):**
```haskell
-- Compliance context conforms to external regulatory requirements
module Compliance.RegulatoryRequirements where

-- External regulation structure (can't change)
data NationalTaxAgencyFiling = NationalTaxAgencyFiling
  { filingType :: Text
  , dueDate :: Date
  , requiredForms :: [Text]
  , electronicFilingMandatory :: Bool
  }

-- Conform to their structure
getNTARequirements :: CompanyId -> IO [NationalTaxAgencyFiling]
getNTARequirements companyId = do
  -- Call external API
  externalAPI.getFilingRequirements companyId
```

**Benefits**:
- Simplicity (no translation layer)
- Direct integration
- Less code to maintain

**Drawbacks**:
- Couples to external model
- No control over changes
- Must adapt to upstream changes

**When to use**:
- External systems (government APIs)
- Third-party services
- Strong power asymmetry
- Cost of translation too high

### 4. Anti-Corruption Layer (ACL)

**Pattern**: Translation layer protects domain model from external systems.

**Application**: Financial Context ↔ External Banking System

**External System Model:**
```haskell
-- External banking API (not under our control)
module External.BankingAPI where

data BankTransaction = BankTransaction
  { txnId :: Text
  , accountNo :: Text
  , txnDate :: Text  -- "YYYY-MM-DD" format
  , amount :: Double  -- Precision issues!
  , currency :: Text  -- "JPY", "USD"
  , description :: Text
  }

fetchTransactions :: Text -> Text -> IO [BankTransaction]
```

**Anti-Corruption Layer:**
```haskell
module Financial.BankingACL where

import qualified External.BankingAPI as External
import Financial.Domain (BankAccount, Transaction, Money, Date)

-- Translate external model to domain model
translateTransaction :: External.BankTransaction -> Either TranslationError Transaction
translateTransaction ext = do
  -- Parse date safely
  date <- parseDate (External.txnDate ext)
  -- Parse currency safely
  currency <- parseCurrency (External.currency ext)
  -- Convert double to precise decimal
  amount <- toPreciseDecimal (External.amount ext) currency
  -- Create domain transaction
  pure $ Transaction
    { transactionId = TransactionId (External.txnId ext)
    , date = date
    , amount = Money amount currency
    , description = External.description ext
    }

-- Public API for domain
fetchBankTransactions :: BankAccount -> DateRange -> IO [Transaction]
fetchBankTransactions account range = do
  -- Call external API
  externalTxns <- External.fetchTransactions
    (accountNumber account)
    (formatDateRange range)
  -- Translate to domain model
  pure $ rights $ map translateTransaction externalTxns

-- Helper: Parse date with error handling
parseDate :: Text -> Either TranslationError Date
parseDate text = case parseTimeM True defaultTimeLocale "%Y-%m-%d" (Text.unpack text) of
  Just date -> Right date
  Nothing -> Left (InvalidDateFormat text)

-- Helper: Convert to precise decimal
toPreciseDecimal :: Double -> Currency -> Either TranslationError Scientific
toPreciseDecimal double currency = case currency of
  JPY -> Right (fromIntegral (round double :: Integer))  -- No decimals for JPY
  USD -> Right (fromFloatDigits double)
  _ -> Right (fromFloatDigits double)
```

**Benefits**:
- Protects domain from external changes
- Clear boundary
- Domain remains pure
- Can swap external systems

**Costs**:
- Additional code
- Translation overhead
- Potential data loss

**When to use**:
- External systems with poor models
- Legacy systems
- Third-party services
- Preventing corruption

### 5. Published Language

**Pattern**: Well-documented shared language for integration.

**Application**: Domain Events (all contexts publish/subscribe)

**Published Event Schema:**
```haskell
-- Published language for domain events
module Events.PublishedLanguage where

-- Well-documented event format
{-|
Event: CompanyIncorporated

Published by: Legal Context
Subscribed by: HR, Financial, Compliance contexts

Triggered when: New company is legally incorporated

Fields:
  - companyId: Unique identifier for the company
  - corporateNumber: 13-digit government-issued number
  - legalName: Official registered name
  - establishmentDate: Date of incorporation
  - fiscalYearEnd: Fiscal year end date (month/day)

Schema version: 2.0
-}
data CompanyIncorporated = CompanyIncorporated
  { companyId :: UUID
  , corporateNumber :: Text
  , legalName :: Text
  , legalNameKana :: Text
  , establishmentDate :: ISO8601Date
  , fiscalYearEnd :: MonthDay
  , occurredAt :: ISO8601Timestamp
  }
  deriving (Generic, ToJSON, FromJSON)

-- JSON Schema for validation
companyIncorporatedSchema :: Schema
companyIncorporatedSchema = object
  [ "companyId" .= string
  , "corporateNumber" .= string  -- pattern: "^\d{13}$"
  , "legalName" .= string
  , "legalNameKana" .= string
  , "establishmentDate" .= string  -- ISO8601 date
  , "fiscalYearEnd" .= object
      [ "month" .= integer
      , "day" .= integer
      ]
  , "occurredAt" .= string  -- ISO8601 timestamp
  ]
```

**Documentation:**
```markdown
# Domain Events Published Language

## CompanyIncorporated Event

**Publisher**: Legal Context
**Subscribers**: HR, Financial, Compliance

**Trigger**: Company is legally incorporated

**Schema**:
```json
{
  "eventType": "CompanyIncorporated",
  "eventId": "uuid",
  "version": "2.0",
  "occurredAt": "2024-03-15T10:30:00Z",
  "payload": {
    "companyId": "uuid",
    "corporateNumber": "1234567890123",
    "legalName": "株式会社サンプル",
    "legalNameKana": "カブシキガイシャサンプル",
    "establishmentDate": "2024-03-15",
    "fiscalYearEnd": {
      "month": 3,
      "day": 31
    }
  }
}
```

**Backward Compatibility**:
- Version 1.0 → 2.0: Added `legalNameKana`, `fiscalYearEnd`
- Consumers must handle both versions
```

### 6. Separate Ways

**Pattern**: No integration between contexts.

**Application**: Operations Context ↮ Compliance Context (minimal overlap)

Operations and Compliance contexts have minimal interaction:
- Operations manages sales/inventory
- Compliance manages regulatory filings
- Integration only through shared events

**Decision**: Keep separate, integrate only via events when necessary.

## Integration Patterns by Context

### Legal Context Integrations

```haskell
-- Legal context relationships
Legal Context (Core Domain)
  ├─ Shared Kernel → All contexts (common value objects)
  ├─ Customer-Supplier → HR Context (provides company info)
  ├─ Customer-Supplier → Financial Context (provides structure)
  ├─ Published Language → All contexts (domain events)
  └─ Conformist → Government APIs (registration)

-- Legal publishes
publishedEvents =
  [ "CompanyIncorporated"
  , "DirectorAppointed"
  , "RepresentativeDirectorChanged"
  , "ArticlesAmended"
  , "CorporateSealRegistered"
  ]

-- Legal subscribes to
subscribedEvents =
  [ "EmployeePromoted" -- If promoted to director
  ]
```

### Financial Context Integrations

```haskell
-- Financial context relationships
Financial Context (Core Domain)
  ├─ Shared Kernel → All contexts (Money, Currency)
  ├─ Customer → Legal Context (company info)
  ├─ Supplier → Operations Context (revenue/expense accounts)
  ├─ Supplier → HR Context (payroll accounts)
  ├─ ACL → External Banking APIs
  ├─ ACL → National Tax Agency API
  └─ Published Language → All contexts (events)

-- Financial publishes
publishedEvents =
  [ "JournalEntryPosted"
  , "FiscalPeriodClosed"
  , "TaxReturnFiled"
  , "PaymentReceived"
  , "InvoiceIssued"
  ]

-- Financial subscribes to
subscribedEvents =
  [ "OrderConfirmed"       -- Create AR
  , "OrderShipped"         -- Recognize revenue
  , "EmployeeHired"        -- Setup payroll
  , "SalaryAdjusted"       -- Update payroll
  , "PurchaseOrderApproved" -- Create AP
  ]
```

### HR Context Integrations

```haskell
-- HR context relationships
HR Context (Supporting)
  ├─ Shared Kernel → All contexts (PersonName, Address)
  ├─ Customer → Legal Context (company/director info)
  ├─ Supplier → Financial Context (salary info)
  ├─ Conformist → Government Labor APIs
  └─ Published Language → All contexts (events)

-- HR publishes
publishedEvents =
  [ "EmployeeHired"
  , "EmploymentTerminated"
  , "SalaryAdjusted"
  , "LeaveGranted"
  , "EmployeePromoted"
  ]

-- HR subscribes to
subscribedEvents =
  [ "DirectorAppointed"    -- Director becomes employee
  , "FiscalPeriodClosed"   -- Trigger year-end adjustment
  ]
```

### Compliance Context Integrations

```haskell
-- Compliance context relationships
Compliance Context (Supporting)
  ├─ Shared Kernel → All contexts (Date, DateRange)
  ├─ Conformist → All government APIs
  ├─ Published Language → All contexts (deadline events)
  └─ Subscriber → All contexts (for audit trail)

-- Compliance publishes
publishedEvents =
  [ "RegulatoryDeadlineApproaching"
  , "FilingCompleted"
  , "ComplianceViolation"
  ]

-- Compliance subscribes to
subscribedEvents =
  -- Subscribes to ALL events for audit trail
  [ "*" ]
```

### Operations Context Integrations

```haskell
-- Operations context relationships
Operations Context (Generic/Supporting)
  ├─ Shared Kernel → All contexts
  ├─ Customer → Legal Context (company info)
  ├─ Customer → Financial Context (accounting)
  └─ Published Language → Financial context

-- Operations publishes
publishedEvents =
  [ "OrderConfirmed"
  , "OrderShipped"
  , "InventoryReceived"
  , "CustomerRegistered"
  ]

-- Operations subscribes to
subscribedEvents =
  [ "InvoiceIssued"        -- Link to order
  , "PaymentReceived"      -- Update AR
  ]
```

## Context Map Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPLIANCE CONTEXT                            │
│                   (Conformist to all)                            │
│              Subscribes to all events for audit                  │
└────────────────────────┬────────────────────────────────────────┘
                         │ Published Language (Events)
                         │
    ┌────────────────────┼────────────────────────────┐
    │                    │                            │
    ▼                    ▼                            ▼
┌───────────────┐  ┌─────────────────┐  ┌────────────────────┐
│ LEGAL CONTEXT │  │  HR CONTEXT     │  │ FINANCIAL CONTEXT  │
│ (Core Domain) │  │  (Supporting)   │  │  (Core Domain)     │
└───────┬───────┘  └────────┬────────┘  └─────────┬──────────┘
        │                   │                      │
        │ Customer-Supplier │                      │
        └──────────────────►│                      │
        │                   │                      │
        │ Customer-Supplier │                      │
        └──────────────────────────────────────────┘
        │                   │                      │
        │                   │ Customer-Supplier    │
        │                   └─────────────────────►│
        │                                          │
        │                                          │ ACL
        │                                          ▼
        │                                  ┌──────────────┐
        │                                  │   Banking    │
        │                                  │     API      │
        │                                  └──────────────┘
        │
        ▼
┌────────────────────┐
│ OPERATIONS CONTEXT │
│ (Generic/Support)  │
└────────────────────┘
        │
        │ Published Language (Events)
        ▼
   [All Contexts]
```

## Event-Driven Integration

### Event Bus Architecture

```haskell
-- Centralized event bus
module EventBus where

-- Event types from all contexts
data EventType
  -- Legal events
  = CompanyIncorporated
  | DirectorAppointed
  | CorporateSealRegistered
  -- Financial events
  | JournalEntryPosted
  | FiscalPeriodClosed
  | InvoiceIssued
  -- HR events
  | EmployeeHired
  | SalaryAdjusted
  | LeaveGranted
  -- Operations events
  | OrderConfirmed
  | OrderShipped
  -- Compliance events
  | RegulatoryDeadlineApproaching
  | FilingCompleted

-- Event routing
routeEvent :: DomainEvent -> IO ()
routeEvent event = case eventType event of
  EmployeeHired -> do
    Financial.handleEmployeeHired event
    Compliance.handleEmployeeHired event
  OrderConfirmed -> do
    Financial.handleOrderConfirmed event
    Operations.handleOrderConfirmed event
  FiscalPeriodClosed -> do
    HR.handleFiscalPeriodClosed event
    Compliance.handleFiscalPeriodClosed event
  _ -> pure ()
```

### Cross-Context Workflows

**Example: Employee Onboarding**

```haskell
-- Workflow: Hire employee
hireEmployeeWorkflow :: PersonalInfo -> Position -> BaseSalary -> IO Employee
hireEmployeeWorkflow personalInfo position salary = do
  -- 1. HR Context: Create employee
  employee <- HR.hireEmployee personalInfo position salary

  -- Event published: EmployeeHired
  publishEvent $ EmployeeHired
    { employeeId = employeeId employee
    , companyId = companyId employee
    , salary = salary
    , ...
    }

  pure employee

-- Financial Context handler
Financial.handleEmployeeHired :: EmployeeHired -> IO ()
handleEmployeeHired event = do
  -- Create payroll account
  createPayrollAccount (employeeId event) (salary event)

  -- Setup withholding tax
  setupWithholdingTax (employeeId event)

  -- Event published: PayrollAccountCreated
  publishEvent $ PayrollAccountCreated {...}

-- Compliance Context handler
Compliance.handleEmployeeHired :: EmployeeHired -> IO ()
handleEmployeeHired event = do
  -- Schedule social insurance enrollment (due in 5 days)
  scheduleFilingDeadline
    SocialInsuranceEnrollment
    (addDays 5 (hireDate event))
    (employeeId event)

  -- Event published: FilingScheduled
  publishEvent $ FilingScheduled {...}
```

**Example: Fiscal Year Closing**

```haskell
-- Workflow: Close fiscal year
closeFiscalYearWorkflow :: FiscalYear -> IO ()
closeFiscalYearWorkflow fiscalYear = do
  -- 1. Financial Context: Close period
  Financial.closeFiscalYear fiscalYear

  -- Event published: FiscalYearClosed
  publishEvent $ FiscalYearClosed {...}

-- HR Context handler
HR.handleFiscalYearClosed :: FiscalYearClosed -> IO ()
handleFiscalYearClosed event = do
  -- Trigger year-end tax adjustment for all employees
  employees <- HR.findAllEmployees (companyId event)
  forM_ employees performYearEndAdjustment

  -- Reset leave balances
  forM_ employees accrueAnnualLeave

-- Compliance Context handler
Compliance.handleFiscalYearClosed :: FiscalYearClosed -> IO ()
handleFiscalYearClosed event = do
  -- Schedule tax return filing (due 2 months after year-end)
  scheduleFilingDeadline
    CorporateTaxReturn
    (addMonths 2 (fiscalYearEnd event))
    (companyId event)

  -- Event published: FilingScheduled
  publishEvent $ FilingScheduled {...}
```

## Anti-Corruption Layer Examples

### Example: National Tax Agency API

```haskell
-- External API (not under our control)
module External.NTA where

data NTATaxReturn = NTATaxReturn
  { corporationNo :: String
  , fiscalPeriod :: String  -- "YYYYMM-YYYYMM"
  , taxAmount :: Int        -- Amount in yen
  , filingType :: Int       -- 1=blue, 2=white
  }

-- Anti-Corruption Layer
module Financial.TaxACL where

import qualified External.NTA as NTA
import Financial.Domain

-- Translate domain to external
toNTATaxReturn :: CorporateTaxReturn -> NTA.NTATaxReturn
toNTATaxReturn tax = NTA.NTATaxReturn
  { NTA.corporationNo = unCorporateNumber (corporateNumber tax)
  , NTA.fiscalPeriod = formatFiscalPeriod (fiscalYear tax)
  , NTA.taxAmount = round (amountInJPY (taxAmount tax))
  , NTA.filingType = if blueFormFiling tax then 1 else 2
  }

-- File tax return through ACL
fileTaxReturn :: CorporateTaxReturn -> IO FilingReceipt
fileTaxReturn tax = do
  let ntaReturn = toNTATaxReturn tax
  result <- NTA.submitTaxReturn ntaReturn
  translateResult result

-- Translate external result to domain
translateResult :: NTA.SubmissionResult -> IO FilingReceipt
translateResult result = case NTA.status result of
  NTA.Accepted -> pure $ FilingReceipt
    { confirmationNumber = NTA.receiptNo result
    , filedAt = parseNTATimestamp (NTA.timestamp result)
    , status = Accepted
    }
  NTA.Rejected -> pure $ FilingReceipt
    { confirmationNumber = Nothing
    , filedAt = now
    , status = Rejected (NTA.errorMessage result)
    }
```

## Best Practices

1. **Keep Shared Kernel Small**: Only truly shared concepts
2. **Use Events for Integration**: Loose coupling via published language
3. **ACL for External Systems**: Protect domain from bad models
4. **Document Integration Points**: Clear contracts between contexts
5. **Version Event Schemas**: Support backward compatibility
6. **Idempotent Event Handlers**: Safe to process multiple times
7. **Monitor Integration Health**: Track event lag, failures
8. **Test Integration**: Integration tests for cross-context flows
9. **Explicit Context Boundaries**: Clear module separation
10. **Minimize Synchronous Calls**: Prefer eventual consistency

## Migration Strategy

### Phase 1: Identify Boundaries
- Map current monolith to bounded contexts
- Identify shared concepts
- Document current integrations

### Phase 2: Extract Shared Kernel
- Move common value objects to shared module
- Version shared types
- Update references

### Phase 3: Implement Event Bus
- Setup event infrastructure
- Define published language
- Implement event publishing

### Phase 4: Migrate Context by Context
- Start with leaf contexts (fewest dependencies)
- Implement ACLs for external integrations
- Migrate Customer-Supplier relationships
- Test thoroughly

### Phase 5: Optimize
- Monitor performance
- Optimize event processing
- Consider CQRS for read-heavy contexts
- Refine boundaries based on learnings
