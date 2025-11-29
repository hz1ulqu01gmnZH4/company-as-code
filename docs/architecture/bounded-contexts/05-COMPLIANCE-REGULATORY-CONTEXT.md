# Compliance & Regulatory Context

## Context Overview

**Domain**: Regulatory compliance, filing deadlines, audit trails
**Type**: Supporting Domain
**Strategic Pattern**: Conformist (to government regulations), Open Host Service

## Ubiquitous Language

### Japanese Terms
- **Teishutsu Kigen (提出期限)**: Filing deadline
- **Houkoku Gimmu (報告義務)**: Reporting obligation
- **Jouhou Koukai (情報公開)**: Information disclosure
- **Naibu Tousei (内部統制)**: Internal controls
- **Kanri Taisei (管理体制)**: Management system
- **Compliance Taisei (コンプライアンス体制)**: Compliance framework
- **Kansa Shouko (監査証跡)**: Audit trail
- **Houki (法規)**: Laws and regulations
- **Shinkoku (申告)**: Declaration/filing
- **Todokede (届出)**: Notification
- **Kensetsu (検査)**: Inspection
- **Houshuu (報酬)**: Compensation/remuneration disclosure

## Aggregate Roots

### 1. Regulatory Requirement Aggregate

**Aggregate Root**: `RegulatoryRequirement`

**Invariants**:
- Deadlines must be in the future when created
- All required fields must be mapped to data sources
- Authorities must be valid government agencies
- Frequency must match legal requirements

**Entities**:
```haskell
data RegulatoryRequirement = RegulatoryRequirement
  { requirementId :: RequirementId
  , companyId :: CompanyId
  , regulationType :: RegulationType
  , regulatoryAuthority :: RegulatoryAuthority
  , requirementName :: Text
  , description :: Text
  , legalBasis :: LegalBasis
  , frequency :: FilingFrequency
  , deadlineCalculation :: DeadlineCalculation
  , applicability :: ApplicabilityCriteria
  , requiredDocuments :: [DocumentRequirement]
  , dataRequirements :: [DataRequirement]
  , status :: RequirementStatus
  }

data RegulationType
  = CorporateFilings
  | TaxFilings
  | LaborFilings
  | FinancialDisclosure
  | EnvironmentalReporting
  | IndustrySpecific Industry
  | DataProtection
  | AntitrusCompliance

data RegulatoryAuthority
  = NationalTaxAgency                    -- 国税庁
  | MinistryOfJustice                    -- 法務省
  | MinistryOfHealthLabourWelfare        -- 厚生労働省
  | FinancialServicesAgency              -- 金融庁
  | MinistryOfEconomy                    -- 経済産業省
  | MinistryOfEnvironment                -- 環境省
  | PrefecturalGovernment Prefecture
  | MunicipalGovernment Municipality
  | OtherAgency Text

data FilingFrequency
  = OneTime Date                         -- Specific date
  | Annual AnnualSchedule
  | Quarterly QuarterlySchedule
  | Monthly MonthlySchedule
  | AsNeeded TriggerConditions
  | OnEvent EventTrigger

data DeadlineCalculation
  = FixedDate
      { month :: Month
      , day :: DayOfMonth
      }
  | RelativeToFiscalYear
      { monthsAfterEnd :: Int
      , dayOfMonth :: DayOfMonth
      }
  | RelativeToEvent
      { eventType :: EventType
      , daysAfterEvent :: Days
      }
  | Custom DeadlineFormula
```

**Commands**:
- `DefineRequirement`
- `UpdateRequirement`
- `ActivateRequirement`
- `DeactivateRequirement`
- `AssignOwner`

**Domain Events**:
- `RequirementDefined`
- `RequirementUpdated`
- `RequirementActivated`
- `RequirementDeactivated`
- `OwnerAssigned`

### 2. Filing Schedule Aggregate

**Aggregate Root**: `FilingSchedule`

**Invariants**:
- All deadlines must have corresponding requirements
- No overlapping deadlines for same requirement
- Reminder dates must precede deadline
- Completion must reference actual filing

