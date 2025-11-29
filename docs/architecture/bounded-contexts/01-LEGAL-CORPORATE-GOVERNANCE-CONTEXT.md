# Legal & Corporate Governance Context

## Context Overview

**Domain**: Legal entity structure, corporate governance, regulatory compliance
**Type**: Core Domain
**Strategic Pattern**: Published Language (emits events), Customer-Supplier (supplies to other contexts)

## Ubiquitous Language

### Japanese Terms
- **Kabushiki Kaisha (株式会社 - KK)**: Stock corporation
- **Godo Kaisha (合同会社 - GK)**: Limited liability company
- **Gomei Kaisha (合名会社)**: Unlimited partnership
- **Goshi Kaisha (合資会社)**: Limited partnership
- **Torishimariyaku (取締役)**: Director
- **Daihyo Torishimariyaku (代表取締役)**: Representative director
- **Kansayaku (監査役)**: Statutory auditor
- **Kabunushi (株主)**: Shareholder
- **Jituin (実印)**: Registered company seal
- **Ginkoin (銀行印)**: Bank seal
- **Mitomein (認印)**: Acknowledgment seal
- **Houjin Bangou (法人番号)**: Corporate number (13-digit identifier)
- **Touki (登記)**: Registration/Filing
- **Teikan (定款)**: Articles of incorporation

## Aggregate Roots

### 1. Company Aggregate

**Aggregate Root**: `Company`

**Invariants**:
- Company must have at least one representative director
- Corporate number must be unique and valid (13 digits)
- Company name must comply with legal naming requirements
- Registered capital must meet minimum requirements for entity type
- Fiscal year end date must be valid

**Entities within Aggregate**:
```haskell
-- Root entity
data Company = Company
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , legalNameKana :: CompanyLegalNameKana
  , entityType :: EntityType
  , establishmentDate :: EstablishmentDate
  , fiscalYearEnd :: FiscalYearEnd
  , registeredCapital :: RegisteredCapital
  , headquarters :: RegisteredAddress
  , representativeDirector :: DirectorId
  , corporateSeals :: CorporateSeals
  , status :: CompanyStatus
  }

-- Value objects
data EntityType
  = KabushikiKaisha
  | GodoKaisha
  | GomeiKaisha
  | GoshiKaisha

data CompanyStatus
  = Active
  | Suspended
  | UnderLiquidation
  | Dissolved

data CorporateSeals = CorporateSeals
  { jituin :: Maybe RegisteredSeal       -- Company seal (registered)
  , ginkoin :: Maybe BankSeal             -- Bank seal
  , mitomein :: Maybe AcknowledgmentSeal  -- Acknowledgment seal
  }
```

**Commands**:
- `IncorporateCompany`: Create new legal entity
- `AmendArticlesOfIncorporation`: Update company bylaws
- `ChangeRepresentativeDirector`: Update legal representative
- `RegisterCorporateSeal`: Register official company seal
- `ChangeRegisteredAddress`: Update headquarters location
- `IncreaseCapital`: Increase registered capital
- `InitiateLiquidation`: Begin company dissolution process
- `DissolveCompany`: Complete dissolution

**Domain Events**:
- `CompanyIncorporated`
- `ArticlesAmended`
- `RepresentativeDirectorChanged`
- `CorporateSealRegistered`
- `RegisteredAddressChanged`
- `CapitalIncreased`
- `LiquidationInitiated`
- `CompanyDissolved`

### 2. Board of Directors Aggregate

**Aggregate Root**: `Board`

**Invariants**:
- Board must have at least one director (per Companies Act)
- Representative director must be a board member
- Director terms must not exceed legal maximum
- Quorum requirements must be met for resolutions

**Entities**:
```haskell
data Board = Board
  { boardId :: BoardId
  , companyId :: CompanyId
  , directors :: NonEmpty Director
  , representativeDirectors :: NonEmpty DirectorId
  , structure :: BoardStructure
  }

data Director = Director
  { directorId :: DirectorId
  , personId :: PersonId
  , appointmentDate :: AppointmentDate
  , termExpiry :: TermExpiry
  , position :: DirectorPosition
  , isRepresentative :: Bool
  , registrationStatus :: RegistrationStatus
  }

data DirectorPosition
  = Chairman
  | President
  | VicePresident
  | ExecutiveDirector
  | OutsideDirector
  | Director

data BoardStructure
  = CompanyWithCommittees  -- 委員会設置会社
  | CompanyWithAuditors    -- 監査役設置会社
  | CompanyWithoutBoard    -- 取締役会非設置会社
```

