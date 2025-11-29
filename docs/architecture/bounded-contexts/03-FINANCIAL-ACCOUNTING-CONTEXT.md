# Financial & Accounting Context

## Context Overview

**Domain**: Financial transactions, accounting, tax compliance
**Type**: Core Domain
**Strategic Pattern**: Shared Kernel (shares financial primitives), Published Language

## Ubiquitous Language

### Japanese Terms
- **Kessan (決算)**: Financial closing/settlement
- **Jigyou Nendo (事業年度)**: Fiscal year
- **Shouhizei (消費税)**: Consumption tax (VAT)
- **Houjinzei (法人税)**: Corporate tax
- **Shotoku Zei (所得税)**: Income tax
- **Koujo (控除)**: Deduction
- **Gensen Choushu (源泉徴収)**: Withholding tax
- **Kanjou Kamoku (勘定科目)**: Chart of accounts
- **Sho (証)**: Voucher/document
- **Choubo (帳簿)**: Books/ledgers
- **Aoiro Shinkoku (青色申告)**: Blue-form tax return (special treatment)
- **Shiroiro Shinkoku (白色申告)**: White-form tax return (simplified)
- **Furikae Denpy (振替伝票)**: Journal voucher
- **Zandaka Shikenhy (残高試算表)**: Trial balance
- **Taishaku Taishohyou (貸借対照表)**: Balance sheet
- **Soneki Keisansho (損益計算書)**: Profit & loss statement
- **Genkin Shuushi Keisansho (現金収支計算書)**: Cash flow statement

## Aggregate Roots

### 1. General Ledger Aggregate

**Aggregate Root**: `GeneralLedger`

**Invariants**:
- Debit and credit must always balance
- All transactions must reference valid accounts
- Fiscal period must be open for posting
- Trial balance must balance before closing

**Entities**:
```haskell
data GeneralLedger = GeneralLedger
  { ledgerId :: LedgerId
  , companyId :: CompanyId
  , chartOfAccounts :: ChartOfAccounts
  , fiscalYear :: FiscalYear
  , entries :: [JournalEntry]
  , closingStatus :: ClosingStatus
  }

data JournalEntry = JournalEntry
  { entryId :: JournalEntryId
  , entryDate :: Date
  , entryType :: EntryType
  , description :: Description
  , lines :: NonEmpty JournalLine  -- At least 2 lines (debit + credit)
  , sourceDocument :: SourceDocument
  , postedBy :: UserId
  , postedAt :: Timestamp
  , reversedBy :: Maybe JournalEntryId
  }

data JournalLine = JournalLine
  { lineId :: LineId
  , account :: AccountCode
  , debitAmount :: Maybe Money
  , creditAmount :: Maybe Money
  , taxCategory :: Maybe TaxCategory
  , description :: Text
  , costCenter :: Maybe CostCenter
  }
  -- Invariant: Exactly one of debit or credit must be present

data EntryType
  = ManualEntry
  | SystemGenerated SourceSystem
  | TaxAdjustment
  | ClosingEntry
  | OpeningEntry
  | ReversalEntry

data ClosingStatus
  = Open
  | Provisional ProvisionalClosing
  | Closed ClosedPeriod
  | Audited AuditInfo
```

**Commands**:
- `PostJournalEntry`
- `ReverseJournalEntry`
- `AdjustEntry`
- `CloseFiscalPeriod`
- `ReopenPeriod`

**Domain Events**:
- `JournalEntryPosted`
- `JournalEntryReversed`
- `EntryAdjusted`
- `FiscalPeriodClosed`
- `PeriodReopened`

### 2. Chart of Accounts Aggregate

**Aggregate Root**: `ChartOfAccounts`

**Invariants**:
- Account codes must be unique
- Account hierarchy must be consistent
- Account types must match parent account type
- Cannot delete accounts with transactions

