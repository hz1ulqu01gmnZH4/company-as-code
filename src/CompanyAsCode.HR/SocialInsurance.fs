namespace CompanyAsCode.HR

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Social insurance management for Japanese companies
module SocialInsurance =

    // ============================================
    // Insurance Types (社会保険の種類)
    // ============================================

    /// Health insurance system type
    type HealthInsuranceSystem =
        | KyokaiKenpo           // 協会けんぽ (Japan Health Insurance Association)
        | KumiaiKenpo           // 組合健保 (Union-managed health insurance)
        | KokuminKenkoHoken     // 国民健康保険 (National Health Insurance)

    /// Pension system type
    type PensionSystem =
        | KoseiNenkin           // 厚生年金 (Employees' Pension)
        | KokuMinNenkin         // 国民年金 (National Pension)

    // ============================================
    // Standard Monthly Remuneration (標準報酬月額)
    // ============================================

    /// Standard monthly remuneration grade
    type StandardMonthlyRemuneration = private {
        _grade: int
        _monthlyAmount: decimal
        _dailyAmount: decimal
    }

    module StandardMonthlyRemuneration =

        /// Health insurance grades (1-50)
        let healthInsuranceGrades : (int * decimal * decimal) list = [
            // (Grade, Monthly Amount, Daily Amount from-to)
            (1, 58_000m, 63_000m)
            (2, 68_000m, 73_000m)
            (3, 78_000m, 83_000m)
            (4, 88_000m, 93_000m)
            (5, 98_000m, 101_000m)
            // ... simplified, full table has 50 grades
            (10, 150_000m, 155_000m)
            (20, 300_000m, 310_000m)
            (30, 530_000m, 545_000m)
            (40, 830_000m, 855_000m)
            (50, 1_390_000m, 99_999_999m)  // Maximum
        ]

        /// Determine grade from actual monthly remuneration
        let fromActualRemuneration (monthly: decimal) : StandardMonthlyRemuneration =
            // Find applicable grade
            let grade, amount, _ =
                healthInsuranceGrades
                |> List.tryFind (fun (_, _, maxRange) -> monthly <= maxRange)
                |> Option.defaultValue (List.last healthInsuranceGrades)

            { _grade = grade; _monthlyAmount = amount; _dailyAmount = amount / 30m }

        let grade (smr: StandardMonthlyRemuneration) = smr._grade
        let monthlyAmount (smr: StandardMonthlyRemuneration) = smr._monthlyAmount
        let dailyAmount (smr: StandardMonthlyRemuneration) = smr._dailyAmount

    // ============================================
    // Insurance Enrollment (社会保険加入)
    // ============================================

    /// Insurance enrollment status
    type EnrollmentStatus =
        | NotEnrolled
        | Enrolled of enrollmentDate: Date
        | Exempted of reason: string
        | Suspended of suspensionDate: Date

    /// Insurance enrollment record
    type InsuranceEnrollment = {
        InsuranceType: Payroll.SocialInsuranceType
        Status: EnrollmentStatus
        InsurerId: string option
        InsuredNumber: string option
        StandardRemuneration: StandardMonthlyRemuneration option
        DependentCount: int
    }

    /// Employee insurance coverage
    type EmployeeInsuranceCoverage = {
        EmployeeId: EmployeeId
        HealthInsurance: InsuranceEnrollment
        WelfarePension: InsuranceEnrollment
        EmploymentInsurance: InsuranceEnrollment
        WorkersCompensation: InsuranceEnrollment
    }

    module EmployeeInsuranceCoverage =

        /// Create default enrollment (all enrolled)
        let createEnrolled
            (employeeId: EmployeeId)
            (enrollmentDate: Date)
            (monthlyRemuneration: decimal)
            : EmployeeInsuranceCoverage =

            let smr = StandardMonthlyRemuneration.fromActualRemuneration monthlyRemuneration

            let enrolledRecord insuranceType = {
                InsuranceType = insuranceType
                Status = Enrolled enrollmentDate
                InsurerId = None
                InsuredNumber = None
                StandardRemuneration = Some smr
                DependentCount = 0
            }

            {
                EmployeeId = employeeId
                HealthInsurance = enrolledRecord Payroll.HealthInsurance
                WelfarePension = enrolledRecord Payroll.WelfarePension
                EmploymentInsurance = enrolledRecord Payroll.EmploymentInsurance
                WorkersCompensation = enrolledRecord Payroll.WorkersCompensation
            }

        /// Check if all required insurance is enrolled
        let isFullyEnrolled (coverage: EmployeeInsuranceCoverage) : bool =
            let isEnrolled = function
                | Enrolled _ -> true
                | _ -> false

            isEnrolled coverage.HealthInsurance.Status &&
            isEnrolled coverage.WelfarePension.Status &&
            isEnrolled coverage.EmploymentInsurance.Status

    // ============================================
    // Premium Calculation (保険料計算)
    // ============================================

    /// Insurance premium breakdown
    type InsurancePremium = {
        HealthInsurance: Money
        NursingCare: Money option   // Only for age 40-64
        WelfarePension: Money
        EmploymentInsurance: Money
        TotalEmployee: Money        // Employee share
        TotalEmployer: Money        // Employer share
        Total: Money
    }

    module InsurancePremium =

        /// Calculate monthly premiums (simplified)
        let calculate
            (standardRemuneration: StandardMonthlyRemuneration)
            (age: int)
            (rates: Payroll.SocialInsuranceRates)
            : InsurancePremium =

            let monthly = StandardMonthlyRemuneration.monthlyAmount standardRemuneration

            let healthEmployee = monthly * rates.HealthInsuranceRate
            let healthEmployer = monthly * rates.HealthInsuranceRate  // Equal split

            let nursingCare =
                if age >= 40 && age < 65 then
                    Some (Money.yen (monthly * rates.NursingCareRate))
                else
                    None

            let nursingCareAmount = nursingCare |> Option.map Money.amount |> Option.defaultValue 0m

            let pensionEmployee = monthly * rates.WelfarePensionRate
            let pensionEmployer = monthly * rates.WelfarePensionRate

            let employmentEmployee = monthly * rates.EmploymentInsuranceRate
            let employmentEmployer = monthly * 0.0095m  // Employer pays more

            let workersCompEmployer = monthly * 0.003m  // Employer only

            let totalEmployee = healthEmployee + nursingCareAmount + pensionEmployee + employmentEmployee
            let totalEmployer = healthEmployer + nursingCareAmount + pensionEmployer + employmentEmployer + workersCompEmployer

            {
                HealthInsurance = Money.yen healthEmployee
                NursingCare = nursingCare
                WelfarePension = Money.yen pensionEmployee
                EmploymentInsurance = Money.yen employmentEmployee
                TotalEmployee = Money.yen totalEmployee
                TotalEmployer = Money.yen totalEmployer
                Total = Money.yen (totalEmployee + totalEmployer)
            }

    // ============================================
    // Monthly Change Notification (算定基礎届)
    // ============================================

    /// Standard remuneration determination (算定基礎届)
    /// Submitted annually in July, determines premiums for September-August
    type StandardRemunerationDetermination = {
        EmployeeId: EmployeeId
        DeterminationYear: int
        AprilRemuneration: Money
        MayRemuneration: Money
        JuneRemuneration: Money
        AverageRemuneration: Money
        NewStandardRemuneration: StandardMonthlyRemuneration
        EffectiveFrom: Date  // Usually September 1
    }

    module StandardRemunerationDetermination =

        /// Calculate annual determination
        let calculate
            (employeeId: EmployeeId)
            (year: int)
            (aprilPay: Money)
            (mayPay: Money)
            (junePay: Money)
            : StandardRemunerationDetermination =

            let avg = (Money.amount aprilPay + Money.amount mayPay + Money.amount junePay) / 3m
            let newSmr = StandardMonthlyRemuneration.fromActualRemuneration avg

            {
                EmployeeId = employeeId
                DeterminationYear = year
                AprilRemuneration = aprilPay
                MayRemuneration = mayPay
                JuneRemuneration = junePay
                AverageRemuneration = Money.yen avg
                NewStandardRemuneration = newSmr
                EffectiveFrom = Date.create year 9 1 |> Result.defaultValue (Date.today())
            }

    // ============================================
    // Monthly Change Report (月額変更届)
    // ============================================

    /// Significant change in remuneration (月額変更届)
    /// Required when salary changes by 2+ grades for 3 consecutive months
    type MonthlyChangeReport = {
        EmployeeId: EmployeeId
        ReportMonth: Date
        PreviousStandardRemuneration: StandardMonthlyRemuneration
        NewStandardRemuneration: StandardMonthlyRemuneration
        Month1Remuneration: Money
        Month2Remuneration: Money
        Month3Remuneration: Money
        EffectiveFrom: Date
    }

    module MonthlyChangeReport =

        /// Check if change report is required
        let isChangeRequired
            (currentSmr: StandardMonthlyRemuneration)
            (month1: Money)
            (month2: Money)
            (month3: Money)
            : bool =

            let avg = (Money.amount month1 + Money.amount month2 + Money.amount month3) / 3m
            let newSmr = StandardMonthlyRemuneration.fromActualRemuneration avg

            let gradeDiff =
                abs (StandardMonthlyRemuneration.grade newSmr -
                     StandardMonthlyRemuneration.grade currentSmr)

            gradeDiff >= 2

    // ============================================
    // Maternity/Childcare Leave Insurance
    // ============================================

    /// Maternity leave benefit
    type MaternityBenefit = {
        EmployeeId: EmployeeId
        LeaveStartDate: Date
        LeaveEndDate: Date
        DailyBenefit: Money  // 2/3 of standard daily remuneration
        TotalBenefit: Money
    }

    /// Childcare leave benefit
    type ChildcareLeaveBenefit = {
        EmployeeId: EmployeeId
        ChildBirthDate: Date
        LeaveStartDate: Date
        LeaveEndDate: Date
        First180DaysBenefit: Money   // 67% of salary
        After180DaysBenefit: Money   // 50% of salary
        TotalBenefit: Money
    }

    module ChildcareLeaveBenefit =

        /// Calculate childcare leave benefit
        let calculate
            (employeeId: EmployeeId)
            (childBirthDate: Date)
            (leaveStart: Date)
            (leaveEnd: Date)
            (standardRemuneration: StandardMonthlyRemuneration)
            : ChildcareLeaveBenefit =

            let totalDays = Date.daysBetween leaveStart leaveEnd
            let dailyAmount = StandardMonthlyRemuneration.dailyAmount standardRemuneration

            let first180Days = min 180 totalDays
            let remainingDays = max 0 (totalDays - 180)

            let first180Benefit = decimal first180Days * dailyAmount * 0.67m
            let afterBenefit = decimal remainingDays * dailyAmount * 0.50m

            {
                EmployeeId = employeeId
                ChildBirthDate = childBirthDate
                LeaveStartDate = leaveStart
                LeaveEndDate = leaveEnd
                First180DaysBenefit = Money.yen first180Benefit
                After180DaysBenefit = Money.yen afterBenefit
                TotalBenefit = Money.yen (first180Benefit + afterBenefit)
            }