**Entities**:
```haskell
data FilingSchedule = FilingSchedule
  { scheduleId :: ScheduleId
  , companyId :: CompanyId
  , fiscalYear :: FiscalYear
  , scheduledFilings :: [ScheduledFiling]
  , completedFilings :: [CompletedFiling]
  , upcomingDeadlines :: [Deadline]
  }

data ScheduledFiling = ScheduledFiling
  { filingId :: FilingId
  , requirementId :: RequirementId
  , dueDate :: Date
  , preparer :: Maybe EmployeeId
  , reviewer :: Maybe EmployeeId
  , status :: FilingStatus
  , reminders :: [ReminderSchedule]
  , dependencies :: [FilingId]  -- Must be completed first
  }

data FilingStatus
  = NotStarted
  | InPreparation PreparationInfo
  | InReview ReviewInfo
  | PendingApproval ApprovalInfo
  | ReadyToFile
  | Filed FilingReceipt
  | Accepted AcceptanceInfo
  | Rejected RejectionInfo

data ReminderSchedule = ReminderSchedule
  { reminderDate :: Date
  , reminderType :: ReminderType
  , recipients :: [EmployeeId]
  , sent :: Bool
  }

data ReminderType
  = EarlyWarning Days    -- 30 days before
  | StandardReminder Days -- 14 days before
  | UrgentReminder Days   -- 7 days before
  | FinalNotice Days      -- 1 day before
  | Overdue Days          -- After deadline

data CompletedFiling = CompletedFiling
  { filingId :: FilingId
  , requirementId :: RequirementId
  , completedDate :: Date
  , filedDate :: Date
  , filingMethod :: FilingMethod
  , confirmationNumber :: Maybe ConfirmationNumber
  , filedBy :: EmployeeId
  , documents :: [DocumentReference]
  }
```

**Commands**:
- `GenerateSchedule`
- `AssignFiling`
- `UpdateFilingStatus`
- `RecordCompletion`
- `RequestExtension`

**Domain Events**:
- `ScheduleGenerated`
- `FilingAssigned`
- `FilingStatusUpdated`
- `FilingCompleted`
- `DeadlineApproaching`
- `DeadlineMissed`

### 3. Compliance Audit Trail Aggregate

**Aggregate Root**: `AuditTrail`

**Invariants**:
- Audit records are immutable once created
- All state changes must be recorded
- Timestamps must be monotonically increasing
- Actor must be identified for all actions

**Entities**:
```haskell
data AuditTrail = AuditTrail
  { auditTrailId :: AuditTrailId
  , companyId :: CompanyId
  , contextType :: BoundedContextType
  , records :: [AuditRecord]
  }

data AuditRecord = AuditRecord
  { recordId :: AuditRecordId
  , timestamp :: Timestamp
  , actor :: Actor
  , actionType :: ActionType
  , entityType :: EntityType
  , entityId :: EntityId
  , previousState :: Maybe StateSnapshot
  , newState :: StateSnapshot
  , changeReason :: Maybe Text
  , ipAddress :: Maybe IPAddress
  , userAgent :: Maybe UserAgent
  }

data Actor
  = HumanUser EmployeeId
  | SystemProcess ProcessId
  | ExternalSystem SystemId
  | AutomatedJob JobId

data ActionType
  = Created
  | Modified [FieldName]
  | Deleted
  | StateTransition StateChange
  | Approved
  | Rejected
  | Filed
  | Exported

data StateSnapshot = StateSnapshot
  { snapshotData :: Value  -- JSON representation
  , hash :: SHA256Hash     -- Integrity verification
  }
```

**Commands**:
- `RecordAction` (automatically triggered)
- `QueryAuditTrail`
- `GenerateAuditReport`

**Domain Events**:
- `AuditRecordCreated` (internal only)

### 4. Internal Controls Aggregate

**Aggregate Root**: `InternalControl`

**Invariants**:
- Controls must have defined test procedures
- Testing frequency must be appropriate for risk
- Control owners must be assigned
- Deficiencies must be tracked until remediated

