namespace CompanyAsCode.Financial

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Account entity and Chart of Accounts
module Account =

    open Events
    open Errors

    // ============================================
    // Account Code (Value Object)
    // ============================================

    /// Account code - follows Japanese standard chart of accounts
    type AccountCode = private AccountCode of string

    module AccountCode =

        /// Japanese standard account code ranges:
        /// 1xx: Assets (資産)
        /// 2xx: Liabilities (負債)
        /// 3xx: Equity (純資産)
        /// 4xx: Revenue (収益)
        /// 5xx: Cost of Sales (売上原価)
        /// 6xx: Selling & Admin Expenses (販売費及び一般管理費)
        /// 7xx: Non-operating Income/Expenses (営業外収益・費用)
        /// 8xx: Extraordinary Items (特別損益)

        let create (code: string) : Result<AccountCode, string> =
            if System.String.IsNullOrWhiteSpace(code) then
                Error "Account code cannot be empty"
            elif code.Length < 3 || code.Length > 10 then
                Error "Account code must be 3-10 characters"
            elif not (code |> Seq.forall System.Char.IsLetterOrDigit) then
                Error "Account code must contain only letters and digits"
            else
                Ok (AccountCode code)

        let value (AccountCode code) = code

        let getAccountType (AccountCode code) : AccountType option =
            if code.Length >= 1 then
                match code.[0] with
                | '1' -> Some Asset
                | '2' -> Some Liability
                | '3' -> Some Equity
                | '4' -> Some Revenue
                | '5' | '6' | '7' | '8' -> Some Expense
                | _ -> None
            else
                None

    // ============================================
    // Account State
    // ============================================

    /// Account status
    type AccountStatus =
        | Active
        | Inactive
        | Suspended

    /// Account state (immutable)
    type AccountState = {
        Id: AccountId
        Code: AccountCode
        Name: string
        NameJapanese: string
        AccountType: AccountType
        SubType: AccountSubType option
        NormalBalance: DebitCredit
        ParentAccountId: AccountId option
        IsControlAccount: bool
        Status: AccountStatus
        CurrentBalance: Money
        CreatedDate: Date
        LastActivityDate: Date option
    }

    module AccountState =

        let create
            (id: AccountId)
            (code: AccountCode)
            (name: string)
            (nameJapanese: string)
            (accountType: AccountType)
            (createdDate: Date)
            : AccountState =

            let normalBalance =
                match accountType with
                | Asset | Expense -> Debit
                | Liability | Equity | Revenue -> Credit

            {
                Id = id
                Code = code
                Name = name
                NameJapanese = nameJapanese
                AccountType = accountType
                SubType = None
                NormalBalance = normalBalance
                ParentAccountId = None
                IsControlAccount = false
                Status = Active
                CurrentBalance = Money.yen 0m
                CreatedDate = createdDate
                LastActivityDate = None
            }

        let isActive (state: AccountState) =
            state.Status = Active

        let hasBalance (state: AccountState) =
            not (Money.isZero state.CurrentBalance)

    // ============================================
    // Account Entity
    // ============================================

    /// Account entity
    type Account private (state: AccountState) =

        member _.State = state
        member _.Id = state.Id
        member _.Code = state.Code
        member _.Name = state.Name
        member _.NameJapanese = state.NameJapanese
        member _.AccountType = state.AccountType
        member _.SubType = state.SubType
        member _.NormalBalance = state.NormalBalance
        member _.IsControlAccount = state.IsControlAccount
        member _.Status = state.Status
        member _.CurrentBalance = state.CurrentBalance
        member _.IsActive = AccountState.isActive state
        member _.HasBalance = AccountState.hasBalance state

        // ============================================
        // Commands
        // ============================================

        /// Post a debit to the account
        member this.PostDebit(amount: Money) (postingDate: Date)
            : Result<Account, AccountError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        (AccountNotActive (AccountCode.value state.Code))

                do! Result.require
                        (Money.isPositive amount || Money.isZero amount)
                        (InvalidAccountCode "Debit amount must be non-negative")

                let newBalance =
                    match state.NormalBalance with
                    | Debit ->
                        Money.add state.CurrentBalance amount
                        |> Result.defaultValue state.CurrentBalance
                    | Credit ->
                        Money.subtract state.CurrentBalance amount
                        |> Result.defaultValue state.CurrentBalance

                return Account({
                    state with
                        CurrentBalance = newBalance
                        LastActivityDate = Some postingDate
                })
            }

        /// Post a credit to the account
        member this.PostCredit(amount: Money) (postingDate: Date)
            : Result<Account, AccountError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        (AccountNotActive (AccountCode.value state.Code))

                do! Result.require
                        (Money.isPositive amount || Money.isZero amount)
                        (InvalidAccountCode "Credit amount must be non-negative")

                let newBalance =
                    match state.NormalBalance with
                    | Credit ->
                        Money.add state.CurrentBalance amount
                        |> Result.defaultValue state.CurrentBalance
                    | Debit ->
                        Money.subtract state.CurrentBalance amount
                        |> Result.defaultValue state.CurrentBalance

                return Account({
                    state with
                        CurrentBalance = newBalance
                        LastActivityDate = Some postingDate
                })
            }

        /// Deactivate the account
        member this.Deactivate()
            : Result<Account, AccountError> =

            result {
                do! Result.require
                        (not (AccountState.hasBalance state))
                        (CannotDeleteAccountWithBalance (AccountCode.value state.Code, state.CurrentBalance))

                return Account({ state with Status = Inactive })
            }

        /// Reactivate the account
        member this.Reactivate()
            : Account =
            Account({ state with Status = Active })

        /// Set parent account (for sub-accounts)
        member this.SetParentAccount(parentId: AccountId)
            : Account =
            Account({ state with ParentAccountId = Some parentId })

        /// Set sub-type
        member this.SetSubType(subType: AccountSubType)
            : Account =
            Account({ state with SubType = Some subType })

        /// Mark as control account
        member this.MarkAsControlAccount()
            : Account =
            Account({ state with IsControlAccount = true })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new account
        static member Create
            (code: string)
            (name: string)
            (nameJapanese: string)
            (accountType: AccountType)
            (createdDate: Date)
            : Result<Account, AccountError> =

            result {
                let! accountCode =
                    AccountCode.create code
                    |> Result.mapError InvalidAccountCode

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidAccountName "Account name cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(nameJapanese)))
                        (InvalidAccountName "Japanese account name cannot be empty")

                let id = AccountId.create()
                let state = AccountState.create id accountCode name nameJapanese accountType createdDate

                return Account(state)
            }

        /// Reconstitute from state
        static member FromState(state: AccountState) : Account =
            Account(state)

    // ============================================
    // Standard Japanese Chart of Accounts
    // ============================================

    module StandardAccounts =

        /// Standard account definitions for Japanese companies
        type StandardAccountDef = {
            Code: string
            Name: string
            NameJapanese: string
            Type: AccountType
            SubType: AccountSubType
        }

        /// Assets (資産)
        let assets = [
            { Code = "100"; Name = "Cash and Cash Equivalents"; NameJapanese = "現金及び預金"; Type = Asset; SubType = CurrentAsset }
            { Code = "101"; Name = "Cash on Hand"; NameJapanese = "現金"; Type = Asset; SubType = CurrentAsset }
            { Code = "102"; Name = "Bank Deposits"; NameJapanese = "預金"; Type = Asset; SubType = CurrentAsset }
            { Code = "110"; Name = "Accounts Receivable"; NameJapanese = "売掛金"; Type = Asset; SubType = CurrentAsset }
            { Code = "111"; Name = "Notes Receivable"; NameJapanese = "受取手形"; Type = Asset; SubType = CurrentAsset }
            { Code = "120"; Name = "Inventory"; NameJapanese = "棚卸資産"; Type = Asset; SubType = CurrentAsset }
            { Code = "130"; Name = "Prepaid Expenses"; NameJapanese = "前払費用"; Type = Asset; SubType = CurrentAsset }
            { Code = "140"; Name = "Accrued Revenue"; NameJapanese = "未収収益"; Type = Asset; SubType = CurrentAsset }
            { Code = "150"; Name = "Buildings"; NameJapanese = "建物"; Type = Asset; SubType = FixedAsset }
            { Code = "151"; Name = "Accumulated Depreciation - Buildings"; NameJapanese = "建物減価償却累計額"; Type = Asset; SubType = FixedAsset }
            { Code = "160"; Name = "Equipment"; NameJapanese = "器具備品"; Type = Asset; SubType = FixedAsset }
            { Code = "161"; Name = "Accumulated Depreciation - Equipment"; NameJapanese = "器具備品減価償却累計額"; Type = Asset; SubType = FixedAsset }
            { Code = "170"; Name = "Land"; NameJapanese = "土地"; Type = Asset; SubType = FixedAsset }
            { Code = "180"; Name = "Intangible Assets"; NameJapanese = "無形固定資産"; Type = Asset; SubType = FixedAsset }
            { Code = "190"; Name = "Deferred Assets"; NameJapanese = "繰延資産"; Type = Asset; SubType = DeferredAsset }
        ]

        /// Liabilities (負債)
        let liabilities = [
            { Code = "200"; Name = "Accounts Payable"; NameJapanese = "買掛金"; Type = Liability; SubType = CurrentLiability }
            { Code = "201"; Name = "Notes Payable"; NameJapanese = "支払手形"; Type = Liability; SubType = CurrentLiability }
            { Code = "210"; Name = "Short-term Borrowings"; NameJapanese = "短期借入金"; Type = Liability; SubType = CurrentLiability }
            { Code = "220"; Name = "Accrued Expenses"; NameJapanese = "未払費用"; Type = Liability; SubType = CurrentLiability }
            { Code = "221"; Name = "Accrued Salaries"; NameJapanese = "未払給与"; Type = Liability; SubType = CurrentLiability }
            { Code = "230"; Name = "Unearned Revenue"; NameJapanese = "前受収益"; Type = Liability; SubType = CurrentLiability }
            { Code = "240"; Name = "Consumption Tax Payable"; NameJapanese = "未払消費税等"; Type = Liability; SubType = CurrentLiability }
            { Code = "241"; Name = "Corporate Tax Payable"; NameJapanese = "未払法人税等"; Type = Liability; SubType = CurrentLiability }
            { Code = "250"; Name = "Long-term Borrowings"; NameJapanese = "長期借入金"; Type = Liability; SubType = LongTermLiability }
            { Code = "260"; Name = "Provision for Retirement Benefits"; NameJapanese = "退職給付引当金"; Type = Liability; SubType = LongTermLiability }
        ]

        /// Equity (純資産)
        let equity = [
            { Code = "300"; Name = "Share Capital"; NameJapanese = "資本金"; Type = Equity; SubType = ShareCapital }
            { Code = "310"; Name = "Capital Surplus"; NameJapanese = "資本剰余金"; Type = Equity; SubType = Reserves }
            { Code = "311"; Name = "Legal Capital Surplus"; NameJapanese = "資本準備金"; Type = Equity; SubType = Reserves }
            { Code = "312"; Name = "Other Capital Surplus"; NameJapanese = "その他資本剰余金"; Type = Equity; SubType = Reserves }
            { Code = "320"; Name = "Retained Earnings"; NameJapanese = "利益剰余金"; Type = Equity; SubType = RetainedEarnings }
            { Code = "321"; Name = "Legal Retained Earnings"; NameJapanese = "利益準備金"; Type = Equity; SubType = Reserves }
            { Code = "322"; Name = "Other Retained Earnings"; NameJapanese = "その他利益剰余金"; Type = Equity; SubType = RetainedEarnings }
            { Code = "330"; Name = "Treasury Stock"; NameJapanese = "自己株式"; Type = Equity; SubType = ShareCapital }
        ]

        /// Revenue (収益)
        let revenue = [
            { Code = "400"; Name = "Sales"; NameJapanese = "売上高"; Type = Revenue; SubType = OperatingRevenue }
            { Code = "410"; Name = "Service Revenue"; NameJapanese = "サービス収入"; Type = Revenue; SubType = OperatingRevenue }
            { Code = "420"; Name = "Interest Income"; NameJapanese = "受取利息"; Type = Revenue; SubType = NonOperatingRevenue }
            { Code = "421"; Name = "Dividend Income"; NameJapanese = "受取配当金"; Type = Revenue; SubType = NonOperatingRevenue }
            { Code = "430"; Name = "Foreign Exchange Gain"; NameJapanese = "為替差益"; Type = Revenue; SubType = NonOperatingRevenue }
            { Code = "440"; Name = "Gain on Sale of Fixed Assets"; NameJapanese = "固定資産売却益"; Type = Revenue; SubType = ExtraordinaryIncome }
        ]

        /// Expenses (費用)
        let expenses = [
            { Code = "500"; Name = "Cost of Goods Sold"; NameJapanese = "売上原価"; Type = Expense; SubType = CostOfSales }
            { Code = "510"; Name = "Beginning Inventory"; NameJapanese = "期首商品棚卸高"; Type = Expense; SubType = CostOfSales }
            { Code = "520"; Name = "Purchases"; NameJapanese = "仕入高"; Type = Expense; SubType = CostOfSales }
            { Code = "600"; Name = "Selling Expenses"; NameJapanese = "販売費"; Type = Expense; SubType = OperatingExpense }
            { Code = "610"; Name = "Salaries and Wages"; NameJapanese = "給料手当"; Type = Expense; SubType = OperatingExpense }
            { Code = "611"; Name = "Bonus"; NameJapanese = "賞与"; Type = Expense; SubType = OperatingExpense }
            { Code = "612"; Name = "Legal Welfare Expenses"; NameJapanese = "法定福利費"; Type = Expense; SubType = OperatingExpense }
            { Code = "620"; Name = "Rent Expense"; NameJapanese = "地代家賃"; Type = Expense; SubType = OperatingExpense }
            { Code = "630"; Name = "Depreciation Expense"; NameJapanese = "減価償却費"; Type = Expense; SubType = OperatingExpense }
            { Code = "640"; Name = "Utilities Expense"; NameJapanese = "水道光熱費"; Type = Expense; SubType = OperatingExpense }
            { Code = "650"; Name = "Communication Expense"; NameJapanese = "通信費"; Type = Expense; SubType = OperatingExpense }
            { Code = "660"; Name = "Travel Expense"; NameJapanese = "旅費交通費"; Type = Expense; SubType = OperatingExpense }
            { Code = "670"; Name = "Entertainment Expense"; NameJapanese = "接待交際費"; Type = Expense; SubType = OperatingExpense }
            { Code = "680"; Name = "Professional Fees"; NameJapanese = "支払報酬"; Type = Expense; SubType = OperatingExpense }
            { Code = "700"; Name = "Interest Expense"; NameJapanese = "支払利息"; Type = Expense; SubType = NonOperatingExpense }
            { Code = "710"; Name = "Foreign Exchange Loss"; NameJapanese = "為替差損"; Type = Expense; SubType = NonOperatingExpense }
            { Code = "800"; Name = "Loss on Sale of Fixed Assets"; NameJapanese = "固定資産売却損"; Type = Expense; SubType = ExtraordinaryLoss }
            { Code = "810"; Name = "Corporate Tax"; NameJapanese = "法人税等"; Type = Expense; SubType = ExtraordinaryLoss }
        ]

        /// All standard accounts
        let all =
            assets @ liabilities @ equity @ revenue @ expenses

    // ============================================
    // Account Logic
    // ============================================

    module AccountLogic =

        /// Determine if account is a balance sheet account
        let isBalanceSheetAccount (accountType: AccountType) : bool =
            match accountType with
            | Asset | Liability | Equity -> true
            | Revenue | Expense -> false

        /// Determine if account is an income statement account
        let isIncomeStatementAccount (accountType: AccountType) : bool =
            match accountType with
            | Revenue | Expense -> true
            | Asset | Liability | Equity -> false

        /// Get normal balance for account type
        let getNormalBalance (accountType: AccountType) : DebitCredit =
            match accountType with
            | Asset | Expense -> Debit
            | Liability | Equity | Revenue -> Credit

        /// Calculate balance direction
        let calculateBalanceImpact
            (normalBalance: DebitCredit)
            (debitCredit: DebitCredit)
            (amount: Money)
            : Money =

            match normalBalance, debitCredit with
            | Debit, Debit -> amount
            | Debit, Credit -> Money.negate amount
            | Credit, Credit -> amount
            | Credit, Debit -> Money.negate amount
