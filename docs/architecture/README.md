# Domain-Driven Design Architecture

## Overview

This directory contains the comprehensive Domain-Driven Design (DDD) architecture for modeling Japanese companies. The architecture encompasses legal structures, corporate governance, employment practices, financial operations, and regulatory compliance specific to Japan's business environment.

## Quick Start

1. **Start here**: [00-DDD-ARCHITECTURE-OVERVIEW.md](./00-DDD-ARCHITECTURE-OVERVIEW.md)
2. **Understand contexts**: Read bounded context specifications in `/bounded-contexts/`
3. **Learn patterns**: Review aggregate patterns, value objects, and events
4. **Implement**: Follow the implementation guide

## Architecture Principles

### Strategic Design (Context Level)

1. **5 Bounded Contexts**:
   - Legal & Corporate Governance (Core Domain)
   - Financial & Accounting (Core Domain)
   - HR & Employment (Supporting)
   - Operations & Business (Generic)
   - Compliance & Regulatory (Supporting)

2. **Integration Patterns**:
   - Shared Kernel for common concepts
   - Event-driven communication
   - Anti-corruption layers for external systems
   - Published language for domain events

### Tactical Design (Code Level)

1. **Aggregates**: Consistency boundaries around entity clusters
2. **Value Objects**: Immutable domain concepts
3. **Domain Events**: State change notifications
4. **Domain Services**: Cross-aggregate business logic
5. **Repositories**: Persistence abstraction
6. **Factories**: Complex object creation

## Document Index

### Core Architecture

| Document | Purpose |
|----------|---------|
| [00-DDD-ARCHITECTURE-OVERVIEW.md](./00-DDD-ARCHITECTURE-OVERVIEW.md) | Executive summary and architecture principles |
| [CONTEXT-MAPPING.md](./CONTEXT-MAPPING.md) | How contexts integrate and communicate |
| [IMPLEMENTATION-GUIDE.md](./IMPLEMENTATION-GUIDE.md) | Technology stack and implementation patterns |

### Bounded Contexts

| Context | Document | Key Concepts |
|---------|----------|--------------|
| Legal & Corporate Governance | [01-LEGAL-CORPORATE-GOVERNANCE-CONTEXT.md](./bounded-contexts/01-LEGAL-CORPORATE-GOVERNANCE-CONTEXT.md) | Company, Board, Shareholders, Corporate Seals |
| Financial & Accounting | [03-FINANCIAL-ACCOUNTING-CONTEXT.md](./bounded-contexts/03-FINANCIAL-ACCOUNTING-CONTEXT.md) | General Ledger, Fiscal Year, Tax Period, Chart of Accounts |
| HR & Employment | [02-HR-EMPLOYMENT-CONTEXT.md](./bounded-contexts/02-HR-EMPLOYMENT-CONTEXT.md) | Employee, Employment Contract, Leave Management, Social Insurance |
| Operations & Business | [04-OPERATIONS-BUSINESS-CONTEXT.md](./bounded-contexts/04-OPERATIONS-BUSINESS-CONTEXT.md) | Customer, Sales Order, Product Catalog, Inventory |
| Compliance & Regulatory | [05-COMPLIANCE-REGULATORY-CONTEXT.md](./bounded-contexts/05-COMPLIANCE-REGULATORY-CONTEXT.md) | Regulatory Requirements, Filing Schedule, Audit Trail |

### Design Patterns

| Pattern | Document | Description |
|---------|----------|-------------|
| Aggregates | [AGGREGATE-DESIGN-PATTERNS.md](./aggregates/AGGREGATE-DESIGN-PATTERNS.md) | How to design aggregate roots and maintain consistency |
| Value Objects | [VALUE-OBJECT-CATALOG.md](./value-objects/VALUE-OBJECT-CATALOG.md) | Complete catalog of immutable value objects |
| Domain Events | [EVENT-DRIVEN-ARCHITECTURE.md](./domain-events/EVENT-DRIVEN-ARCHITECTURE.md) | Event schemas, publishing, and handling patterns |
| Repositories | [REPOSITORY-PATTERNS.md](./repositories/REPOSITORY-PATTERNS.md) | Persistence abstraction and factory patterns |

### Architecture Decision Records

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-001](./adrs/ADR-001-BOUNDED-CONTEXTS.md) | Use 5 bounded contexts for separation of concerns | Accepted |
| [ADR-002](./adrs/ADR-002-EVENT-DRIVEN-INTEGRATION.md) | Event-driven integration between contexts | Accepted |

## Key Features

### Japanese Business Compliance

- **Corporate Structures**: Kabushiki Kaisha (KK), Godo Kaisha (GK), etc.
- **Corporate Numbers**: 13-digit government-issued identifiers
- **Fiscal Year**: Japanese fiscal year patterns (March 31, December 31, etc.)
- **Consumption Tax**: Standard (10%) and reduced (8%) rates
- **Employment Types**: Seishain, Keiyaku Shain, Haken Shain, etc.
- **Social Insurance**: Health, pension, employment, workers' compensation
- **Corporate Seals**: Jituin (registered seal), Ginkoin (bank seal), Mitomein