**Entities**:
```haskell
data ChartOfAccounts = ChartOfAccounts
  { coaId :: ChartOfAccountsId
  , companyId :: CompanyId
  , version :: Version
  , accounts :: [Account]
  , accountingStandard :: AccountingStandard
  }

data Account = Account
  { accountCode :: AccountCode
  , accountName :: AccountName
  , accountNameKana :: AccountNameKana
  , accountType :: AccountType
  , parentAccount :: Maybe AccountCode
  , normalBalance :: BalanceSide
  , isActive :: Bool
  , taxTreatment :: TaxTreatment
  , requiresCostCenter :: Bool
  , requiresProject :: Bool
  }

data AccountType
  = Asset AssetType
  | Liability LiabilityType
  | Equity EquityType
  | Revenue RevenueType
  | Expense ExpenseType

data AssetType
  = CurrentAsset
      { subType :: CurrentAssetType }
  | FixedAsset
      { subType :: FixedAssetType
      , depreciationMethod :: Maybe DepreciationMethod
      }

data CurrentAssetType
  = Cash
  | BankAccount
  | AccountsReceivable
  | Inventory
  | PrepaidExpenses
  | ShortTermInvestments

data FixedAssetType
  = TangibleAsset TangibleAssetCategory
  | IntangibleAsset IntangibleAssetCategory
  | InvestmentAsset

data BalanceSide = Debit | Credit

data AccountingStandard
  = JapaneseGAAP          -- 日本基準
  | IFRS                  -- 国際財務報告基準
  | USGAAP                -- 米国基準
```

**Commands**:
- `CreateAccount`
- `ModifyAccount`
- `DeactivateAccount`
- `RestructureAccounts`

**Domain Events**:
- `AccountCreated`
- `AccountModified`
- `AccountDeactivated`
- `AccountsRestructured`

### 3. Fiscal Year Aggregate

**Aggregate Root**: `FiscalYear`

**Invariants**:
- Fiscal year periods must not overlap
- Total periods must equal 12 months (or 13 for special cases)
- Opening balance must equal prior year closing
- Cannot have multiple open periods

**Entities**:
```haskell
data FiscalYear = FiscalYear
  { fiscalYearId :: FiscalYearId
  , companyId :: CompanyId
  , yearNumber :: YearNumber
  , startDate :: Date
  , endDate :: Date
  , periods :: NonEmpty FiscalPeriod
  , openingBalances :: [OpeningBalance]
  , closingBalances :: [ClosingBalance]
  , status :: FiscalYearStatus
  }

data FiscalPeriod = FiscalPeriod
  { periodId :: PeriodId
  , periodNumber :: PeriodNumber
  , startDate :: Date
  , endDate :: Date
  , periodType :: PeriodType
  , status :: PeriodStatus
  }

data PeriodType
  = RegularPeriod
  | AdjustmentPeriod  -- 13th period for year-end adjustments

data PeriodStatus
  = FuturePeriod
  | OpenForPosting
  | ProvisionalClosed
  | FinalClosed
  | Locked

data FiscalYearStatus
  = InProgress
  | Closed
  | Audited
  | Locked
```

**Commands**:
- `CreateFiscalYear`
- `OpenPeriod`
- `ClosePeriod`
- `LockFiscalYear`

**Domain Events**:
- `FiscalYearCreated`
- `PeriodOpened`
- `PeriodClosed`
- `FiscalYearLocked`

### 4. Tax Management Aggregate

**Aggregate Root**: `TaxPeriod`

**Invariants**:
- Tax calculations must match transaction totals
- Consumption tax must balance (input vs output)
- Withholding tax must be properly categorized
- Filing deadlines must be tracked