**Entities**:
```haskell
data InternalControl = InternalControl
  { controlId :: ControlId
  , companyId :: CompanyId
  , controlName :: Text
  , controlObjective :: Text
  , controlCategory :: ControlCategory
  , riskArea :: RiskArea
  , controlType :: ControlType
  , frequency :: ControlFrequency
  , owner :: EmployeeId
  , testProcedures :: [TestProcedure]
  , lastTestDate :: Maybe Date
  , effectiveness :: Maybe EffectivenessRating
  , deficiencies :: [ControlDeficiency]
  }

data ControlCategory
  = EntityLevel             -- 全社レベル
  | ProcessLevel            -- プロセスレベル
  | ITGeneral               -- IT全般統制
  | ITApplication           -- IT業務処理統制

data RiskArea
  = FinancialReporting
  | AssetSafeguarding
  | RegulatoryCompliance
  | OperationalEfficiency
  | DataSecurity
  | FraudPrevention

data ControlType
  = Preventive              -- 予防統制
  | Detective               -- 発見統制
  | Corrective              -- 是正統制
  | Directive               -- 指示統制

data TestProcedure = TestProcedure
  { procedureId :: ProcedureId
  , description :: Text
  , testMethod :: TestMethod
  , sampleSize :: Maybe Int
  , testFrequency :: TestFrequency
  }

data EffectivenessRating
  = Effective
  | EffectiveWithMinorDeficiencies [MinorDeficiency]
  | IneffectiveMaterialWeakness MaterialWeakness
  | NotTested

data ControlDeficiency = ControlDeficiency
  { deficiencyId :: DeficiencyId
  , identifiedDate :: Date
  , severity :: DeficiencySeverity
  , description :: Text
  , rootCause :: Text
  , remediationPlan :: RemediationPlan
  , status :: RemediationStatus
  }
```

**Commands**:
- `DefineControl`
- `TestControl`
- `RecordDeficiency`
- `RemediateDeficiency`
- `AssessEffectiveness`

**Domain Events**:
- `ControlDefined`
- `ControlTested`
- `DeficiencyIdentified`
- `DeficiencyRemediated`
- `EffectivenessAssessed`

### 5. Regulatory Change Management Aggregate

**Aggregate Root**: `RegulatoryChange`

**Invariants**:
- Effective date must be in the future
- Impact assessment must be completed before implementation
- All affected requirements must be identified

**Entities**:
```haskell
data RegulatoryChange = RegulatoryChange
  { changeId :: ChangeId
  , companyId :: CompanyId
  , changeTitle :: Text
  , regulatoryAuthority :: RegulatoryAuthority
  , changeDescription :: Text
  , effectiveDate :: Date
  , announcementDate :: Date
  , impactAssessment :: Maybe ImpactAssessment
  , affectedRequirements :: [RequirementId]
  , implementationPlan :: Maybe ImplementationPlan
  , status :: ChangeStatus
  }

data ImpactAssessment = ImpactAssessment
  { assessmentDate :: Date
  , assessedBy :: EmployeeId
  , impactLevel :: ImpactLevel
  , affectedProcesses :: [ProcessName]
  , affectedSystems :: [SystemName]
  , estimatedEffort :: Maybe EffortEstimate
  , complianceGap :: [ComplianceGap]
  }

data ImpactLevel
  = High      -- Significant changes required
  | Medium    -- Moderate changes required
  | Low       -- Minor adjustments
  | None      -- No action needed

data ImplementationPlan = ImplementationPlan
  { tasks :: [ImplementationTask]
  , timeline :: Timeline
  , resources :: ResourceAllocation
  , responsibleParty :: EmployeeId
  }
```

## Value Objects

