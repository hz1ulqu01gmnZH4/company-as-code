namespace CompanyAsCode.HR

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Payroll calculations for Japanese tax and social insurance
module Payroll =

    open Employment

    // ============================================
    // Tax Brackets (所得税率)
    // ============================================

    /// Japanese progressive income tax brackets (2024)
    type TaxBracket = {
        MinIncome: decimal
        MaxIncome: decimal option
        Rate: decimal
        Deduction: decimal
    }

    /// Income tax brackets for Japan
    let incomeTaxBrackets : TaxBracket list = [
        { MinIncome = 0m; MaxIncome = Some 1_950_000m; Rate = 0.05m; Deduction = 0m }
        { MinIncome = 1_950_000m; MaxIncome = Some 3_300_000m; Rate = 0.10m; Deduction = 97_500m }
        { MinIncome = 3_300_000m; MaxIncome = Some 6_950_000m; Rate = 0.20m; Deduction = 427_500m }
        { MinIncome = 6_950_000m; MaxIncome = Some 9_000_000m; Rate = 0.23m; Deduction = 636_000m }
        { MinIncome = 9_000_000m; MaxIncome = Some 18_000_000m; Rate = 0.33m; Deduction = 1_536_000m }
        { MinIncome = 18_000_000m; MaxIncome = Some 40_000_000m; Rate = 0.40m; Deduction = 2_796_000m }
        { MinIncome = 40_000_000m; MaxIncome = None; Rate = 0.45m; Deduction = 4_796_000m }
    ]

    /// Reconstruction tax rate (復興特別所得税)
    let reconstructionTaxRate = 0.021m

    // ============================================
    // Social Insurance Rates (社会保険料率)
    // ============================================

    /// Social insurance type
    type SocialInsuranceType =
        | HealthInsurance       // 健康保険
        | NursingCare           // 介護保険 (age 40+)
        | WelfarePension        // 厚生年金
        | EmploymentInsurance   // 雇用保険
        | WorkersCompensation   // 労災保険

    /// Social insurance rates (approximate, varies by prefecture and industry)
    type SocialInsuranceRates = {
        HealthInsuranceRate: decimal     // ~5% employee share
        NursingCareRate: decimal         // ~0.9% employee share (age 40+)
        WelfarePensionRate: decimal      // 9.15% employee share
        EmploymentInsuranceRate: decimal // 0.6% employee share
        WorkersCompensationRate: decimal // 0% employee (employer pays)
    }

    let standardRates : SocialInsuranceRates = {
        HealthInsuranceRate = 0.05m
        NursingCareRate = 0.009m
        WelfarePensionRate = 0.0915m
        EmploymentInsuranceRate = 0.006m
        WorkersCompensationRate = 0m
    }

    /// Calculate social insurance premium
    let calculateSocialInsurance
        (monthlyIncome: decimal)
        (age: int)
        (rates: SocialInsuranceRates)
        : decimal =

        let healthInsurance = monthlyIncome * rates.HealthInsuranceRate
        let nursingCare = if age >= 40 then monthlyIncome * rates.NursingCareRate else 0m
        let welfarePension = monthlyIncome * rates.WelfarePensionRate
        let employmentInsurance = monthlyIncome * rates.EmploymentInsuranceRate

        healthInsurance + nursingCare + welfarePension + employmentInsurance

    // ============================================
    // Deductions (控除)
    // ============================================

    /// Deduction types
    type DeductionType =
        | IncomeTax                 // 所得税
        | ResidentTax               // 住民税
        | HealthInsurance           // 健康保険
        | NursingCareInsurance      // 介護保険
        | WelfarePension            // 厚生年金
        | EmploymentInsurance       // 雇用保険
        | UnionDues                 // 組合費
        | CompanyHousing            // 社宅費
        | Other of name: string

    /// Deduction record
    type Deduction = {
        Type: DeductionType
        Amount: Money
        Description: string option
    }

    // ============================================
    // Payslip (給与明細)
    // ============================================

    /// Earnings component
    type EarningsComponent = {
        Type: string
        Amount: Money
        Taxable: bool
    }

    /// Payslip for a pay period
    type Payslip = {
        EmployeeId: EmployeeId
        PayPeriod: DateRange
        PaymentDate: Date

        // Earnings
        BaseSalary: Money
        Allowances: EarningsComponent list
        Overtime: EarningsComponent option
        Bonus: Money option
        TotalEarnings: Money

        // Deductions
        Deductions: Deduction list
        TotalDeductions: Money

        // Net
        NetPay: Money

        // Social Insurance breakdown
        SocialInsuranceTotal: Money

        // Tax breakdown
        IncomeTaxWithheld: Money
        ResidentTax: Money
    }

    module Payslip =

        let create
            (employeeId: EmployeeId)
            (payPeriod: DateRange)
            (paymentDate: Date)
            (baseSalary: Money)
            (allowances: EarningsComponent list)
            (overtime: EarningsComponent option)
            (bonus: Money option)
            (deductions: Deduction list)
            : Payslip =

            let totalAllowances =
                allowances
                |> List.sumBy (fun a -> Money.amount a.Amount)

            let overtimeAmount =
                overtime
                |> Option.map (fun o -> Money.amount o.Amount)
                |> Option.defaultValue 0m

            let bonusAmount =
                bonus
                |> Option.map Money.amount
                |> Option.defaultValue 0m

            let totalEarnings = Money.amount baseSalary + totalAllowances + overtimeAmount + bonusAmount

            let totalDeductions =
                deductions
                |> List.sumBy (fun d -> Money.amount d.Amount)

            let socialInsurance =
                deductions
                |> List.filter (fun d ->
                    match d.Type with
                    | HealthInsurance | NursingCareInsurance | WelfarePension | EmploymentInsurance -> true
                    | _ -> false)
                |> List.sumBy (fun d -> Money.amount d.Amount)

            let incomeTax =
                deductions
                |> List.tryFind (fun d -> d.Type = IncomeTax)
                |> Option.map (fun d -> Money.amount d.Amount)
                |> Option.defaultValue 0m

            let residentTax =
                deductions
                |> List.tryFind (fun d -> d.Type = ResidentTax)
                |> Option.map (fun d -> Money.amount d.Amount)
                |> Option.defaultValue 0m

            {
                EmployeeId = employeeId
                PayPeriod = payPeriod
                PaymentDate = paymentDate
                BaseSalary = baseSalary
                Allowances = allowances
                Overtime = overtime
                Bonus = bonus
                TotalEarnings = Money.yen totalEarnings
                Deductions = deductions
                TotalDeductions = Money.yen totalDeductions
                NetPay = Money.yen (totalEarnings - totalDeductions)
                SocialInsuranceTotal = Money.yen socialInsurance
                IncomeTaxWithheld = Money.yen incomeTax
                ResidentTax = Money.yen residentTax
            }

    // ============================================
    // Payroll Calculation Service
    // ============================================

    /// Payroll calculation parameters
    type PayrollParams = {
        SocialInsuranceRates: SocialInsuranceRates
        TaxBrackets: TaxBracket list
        HasDependents: bool
        DependentCount: int
        Age: int
    }

    module PayrollCalculation =

        /// Calculate monthly income tax withholding (simplified)
        let calculateMonthlyWithholding
            (annualizedIncome: decimal)
            (brackets: TaxBracket list)
            : decimal =

            let applicableBracket =
                brackets
                |> List.tryFind (fun b ->
                    annualizedIncome >= b.MinIncome &&
                    (b.MaxIncome |> Option.map (fun max -> annualizedIncome < max) |> Option.defaultValue true))
                |> Option.defaultValue (List.last brackets)

            let baseTax = annualizedIncome * applicableBracket.Rate - applicableBracket.Deduction
            let withReconstruction = baseTax * (1m + reconstructionTaxRate)

            // Return monthly amount
            max 0m (withReconstruction / 12m)

        /// Calculate overtime pay
        let calculateOvertimePay
            (hourlyRate: decimal)
            (regularHours: decimal)
            (overtimeHours: decimal)
            (lateNightHours: decimal)
            (holidayHours: decimal)
            : decimal =

            let regularOvertime = overtimeHours * hourlyRate * 1.25m
            let lateNight = lateNightHours * hourlyRate * 1.5m  // After 10pm
            let holiday = holidayHours * hourlyRate * 1.35m

            regularOvertime + lateNight + holiday

        /// Calculate hourly rate from monthly salary
        let calculateHourlyRate (monthlySalary: decimal) (monthlyWorkHours: decimal) : decimal =
            if monthlyWorkHours = 0m then 0m
            else monthlySalary / monthlyWorkHours

        /// Generate payslip
        let generatePayslip
            (employee: Employee.Employee)
            (payPeriod: DateRange)
            (paymentDate: Date)
            (overtimeHours: decimal)
            (params': PayrollParams)
            : Payslip =

            let salary = employee.State.SalaryStructure
            let baseSalary = salary.BaseSalary

            // Allowances
            let allowances =
                salary.Allowances
                |> List.map (fun (aType, amount) ->
                    {
                        Type = sprintf "%A" aType
                        Amount = amount
                        Taxable =
                            match aType with
                            | AllowanceType.Commuting _ -> false  // Non-taxable up to limit
                            | _ -> true
                    })

            // Overtime
            let hourlyRate =
                calculateHourlyRate (Money.amount baseSalary) (salary.PaymentFrequency |> function
                    | Monthly -> 160m  // ~8h * 20 days
                    | _ -> 40m)

            let overtimePay = calculateOvertimePay hourlyRate 0m overtimeHours 0m 0m
            let overtime =
                if overtimeHours > 0m then
                    Some { Type = "時間外手当"; Amount = Money.yen overtimePay; Taxable = true }
                else
                    None

            // Calculate gross for deductions
            let grossMonthly =
                Money.amount baseSalary +
                (allowances |> List.sumBy (fun a -> Money.amount a.Amount)) +
                overtimePay

            // Social insurance deductions
            let socialInsurance = calculateSocialInsurance grossMonthly params'.Age params'.SocialInsuranceRates

            // Taxable income (gross minus social insurance and some allowances)
            let nonTaxableAllowances =
                allowances
                |> List.filter (fun a -> not a.Taxable)
                |> List.sumBy (fun a -> Money.amount a.Amount)

            let taxableMonthly = grossMonthly - socialInsurance - nonTaxableAllowances
            let annualizedTaxable = taxableMonthly * 12m

            // Income tax
            let monthlyTax = calculateMonthlyWithholding annualizedTaxable params'.TaxBrackets

            // Build deductions
            let deductions = [
                { Type = IncomeTax; Amount = Money.yen monthlyTax; Description = Some "源泉所得税" }
                { Type = ResidentTax; Amount = Money.yen (grossMonthly * 0.10m / 12m); Description = Some "住民税" }
                { Type = HealthInsurance; Amount = Money.yen (grossMonthly * params'.SocialInsuranceRates.HealthInsuranceRate); Description = Some "健康保険" }
                { Type = WelfarePension; Amount = Money.yen (grossMonthly * params'.SocialInsuranceRates.WelfarePensionRate); Description = Some "厚生年金" }
                { Type = EmploymentInsurance; Amount = Money.yen (grossMonthly * params'.SocialInsuranceRates.EmploymentInsuranceRate); Description = Some "雇用保険" }
            ]

            let deductionsWithNursing =
                if params'.Age >= 40 then
                    { Type = NursingCareInsurance; Amount = Money.yen (grossMonthly * params'.SocialInsuranceRates.NursingCareRate); Description = Some "介護保険" }
                    :: deductions
                else
                    deductions

            Payslip.create
                employee.Id
                payPeriod
                paymentDate
                baseSalary
                allowances
                overtime
                None
                deductionsWithNursing

    // ============================================
    // Year-End Adjustment (年末調整)
    // ============================================

    /// Year-end adjustment calculation
    type YearEndAdjustment = {
        EmployeeId: EmployeeId
        TaxYear: int
        TotalEarnings: Money
        TotalTaxableIncome: Money
        TotalTaxWithheld: Money
        CalculatedTax: Money
        Adjustment: Money  // Positive = refund to employee, Negative = additional payment
        Deductions: (string * Money) list
    }

    module YearEndAdjustment =

        /// Calculate year-end adjustment
        let calculate
            (employeeId: EmployeeId)
            (taxYear: int)
            (payslips: Payslip list)
            (additionalDeductions: (string * Money) list)  // Insurance, mortgage interest, etc.
            : YearEndAdjustment =

            let totalEarnings =
                payslips
                |> List.sumBy (fun p -> Money.amount p.TotalEarnings)

            let totalSocialInsurance =
                payslips
                |> List.sumBy (fun p -> Money.amount p.SocialInsuranceTotal)

            let totalWithheld =
                payslips
                |> List.sumBy (fun p -> Money.amount p.IncomeTaxWithheld)

            // Taxable income after deductions
            let additionalDeductionTotal =
                additionalDeductions
                |> List.sumBy (fun (_, amount) -> Money.amount amount)

            let taxableIncome = totalEarnings - totalSocialInsurance - additionalDeductionTotal

            // Calculate actual tax due
            let applicableBracket =
                incomeTaxBrackets
                |> List.tryFind (fun b ->
                    taxableIncome >= b.MinIncome &&
                    (b.MaxIncome |> Option.map (fun max -> taxableIncome < max) |> Option.defaultValue true))
                |> Option.defaultValue (List.last incomeTaxBrackets)

            let baseTax = taxableIncome * applicableBracket.Rate - applicableBracket.Deduction
            let calculatedTax = baseTax * (1m + reconstructionTaxRate)

            let adjustment = totalWithheld - calculatedTax

            {
                EmployeeId = employeeId
                TaxYear = taxYear
                TotalEarnings = Money.yen totalEarnings
                TotalTaxableIncome = Money.yen taxableIncome
                TotalTaxWithheld = Money.yen totalWithheld
                CalculatedTax = Money.yen (max 0m calculatedTax)
                Adjustment = Money.yen adjustment
                Deductions = additionalDeductions
            }