**Entities**:
```haskell
data TaxPeriod = TaxPeriod
  { taxPeriodId :: TaxPeriodId
  , companyId :: CompanyId
  , taxYear :: TaxYear
  , periodStart :: Date
  , periodEnd :: Date
  , consumptionTax :: ConsumptionTaxCalculation
  , corporateTax :: CorporateTaxCalculation
  , withholdingTax :: WithholdingTaxCalculation
  , filingStatus :: FilingStatus
  }

data ConsumptionTaxCalculation = ConsumptionTaxCalculation
  { taxableRevenue :: Money
  , outputTax :: Money
  , inputTax :: Money
  , taxDue :: Money
  , exemptTransactions :: Money
  , zeroRatedTransactions :: Money
  , calculationMethod :: ConsumptionTaxMethod
  }

data ConsumptionTaxMethod
  = StandardMethod        -- 原則課税 (actual input/output)
  | SimplifiedMethod      -- 簡易課税 (deemed input credit)
      { businessCategory :: BusinessCategory
      , deemedCreditRate :: Percentage
      }

data CorporateTaxCalculation = CorporateTaxCalculation
  { accountingIncome :: Money
  , taxAdjustments :: [TaxAdjustment]
  , taxableIncome :: Money
  , taxRate :: TaxRate
  , corporateTaxAmount :: Money
  , localCorporateTax :: Money
  , enterpriseTax :: Money
  , totalTax :: Money
  }

data TaxAdjustment
  = PermanentDifference
      { description :: Text
      , amount :: Money
      , adjustmentType :: AdjustmentType
      }
  | TemporaryDifference
      { description :: Text
      , amount :: Money
      , reversalYear :: Maybe FiscalYear
      }

data WithholdingTaxCalculation = WithholdingTaxCalculation
  { salaryWithholding :: Money
  , bonusWithholding :: Money
  , contractorWithholding :: Money
  , dividendWithholding :: Money
  , totalWithheld :: Money
  }
```

**Commands**:
- `CalculateConsumptionTax`
- `CalculateCorporateTax`
- `CalculateWithholdingTax`
- `FileTaxReturn`
- `PayTax`

**Domain Events**:
- `ConsumptionTaxCalculated`
- `CorporateTaxCalculated`
- `WithholdingTaxCalculated`
- `TaxReturnFiled`
- `TaxPaymentMade`

### 5. Accounts Payable/Receivable Aggregates

**Aggregate Root**: `AccountsReceivable` / `AccountsPayable`

**Invariants**:
- Invoice totals must match line items + tax
- Payments must not exceed outstanding balance
- Aging must be calculated correctly
- Credit terms must be enforced

**Entities (Receivable)**:
```haskell
data AccountsReceivable = AccountsReceivable
  { arId :: ARId
  , companyId :: CompanyId
  , customerId :: CustomerId
  , invoices :: [Invoice]
  , payments :: [PaymentReceived]
  , creditLimit :: Maybe Money
  , paymentTerms :: PaymentTerms
  }

data Invoice = Invoice
  { invoiceId :: InvoiceId
  , invoiceNumber :: InvoiceNumber
  , invoiceDate :: Date
  , dueDate :: Date
  , lineItems :: NonEmpty InvoiceLineItem
  , subtotal :: Money
  , consumptionTax :: Money
  , total :: Money
  , status :: InvoiceStatus
  , payments :: [PaymentApplication]
  }

data InvoiceLineItem = InvoiceLineItem
  { itemId :: ItemId
  , description :: Text
  , quantity :: Quantity
  , unitPrice :: Money
  , amount :: Money
  , taxCategory :: TaxCategory
  , account :: AccountCode
  }

data PaymentReceived = PaymentReceived
  { paymentId :: PaymentId
  , paymentDate :: Date
  , amount :: Money
  , paymentMethod :: PaymentMethod
  , bankAccount :: Maybe BankAccountId
  , applications :: [PaymentApplication]
  }

data PaymentApplication = PaymentApplication
  { invoiceId :: InvoiceId
  , appliedAmount :: Money
  , appliedDate :: Date
  }

data InvoiceStatus
  = Draft
  | Issued
  | PartiallyPaid Money
  | FullyPaid
  | Overdue Days
  | WrittenOff
```

## Value Objects

### Money and Amounts
```haskell
data Money = Money
  { amount :: Scientific    -- High precision
  , currency :: Currency
  }
  deriving (Eq, Ord)

-- Japanese Yen specific operations
data JPY = JPY Scientific
  -- No fractional yen in practice

-- Tax inclusive/exclusive amounts
data TaxAmount = TaxAmount
  { exclusive :: Money
  , tax :: Money
  , inclusive :: Money
  , taxRate :: TaxRate
  }
```