### Type Safety

All domain concepts are represented with strong types:

```haskell
newtype CorporateNumber = CorporateNumber Text  -- 13 digits with checksum
newtype EmployeeNumber = EmployeeNumber Text
newtype Money = Money { amount :: Scientific, currency :: Currency }
data Prefecture = Tokyo | Osaka | ...  -- All 47 prefectures
```

### Business Invariants

Enforced at compile-time and runtime:

- Company must have representative director
- Journal entries must balance (debits = credits)
- Invoice total = subtotal + tax
- Employee must have valid employment contract
- Shareholder percentages must sum to 100%

### Audit Trail

Complete event log for compliance:

- All state changes recorded as immutable events
- Audit trail across all contexts
- Support for temporal queries
- Regulatory reporting

## Architecture Diagrams

### Context Map

```
                    Compliance Context
                    (Conformist Layer)
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
    Legal/Corp         HR/Employment     Financial/Acct
    (Core Domain)      (Supporting)      (Core Domain)
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                           ▼
                   Operations Context
                   (Generic/Support)
```

### Event Flow Example

```
CompanyIncorporated (Legal)
  ↓
  ├─→ Create Chart of Accounts (Financial)
  └─→ Schedule Incorporation Filing (Compliance)

EmployeeHired (HR)
  ↓
  ├─→ Create Payroll Account (Financial)
  ├─→ Setup Withholding Tax (Financial)
  └─→ Schedule Insurance Enrollment (Compliance)

OrderConfirmed (Operations)
  ↓
  ├─→ Reserve Inventory (Operations)
  ├─→ Generate Invoice (Financial)
  └─→ Schedule Payment Due (Financial)
```

## Technology Recommendations

### Functional Object-Oriented Languages

1. **Scala** with Cats/ZIO (Recommended)
   - Strong type system
   - JVM ecosystem
   - Production-ready effect systems

2. **F#** (.NET)
   - Functional-first
   - .NET ecosystem
   - Excellent type inference

3. **Haskell**
   - Pure functional
   - Most advanced type system
   - Strong correctness guarantees

See [IMPLEMENTATION-GUIDE.md](./IMPLEMENTATION-GUIDE.md) for detailed examples.

## Implementation Checklist

- [ ] Choose technology stack
- [ ] Setup project structure
- [ ] Implement shared kernel (value objects)
- [ ] Implement aggregates per context
- [ ] Define domain events schema
- [ ] Setup event bus infrastructure
- [ ] Implement repositories
- [ ] Implement factories
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Setup database migrations
- [ ] Implement anti-corruption layers
- [ ] Setup monitoring and logging
- [ ] Document APIs
- [ ] Deploy modular monolith

## Usage Examples

### Creating a Company

```haskell
-- Legal context
incorporateCompany
  :: CompanyLegalName
  -> EntityType
  -> RegisteredCapital
  -> DirectorId
  -> IO (Either CompanyError Company)

-- Usage
result <- incorporateCompany
  (CompanyLegalName "株式会社サンプル")
  KabushikiKaisha
  (RegisteredCapital (Money 10000000 JPY))
  directorId

case result of
  Right company -> do
    -- Company created, events published
    -- Financial context will create chart of accounts
    -- Compliance context will schedule filings
    pure company
  Left err ->
    handleError err
```

### Hiring an Employee

```haskell
-- HR context
hireEmployee
  :: CompanyId
  -> PersonalInfo
  -> EmploymentType
  -> Position
  -> BaseSalary
  -> IO (Either EmployeeError Employee)

-- Usage
result <- hireEmployee
  companyId
  personalInfo
  RegularEmployee
  (Position "Engineer")
  (MonthlySalary (Money 400000 JPY))

case result of
  Right employee -> do
    -- Employee hired, events published
    -- Financial context creates payroll account
    -- Compliance context schedules insurance enrollment
    pure employee
  Left err ->
    handleError err
```

## Contributing

When adding new features:

1. **Identify the bounded context**: Where does this belong?
2. **Design aggregates**: What are the consistency boundaries?
3. **Define value objects**: What immutable concepts are needed?
4. **Specify events**: What events should be published?
5. **Implement domain services**: What cross-aggregate logic is needed?
6. **Write tests**: Unit tests for invariants, integration tests for flows
7. **Update documentation**: Keep architecture docs current

## References

### Books

- Evans, Eric. "Domain-Driven Design: Tackling Complexity in the Heart of Software"
- Vernon, Vaughn. "Implementing Domain-Driven Design"
- Vernon, Vaughn. "Domain-Driven Design Distilled"
- Kleppmann, Martin. "Designing Data-Intensive Applications"

### Japanese Business Law

- Companies Act (会社法)
- Corporate Tax Act (法人税法)
- Consumption Tax Act (消費税法)
- Labor Standards Act (労働基準法)
- Financial Instruments and Exchange Act (金融商品取引法)

## License

This architecture documentation is provided as-is for reference and implementation purposes.
