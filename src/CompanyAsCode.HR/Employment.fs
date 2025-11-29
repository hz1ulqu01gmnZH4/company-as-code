namespace CompanyAsCode.HR

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Employment types and contracts for Japanese labor law
module Employment =

    // ============================================
    // Employment Types (雇用形態)
    // ============================================

    /// Employment type classification
    type EmploymentType =
        | Seishain           // 正社員 - Regular full-time employee
        | KeiyakuShain       // 契約社員 - Contract employee
        | PartTime           // パートタイム - Part-time worker
        | Arubaito           // アルバイト - Part-time/temporary worker
        | Haken              // 派遣社員 - Dispatched worker
        | Shokutaku          // 嘱託 - Re-employed after retirement
        | Executive          // 役員 - Executive/board member

    module EmploymentType =

        let toJapanese = function
            | Seishain -> "正社員"
            | KeiyakuShain -> "契約社員"
            | PartTime -> "パートタイム"
            | Arubaito -> "アルバイト"
            | Haken -> "派遣社員"
            | Shokutaku -> "嘱託社員"
            | Executive -> "役員"

        let isRegular = function
            | Seishain -> true
            | _ -> false

        let requiresContract = function
            | KeiyakuShain | Haken -> true
            | _ -> false

        let hasFixedTerm = function
            | Seishain -> false
            | Executive -> false
            | _ -> true

    // ============================================
    // Working Hours (労働時間)
    // ============================================

    /// Standard working hours per day
    let standardDailyHours = 8m

    /// Standard working hours per week
    let standardWeeklyHours = 40m

    /// Maximum overtime hours per month (general limit)
    let maxOvertimeMonthly = 45m

    /// Maximum overtime hours per year
    let maxOvertimeYearly = 360m

    /// Working hours configuration
    type WorkingHours = {
        DailyHours: decimal
        WeeklyHours: decimal
        StartTime: TimeSpan
        EndTime: TimeSpan
        BreakMinutes: int
        FlexTime: bool
    }

    module WorkingHours =

        let standard : WorkingHours = {
            DailyHours = 8m
            WeeklyHours = 40m
            StartTime = TimeSpan(9, 0, 0)
            EndTime = TimeSpan(18, 0, 0)
            BreakMinutes = 60
            FlexTime = false
        }

        let partTime (hours: decimal) : WorkingHours = {
            DailyHours = hours
            WeeklyHours = hours * 5m
            StartTime = TimeSpan(10, 0, 0)
            EndTime = TimeSpan(10, 0, 0).Add(TimeSpan.FromHours(float hours))
            BreakMinutes = if hours > 6m then 45 else 0
            FlexTime = false
        }

        let validate (wh: WorkingHours) : Result<unit, string> =
            if wh.DailyHours > 8m then
                Error "Daily hours cannot exceed 8 hours without 36 agreement"
            elif wh.WeeklyHours > 40m then
                Error "Weekly hours cannot exceed 40 hours without 36 agreement"
            elif wh.DailyHours > 6m && wh.BreakMinutes < 45 then
                Error "Work over 6 hours requires at least 45 minutes break"
            elif wh.DailyHours > 8m && wh.BreakMinutes < 60 then
                Error "Work over 8 hours requires at least 60 minutes break"
            else
                Ok ()

    // ============================================
    // Contract Terms (契約条件)
    // ============================================

    /// Contract duration
    type ContractDuration =
        | Indefinite                    // 無期
        | FixedTerm of months: int      // 有期
        | ProjectBased of endDate: Date // プロジェクト型

    /// Probation period
    type ProbationPeriod =
        | NoProbation
        | Probation of months: int  // Usually 3-6 months

    /// Employment contract
    type EmploymentContract = {
        ContractId: ContractId
        EmploymentType: EmploymentType
        StartDate: Date
        Duration: ContractDuration
        Probation: ProbationPeriod
        WorkingHours: WorkingHours
        WorkLocation: string
        JobDescription: string
        BaseSalary: Money
        PaymentDate: int  // Day of month
        RenewalTerms: string option
    }

    module EmploymentContract =

        let create
            (employmentType: EmploymentType)
            (startDate: Date)
            (baseSalary: Money)
            : EmploymentContract =
            {
                ContractId = ContractId.create()
                EmploymentType = employmentType
                StartDate = startDate
                Duration = if EmploymentType.hasFixedTerm employmentType
                           then FixedTerm 12
                           else Indefinite
                Probation = Probation 3
                WorkingHours = WorkingHours.standard
                WorkLocation = ""
                JobDescription = ""
                BaseSalary = baseSalary
                PaymentDate = 25
                RenewalTerms = None
            }

        let isExpiring (asOfDate: Date) (contract: EmploymentContract) : bool =
            match contract.Duration with
            | Indefinite -> false
            | FixedTerm months ->
                let endDate = contract.StartDate |> Date.addMonths months
                let daysRemaining = Date.daysBetween asOfDate endDate
                daysRemaining <= 30
            | ProjectBased endDate ->
                let daysRemaining = Date.daysBetween asOfDate endDate
                daysRemaining <= 30

        let contractEndDate (contract: EmploymentContract) : Date option =
            match contract.Duration with
            | Indefinite -> None
            | FixedTerm months -> Some (contract.StartDate |> Date.addMonths months)
            | ProjectBased endDate -> Some endDate

    // ============================================
    // Leave Entitlements (休暇)
    // ============================================

    /// Leave type
    type LeaveType =
        | AnnualPaid            // 年次有給休暇
        | Sick                  // 病気休暇
        | Maternity             // 産前産後休業
        | Paternity             // 育児休業
        | ChildCare             // 育児休暇
        | NursingCare           // 介護休業
        | Bereavement           // 忌引休暇
        | Marriage              // 結婚休暇
        | Special of string     // 特別休暇

    /// Annual paid leave calculation based on tenure
    let rec calculateAnnualLeave (tenureYears: int) (monthlyWorkDays: decimal) : int =
        // Standard calculation for full-time employees
        if monthlyWorkDays >= 20m then
            match tenureYears with
            | 0 -> 10  // 6 months to 1 year
            | 1 -> 11
            | 2 -> 12
            | 3 -> 14
            | 4 -> 16
            | 5 -> 18
            | _ -> 20  // Maximum 20 days
        else
            // Pro-rated for part-time (simplified)
            let ratio = monthlyWorkDays / 20m
            int (decimal (calculateAnnualLeave tenureYears 20m) * ratio)

    /// Leave balance
    type LeaveBalance = {
        LeaveType: LeaveType
        Entitled: decimal
        Used: decimal
        Remaining: decimal
        CarryOver: decimal
        ExpiryDate: Date option
    }

    // ============================================
    // Salary Components (給与構成)
    // ============================================

    /// Salary payment frequency
    type PaymentFrequency =
        | Monthly       // 月給
        | BiWeekly      // 隔週
        | Weekly        // 週給
        | Daily         // 日給
        | Hourly        // 時給

    /// Allowance types
    type AllowanceType =
        | Commuting of maxAmount: Money     // 通勤手当
        | Housing                            // 住宅手当
        | Family                             // 家族手当
        | Position                           // 役職手当
        | Skill                              // 技能手当
        | Overtime                           // 時間外手当
        | NightShift                         // 深夜手当
        | HolidayWork                        // 休日手当
        | Remote                             // 在宅勤務手当
        | Other of name: string

    /// Salary structure
    type SalaryStructure = {
        BaseSalary: Money
        Allowances: (AllowanceType * Money) list
        PaymentFrequency: PaymentFrequency
        BonusMonths: int list   // Usually [6; 12] for June and December
        BonusRatio: decimal     // e.g., 2.0 = 2 months salary
    }

    module SalaryStructure =

        let totalMonthlyAllowances (salary: SalaryStructure) : Money =
            salary.Allowances
            |> List.map snd
            |> List.fold (fun acc m ->
                match Money.add acc m with
                | Ok sum -> sum
                | Error _ -> acc) (Money.yen 0m)

        let totalMonthlyGross (salary: SalaryStructure) : Money =
            let allowances = totalMonthlyAllowances salary
            match Money.add salary.BaseSalary allowances with
            | Ok total -> total
            | Error _ -> salary.BaseSalary

        let annualSalary (salary: SalaryStructure) : Money =
            let monthlyGross = totalMonthlyGross salary
            let monthlyAmount = Money.amount monthlyGross
            let bonusAmount = monthlyAmount * salary.BonusRatio * decimal salary.BonusMonths.Length
            let annualAmount = monthlyAmount * 12m + bonusAmount
            Money.yen annualAmount

    // ============================================
    // Employment Errors
    // ============================================

    type EmploymentError =
        | InvalidContract of message: string
        | ContractExpired
        | InsufficientLeaveBalance of leaveType: LeaveType * requested: decimal * available: decimal
        | InvalidWorkingHours of message: string
        | ProbationNotCompleted
        | EmployeeNotFound of EmployeeId
        | DuplicateEmployeeNumber of EmployeeNumber