### Tax Categories
```haskell
data TaxCategory
  = StandardRate TaxRate           -- 標準税率 (10%)
  | ReducedRate TaxRate            -- 軽減税率 (8% for food/newspapers)
  | TaxExempt                      -- 非課税
  | TaxFree                        -- 免税 (exports)
  | OutOfScope                     -- 不課税

data TaxRate = TaxRate
  { nationalRate :: Percentage     -- 7.8% (of 10%)
  , localRate :: Percentage        -- 2.2% (of 10%)
  , effectiveDate :: Date
  }

-- Historical rates
historicalRates :: [(DateRange, TaxRate)]
historicalRates =
  [ (before "2014-04-01", TaxRate 4 1)     -- 5% total
  , (between "2014-04-01" "2019-10-01", TaxRate 6.3 1.7)  -- 8% total
  , (after "2019-10-01", TaxRate 7.8 2.2)  -- 10% total
  ]
```

### Fiscal Periods
```haskell
data FiscalYearEnd = FiscalYearEnd
  { month :: Month
  , day :: DayOfMonth
  }

-- Common fiscal year ends in Japan
commonFiscalYearEnds :: [FiscalYearEnd]
commonFiscalYearEnds =
  [ FiscalYearEnd March 31      -- Most common
  , FiscalYearEnd December 31   -- Calendar year
  , FiscalYearEnd September 30  -- Also common
  ]

data DateRange = DateRange
  { startDate :: Date
  , endDate :: Date
  }
```

### Account Codes
```haskell
newtype AccountCode = AccountCode Text
  -- Format: Often hierarchical (e.g., 1001, 1002, 1101)
  -- First digit: Account type
  --   1xx: Assets
  --   2xx: Liabilities
  --   3xx: Equity
  --   4xx: Revenue
  --   5xx: Expenses

-- Standard account codes (example)
standardAccounts :: [(AccountCode, AccountName)]
standardAccounts =
  [ ("101", "現金")                    -- Cash
  , ("102", "普通預金")                -- Bank deposit
  , ("103", "当座預金")                -- Checking account
  , ("111", "売掛金")                  -- Accounts receivable
  , ("114", "未収入金")                -- Accrued revenue
  , ("141", "商品")                    -- Merchandise
  , ("201", "買掛金")                  -- Accounts payable
  , ("211", "未払金")                  -- Accrued expenses
  , ("213", "預り金")                  -- Deposits received
  , ("214", "仮受消費税")              -- Consumption tax payable
  , ("301", "資本金")                  -- Capital stock
  , ("401", "売上高")                  -- Sales revenue
  , ("501", "仕入高")                  -- Cost of sales
  , ("601", "給料手当")                -- Salaries
  , ("602", "法定福利費")              -- Statutory welfare
  , ("603", "地代家賃")                -- Rent
  ]
```

## Domain Services

### 1. Journal Entry Service
```haskell
class JournalEntryService m where
  createJournalEntry
    :: [JournalLine]
    -> Description
    -> SourceDocument
    -> m (Either EntryError JournalEntry)

  validateBalancing
    :: NonEmpty JournalLine
    -> Either BalancingError Validated

  postEntry
    :: JournalEntry
    -> m (Either PostingError Posted)
```

**Business Rules**:
- Debits must equal credits
- All lines must reference valid accounts
- Posting date must be in open period
- Tax calculations must be correct

### 2. Tax Calculation Service
```haskell
class TaxCalculationService m where
  calculateConsumptionTax
    :: Money
    -> TaxCategory
    -> Date
    -> m TaxAmount

  calculateWithholdingTax
    :: IncomeType
    -> Money
    -> m Money

  applyTaxExemptions
    :: [Transaction]
    -> m [Transaction]
```

**Consumption Tax Rules**:
- Standard rate: 10% (national 7.8% + local 2.2%)
- Reduced rate: 8% (food, newspapers)
- Tax-free: Exports, international services
- Exempt: Medical, education, residential rent

**Withholding Tax Rules**:
- Salary: Progressive rates based on amount
- Bonus: Calculated differently from regular salary
- Contractors: 10.21% (national + reconstruction)
- Dividends: 20.315% (national + local + reconstruction)

### 3. Financial Closing Service
```haskell
class FinancialClosingService m where
  performPeriodClose
    :: FiscalPeriod
    -> m (Either ClosingError ClosedPeriod)

  generateTrialBalance
    :: FiscalPeriod
    -> m TrialBalance

  calculateRetainedEarnings
    :: FiscalYear
    -> m Money

  generateFinancialStatements
    :: FiscalYear
    -> m FinancialStatements
```

