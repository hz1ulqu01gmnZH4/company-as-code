# Japanese Company-as-Code: Implementation Plan

## Executive Summary

This document outlines a comprehensive plan for implementing abstract classes modeling Japanese companies according to the Companies Act (会社法), incorporating legal structures, business functions, and DDD principles.

---

## Part 1: Research Findings Summary

### 1.1 Japanese Companies Act (会社法) Key Structures

#### Company Types (会社の種類)

| Type | Japanese | Liability | Min Directors | Min Capital | Use Case |
|------|----------|-----------|---------------|-------------|----------|
| **K.K.** | 株式会社 | Limited | 1 (3 with board) | ¥1 (¥10M practical) | Standard corporations |
| **G.K.** | 合同会社 | Limited | N/A (members manage) | ¥1 (¥3M practical) | LLCs, subsidiaries |
| **Gomei** | 合名会社 | Unlimited | All members | None | Rare, partnerships |
| **Goshi** | 合資会社 | Mixed | Min 2 (1 each type) | None | Rare, limited partnerships |

#### Governance Models (3 Types)

1. **Company with Statutory Auditor** (監査役設置会社) - 55-57% of listed companies
2. **Company with Audit Committee** (監査等委員会設置会社) - 41-43% of listed companies
3. **Company with Three Committees** (指名委員会等設置会社) - ~2% of listed companies

#### Critical Legal Requirements

- **Corporate Number** (法人番号): 13-digit unique identifier
- **Corporate Seals** (会社印): 実印 (registered), 銀行印 (bank), 角印 (acknowledgment)
- **Fiscal Year**: Typically April 1 - March 31
- **Director Terms**: Maximum 2 years (1 year for committee members)
- **Capital Maintenance**: Cannot pay dividends if net assets < ¥3,000,000
- **Reserve Requirement**: 10% of dividends until reserves = 25% of capital

### 1.2 Business Function Domains

#### Human Resources (人事)
- **Employment Types**: 正社員, 契約社員, パート, 派遣社員
- **Social Insurance** (社会保険): Health, pension, employment, workers' comp, nursing care
- **Year-End Adjustment** (年末調整): December tax reconciliation
- **Labor Standards Act** compliance

#### Accounting & Finance (経理・財務)
- **Japanese GAAP / IFRS** compliance
- **Corporate Tax**: 23.2% national + local taxes
- **Consumption Tax**: 10% standard / 8% reduced rate
- **Qualified Invoice System** (インボイス制度)

#### Administration (総務)
- Corporate registration maintenance
- Seal management and authorization
- Document retention (7-10 years)
- Regulatory filings

#### Compliance (コンプライアンス)
- **J-SOX** for listed companies
- **APPI** (個人情報保護法) data protection
- Whistleblower protection
- Anti-corruption (UCPA)

---

## Part 2: Domain-Driven Design Architecture

### 2.1 Bounded Contexts (5 Primary)

```
┌─────────────────────────────────────────────────────────────────┐
│              Compliance & Regulatory Context                     │
│                    (Conformist Layer)                            │
└────────┬─────────────────┬──────────────────┬───────────────────┘
         │                 │                  │
         ▼                 ▼                  ▼
┌────────────────┐  ┌──────────────┐  ┌─────────────────┐
│ Legal/Corp Gov │  │  HR/Employment│  │  Financial/Acct │
│    Context     │◄─┤    Context    │◄─┤     Context     │
│  (Core Domain) │  │  (Supporting) │  │  (Core Domain)  │
└────────┬───────┘  └───────┬───────┘  └────────┬────────┘
         │                  │                    │
         └──────────────────┼────────────────────┘
                            ▼
                  ┌─────────────────┐
                  │   Operations    │
                  │  & Business     │
                  │    Context      │
                  │ (Generic/Support)│
                  └─────────────────┘
```

### 2.2 Aggregate Roots by Context

#### Legal & Corporate Governance Context
| Aggregate | Root Entity | Key Entities |
|-----------|-------------|--------------|
| **Company** | Company | CorporateSeals, ArticlesOfIncorporation |
| **Board** | Board | Director, BoardMeeting, Resolution |
| **ShareholderRegister** | ShareholderRegister | Shareholding, ShareClass |
| **StatutoryAuditor** | AuditorBoard | Auditor, AuditReport |