**Commands**:
- `AppointDirector`
- `DesignateRepresentativeDirector`
- `RemoveDirector`
- `ConveneBoardMeeting`
- `RecordBoardResolution`

**Domain Events**:
- `DirectorAppointed`
- `RepresentativeDirectorDesignated`
- `DirectorRemoved`
- `BoardMeetingConvened`
- `BoardResolutionPassed`

### 3. Shareholder Register Aggregate

**Aggregate Root**: `ShareholderRegister`

**Invariants**:
- Total issued shares must equal sum of shareholder holdings
- Share transfers must be properly authorized
- Par value must be consistent with issued capital
- Shareholder registry must be maintained per law

**Entities**:
```haskell
data ShareholderRegister = ShareholderRegister
  { registerId :: RegisterId
  , companyId :: CompanyId
  , authorizedShares :: AuthorizedShares
  , issuedShares :: IssuedShares
  , parValue :: ParValue
  , shareholders :: [Shareholding]
  , shareClass :: ShareClass
  }

data Shareholding = Shareholding
  { shareholderId :: ShareholderId
  , shareholder :: ShareholderInfo
  , sharesHeld :: NumberOfShares
  , acquisitionDate :: AcquisitionDate
  , votingRights :: VotingRights
  }

data ShareholderInfo
  = IndividualShareholder PersonId PersonName Address
  | CorporateShareholder CompanyId CompanyLegalName CorporateNumber
  | ForeignShareholder ForeignEntityInfo

data ShareClass
  = CommonShares
  | PreferredShares SharePreferences
  | TreasuryShares
```

**Commands**:
- `IssueShares`
- `TransferShares`
- `RecordShareAcquisition`
- `BuyBackShares`
- `DeclareShareSplit`

**Domain Events**:
- `SharesIssued`
- `SharesTransferred`
- `ShareAcquisitionRecorded`
- `SharesBoughtBack`
- `ShareSplitDeclared`

### 4. Statutory Auditor Aggregate (for 監査役設置会社)

**Aggregate Root**: `AuditorBoard`

**Invariants**:
- At least one statutory auditor required (or audit committee)
- Auditors must be independent
- Audit reports must be filed per statutory schedule

**Entities**:
```haskell
data AuditorBoard = AuditorBoard
  { auditorBoardId :: AuditorBoardId
  , companyId :: CompanyId
  , auditors :: NonEmpty StatutoryAuditor
  , auditorType :: AuditorType
  }

data StatutoryAuditor = StatutoryAuditor
  { auditorId :: AuditorId
  , personId :: PersonId
  , appointmentDate :: AppointmentDate
  , termExpiry :: TermExpiry
  , isFullTime :: Bool
  , qualifications :: [AuditorQualification]
  }

data AuditorType
  = StatutoryAuditor       -- 監査役
  | AuditAndSupervisory    -- 監査等委員
  | AuditCommittee         -- 監査委員会
```

## Value Objects

### Corporate Identity
```haskell
-- Phantom type for compile-time validation
newtype CorporateNumber = CorporateNumber Text
  -- Invariant: Exactly 13 digits, checksum valid

newtype CompanyLegalName = CompanyLegalName
  { japanese :: Text
  , prefix :: Maybe EntityTypePrefix  -- 株式会社, 合同会社, etc.
  , suffix :: Maybe EntityTypeSuffix
  }

data CompanyLegalNameKana = CompanyLegalNameKana Text
  -- Katakana representation for legal filings
```