### Legal Basis
```haskell
data LegalBasis = LegalBasis
  { lawName :: Text
  , lawNameJapanese :: Text
  , article :: Maybe ArticleNumber
  , paragraph :: Maybe ParagraphNumber
  , effectiveDate :: Date
  , url :: Maybe URL
  }

-- Major Japanese laws
majorLaws :: [LegalBasis]
majorLaws =
  [ LegalBasis "Companies Act" "会社法" Nothing Nothing (fromGregorian 2006 5 1) Nothing
  , LegalBasis "Corporate Tax Act" "法人税法" Nothing Nothing (fromGregorian 1965 3 31) Nothing
  , LegalBasis "Labor Standards Act" "労働基準法" Nothing Nothing (fromGregorian 1947 4 7) Nothing
  , LegalBasis "Financial Instruments and Exchange Act" "金融商品取引法" Nothing Nothing (fromGregorian 2006 6 14) Nothing
  ]
```

### Filing Methods
```haskell
data FilingMethod
  = ElectronicFiling OnlineSystem
  | PaperFiling SubmissionLocation
  | HybridFiling
      { online :: OnlineSystem
      , physical :: SubmissionLocation
      }

data OnlineSystem
  = ETax                -- e-Tax (国税電子申告・納税システム)
  | EDINET               -- 金融庁 EDINET
  | JobCan              -- ハローワーク JobCan
  | ElectronicCertification  -- 電子認証
  | CustomPortal URL

data SubmissionLocation
  = TaxOffice TaxOfficeCode
  | LegalAffairsBureau Location
  | LaborStandardsOffice Location
  | HelloWork Location
  | ByMail MailingAddress
```

### Deadline Information
```haskell
data Deadline = Deadline
  { requirementId :: RequirementId
  , dueDate :: Date
  , urgency :: UrgencyLevel
  , daysRemaining :: Days
  , penalties :: [Penalty]
  }

data UrgencyLevel
  = Critical Days   -- < 7 days
  | High Days       -- < 14 days
  | Medium Days     -- < 30 days
  | Low Days        -- > 30 days

data Penalty
  = LateFee
      { baseFee :: Money
      , dailyRate :: Maybe Percentage
      }
  | TaxPenalty
      { penaltyRate :: Percentage
      , additionalTax :: Money
      }
  | Prosecution
      { description :: Text
      , severity :: CriminalSeverity
      }
```

## Domain Services

### 1. Deadline Calculation Service
```haskell
class DeadlineCalculationService m where
  calculateDeadline
    :: RegulatoryRequirement
    -> FiscalYear
    -> m Date

  adjustForHolidays
    :: Date
    -> m Date  -- Next business day if holiday

  calculateReminders
    :: Date
    -> [ReminderType]
    -> m [ReminderSchedule]
```

**Calculation Rules**:
- If deadline falls on weekend/holiday, use next business day
- Consider Japanese national holidays (祝日)
- Account for year-end/New Year closures
- Handle Golden Week (late April/early May)

### 2. Filing Orchestration Service
```haskell
class FilingOrchestrationService m where
  prepareFiling
    :: ScheduledFiling
    -> m (Either PreparationError FilingPackage)

  validateFiling
    :: FilingPackage
    -> m (Either ValidationError Validated)

  submitFiling
    :: FilingPackage
    -> FilingMethod
    -> m (Either SubmissionError FilingReceipt)

  trackFilingStatus
    :: FilingReceipt
    -> m FilingStatus
```

**Orchestration Steps**:
1. Gather required data from source contexts
2. Generate required documents
3. Validate completeness and accuracy
4. Obtain necessary approvals
5. Submit to regulatory authority
6. Track acceptance/rejection
7. Archive confirmation

### 3. Compliance Monitoring Service
```haskell
class ComplianceMonitoringService m where
  checkCompliance
    :: CompanyId
    -> Date
    -> m ComplianceStatus

  identifyGaps
    :: [RegulatoryRequirement]
    -> [CompletedFiling]
    -> m [ComplianceGap]

  generateComplianceReport
    :: CompanyId
    -> DateRange
    -> m ComplianceReport
```

**Monitoring Activities**:
- Daily deadline checks
- Weekly compliance gap analysis
- Monthly filing completion review
- Quarterly internal control testing
- Annual regulatory requirement review