#### HR & Employment Context
| Aggregate | Root Entity | Key Entities |
|-----------|-------------|--------------|
| **Employee** | Employee | EmploymentContract, JobAssignment |
| **Organization** | Department | Position, Team |
| **Payroll** | PayrollPeriod | SalaryCalculation, Deductions |
| **Leave** | LeaveEntitlement | LeaveRequest, LeaveBalance |

#### Financial & Accounting Context
| Aggregate | Root Entity | Key Entities |
|-----------|-------------|--------------|
| **GeneralLedger** | Ledger | Account, JournalEntry |
| **FiscalYear** | FiscalYear | AccountingPeriod, TrialBalance |
| **TaxPeriod** | TaxPeriod | TaxCalculation, TaxFiling |
| **Invoice** | Invoice | LineItem, Payment |

#### Operations & Business Context
| Aggregate | Root Entity | Key Entities |
|-----------|-------------|--------------|
| **Customer** | Customer | Contact, CreditTerms |
| **SalesOrder** | Order | OrderLine, Shipment |
| **Product** | Product | Variant, Pricing |
| **Vendor** | Vendor | Contract, PurchaseOrder |

#### Compliance & Regulatory Context
| Aggregate | Root Entity | Key Entities |
|-----------|-------------|--------------|
| **RegulatoryRequirement** | Requirement | FilingObligation, Deadline |
| **AuditTrail** | AuditLog | AuditEntry, Evidence |
| **RiskAssessment** | Assessment | Risk, Control, Mitigation |

### 2.3 Core Value Objects

```
┌─────────────────────────────────────────────────────────────┐
│                    SHARED KERNEL                             │
├─────────────────────────────────────────────────────────────┤
│  Identity           │  Japanese-Specific   │  Financial     │
│  ─────────────────  │  ──────────────────  │  ──────────── │
│  • CompanyId        │  • CorporateNumber   │  • Money       │
│  • EmployeeId       │  • CorporateSeal     │  • TaxRate     │
│  • DirectorId       │  • Prefecture        │  • Currency    │
│  • CustomerId       │  • FiscalYearEnd     │  • Percentage  │
│                     │  • JapaneseDate      │                │
├─────────────────────┼──────────────────────┼────────────────┤
│  Contact Info       │  Document Types      │  Temporal      │
│  ─────────────────  │  ──────────────────  │  ──────────── │
│  • PersonName       │  • Seikyuusho        │  • DateRange   │
│  • Address          │  • Mitsumorisho      │  • TermPeriod  │
│  • Email            │  • Chuumonsha        │  • Deadline    │
│  • PhoneNumber      │  • Ryoushuusho       │  • Duration    │
└─────────────────────┴──────────────────────┴────────────────┘
```

### 2.4 Domain Events (Key Examples)

**Legal Context Events**:
- `CompanyIncorporated { companyId, entityType, establishmentDate }`
- `DirectorAppointed { companyId, directorId, position, term }`
- `CapitalIncreased { companyId, previousAmount, newAmount, date }`
- `CorporateSealRegistered { companyId, sealType, registrationDate }`

**HR Context Events**:
- `EmployeeHired { employeeId, contractType, startDate, department }`
- `SalaryAdjusted { employeeId, previousSalary, newSalary, effectiveDate }`
- `SocialInsuranceEnrolled { employeeId, insuranceTypes, startDate }`

**Financial Context Events**:
- `FiscalYearOpened { fiscalYearId, startDate, endDate }`
- `JournalEntryPosted { entryId, accounts, amount, postingDate }`
- `TaxFilingSubmitted { filingId, taxType, period, amount }`

---

## Part 3: Abstract Class Hierarchy

### 3.1 Core Abstractions