### Seals (Hanko)
```haskell
data RegisteredSeal = RegisteredSeal
  { sealId :: SealId
  , sealImage :: SealImage  -- Binary representation
  , registrationDate :: RegistrationDate
  , registrationLocation :: LegalAffairsBorough
  , certificateNumber :: SealCertificateNumber
  , issuedDate :: IssuedDate
  , expiryDate :: ExpiryDate
  }

-- Seal usage tracking
data SealImpression = SealImpression
  { impressionId :: ImpressionId
  , sealId :: SealId
  , documentId :: DocumentId
  , timestamp :: Timestamp
  , authorizedBy :: DirectorId
  , purpose :: SealUsagePurpose
  }
```

### Legal Address
```haskell
data RegisteredAddress = RegisteredAddress
  { postalCode :: PostalCode
  , prefecture :: Prefecture
  , city :: City
  , ward :: Maybe Ward
  , street :: StreetAddress
  , building :: Maybe BuildingInfo
  , roomNumber :: Maybe RoomNumber
  }

-- Japan-specific
data Prefecture
  = Tokyo | Osaka | Kyoto | Hokkaido
  | ... -- All 47 prefectures as sum type
```

### Financial Concepts
```haskell
data RegisteredCapital = RegisteredCapital
  { amount :: Money
  , currency :: Currency  -- Typically JPY
  , lastUpdated :: Date
  }

data Money = Money
  { amount :: Scientific  -- High-precision decimal
  , currency :: Currency
  }

data FiscalYearEnd = FiscalYearEnd
  { month :: Month
  , day :: DayOfMonth
  }
  -- Common: March 31, December 31, September 30
```

## Domain Services

### 1. Company Incorporation Service
```haskell
class CompanyIncorporationService where
  incorporateCompany
    :: ArticlesOfIncorporation
    -> InitialDirectors
    -> InitialCapital
    -> RegisteredAddress
    -> Either IncorporationError (Company, [DomainEvent])

  validateArticles
    :: ArticlesOfIncorporation
    -> Either ValidationError ValidatedArticles

  reserveCompanyName
    :: CompanyLegalName
    -> Either NameReservationError NameReservation
```

**Business Rules**:
- Company name must not conflict with existing registrations
- Minimum capital requirements vary by entity type
- At least one representative director required
- Articles must include mandatory provisions

### 2. Corporate Seal Registration Service
```haskell
class SealRegistrationService where
  registerSeal
    :: Company
    -> SealImage
    -> DirectorId
    -> Either SealError RegisteredSeal

  verifySealImpression
    :: SealImpression
    -> RegisteredSeal
    -> Either VerificationError Verified

  issueSealCertificate
    :: RegisteredSeal
    -> Either CertificateError SealCertificate
```

**Business Rules**:
- Only representative director can register company seal
- Seal must meet legal format requirements
- Seal certificates expire after 3 months

### 3. Corporate Filing Service
```haskell
class CorporateFilingService where
  fileRegistration
    :: Company
    -> RegistrationType
    -> FilingDocuments
    -> Either FilingError FilingReceipt

  generateRegistrationForms
    :: Company
    -> RegistrationType
    -> Either GenerationError RegistrationForms
```

**Registration Types**:
- Establishment registration (設立登記)
- Change registration (変更登記)
- Dissolution registration (解散登記)

## Domain Events

### Event Schemas
```haskell
data CompanyIncorporated = CompanyIncorporated
  { companyId :: CompanyId
  , corporateNumber :: CorporateNumber
  , legalName :: CompanyLegalName
  , entityType :: EntityType
  , establishmentDate :: EstablishmentDate
  , initialCapital :: RegisteredCapital
  , registeredAddress :: RegisteredAddress
  , representativeDirector :: DirectorId
  , occurredAt :: Timestamp
  }

data DirectorAppointed = DirectorAppointed
  { companyId :: CompanyId
  , directorId :: DirectorId
  , personId :: PersonId
  , position :: DirectorPosition
  , isRepresentative :: Bool
  , appointmentDate :: AppointmentDate
  , termExpiry :: TermExpiry
  , occurredAt :: Timestamp
  }

data CorporateSealRegistered = CorporateSealRegistered
  { companyId :: CompanyId
  , sealId :: SealId
  , sealType :: SealType
  , registeredBy :: DirectorId
  , registrationDate :: RegistrationDate
  , occurredAt :: Timestamp
  }
```