**Closing Process**:
1. Verify all transactions posted
2. Generate trial balance
3. Post adjustment entries
4. Calculate tax provisions
5. Generate financial statements
6. Lock period

### 4. Depreciation Service
```haskell
class DepreciationService m where
  calculateDepreciation
    :: FixedAsset
    -> FiscalPeriod
    -> m DepreciationAmount

  generateDepreciationSchedule
    :: FixedAsset
    -> m DepreciationSchedule
```

**Depreciation Methods** (Japan):
- **Straight-line (定額法)**: (Cost - Residual) / Useful life
- **Declining balance (定率法)**: Book value × Rate
- **Mandatory for buildings since 2016**: Straight-line only
- **Useful life tables**: Defined by tax law

## Domain Events

```haskell
data JournalEntryPosted = JournalEntryPosted
  { entryId :: JournalEntryId
  , companyId :: CompanyId
  , fiscalPeriodId :: PeriodId
  , entryDate :: Date
  , lines :: NonEmpty JournalLine
  , totalDebit :: Money
  , totalCredit :: Money
  , occurredAt :: Timestamp
  }

data FiscalPeriodClosed = FiscalPeriodClosed
  { periodId :: PeriodId
  , companyId :: CompanyId
  , periodEnd :: Date
  , trialBalance :: TrialBalance
  , netIncome :: Money
  , closedAt :: Timestamp
  , closedBy :: UserId
  }

data TaxReturnFiled = TaxReturnFiled
  { taxPeriodId :: TaxPeriodId
  , companyId :: CompanyId
  , taxType :: TaxType
  , taxAmount :: Money
  , filingDate :: Date
  , dueDate :: Date
  , occurredAt :: Timestamp
  }
```

## Repositories

```haskell
class GeneralLedgerRepository m where
  save :: GeneralLedger -> m ()
  findByCompany :: CompanyId -> FiscalYear -> m (Maybe GeneralLedger)
  findEntriesByPeriod :: PeriodId -> m [JournalEntry]
  findEntriesByAccount :: AccountCode -> DateRange -> m [JournalEntry]

class TaxPeriodRepository m where
  save :: TaxPeriod -> m ()
  findByCompany :: CompanyId -> TaxYear -> m (Maybe TaxPeriod)
  findFilingsDue :: Date -> m [TaxPeriod]

class InvoiceRepository m where
  save :: Invoice -> m ()
  findById :: InvoiceId -> m (Maybe Invoice)
  findByCustomer :: CustomerId -> m [Invoice]
  findOverdue :: Date -> m [Invoice]
  findByStatus :: InvoiceStatus -> m [Invoice]
```

## Integration Points

### Inbound Dependencies
- **Legal Context**: Company incorporation → Chart of accounts setup
- **HR Context**: Salary payments → Payroll journal entries
- **Operations Context**: Sales/purchases → AR/AP entries

### Outbound Integrations
- **Compliance Context**: Tax calculations → Regulatory filings
- **Banking**: Payment processing → Bank reconciliation

## Business Rules Summary

1. **Accounting Principles**:
   - Double-entry bookkeeping mandatory
   - Accrual basis accounting (with cash basis option for small companies)
   - Historical cost principle
   - Consistency principle

2. **Fiscal Year**:
   - Any 12-month period allowed
   - Most common: April 1 - March 31
   - Can use calendar year (Jan 1 - Dec 31)

3. **Consumption Tax**:
   - File monthly, quarterly, or annually based on size
   - Standard method vs simplified method
   - Invoice preservation requirement (8 years)

4. **Corporate Tax**:
   - National tax: 23.2% (as of 2024)
   - Local tax: ~10% (varies by prefecture)
   - Effective rate: ~30-34%
   - File within 2 months of fiscal year end

## Compliance Requirements

- **Companies Act (会社法)**: Financial statement requirements
- **Corporate Tax Act (法人税法)**: Tax calculations and filing
- **Consumption Tax Act (消費税法)**: VAT compliance
- **Tax Payment Act (国税通則法)**: Payment procedures
- **Certified Public Accountants Act (公認会計士法)**: Audit requirements