```
AbstractCompany
├── LegalEntity (aggregate root behavior)
│   ├── KabushikiKaisha (株式会社)
│   └── GodoKaisha (合同会社)
│
├── Governance
│   ├── BoardOfDirectors
│   ├── StatutoryAuditors
│   └── Committees
│
├── BusinessFunctions
│   ├── HumanResources
│   │   ├── Employment
│   │   ├── Payroll
│   │   └── SocialInsurance
│   │
│   ├── Finance
│   │   ├── Accounting
│   │   ├── Taxation
│   │   └── Treasury
│   │
│   ├── Administration
│   │   ├── SealManagement
│   │   ├── Registration
│   │   └── DocumentControl
│   │
│   └── Operations
│       ├── Sales
│       ├── Procurement
│       └── Production
│
└── Compliance
    ├── RegulatoryFilings
    ├── InternalControls
    └── AuditTrail
```

### 3.2 Type System Requirements

| Requirement | Purpose | Implementation |
|-------------|---------|----------------|
| **Algebraic Data Types** | Model company types, governance structures | Sum types (sealed traits/enums) |
| **Phantom Types** | Compile-time state validation | Type parameters for states |
| **Type Classes** | Polymorphic aggregate behavior | Traits with type bounds |
| **Effect System** | IO separation, transactions | Effect monads (IO, Task, ZIO) |
| **Pattern Matching** | Exhaustive case handling | Match expressions |
| **Immutability** | Event sourcing, audit trails | Persistent data structures |

### 3.3 Module Structure

```
company-as-code/
├── shared-kernel/
│   ├── identity/         # IDs, references
│   ├── japanese/         # Prefecture, CorporateNumber, Seals
│   ├── financial/        # Money, TaxRate, Currency
│   └── temporal/         # FiscalYear, DateRange, Deadline
│
├── legal-context/
│   ├── company/          # Company aggregate
│   ├── governance/       # Board, Directors, Auditors
│   ├── shareholders/     # ShareholderRegister
│   └── registration/     # Legal filings
│
├── hr-context/
│   ├── employee/         # Employee aggregate
│   ├── organization/     # Department, Position
│   ├── payroll/          # Salary, Deductions
│   └── social-insurance/ # Health, Pension, etc.
│
├── financial-context/
│   ├── accounting/       # GeneralLedger, ChartOfAccounts
│   ├── fiscal/           # FiscalYear, Periods
│   ├── taxation/         # Corporate, Consumption tax
│   └── invoicing/        # Invoice, Payment
│
├── operations-context/
│   ├── customer/         # Customer management
│   ├── order/            # Sales orders
│   ├── product/          # Product catalog
│   └── vendor/           # Vendor management
│
└── compliance-context/
    ├── regulatory/       # Requirements, Deadlines
    ├── audit/            # AuditTrail, Evidence
    └── risk/             # Assessment, Controls
```

---

## Part 4: Implementation Phases

### Phase 1: Foundation (Shared Kernel + Legal Context)
1. Implement core value objects (Money, CorporateNumber, Address)
2. Build Company aggregate with entity types
3. Implement Board and Directors
4. Create ShareholderRegister
5. Domain events for legal lifecycle

### Phase 2: HR & Payroll
1. Employee aggregate and employment types
2. Organization structure (Department, Position)
3. Social insurance calculations
4. Payroll processing with Japanese tax rules
5. Year-end adjustment (年末調整) logic

### Phase 3: Financial & Accounting
1. Chart of accounts (Japanese standard)
2. General ledger with journal entries
3. Fiscal year management
4. Corporate tax calculations
5. Consumption tax (invoice system)

### Phase 4: Operations
1. Customer and vendor management
2. Sales order processing
3. Invoice generation (請求書)
4. Payment tracking

### Phase 5: Compliance & Integration
1. Regulatory deadline tracking
2. Filing orchestration
3. Audit trail system
4. Cross-context integration
5. Event-driven workflows

---

## Part 5: Language Selection

### Recommended Languages (Type-Strict, Functional OO)

| Language | Strengths | Ecosystem | Learning Curve |
|----------|-----------|-----------|----------------|
| **Scala 3** | ADTs, type classes, ZIO/Cats, JVM | Mature, enterprise | Moderate |
| **F#** | ADTs, type inference, .NET ecosystem | Strong, Microsoft | Moderate |
| **Kotlin** | Sealed classes, coroutines, JVM | Growing, Android+ | Low-Moderate |
| **Rust** | Ownership, enums, traits, no GC | Growing, systems | High |
| **TypeScript** | Discriminated unions, widespread | Very large, web | Low |
| **Haskell** | Pure FP, maximum type safety | Academic, niche | High |

