namespace CompanyAsCode.Financial

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Tax calculation and filing
module Tax =

    open Events
    open Errors

    // ============================================
    // Japanese Tax Constants
    // ============================================

    /// Japanese corporate tax rates (2024)
    module CorporateTaxRates =

        /// National corporate tax rate
        let nationalRate = 23.2m  // %

        /// Local corporate tax rate (as % of national tax)
        let localCorporateRate = 10.3m  // %

        /// Inhabitant tax rate (prefectural + municipal)
        let inhabitantRate = 12.9m  // % (varies by region)

        /// Enterprise tax rate (standard)
        let enterpriseRate = 7.0m  // % (varies by income bracket)

        /// Special corporate tax rate (for SMEs, income <= ¥8M)
        let smeRate = 15.0m  // %

        /// SME income threshold
        let smeThreshold = 8_000_000m

        /// Effective combined rate (approximate)
        let effectiveRate = 29.74m  // %

    /// Japanese consumption tax rates
    module ConsumptionTaxRates =

        /// Standard rate
        let standardRate = 10.0m  // %

        /// Reduced rate (food, newspapers)
        let reducedRate = 8.0m  // %

        /// National consumption tax portion
        let nationalPortion = 7.8m  // %

        /// Local consumption tax portion
        let localPortion = 2.2m  // %

    // ============================================
    // Tax Calculation Value Objects
    // ============================================

    /// Taxable income calculation
    type TaxableIncome = {
        GrossIncome: Money
        Deductions: Money
        TaxableAmount: Money
    }

    module TaxableIncome =

        let calculate (grossIncome: Money) (deductions: Money) : TaxableIncome =
            let taxable =
                Money.subtract grossIncome deductions
                |> Result.defaultValue (Money.yen 0m)

            {
                GrossIncome = grossIncome
                Deductions = deductions
                TaxableAmount = if Money.isPositive taxable then taxable else Money.yen 0m
            }

    /// Corporate tax calculation result
    type CorporateTaxCalculation = {
        TaxableIncome: TaxableIncome
        NationalTax: Money
        LocalCorporateTax: Money
        InhabitantTax: Money
        EnterpriseTax: Money
        TotalTax: Money
        EffectiveRate: decimal
    }

    module CorporateTaxCalculation =

        let calculate (taxableIncome: TaxableIncome) : CorporateTaxCalculation =
            let income = Money.amount taxableIncome.TaxableAmount

            // National corporate tax (with SME rate for first ¥8M)
            let nationalTax =
                if income <= CorporateTaxRates.smeThreshold then
                    income * (CorporateTaxRates.smeRate / 100m)
                else
                    let smePortionTax = CorporateTaxRates.smeThreshold * (CorporateTaxRates.smeRate / 100m)
                    let remainingTax = (income - CorporateTaxRates.smeThreshold) * (CorporateTaxRates.nationalRate / 100m)
                    smePortionTax + remainingTax

            // Local corporate tax
            let localCorporateTax = nationalTax * (CorporateTaxRates.localCorporateRate / 100m)

            // Inhabitant tax
            let inhabitantTax = nationalTax * (CorporateTaxRates.inhabitantRate / 100m)

            // Enterprise tax
            let enterpriseTax = income * (CorporateTaxRates.enterpriseRate / 100m)

            let totalTax = nationalTax + localCorporateTax + inhabitantTax + enterpriseTax

            let effectiveRate =
                if income > 0m then (totalTax / income) * 100m
                else 0m

            {
                TaxableIncome = taxableIncome
                NationalTax = Money.yen nationalTax
                LocalCorporateTax = Money.yen localCorporateTax
                InhabitantTax = Money.yen inhabitantTax
                EnterpriseTax = Money.yen enterpriseTax
                TotalTax = Money.yen totalTax
                EffectiveRate = effectiveRate
            }

    /// Consumption tax calculation
    type ConsumptionTaxCalculation = {
        TaxableSales: Money
        TaxRate: decimal
        OutputTax: Money          // 売上に係る消費税
        TaxablePurchases: Money
        InputTax: Money           // 仕入れに係る消費税
        NetTaxPayable: Money      // 納付税額
        TaxCredit: Money          // 控除税額
    }

    module ConsumptionTaxCalculation =

        let calculate
            (taxableSales: Money)
            (taxablePurchases: Money)
            (rate: decimal)
            : ConsumptionTaxCalculation =

            let outputTax = Money.amount taxableSales * (rate / 100m)
            let inputTax = Money.amount taxablePurchases * (rate / 100m)
            let netTax = outputTax - inputTax

            {
                TaxableSales = taxableSales
                TaxRate = rate
                OutputTax = Money.yen outputTax
                TaxablePurchases = taxablePurchases
                InputTax = Money.yen inputTax
                NetTaxPayable = if netTax > 0m then Money.yen netTax else Money.yen 0m
                TaxCredit = if netTax < 0m then Money.yen (abs netTax) else Money.yen 0m
            }

        let calculateWithReducedRate
            (standardSales: Money)
            (reducedSales: Money)
            (standardPurchases: Money)
            (reducedPurchases: Money)
            : ConsumptionTaxCalculation =

            let standardOutput = Money.amount standardSales * (ConsumptionTaxRates.standardRate / 100m)
            let reducedOutput = Money.amount reducedSales * (ConsumptionTaxRates.reducedRate / 100m)
            let totalOutput = standardOutput + reducedOutput

            let standardInput = Money.amount standardPurchases * (ConsumptionTaxRates.standardRate / 100m)
            let reducedInput = Money.amount reducedPurchases * (ConsumptionTaxRates.reducedRate / 100m)
            let totalInput = standardInput + reducedInput

            let netTax = totalOutput - totalInput

            let totalSales =
                Money.add standardSales reducedSales
                |> Result.defaultValue standardSales

            let totalPurchases =
                Money.add standardPurchases reducedPurchases
                |> Result.defaultValue standardPurchases

            {
                TaxableSales = totalSales
                TaxRate = ConsumptionTaxRates.standardRate  // Primary rate
                OutputTax = Money.yen totalOutput
                TaxablePurchases = totalPurchases
                InputTax = Money.yen totalInput
                NetTaxPayable = if netTax > 0m then Money.yen netTax else Money.yen 0m
                TaxCredit = if netTax < 0m then Money.yen (abs netTax) else Money.yen 0m
            }

    // ============================================
    // Tax Filing State
    // ============================================

    /// Tax filing state
    type TaxFilingState = {
        FilingId: Guid
        CompanyId: CompanyId
        TaxType: TaxType
        TaxPeriod: DateRange
        DueDate: Date
        TaxableBase: Money
        TaxAmount: Money
        Status: FilingStatus
        FilingDate: Date option
        FilingReference: string option
        PaymentDate: Date option
        PaymentAmount: Money option
        CreatedAt: DateTimeOffset
        SubmittedAt: DateTimeOffset option
    }

    module TaxFilingState =

        let create
            (companyId: CompanyId)
            (taxType: TaxType)
            (taxPeriod: DateRange)
            (dueDate: Date)
            (taxableBase: Money)
            (taxAmount: Money)
            : TaxFilingState =
            {
                FilingId = Guid.NewGuid()
                CompanyId = companyId
                TaxType = taxType
                TaxPeriod = taxPeriod
                DueDate = dueDate
                TaxableBase = taxableBase
                TaxAmount = taxAmount
                Status = FilingStatus.Draft
                FilingDate = None
                FilingReference = None
                PaymentDate = None
                PaymentAmount = None
                CreatedAt = DateTimeOffset.UtcNow
                SubmittedAt = None
            }

        let isSubmitted (state: TaxFilingState) =
            match state.Status with
            | FilingStatus.Submitted | FilingStatus.Accepted | FilingStatus.Amended -> true
            | _ -> false

        let isPaid (state: TaxFilingState) =
            state.PaymentDate.IsSome

    // ============================================
    // Tax Filing Aggregate
    // ============================================

    /// Tax filing aggregate root
    type TaxFiling private (state: TaxFilingState) =

        member _.State = state
        member _.FilingId = state.FilingId
        member _.CompanyId = state.CompanyId
        member _.TaxType = state.TaxType
        member _.TaxPeriod = state.TaxPeriod
        member _.DueDate = state.DueDate
        member _.TaxableBase = state.TaxableBase
        member _.TaxAmount = state.TaxAmount
        member _.Status = state.Status
        member _.IsSubmitted = TaxFilingState.isSubmitted state
        member _.IsPaid = TaxFilingState.isPaid state

        // ============================================
        // Commands
        // ============================================

        /// Submit the filing
        member this.Submit(filingDate: Date) (filingMethod: string)
            : Result<TaxFiling * FinancialEvent, TaxError> =

            result {
                do! Result.require
                        (state.Status = FilingStatus.Draft || state.Status = FilingStatus.UnderReview)
                        (TaxFilingAlreadySubmitted (state.FilingId.ToString()))

                let newState = {
                    state with
                        Status = FilingStatus.Submitted
                        FilingDate = Some filingDate
                        SubmittedAt = Some DateTimeOffset.UtcNow
                }

                let event = TaxFilingSubmitted {
                    Meta = FinancialEventMeta.create state.CompanyId
                    FilingId = state.FilingId
                    TaxType = state.TaxType
                    TaxPeriod = state.TaxPeriod
                    FilingDate = filingDate
                    DueDate = state.DueDate
                    TaxAmount = state.TaxAmount
                    FilingMethod = filingMethod
                }

                return (TaxFiling(newState), event)
            }

        /// Mark as accepted
        member this.MarkAccepted(reference: string)
            : Result<TaxFiling, TaxError> =

            result {
                do! Result.require
                        (state.Status = FilingStatus.Submitted)
                        (InvalidTaxPeriod "Filing must be submitted to be accepted")

                return TaxFiling({
                    state with
                        Status = FilingStatus.Accepted
                        FilingReference = Some reference
                })
            }

        /// Mark as rejected
        member this.MarkRejected(reason: string)
            : Result<TaxFiling, TaxError> =

            result {
                do! Result.require
                        (state.Status = FilingStatus.Submitted)
                        (InvalidTaxPeriod "Filing must be submitted to be rejected")

                return TaxFiling({ state with Status = FilingStatus.Rejected reason })
            }

        /// Record payment
        member this.RecordPayment(paymentDate: Date) (paymentAmount: Money)
            : Result<TaxFiling, TaxError> =

            result {
                do! Result.require
                        (TaxFilingState.isSubmitted state)
                        (InvalidTaxPeriod "Filing must be submitted to record payment")

                return TaxFiling({
                    state with
                        PaymentDate = Some paymentDate
                        PaymentAmount = Some paymentAmount
                })
            }

        /// Amend the filing
        member this.Amend(newTaxableBase: Money) (newTaxAmount: Money)
            : Result<TaxFiling, TaxError> =

            result {
                do! Result.require
                        (TaxFilingState.isSubmitted state)
                        (InvalidTaxPeriod "Only submitted filings can be amended")

                return TaxFiling({
                    state with
                        Status = FilingStatus.Amended
                        TaxableBase = newTaxableBase
                        TaxAmount = newTaxAmount
                        FilingDate = None
                        SubmittedAt = None
                })
            }

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a corporate tax filing
        static member CreateCorporateTax
            (companyId: CompanyId)
            (taxPeriod: DateRange)
            (dueDate: Date)
            (calculation: CorporateTaxCalculation)
            : TaxFiling =

            let state = TaxFilingState.create
                            companyId
                            CorporateTax
                            taxPeriod
                            dueDate
                            calculation.TaxableIncome.TaxableAmount
                            calculation.TotalTax

            TaxFiling(state)

        /// Create a consumption tax filing
        static member CreateConsumptionTax
            (companyId: CompanyId)
            (taxPeriod: DateRange)
            (dueDate: Date)
            (calculation: ConsumptionTaxCalculation)
            : TaxFiling =

            let state = TaxFilingState.create
                            companyId
                            ConsumptionTax
                            taxPeriod
                            dueDate
                            calculation.TaxableSales
                            calculation.NetTaxPayable

            TaxFiling(state)

        /// Reconstitute from state
        static member FromState(state: TaxFilingState) : TaxFiling =
            TaxFiling(state)

    // ============================================
    // Tax Logic
    // ============================================

    module TaxLogic =

        /// Calculate corporate tax due date (2 months after fiscal year end)
        let corporateTaxDueDate (fiscalYearEnd: Date) : Date =
            Date.addMonths 2 fiscalYearEnd

        /// Calculate consumption tax due date (2 months after period end)
        let consumptionTaxDueDate (periodEnd: Date) : Date =
            Date.addMonths 2 periodEnd

        /// Determine if company is subject to consumption tax
        /// (Taxable sales in base period > ¥10,000,000)
        let isConsumptionTaxable (basePeriodSales: Money) : bool =
            Money.amount basePeriodSales > 10_000_000m

        /// Calculate withholding tax on dividends
        let dividendWithholdingTax (grossDividend: Money) : Money =
            let rate = 20.42m  // 20.42% (income + reconstruction tax)
            Money.multiply (rate / 100m) grossDividend

        /// Calculate withholding tax on professional fees
        let professionalFeeWithholdingTax (grossFee: Money) : Money =
            let feeAmount = Money.amount grossFee
            if feeAmount <= 1_000_000m then
                Money.multiply 0.1021m grossFee  // 10.21%
            else
                let first1M = 1_000_000m * 0.1021m
                let remainder = (feeAmount - 1_000_000m) * 0.2042m
                Money.yen (first1M + remainder)

        /// Check if filing is overdue
        let isOverdue (filing: TaxFiling) (asOfDate: Date) : bool =
            Date.isAfter asOfDate filing.DueDate &&
            not filing.IsSubmitted

        /// Calculate penalty for late filing (simplified)
        let latePenalty (originalTax: Money) (daysLate: int) : Money =
            if daysLate <= 0 then
                Money.yen 0m
            else
                // Simplified: 4.3% for first 2 months, 8.7% thereafter
                let taxAmount = Money.amount originalTax
                let penaltyRate =
                    if daysLate <= 60 then 4.3m
                    else 8.7m
                Money.yen (taxAmount * (penaltyRate / 100m) * (decimal daysLate / 365m))
