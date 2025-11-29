# HR & Employment Context

## Context Overview

**Domain**: Employment relationships, organizational structure, HR operations
**Type**: Supporting Domain
**Strategic Pattern**: Customer-Supplier (consumes from Legal context)

## Ubiquitous Language

### Japanese Terms
- **Seishain (正社員)**: Regular full-time employee
- **Keiyaku Shain (契約社員)**: Contract employee
- **Haken Shain (派遣社員)**: Dispatched worker
- **Paato (パート)**: Part-time worker
- **Arubaito (アルバイト)**: Temporary worker
- **Shain Bangou (社員番号)**: Employee number
- **Nyuusha (入社)**: Joining company
- **Taisha (退社)**: Leaving company
- **Shakai Hoken (社会保険)**: Social insurance
- **Koyou Hoken (雇用保険)**: Employment insurance
- **Nenkin (年金)**: Pension
- **Nenmatsu Chosei (年末調整)**: Year-end tax adjustment
- **Kyuuyo (給与)**: Salary/wages
- **Shouyo (賞与)**: Bonus
- **Zangyo (残業)**: Overtime
- **Yuukyuu Kyuuka (有給休暇)**: Paid leave

## Aggregate Roots

### 1. Employee Aggregate

**Aggregate Root**: `Employee`

**Invariants**:
- Employee number must be unique within company
- Employment contract must be valid for employment period
- Social insurance enrollment required for regular employees
- Salary must meet minimum wage requirements

**Entities**:
```haskell
data Employee = Employee
  { employeeId :: EmployeeId
  , companyId :: CompanyId
  , employeeNumber :: EmployeeNumber
  , personalInfo :: PersonalInfo
  , employmentStatus :: EmploymentStatus
  , currentPosition :: Position
  , employmentHistory :: [EmploymentRecord]
  , compensation :: CompensationPackage
  , benefits :: BenefitEnrollments
  , taxInfo :: TaxInformation
  }

data EmploymentStatus
  = Active ActiveEmployment
  | OnLeave LeaveDetails
  | Suspended SuspensionDetails
  | Terminated TerminationDetails

data ActiveEmployment = ActiveEmployment
  { employmentType :: EmploymentType
  , hireDate :: HireDate
  , contract :: EmploymentContract
  , workLocation :: WorkLocation
  , department :: DepartmentId
  , manager :: Maybe EmployeeId
  }

data EmploymentType
  = RegularEmployee        -- 正社員
  | ContractEmployee       -- 契約社員
      { contractPeriod :: ContractPeriod
      , renewalCount :: RenewalCount
      }
  | DispatchedWorker       -- 派遣社員
      { dispatchingAgency :: DispatchAgencyId
      , dispatchPeriod :: DispatchPeriod
      }
  | PartTimeWorker         -- パート
      { hoursPerWeek :: HoursPerWeek
      }
  | TemporaryWorker        -- アルバイト
      { expectedEndDate :: Maybe EndDate
      }
```

**Commands**:
- `HireEmployee`
- `PromoteEmployee`
- `TransferEmployee`
- `GrantLeave`
- `SuspendEmployee`
- `TerminateEmployment`
- `AdjustCompensation`
- `EnrollInBenefits`

**Domain Events**:
- `EmployeeHired`
- `EmployeePromoted`
- `EmployeeTransferred`
- `LeaveGranted`
- `EmployeeSuspended`
- `EmploymentTerminated`
- `CompensationAdjusted`
- `BenefitsEnrolled`

### 2. Organization Structure Aggregate

**Aggregate Root**: `OrganizationalUnit`

**Invariants**:
- Organizational hierarchy must be acyclic (no circular reporting)
- Each unit must have a designated head
- Unit codes must be unique within company

**Entities**:
```haskell
data OrganizationalUnit = OrganizationalUnit
  { unitId :: OrganizationalUnitId
  , companyId :: CompanyId
  , unitCode :: UnitCode
  , unitName :: UnitName
  , unitType :: UnitType
  , parentUnit :: Maybe OrganizationalUnitId
  , headOfUnit :: EmployeeId
  , members :: [EmployeeId]
  , establishedDate :: EstablishedDate
  , budget :: Maybe BudgetAllocation
  }

data UnitType
  = Division        -- 事業部
  | Department      -- 部
  | Section         -- 課
  | Team            -- チーム
  | Office          -- 営業所
  | Branch          -- 支店

data BudgetAllocation = BudgetAllocation
  { fiscalYear :: FiscalYear
  , allocatedAmount :: Money
  , spent :: Money
  , remaining :: Money
  }
```

**Commands**:
- `CreateOrganizationalUnit`
- `RestructureUnit`
- `MergeUnits`
- `DissolveUnit`
- `AssignUnitHead`
- `AllocateBudget`