### 4. Audit Trail Query Service
```haskell
class AuditTrailQueryService m where
  queryByEntity
    :: EntityType
    -> EntityId
    -> DateRange
    -> m [AuditRecord]

  queryByActor
    :: Actor
    -> DateRange
    -> m [AuditRecord]

  queryByAction
    :: ActionType
    -> DateRange
    -> m [AuditRecord]

  generateAuditReport
    :: AuditQuery
    -> m AuditReport

  verifyIntegrity
    :: [AuditRecord]
    -> m (Either IntegrityError Verified)
```

## Domain Events

```haskell
data DeadlineApproaching = DeadlineApproaching
  { filingId :: FilingId
  , requirementId :: RequirementId
  , dueDate :: Date
  , daysRemaining :: Days
  , urgency :: UrgencyLevel
  , assignedTo :: Maybe EmployeeId
  , occurredAt :: Timestamp
  }

data FilingCompleted = FilingCompleted
  { filingId :: FilingId
  , requirementId :: RequirementId
  , completedDate :: Date
  , filingMethod :: FilingMethod
  , confirmationNumber :: Maybe ConfirmationNumber
  , filedBy :: EmployeeId
  , occurredAt :: Timestamp
  }

data ComplianceViolation = ComplianceViolation
  { violationId :: ViolationId
  , requirementId :: RequirementId
  , violationType :: ViolationType
  , severity :: ViolationSeverity
  , description :: Text
  , identifiedDate :: Date
  , occurredAt :: Timestamp
  }
```

## Repositories

```haskell
class RegulatoryRequirementRepository m where
  save :: RegulatoryRequirement -> m ()
  findById :: RequirementId -> m (Maybe RegulatoryRequirement)
  findByAuthority :: RegulatoryAuthority -> m [RegulatoryRequirement]
  findActive :: CompanyId -> m [RegulatoryRequirement]

class FilingScheduleRepository m where
  save :: FilingSchedule -> m ()
  findByCompany :: CompanyId -> FiscalYear -> m (Maybe FilingSchedule)
  findUpcomingDeadlines :: CompanyId -> Days -> m [Deadline]
  findOverdue :: CompanyId -> m [ScheduledFiling]

class AuditTrailRepository m where
  append :: AuditRecord -> m ()  -- Append-only
  query :: AuditQuery -> m [AuditRecord]
  -- No delete or update operations allowed
```

## Integration Points

### Inbound Dependencies
- **Legal Context**: Corporate structure changes → Filing requirements
- **Financial Context**: Tax calculations → Tax filings
- **HR Context**: Employment changes → Labor filings

### Outbound Integrations
- All contexts subscribe to `DeadlineApproaching` events
- Compliance reports published to all contexts
- Audit trail available for all contexts

## Key Filings and Deadlines

### Corporate Filings
- **Corporate tax return (法人税申告)**: Within 2 months of fiscal year-end
- **Consumption tax return (消費税申告)**: Various schedules (monthly/quarterly/annual)
- **Registration changes (変更登記)**: Within 2 weeks of change
- **Financial statements (決算報告)**: Annual, within specified period

### Labor Filings
- **Social insurance enrollment (社会保険加入)**: Within 5 days of hire
- **Employment insurance enrollment (雇用保険加入)**: Within 10 days of hire
- **Year-end adjustment (年末調整)**: January 31
- **Labor standards report (労働基準監督署報告)**: Annual

### Tax Filings
- **Withholding tax (源泉徴収)**: Monthly (by 10th of following month)
- **Fixed asset tax (固定資産税)**: Quarterly payments
- **Resident tax (住民税)**: Monthly payments (withheld from salary)

## Compliance Requirements

- **Internal Control Reporting**: Financial Instruments and Exchange Act (J-SOX)
- **Data Retention**: 7-10 years for most business records
- **Information Disclosure**: Listed companies have extensive requirements
- **Privacy Protection**: Act on Protection of Personal Information (個人情報保護法)

## Testing Strategy

### Compliance Testing
- Deadline calculation accuracy
- Filing workflow completeness
- Audit trail integrity
- Control effectiveness

### Integration Testing
- Cross-context event flow
- Data gathering from source contexts
- Filing submission to external systems
