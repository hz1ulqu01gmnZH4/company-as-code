namespace CompanyAsCode.Financial

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// General Ledger aggregate
module GeneralLedger =

    open Events
    open Errors
    open Account
    open JournalEntry

    // ============================================
    // Account Balance (Value Object)
    // ============================================

    /// Running balance for an account
    type AccountBalance = {
        AccountId: AccountId
        AccountCode: string
        AccountName: string
        AccountType: AccountType
        OpeningBalance: Money
        TotalDebits: Money
        TotalCredits: Money
        ClosingBalance: Money
    }

    module AccountBalance =

        let create
            (accountId: AccountId)
            (accountCode: string)
            (accountName: string)
            (accountType: AccountType)
            (openingBalance: Money)
            : AccountBalance =
            {
                AccountId = accountId
                AccountCode = accountCode
                AccountName = accountName
                AccountType = accountType
                OpeningBalance = openingBalance
                TotalDebits = Money.yen 0m
                TotalCredits = Money.yen 0m
                ClosingBalance = openingBalance
            }

        let postDebit (amount: Money) (balance: AccountBalance) : AccountBalance =
            let newDebits =
                Money.add balance.TotalDebits amount
                |> Result.defaultValue balance.TotalDebits

            let closing =
                match balance.AccountType with
                | Asset | Expense ->
                    Money.add balance.OpeningBalance newDebits
                    |> Result.bind (fun b -> Money.subtract b balance.TotalCredits)
                    |> Result.defaultValue balance.ClosingBalance
                | Liability | Equity | Revenue ->
                    Money.add balance.OpeningBalance balance.TotalCredits
                    |> Result.bind (fun b -> Money.subtract b newDebits)
                    |> Result.defaultValue balance.ClosingBalance

            { balance with TotalDebits = newDebits; ClosingBalance = closing }

        let postCredit (amount: Money) (balance: AccountBalance) : AccountBalance =
            let newCredits =
                Money.add balance.TotalCredits amount
                |> Result.defaultValue balance.TotalCredits

            let closing =
                match balance.AccountType with
                | Asset | Expense ->
                    Money.add balance.OpeningBalance balance.TotalDebits
                    |> Result.bind (fun b -> Money.subtract b newCredits)
                    |> Result.defaultValue balance.ClosingBalance
                | Liability | Equity | Revenue ->
                    Money.add balance.OpeningBalance newCredits
                    |> Result.bind (fun b -> Money.subtract b balance.TotalDebits)
                    |> Result.defaultValue balance.ClosingBalance

            { balance with TotalCredits = newCredits; ClosingBalance = closing }

    // ============================================
    // Trial Balance
    // ============================================

    /// Trial balance summary
    type TrialBalance = {
        AsOfDate: Date
        Accounts: AccountBalance list
        TotalDebits: Money
        TotalCredits: Money
        IsBalanced: bool
    }

    module TrialBalance =

        let calculate (asOfDate: Date) (balances: AccountBalance list) : TrialBalance =
            let totalDebits =
                balances
                |> List.sumBy (fun b -> Money.amount b.TotalDebits)
                |> Money.yen

            let totalCredits =
                balances
                |> List.sumBy (fun b -> Money.amount b.TotalCredits)
                |> Money.yen

            {
                AsOfDate = asOfDate
                Accounts = balances
                TotalDebits = totalDebits
                TotalCredits = totalCredits
                IsBalanced = Money.amount totalDebits = Money.amount totalCredits
            }

    // ============================================
    // Accounting Period
    // ============================================

    /// Accounting period within a fiscal year
    type AccountingPeriod = {
        PeriodId: Guid
        PeriodNumber: int
        PeriodName: string
        StartDate: Date
        EndDate: Date
        Status: PeriodStatus
        ClosedDate: Date option
        ClosedBy: string option
    }

    module AccountingPeriod =

        let create
            (periodNumber: int)
            (startDate: Date)
            (endDate: Date)
            : AccountingPeriod =
            {
                PeriodId = Guid.NewGuid()
                PeriodNumber = periodNumber
                PeriodName = $"Period {periodNumber}"
                StartDate = startDate
                EndDate = endDate
                Status = NotStarted
                ClosedDate = None
                ClosedBy = None
            }

        let isOpen (period: AccountingPeriod) =
            period.Status = Open

        let canPost (period: AccountingPeriod) =
            match period.Status with
            | Open -> true
            | SoftClosed -> true  // Allow adjusting entries
            | NotStarted | HardClosed -> false

    // ============================================
    // Ledger State
    // ============================================

    /// Ledger status
    type LedgerStatus =
        | Active
        | ClosingInProgress
        | Closed

    /// General ledger state
    type LedgerState = {
        LedgerId: Guid
        CompanyId: CompanyId
        FiscalYearId: FiscalYearId
        FiscalYearNumber: int
        StartDate: Date
        EndDate: Date
        Status: LedgerStatus
        Accounts: Map<AccountId, Account.Account>
        AccountBalances: Map<AccountId, AccountBalance>
        Periods: AccountingPeriod list
        CurrentPeriod: int option
        PostedEntries: JournalEntryId list
        CreatedAt: DateTimeOffset
        ClosedAt: DateTimeOffset option
    }

    module LedgerState =

        let create
            (companyId: CompanyId)
            (fiscalYearId: FiscalYearId)
            (fiscalYearNumber: int)
            (startDate: Date)
            (endDate: Date)
            : LedgerState =
            {
                LedgerId = Guid.NewGuid()
                CompanyId = companyId
                FiscalYearId = fiscalYearId
                FiscalYearNumber = fiscalYearNumber
                StartDate = startDate
                EndDate = endDate
                Status = Active
                Accounts = Map.empty
                AccountBalances = Map.empty
                Periods = []
                CurrentPeriod = None
                PostedEntries = []
                CreatedAt = DateTimeOffset.UtcNow
                ClosedAt = None
            }

        let isActive (state: LedgerState) =
            state.Status = Active

        let isClosed (state: LedgerState) =
            state.Status = Closed

    // ============================================
    // General Ledger Aggregate
    // ============================================

    /// General ledger aggregate root
    type GeneralLedger private (state: LedgerState) =

        member _.State = state
        member _.LedgerId = state.LedgerId
        member _.CompanyId = state.CompanyId
        member _.FiscalYearId = state.FiscalYearId
        member _.FiscalYearNumber = state.FiscalYearNumber
        member _.StartDate = state.StartDate
        member _.EndDate = state.EndDate
        member _.Status = state.Status
        member _.IsActive = LedgerState.isActive state
        member _.IsClosed = LedgerState.isClosed state
        member _.AccountCount = state.Accounts.Count
        member _.PostedEntryCount = state.PostedEntries.Length

        /// Get account by ID
        member _.GetAccount(accountId: AccountId) : Account.Account option =
            Map.tryFind accountId state.Accounts

        /// Get account balance
        member _.GetAccountBalance(accountId: AccountId) : AccountBalance option =
            Map.tryFind accountId state.AccountBalances

        /// Get all account balances
        member _.GetAllBalances() : AccountBalance list =
            state.AccountBalances |> Map.toList |> List.map snd

        /// Get current period
        member _.GetCurrentPeriod() : AccountingPeriod option =
            state.CurrentPeriod
            |> Option.bind (fun num ->
                state.Periods |> List.tryFind (fun p -> p.PeriodNumber = num))

        // ============================================
        // Commands
        // ============================================

        /// Add an account to the ledger
        member this.AddAccount(account: Account.Account)
            : Result<GeneralLedger, LedgerError> =

            result {
                do! Result.require
                        (LedgerState.isActive state)
                        (LedgerAlreadyClosed state.FiscalYearNumber)

                do! Result.require
                        (not (Map.containsKey account.Id state.Accounts))
                        (AccountError (AccountAlreadyExists (AccountCode.value account.Code)))

                let balance = AccountBalance.create
                                account.Id
                                (AccountCode.value account.Code)
                                account.Name
                                account.AccountType
                                (Money.yen 0m)

                return GeneralLedger({
                    state with
                        Accounts = Map.add account.Id account state.Accounts
                        AccountBalances = Map.add account.Id balance state.AccountBalances
                })
            }

        /// Initialize accounting periods (typically 12 monthly periods)
        member this.InitializePeriods(periodCount: int)
            : Result<GeneralLedger, LedgerError> =

            result {
                do! Result.require
                        (state.Periods.Length = 0)
                        (InvalidFiscalYear "Periods already initialized")

                do! Result.require
                        (periodCount >= 1 && periodCount <= 12)
                        (InvalidFiscalYear "Period count must be 1-12")

                let totalDays = Date.daysBetween state.StartDate state.EndDate + 1
                let daysPerPeriod = totalDays / periodCount

                let periods =
                    [1..periodCount]
                    |> List.map (fun i ->
                        let periodStart =
                            if i = 1 then state.StartDate
                            else Date.addDays ((i - 1) * daysPerPeriod) state.StartDate

                        let periodEnd =
                            if i = periodCount then state.EndDate
                            else Date.addDays (i * daysPerPeriod - 1) state.StartDate

                        AccountingPeriod.create i periodStart periodEnd)

                return GeneralLedger({
                    state with
                        Periods = periods
                        CurrentPeriod = Some 1
                })
            }

        /// Open a period
        member this.OpenPeriod(periodNumber: int)
            : Result<GeneralLedger, LedgerError> =

            result {
                let! period =
                    state.Periods
                    |> List.tryFind (fun p -> p.PeriodNumber = periodNumber)
                    |> Result.ofOption (PeriodNotOpen $"Period {periodNumber}")

                do! Result.require
                        (period.Status = NotStarted || period.Status = SoftClosed)
                        (PeriodAlreadyClosed $"Period {periodNumber}")

                let updatedPeriods =
                    state.Periods
                    |> List.map (fun p ->
                        if p.PeriodNumber = periodNumber
                        then { p with Status = Open }
                        else p)

                return GeneralLedger({
                    state with
                        Periods = updatedPeriods
                        CurrentPeriod = Some periodNumber
                })
            }

        /// Close a period
        member this.ClosePeriod(periodNumber: int) (closedBy: string) (isSoftClose: bool)
            : Result<GeneralLedger, LedgerError> =

            result {
                let! period =
                    state.Periods
                    |> List.tryFind (fun p -> p.PeriodNumber = periodNumber)
                    |> Result.ofOption (PeriodNotOpen $"Period {periodNumber}")

                do! Result.require
                        (period.Status = Open || period.Status = SoftClosed)
                        (PeriodNotOpen $"Period {periodNumber}")

                let newStatus = if isSoftClose then SoftClosed else HardClosed

                let updatedPeriods =
                    state.Periods
                    |> List.map (fun p ->
                        if p.PeriodNumber = periodNumber then
                            { p with
                                Status = newStatus
                                ClosedDate = Some (Date.today())
                                ClosedBy = Some closedBy }
                        else p)

                let nextPeriod =
                    if periodNumber < state.Periods.Length
                    then Some (periodNumber + 1)
                    else state.CurrentPeriod

                return GeneralLedger({
                    state with
                        Periods = updatedPeriods
                        CurrentPeriod = nextPeriod
                })
            }

        /// Post a journal entry
        member this.PostEntry(entry: JournalEntry.JournalEntry)
            : Result<GeneralLedger, LedgerError> =

            result {
                do! Result.require
                        (LedgerState.isActive state)
                        (LedgerAlreadyClosed state.FiscalYearNumber)

                do! Result.require
                        entry.IsPosted
                        (JournalEntryError (EntryNotFound (entry.EntryNumber)))

                do! Result.require
                        (not (List.contains entry.Id state.PostedEntries))
                        (JournalEntryError (EntryAlreadyPosted entry.EntryNumber))

                // Verify posting date is within an open period
                let postingDate = entry.PostingDate |> Option.defaultValue entry.TransactionDate
                let! period =
                    state.Periods
                    |> List.tryFind (fun p ->
                        Date.isOnOrAfter postingDate p.StartDate &&
                        Date.isOnOrBefore postingDate p.EndDate &&
                        AccountingPeriod.canPost p)
                    |> Result.ofOption (CannotPostToClosedPeriod (Date.format postingDate))

                // Update account balances
                let updatedBalances =
                    entry.Lines
                    |> List.fold (fun balances line ->
                        match Map.tryFind line.AccountId balances with
                        | Some balance ->
                            let updated =
                                match line.DebitCredit with
                                | Debit -> AccountBalance.postDebit line.Amount balance
                                | Credit -> AccountBalance.postCredit line.Amount balance
                            Map.add line.AccountId updated balances
                        | None -> balances
                    ) state.AccountBalances

                return GeneralLedger({
                    state with
                        AccountBalances = updatedBalances
                        PostedEntries = entry.Id :: state.PostedEntries
                })
            }

        /// Generate trial balance
        member this.GenerateTrialBalance(asOfDate: Date) : TrialBalance =
            let balances =
                state.AccountBalances
                |> Map.toList
                |> List.map snd

            TrialBalance.calculate asOfDate balances

        /// Close the fiscal year
        member this.CloseFiscalYear(closedBy: string)
            : Result<GeneralLedger * FinancialEvent, LedgerError> =

            result {
                do! Result.require
                        (LedgerState.isActive state)
                        (LedgerAlreadyClosed state.FiscalYearNumber)

                // Verify all periods are closed
                let allPeriodsClosed =
                    state.Periods
                    |> List.forall (fun p -> p.Status = HardClosed)

                do! Result.require
                        allPeriodsClosed
                        (InvalidFiscalYear "All periods must be closed before closing fiscal year")

                // Calculate net income
                let incomeAccounts =
                    state.AccountBalances
                    |> Map.toList
                    |> List.filter (fun (_, b) -> b.AccountType = Revenue)

                let expenseAccounts =
                    state.AccountBalances
                    |> Map.toList
                    |> List.filter (fun (_, b) -> b.AccountType = Expense)

                let totalIncome =
                    incomeAccounts
                    |> List.sumBy (fun (_, b) -> Money.amount b.ClosingBalance)

                let totalExpenses =
                    expenseAccounts
                    |> List.sumBy (fun (_, b) -> Money.amount b.ClosingBalance)

                let netIncome = Money.yen (totalIncome - totalExpenses)

                let newState = {
                    state with
                        Status = Closed
                        ClosedAt = Some DateTimeOffset.UtcNow
                }

                let event = FiscalYearClosed {
                    Meta = FinancialEventMeta.create state.CompanyId
                           |> FinancialEventMeta.withFiscalYear state.FiscalYearId
                    FiscalYearId = state.FiscalYearId
                    ClosedDate = Date.today()
                    ClosingEntryId = None
                    NetIncome = netIncome
                    RetainedEarningsCarryForward = netIncome
                }

                return (GeneralLedger(newState), event)
            }

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new general ledger
        static member Create
            (companyId: CompanyId)
            (fiscalYearNumber: int)
            (startDate: Date)
            (endDate: Date)
            : Result<GeneralLedger * FinancialEvent, LedgerError> =

            result {
                do! Result.require
                        (Date.isBefore startDate endDate)
                        (InvalidFiscalYear "Start date must be before end date")

                let daysDiff = Date.daysBetween startDate endDate
                do! Result.require
                        (daysDiff <= 366)
                        (InvalidFiscalYear "Fiscal year cannot exceed 366 days")

                let fiscalYearId = FiscalYearId.create()

                let state = LedgerState.create
                                companyId
                                fiscalYearId
                                fiscalYearNumber
                                startDate
                                endDate

                let event = FiscalYearOpened {
                    Meta = FinancialEventMeta.create companyId
                           |> FinancialEventMeta.withFiscalYear fiscalYearId
                    FiscalYearId = fiscalYearId
                    FiscalYearNumber = fiscalYearNumber
                    StartDate = startDate
                    EndDate = endDate
                    PeriodCount = 12
                }

                return (GeneralLedger(state), event)
            }

        /// Reconstitute from state
        static member FromState(state: LedgerState) : GeneralLedger =
            GeneralLedger(state)

    // ============================================
    // Ledger Logic
    // ============================================

    module LedgerLogic =

        /// Calculate account type totals
        let calculateTypeTotal (accountType: AccountType) (balances: AccountBalance list) : Money =
            balances
            |> List.filter (fun b -> b.AccountType = accountType)
            |> List.sumBy (fun b -> Money.amount b.ClosingBalance)
            |> Money.yen

        /// Calculate total assets
        let totalAssets (balances: AccountBalance list) : Money =
            calculateTypeTotal Asset balances

        /// Calculate total liabilities
        let totalLiabilities (balances: AccountBalance list) : Money =
            calculateTypeTotal Liability balances

        /// Calculate total equity
        let totalEquity (balances: AccountBalance list) : Money =
            calculateTypeTotal Equity balances

        /// Calculate total revenue
        let totalRevenue (balances: AccountBalance list) : Money =
            calculateTypeTotal Revenue balances

        /// Calculate total expenses
        let totalExpenses (balances: AccountBalance list) : Money =
            calculateTypeTotal Expense balances

        /// Calculate net income
        let netIncome (balances: AccountBalance list) : Money =
            let revenue = Money.amount (totalRevenue balances)
            let expenses = Money.amount (totalExpenses balances)
            Money.yen (revenue - expenses)

        /// Verify accounting equation: Assets = Liabilities + Equity
        let verifyAccountingEquation (balances: AccountBalance list) : bool =
            let assets = Money.amount (totalAssets balances)
            let liabilities = Money.amount (totalLiabilities balances)
            let equity = Money.amount (totalEquity balances)
            assets = liabilities + equity