**Domain Events**:
- `OrganizationalUnitCreated`
- `UnitRestructured`
- `UnitsMerged`
- `UnitDissolved`
- `UnitHeadAssigned`
- `BudgetAllocated`

### 3. Employment Contract Aggregate

**Aggregate Root**: `EmploymentContract`

**Invariants**:
- Contract terms must comply with Labor Standards Act
- Salary must meet or exceed minimum wage for region
- Working hours must not exceed legal limits
- Trial period cannot exceed legal maximum

**Entities**:
```haskell
data EmploymentContract = EmploymentContract
  { contractId :: ContractId
  , employeeId :: EmployeeId
  , companyId :: CompanyId
  , contractType :: ContractType
  , effectiveDate :: EffectiveDate
  , expiryDate :: Maybe ExpiryDate
  , workingConditions :: WorkingConditions
  , compensationTerms :: CompensationTerms
  , benefits :: BenefitTerms
  , terminationConditions :: TerminationConditions
  , specialClauses :: [SpecialClause]
  , signedBy :: ContractSignatures
  }

data WorkingConditions = WorkingConditions
  { standardWorkingHours :: WorkingHours
  , workDays :: WorkDays
  , breakTime :: BreakTime
  , overtimeArrangement :: OvertimeArrangement
  , workLocation :: WorkLocation
  , remoteWorkPolicy :: Maybe RemoteWorkPolicy
  }

data WorkingHours = WorkingHours
  { hoursPerDay :: Hours
  , hoursPerWeek :: Hours
  , flexTimeEnabled :: Bool
  }
  -- Must comply with Labor Standards Act limits

data CompensationTerms = CompensationTerms
  { baseSalary :: BaseSalary
  , paymentFrequency :: PaymentFrequency
  , allowances :: [Allowance]
  , bonusStructure :: Maybe BonusStructure
  , overtimePayRate :: OvertimeRate
  , paymentMethod :: PaymentMethod
  }

data BonusStructure = BonusStructure
  { numberOfPayments :: Int  -- Typically 2 (summer/winter)
  , baseAmount :: Maybe Money
  , performanceLinked :: Bool
  , paymentMonths :: [Month]
  }
```

**Commands**:
- `DraftContract`
- `AmendContract`
- `RenewContract`
- `TerminateContract`

**Domain Events**:
- `ContractDrafted`
- `ContractAmended`
- `ContractRenewed`
- `ContractTerminated`

### 4. Leave Management Aggregate

**Aggregate Root**: `LeaveEntitlement`

**Invariants**:
- Paid leave accrual follows legal requirements
- Leave balance cannot go negative
- Statutory leave types must be granted
- Leave expiry follows legal timelines

**Entities**:
```haskell
data LeaveEntitlement = LeaveEntitlement
  { entitlementId :: EntitlementId
  , employeeId :: EmployeeId
  , fiscalYear :: FiscalYear
  , paidLeave :: PaidLeaveBalance
  , specialLeave :: [SpecialLeaveBalance]
  , leaveHistory :: [LeaveRecord]
  }

data PaidLeaveBalance = PaidLeaveBalance
  { totalDays :: Days
  , usedDays :: Days
  , remainingDays :: Days
  , expiryDate :: ExpiryDate
  , accruedThisYear :: Days
  , carriedForward :: Days
  }
  -- Yuukyuu Kyuuka (有給休暇)
  -- Accrual: 10 days after 6 months, increases with tenure

data SpecialLeaveBalance = SpecialLeaveBalance
  { leaveType :: SpecialLeaveType
  , entitled :: Days
  , used :: Days
  , remaining :: Days
  }

data SpecialLeaveType
  = MaternityLeave        -- 産前産後休暇
  | ChildcareLeave        -- 育児休暇
  | NursingCareLeave      -- 介護休暇
  | BereavementLeave      -- 忌引休暇
  | SickLeave             -- 病気休暇
  | CompensatoryLeave     -- 代休

data LeaveRecord = LeaveRecord
  { recordId :: LeaveRecordId
  , leaveType :: LeaveType
  , startDate :: Date
  , endDate :: Date
  , totalDays :: Days
  , reason :: LeaveReason
  , approvedBy :: EmployeeId
  , status :: LeaveStatus
  }
```

**Commands**:
- `RequestLeave`
- `ApproveLeave`
- `RejectLeave`
- `CancelLeave`
- `AccruePaidLeave`

**Domain Events**:
- `LeaveRequested`
- `LeaveApproved`
- `LeaveRejected`
- `LeaveCancelled`
- `PaidLeaveAccrued`

### 5. Social Insurance Enrollment Aggregate

**Aggregate Root**: `SocialInsuranceEnrollment`

**Invariants**:
- Regular employees must be enrolled in all statutory insurance
- Enrollment must occur within legal deadlines
- Dependent information must be accurate
- Insurance numbers must be unique