### Event Consumers
- **HR Context**: Subscribes to `DirectorAppointed`, `RepresentativeDirectorChanged`
- **Financial Context**: Subscribes to `CompanyIncorporated`, `CapitalIncreased`
- **Compliance Context**: Subscribes to all events for audit trail
- **Operations Context**: Subscribes to `CompanyIncorporated`, `RegisteredAddressChanged`

## Repositories

### Company Repository
```haskell
class CompanyRepository m where
  save :: Company -> m ()
  findById :: CompanyId -> m (Maybe Company)
  findByCorporateNumber :: CorporateNumber -> m (Maybe Company)
  findByLegalName :: CompanyLegalName -> m [Company]
  exists :: CompanyId -> m Bool
```

### Board Repository
```haskell
class BoardRepository m where
  save :: Board -> m ()
  findByCompanyId :: CompanyId -> m (Maybe Board)
  findDirectorsByCompany :: CompanyId -> m [Director]
  findActiveDirectors :: CompanyId -> m [Director]
```

### Shareholder Register Repository
```haskell
class ShareholderRegisterRepository m where
  save :: ShareholderRegister -> m ()
  findByCompanyId :: CompanyId -> m (Maybe ShareholderRegister)
  findShareholdersByCompany :: CompanyId -> m [Shareholding]
  getTotalSharesIssued :: CompanyId -> m IssuedShares
```

## Factories

### Company Factory
```haskell
class CompanyFactory where
  createKabushikiKaisha
    :: CompanyLegalName
    -> RegisteredAddress
    -> RegisteredCapital
    -> FiscalYearEnd
    -> Either CreationError Company

  createGodoKaisha
    :: CompanyLegalName
    -> RegisteredAddress
    -> RegisteredCapital
    -> FiscalYearEnd
    -> Either CreationError Company

  -- Ensures all invariants are met at creation
```

### Board Factory
```haskell
class BoardFactory where
  createBoard
    :: CompanyId
    -> NonEmpty Director
    -> BoardStructure
    -> Either CreationError Board

  appointInitialDirector
    :: PersonId
    -> DirectorPosition
    -> Bool  -- is representative
    -> Either CreationError Director
```

## Integration Points

### Inbound Dependencies
- None (Core Domain)

### Outbound Integrations
- **HR Context**: Director appointments become employment records
- **Financial Context**: Capital structure feeds accounting setup
- **Compliance Context**: Filing deadlines and regulatory requirements
- **Operations Context**: Company identity for invoicing

### Published Events
All domain events published to event bus for cross-context communication.

## Business Rules Summary

1. **Company Formation**:
   - KK requires minimum 1 yen capital (though practically higher)
   - At least one director required
   - Representative director must be designated
   - Articles of incorporation must be notarized

2. **Board Composition**:
   - Director terms typically 2 years (renewable)
   - Representative director has signing authority
   - Outside directors required for large companies

3. **Shareholder Rights**:
   - One share = one vote (unless special share classes)
   - Shareholder meetings required annually
   - Major decisions require shareholder approval

4. **Corporate Seals**:
   - Jituin (registered seal) required for legal documents
   - Seal certificates expire after 3 months
   - Only representative director can register company seal

## Compliance Requirements

- **Companies Act (会社法)**: Entity formation, governance
- **Commercial Registration Act (商業登記法)**: Filing requirements
- **Financial Instruments and Exchange Act (金融商品取引法)**: Public company disclosures
- **Electronic Records Act (電子記録債権法)**: Digital documentation

## Testing Strategy

### Unit Tests
- Aggregate invariant validation
- Value object construction rules
- Business rule enforcement

### Integration Tests
- Repository persistence
- Event publishing
- Cross-aggregate consistency

### Property-Based Tests
- Corporate number checksum algorithm
- Share distribution totals
- Director appointment validity periods

## Migration Considerations

### Legacy System Integration
- Corporate number as primary external identifier
- Seal image digitization strategy
- Historical director records reconciliation

### Data Quality
- Corporate number validation against national registry
- Legal name canonicalization
- Address standardization (Japan Post format)