### Selection Criteria

1. **Type Safety**: Strong static typing with ADTs
2. **Immutability**: Default or enforced immutability
3. **Pattern Matching**: Exhaustive matching support
4. **Effect System**: Separation of pure/impure code
5. **Ecosystem**: Libraries for DDD, event sourcing
6. **Team Familiarity**: Existing skills and hiring
7. **Japanese Support**: Unicode/i18n capabilities

---

## Appendix A: Japanese Business Terminology

| English | Japanese | Romaji | Context |
|---------|----------|--------|---------|
| Stock Company | 株式会社 | Kabushiki Kaisha | Legal |
| LLC | 合同会社 | Godo Kaisha | Legal |
| Director | 取締役 | Torishimariyaku | Governance |
| Representative Director | 代表取締役 | Daihyo Torishimariyaku | Governance |
| Statutory Auditor | 監査役 | Kansayaku | Governance |
| Shareholder | 株主 | Kabunushi | Governance |
| Corporate Number | 法人番号 | Houjin Bangou | Legal |
| Company Seal (Registered) | 実印 | Jituin | Administration |
| Bank Seal | 銀行印 | Ginkoin | Administration |
| Articles of Incorporation | 定款 | Teikan | Legal |
| Fiscal Year | 事業年度 | Jigyou Nendo | Financial |
| Financial Closing | 決算 | Kessan | Financial |
| Consumption Tax | 消費税 | Shouhizei | Taxation |
| Corporate Tax | 法人税 | Houjinzei | Taxation |
| Regular Employee | 正社員 | Seishain | HR |
| Contract Employee | 契約社員 | Keiyaku Shain | HR |
| Social Insurance | 社会保険 | Shakai Hoken | HR |
| Year-End Adjustment | 年末調整 | Nenmatsu Chosei | HR/Tax |
| Invoice | 請求書 | Seikyuusho | Operations |
| Quotation | 見積書 | Mitsumorisho | Operations |

---

## Appendix B: Key Legal Constraints (Business Rules)

```
INVARIANTS:
├── Company
│   ├── KK requires at least 1 director
│   ├── KK with board requires 3+ directors
│   ├── Representative director must exist
│   ├── Corporate number is 13 digits with checksum
│   └── Cannot pay dividends if net assets < ¥3,000,000
│
├── Board
│   ├── Director term ≤ 2 years
│   ├── Committee member term ≤ 1 year
│   ├── Outside directors required for listed companies
│   └── Quorum: majority present, majority vote
│
├── Shareholders
│   ├── Total issued shares = sum of all holdings
│   ├── Ordinary resolution: majority of present votes
│   ├── Special resolution: 2/3 of present votes
│   └── Share transfers may require board approval
│
├── Finance
│   ├── Fiscal year must be ≤ 12 months
│   ├── Reserve 10% of dividends until = 25% of capital
│   ├── Consumption tax: 10% standard, 8% reduced
│   └── Corporate tax: ~23.2% + local taxes
│
└── Compliance
    ├── Registration changes within 2 weeks
    ├── Annual securities report within 3 months
    ├── Document retention 7-10 years
    └── APPI breach reporting within 72 hours
```

---

## Next Step: Language Selection

**Please choose your preferred programming language:**

1. **Scala 3** - Best for JVM enterprise, mature DDD libraries (Cats, ZIO)
2. **F#** - Best for .NET ecosystem, excellent ADT support
3. **Kotlin** - Best balance of familiarity and type safety (JVM)
4. **Rust** - Best for systems/performance, strictest guarantees
5. **TypeScript** - Best for web/full-stack, widest adoption
6. **Haskell** - Best for pure FP, maximum type safety
7. **Other** - Specify your preference

---

*Research conducted: 2025-11-28*
*Architecture documentation available in: `/docs/architecture/`*
*Full research in: `/docs/research/`*