**Entities**:
```haskell
data SocialInsuranceEnrollment = SocialInsuranceEnrollment
  { enrollmentId :: EnrollmentId
  , employeeId :: EmployeeId
  , companyId :: CompanyId
  , healthInsurance :: HealthInsuranceEnrollment
  , pensionInsurance :: PensionEnrollment
  , employmentInsurance :: EmploymentInsuranceEnrollment
  , workersCompensation :: WorkersCompEnrollment
  , dependents :: [DependentInfo]
  }

data HealthInsuranceEnrollment = HealthInsuranceEnrollment
  { insuranceNumber :: HealthInsuranceNumber
  , insuranceProvider :: InsuranceProvider
  , enrollmentDate :: EnrollmentDate
  , monthlyPremium :: Money
  , employeeShare :: Money
  , employerShare :: Money
  , status :: EnrollmentStatus
  }

data PensionEnrollment = PensionEnrollment
  { pensionNumber :: PensionNumber
  , pensionType :: PensionType
  , enrollmentDate :: EnrollmentDate
  , monthlyPremium :: Money
  , employeeShare :: Money
  , employerShare :: Money
  }

data PensionType
  = EmployeesPension      -- 厚生年金
  | NationalPension       -- 国民年金
  | CorporatePension      -- 企業年金

data EmploymentInsuranceEnrollment = EmploymentInsuranceEnrollment
  { insuranceNumber :: EmploymentInsuranceNumber
  , enrollmentDate :: EnrollmentDate
  , premiumRate :: Percentage
  }
```

## Value Objects

### Employee Identity
```haskell
newtype EmployeeNumber = EmployeeNumber Text
  -- Format varies by company, often sequential or coded

data PersonalInfo = PersonalInfo
  { legalName :: PersonName
  , legalNameKana :: PersonNameKana
  , dateOfBirth :: DateOfBirth
  , gender :: Gender
  , nationality :: Nationality
  , residenceStatus :: Maybe ResidenceStatus  -- For foreign employees
  , myNumber :: Maybe MyNumber                -- Individual number (マイナンバー)
  , contactInfo :: ContactInformation
  , emergencyContact :: EmergencyContact
  }

-- My Number (マイナンバー) - 12-digit identifier
newtype MyNumber = MyNumber Text
  -- Highly sensitive, encrypted storage required
```

### Compensation
```haskell
data BaseSalary
  = MonthlySalary Money
  | HourlyWage Money
  | DailySalary Money
  | AnnualSalary Money

data Allowance
  = TransportationAllowance Money
  | HousingAllowance Money
  | FamilyAllowance Money
  | RoleAllowance Money
  | OverseasAllowance Money
  | SkillAllowance Money

data OvertimeRate = OvertimeRate
  { regularRate :: Percentage      -- Minimum 125% of base
  , lateNightRate :: Percentage    -- Minimum 150%
  , holidayRate :: Percentage      -- Minimum 135%
  }
  -- Rates defined by Labor Standards Act
```

### Work Schedule
```haskell
data WorkDays
  = FixedSchedule [DayOfWeek]
  | FlexibleSchedule
      { daysPerWeek :: Int
      , mustWorkDays :: [DayOfWeek]
      }
  | ShiftBased ShiftSchedule

data ShiftSchedule = ShiftSchedule
  { shifts :: [Shift]
  , rotationPattern :: RotationPattern
  }

data Shift = Shift
  { shiftId :: ShiftId
  , shiftName :: Text
  , startTime :: Time
  , endTime :: Time
  , breakTime :: Duration
  }
```

## Domain Services

### 1. Employee Onboarding Service
```haskell
class EmployeeOnboardingService m where
  onboardEmployee
    :: PersonalInfo
    -> EmploymentContract
    -> Position
    -> m (Either OnboardingError Employee)

  generateEmployeeNumber
    :: CompanyId
    -> HireDate
    -> m EmployeeNumber

  setupSocialInsurance
    :: Employee
    -> m SocialInsuranceEnrollment

  createPayrollRecord
    :: Employee
    -> m PayrollRecordCreated
```

**Business Rules**:
- Employee number generated based on company policy
- Social insurance enrollment within 5 days of hire
- Payroll setup before first payment date
- Tax documentation collected at onboarding

### 2. Leave Accrual Service
```haskell
class LeaveAccrualService m where
  calculatePaidLeaveAccrual
    :: Employee
    -> Date
    -> m Days

  calculateAnniversaryLeave
    :: HireDate
    -> Date
    -> m Days

  handleLeaveExpiry
    :: LeaveEntitlement
    -> Date
    -> m LeaveEntitlement
```

**Accrual Rules** (Labor Standards Act):
- 10 days after 6 months of continuous employment
- Additional days based on years of service:
  - 1.5 years: 11 days
  - 2.5 years: 12 days
  - 3.5 years: 14 days
  - 4.5 years: 16 days
  - 5.5 years: 18 days
  - 6.5+ years: 20 days
