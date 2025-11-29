# ADR-001: Bounded Contexts for Japanese Company Model

## Status
Accepted

## Context

We need to design a Domain-Driven Design architecture for modeling Japanese companies that encompasses:
- Legal entity structure and corporate governance
- Human resources and employment management
- Financial accounting and tax compliance
- Business operations (sales, procurement, inventory)
- Regulatory compliance and reporting

A monolithic domain model would create excessive coupling and make it difficult to evolve different aspects of the business independently.

## Decision

We will organize the system into 5 bounded contexts:

### 1. Legal & Corporate Governance Context (Core Domain)
- **Responsibility**: Legal entity structure, board of directors, shareholders, corporate seals
- **Rationale**: Foundation for all other contexts; strict legal requirements
- **Japanese specifics**: Kabushiki Kaisha/Godo Kaisha structures, hanko management, corporate numbers

### 2. Financial & Accounting Context (Core Domain)
- **Responsibility**: General ledger, fiscal periods, tax calculations, financial reporting
- **Rationale**: Critical for business operations and compliance
- **Japanese specifics**: Japanese fiscal year, consumption tax, corporate tax

### 3. HR & Employment Context (Supporting Domain)
- **Responsibility**: Employee lifecycle, employment contracts, benefits, leave management
- **Rationale**: Important but standardizable
- **Japanese specifics**: Seishain/keiyaku shain, social insurance, year-end adjustment

### 4. Operations & Business Context (Generic Domain)
- **Responsibility**: Customers, sales, procurement, inventory
- **Rationale**: Standard business operations
- **Japanese specifics**: Japanese invoicing practices, payment terms

### 5. Compliance & Regulatory Context (Supporting Domain)
- **Responsibility**: Filing deadlines, audit trails, regulatory requirements
- **Rationale**: Cross-cutting concern requiring orchestration
- **Japanese specifics**: Japanese regulatory calendar, government APIs

## Consequences

### Positive
- **Clear boundaries**: Each context has well-defined responsibility
- **Independent evolution**: Contexts can evolve separately
- **Team organization**: Teams can own specific contexts
- **Technology flexibility**: Different contexts can use different tech stacks if needed
- **Compliance isolation**: Audit requirements don't pollute business logic

### Negative
- **Integration complexity**: Contexts must communicate via events/APIs
- **Eventual consistency**: Cross-context operations are eventually consistent
- **Distributed transactions**: Some operations span multiple contexts
- **Learning curve**: Developers must understand context boundaries

### Mitigation
- **Shared Kernel**: Common value objects reduce duplication
- **Event-driven integration**: Loose coupling via domain events
- **Anti-corruption layers**: Protect contexts from external changes
- **Clear documentation**: Context maps and integration patterns documented

## Alternatives Considered

### 1. Single Monolithic Model
- **Rejected**: Too much coupling, difficult to evolve
- **Issue**: Changes to compliance logic affect business operations

### 2. Microservices from Day One
- **Rejected**: Premature optimization, operational complexity
- **Issue**: Network overhead, distributed transactions

### 3. Three Contexts (Merge HR into Legal, Operations into Financial)
- **Rejected**: Contexts too large, mixed concerns
- **Issue**: HR employment logic mixed with legal governance

## Implementation Notes

- Start with modular monolith
- Clear module boundaries in code
- Event bus for cross-context communication
- Extract to microservices only when scaling requires it
- Document integration points explicitly

## References

- Evans, Eric. "Domain-Driven Design" (Chapter on Bounded Contexts)
- Vernon, Vaughn. "Implementing Domain-Driven Design" (Strategic Design)
- Japanese Companies Act (会社法)
- Japanese Tax Code (税法)