- Unused leave expires after 2 years

### 3. Salary Calculation Service
```haskell
class SalaryCalculationService m where
  calculateMonthlySalary
    :: Employee
    -> Month
    -> AttendanceRecord
    -> m GrossSalary

  calculateOvertimePay
    :: Employee
    -> OvertimeHours
    -> m Money

  applyDeductions
    :: GrossSalary
    -> TaxInformation
    -> SocialInsuranceEnrollment
    -> m NetSalary
```

**Calculation Rules**:
- Base salary + allowances + overtime
- Deductions: Income tax, resident tax, social insurance
- Overtime rates: 25% (regular), 50% (late night), 35% (holiday)
- Monthly calculation for regular employees

### 4. Employment Compliance Service
```haskell
class EmploymentComplianceService m where
  validateWorkingHours
    :: Employee
    -> AttendanceRecord
    -> Either ComplianceViolation Validated

  checkOvertimeLimits
    :: Employee
    -> OvertimeHours
    -> Either OvertimeViolation Validated

  validateMinimumWage
    :: Employee
    -> Prefecture
    -> Either WageViolation Validated
```

**Compliance Rules**:
- Maximum 40 hours per week (8 hours per day)
- Overtime limit: 45 hours/month, 360 hours/year
- Special circumstances: 100 hours/month max
- Minimum wage varies by prefecture

## Domain Events

```haskell
data EmployeeHired = EmployeeHired
  { employeeId :: EmployeeId
  , companyId :: CompanyId
  , employeeNumber :: EmployeeNumber
  , personalInfo :: PersonalInfo
  , employmentType :: EmploymentType
  , position :: Position
  , hireDate :: HireDate
  , salary :: BaseSalary
  , occurredAt :: Timestamp
  }

data EmploymentTerminated = EmploymentTerminated
  { employeeId :: EmployeeId
  , companyId :: CompanyId
  , terminationDate :: Date
  , terminationReason :: TerminationReason
  , finalPayment :: Money
  , occurredAt :: Timestamp
  }

data LeaveRequested = LeaveRequested
  { employeeId :: EmployeeId
  , leaveType :: LeaveType
  , startDate :: Date
  , endDate :: Date
  , totalDays :: Days
  , reason :: LeaveReason
  , requestedAt :: Timestamp
  }
```

## Repositories

```haskell
class EmployeeRepository m where
  save :: Employee -> m ()
  findById :: EmployeeId -> m (Maybe Employee)
  findByEmployeeNumber :: EmployeeNumber -> m (Maybe Employee)
  findByCompany :: CompanyId -> m [Employee]
  findActiveEmployees :: CompanyId -> m [Employee]

class OrganizationRepository m where
  save :: OrganizationalUnit -> m ()
  findById :: OrganizationalUnitId -> m (Maybe OrganizationalUnit)
  findByCompany :: CompanyId -> m [OrganizationalUnit]
  findSubordinateUnits :: OrganizationalUnitId -> m [OrganizationalUnit]

class LeaveRepository m where
  save :: LeaveEntitlement -> m ()
  findByEmployee :: EmployeeId -> FiscalYear -> m (Maybe LeaveEntitlement)
  findPendingLeaveRequests :: CompanyId -> m [LeaveRecord]
```

## Integration Points

### Inbound Dependencies
- **Legal Context**: Director appointments → Employee records
- **Financial Context**: Salary payment → Payroll records

### Outbound Integrations
- **Financial Context**: Salary calculations → Accounting entries
- **Compliance Context**: Employment compliance → Regulatory filings

## Business Rules Summary

1. **Employment Types**:
   - Regular employees: Indefinite employment
   - Contract employees: Max 5 years total (converts to regular)
   - Dispatched workers: Max 3 years at same location

2. **Working Hours**:
   - Standard: 8 hours/day, 40 hours/week
   - Break: 45 min (6-8 hours), 1 hour (8+ hours)
   - Overtime: Requires 36 agreement (労使協定)

3. **Paid Leave**:
   - Mandatory grant after 6 months
   - Employer must ensure 5 days taken annually
   - Expires after 2 years

4. **Social Insurance**:
   - Mandatory for regular employees
   - Enrollment within 5 days of hire
   - Coverage: Health, pension, employment, workers' comp

## Compliance Requirements

- **Labor Standards Act (労働基準法)**: Working hours, leave, termination
- **Labor Contract Act (労働契約法)**: Contract employee conversion
- **Worker Dispatch Act (労働者派遣法)**: Dispatched worker regulations
- **Employment Insurance Act (雇用保険法)**: Unemployment benefits
- **Health Insurance Act (健康保険法)**: Medical insurance
