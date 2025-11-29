namespace CompanyAsCode.SharedKernel

open System

/// Financial value objects for monetary calculations
module Financial =

    // ============================================
    // Currency (通貨)
    // ============================================

    /// Supported currencies
    type Currency =
        | JPY   // Japanese Yen (primary)
        | USD   // US Dollar
        | EUR   // Euro
        | GBP   // British Pound
        | CNY   // Chinese Yuan

    module Currency =

        let toCode = function
            | JPY -> "JPY"
            | USD -> "USD"
            | EUR -> "EUR"
            | GBP -> "GBP"
            | CNY -> "CNY"

        let toSymbol = function
            | JPY -> "¥"
            | USD -> "$"
            | EUR -> "€"
            | GBP -> "£"
            | CNY -> "¥"

        /// Decimal places for currency (JPY has 0)
        let decimalPlaces = function
            | JPY -> 0
            | USD -> 2
            | EUR -> 2
            | GBP -> 2
            | CNY -> 2

        let fromCode (code: string) : Currency option =
            match code.ToUpperInvariant() with
            | "JPY" -> Some JPY
            | "USD" -> Some USD
            | "EUR" -> Some EUR
            | "GBP" -> Some GBP
            | "CNY" -> Some CNY
            | _ -> None

    // ============================================
    // Money (金額)
    // ============================================

    /// Represents a monetary amount with currency
    [<Struct>]
    type Money = private {
        _amount: decimal
        _currency: Currency
    }

    module Money =

        /// Create money with validation
        let create (amount: decimal) (currency: Currency) : Result<Money, string> =
            let decimalPlaces = Currency.decimalPlaces currency
            let scale = decimal (pown 10 decimalPlaces)
            let rounded = Math.Round(amount * scale) / scale

            if rounded <> amount then
                Error $"Amount {amount} exceeds precision for {Currency.toCode currency}"
            else
                Ok { _amount = amount; _currency = currency }

        /// Create money, rounding to currency precision
        let createRounded (amount: decimal) (currency: Currency) : Money =
            let decimalPlaces = Currency.decimalPlaces currency
            let scale = decimal (pown 10 decimalPlaces)
            let rounded = Math.Round(amount * scale) / scale
            { _amount = rounded; _currency = currency }

        /// Create Japanese Yen
        let yen (amount: decimal) : Money =
            { _amount = Math.Round(amount); _currency = JPY }

        /// Create from integer yen
        let yenInt (amount: int) : Money =
            { _amount = decimal amount; _currency = JPY }

        let amount (m: Money) = m._amount
        let currency (m: Money) = m._currency

        let zero (currency: Currency) : Money =
            { _amount = 0m; _currency = currency }

        let isZero (m: Money) = m._amount = 0m
        let isPositive (m: Money) = m._amount > 0m
        let isNegative (m: Money) = m._amount < 0m

        /// Add two money values (must be same currency)
        let add (m1: Money) (m2: Money) : Result<Money, string> =
            if m1._currency <> m2._currency then
                Error $"Cannot add {Currency.toCode m1._currency} to {Currency.toCode m2._currency}"
            else
                Ok { m1 with _amount = m1._amount + m2._amount }

        /// Subtract money (must be same currency)
        let subtract (m1: Money) (m2: Money) : Result<Money, string> =
            if m1._currency <> m2._currency then
                Error $"Cannot subtract {Currency.toCode m2._currency} from {Currency.toCode m1._currency}"
            else
                Ok { m1 with _amount = m1._amount - m2._amount }

        /// Multiply by scalar
        let multiply (factor: decimal) (m: Money) : Money =
            createRounded (m._amount * factor) m._currency

        /// Divide by scalar
        let divide (divisor: decimal) (m: Money) : Result<Money, string> =
            if divisor = 0m then
                Error "Cannot divide by zero"
            else
                Ok (createRounded (m._amount / divisor) m._currency)

        /// Negate amount
        let negate (m: Money) : Money =
            { m with _amount = -m._amount }

        /// Absolute value
        let abs (m: Money) : Money =
            { m with _amount = Math.Abs(m._amount) }

        /// Format for display
        let format (m: Money) : string =
            let symbol = Currency.toSymbol m._currency
            let decimalPlaces = Currency.decimalPlaces m._currency
            let formatStr = if decimalPlaces = 0 then "N0" else $"N{decimalPlaces}"
            $"{symbol}{m._amount.ToString(formatStr)}"

        /// Format with currency code
        let formatWithCode (m: Money) : string =
            let code = Currency.toCode m._currency
            let decimalPlaces = Currency.decimalPlaces m._currency
            let formatStr = if decimalPlaces = 0 then "N0" else $"N{decimalPlaces}"
            $"{m._amount.ToString(formatStr)} {code}"

    // ============================================
    // Percentage (パーセント)
    // ============================================

    /// Percentage value object (stored as decimal, 100% = 1.0)
    type Percentage = private Percentage of decimal

    module Percentage =

        /// Create from decimal (0.1 = 10%)
        let fromDecimal (value: decimal) : Percentage =
            Percentage value

        /// Create from percentage value (10 = 10%)
        let fromPercent (value: decimal) : Percentage =
            Percentage (value / 100m)

        let value (Percentage v) = v

        let toPercent (Percentage v) = v * 100m

        let zero = Percentage 0m
        let hundred = Percentage 1m

        /// Apply percentage to money
        let applyTo (m: Money) (Percentage p) : Money =
            Money.multiply p m

        /// Format as percentage
        let format (Percentage v) : string =
            $"{(v * 100m):N2}%%"

    // ============================================
    // Tax Rate (税率)
    // ============================================

    /// Japanese tax categories
    type TaxCategory =
        | Standard          // 標準税率 (10%)
        | Reduced           // 軽減税率 (8%)
        | Exempt            // 非課税
        | OutOfScope        // 不課税

    /// Tax rate with category
    type TaxRate = {
        Category: TaxCategory
        Rate: Percentage
    }

    module TaxRate =

        /// Standard consumption tax rate (10%)
        let standardConsumptionTax : TaxRate = {
            Category = Standard
            Rate = Percentage.fromPercent 10m
        }

        /// Reduced consumption tax rate (8%)
        let reducedConsumptionTax : TaxRate = {
            Category = Reduced
            Rate = Percentage.fromPercent 8m
        }

        /// Tax exempt
        let exempt : TaxRate = {
            Category = Exempt
            Rate = Percentage.zero
        }

        /// Out of scope (not subject to tax)
        let outOfScope : TaxRate = {
            Category = OutOfScope
            Rate = Percentage.zero
        }

        /// Calculate tax amount
        let calculateTax (amount: Money) (rate: TaxRate) : Money =
            Percentage.applyTo amount rate.Rate

        /// Calculate amount including tax
        let withTax (amount: Money) (rate: TaxRate) : Result<Money, string> =
            let tax = calculateTax amount rate
            Money.add amount tax

        /// Format tax rate for display
        let format (rate: TaxRate) : string =
            match rate.Category with
            | Exempt -> "非課税"
            | OutOfScope -> "不課税"
            | _ -> Percentage.format rate.Rate

    // ============================================
    // Capital (資本金)
    // ============================================

    /// Registered capital amount
    type RegisteredCapital = private RegisteredCapital of Money

    module RegisteredCapital =

        let create (money: Money) : Result<RegisteredCapital, string> =
            if Money.currency money <> JPY then
                Error "Registered capital must be in Japanese Yen"
            elif Money.isNegative money then
                Error "Registered capital cannot be negative"
            else
                Ok (RegisteredCapital money)

        let value (RegisteredCapital m) = m

        let amount (RegisteredCapital m) = Money.amount m

        /// Check if capital meets minimum for entity type
        let meetsMinimum (entityType: Japanese.EntityType) (RegisteredCapital m) : bool =
            Money.amount m >= Japanese.EntityType.minimumCapital entityType

        /// Check if capital meets recommended amount
        let meetsRecommended (entityType: Japanese.EntityType) (RegisteredCapital m) : bool =
            Money.amount m >= Japanese.EntityType.recommendedCapital entityType

    // ============================================
    // Share Value (株式価値)
    // ============================================

    /// Number of shares
    type ShareCount = private ShareCount of int64

    module ShareCount =

        let create (count: int64) : Result<ShareCount, string> =
            if count < 0L then
                Error "Share count cannot be negative"
            else
                Ok (ShareCount count)

        let value (ShareCount c) = c

        let zero = ShareCount 0L

        let add (ShareCount a) (ShareCount b) = ShareCount (a + b)

        let subtract (ShareCount a) (ShareCount b) : Result<ShareCount, string> =
            if a < b then
                Error "Cannot have negative shares"
            else
                Ok (ShareCount (a - b))

    /// Par value per share (額面)
    type ParValue = private ParValue of Money

    module ParValue =

        let create (money: Money) : Result<ParValue, string> =
            if Money.currency money <> JPY then
                Error "Par value must be in Japanese Yen"
            elif not (Money.isPositive money) then
                Error "Par value must be positive"
            else
                Ok (ParValue money)

        let value (ParValue m) = m

        /// Calculate total value of shares
        let totalValue (ParValue pv) (ShareCount count) : Money =
            Money.multiply (decimal count) pv

    // ============================================
    // Bank Account (銀行口座)
    // ============================================

    /// Bank account number
    type BankAccountNumber = private BankAccountNumber of string

    module BankAccountNumber =

        let create (value: string) : Result<BankAccountNumber, string> =
            let cleaned = value.Replace("-", "").Replace(" ", "")
            if String.IsNullOrWhiteSpace(cleaned) then
                Error "Bank account number cannot be empty"
            elif cleaned.Length < 4 || cleaned.Length > 10 then
                Error "Bank account number must be 4-10 digits"
            elif not (cleaned |> Seq.forall Char.IsDigit) then
                Error "Bank account number must contain only digits"
            else
                Ok (BankAccountNumber cleaned)

        let value (BankAccountNumber v) = v

    /// Bank branch code (支店コード)
    type BranchCode = private BranchCode of string

    module BranchCode =

        let create (value: string) : Result<BranchCode, string> =
            let cleaned = value.Replace("-", "").Replace(" ", "")
            if cleaned.Length <> 3 then
                Error "Branch code must be 3 digits"
            elif not (cleaned |> Seq.forall Char.IsDigit) then
                Error "Branch code must contain only digits"
            else
                Ok (BranchCode cleaned)

        let value (BranchCode v) = v

    /// Bank code (銀行コード)
    type BankCode = private BankCode of string

    module BankCode =

        let create (value: string) : Result<BankCode, string> =
            let cleaned = value.Replace("-", "").Replace(" ", "")
            if cleaned.Length <> 4 then
                Error "Bank code must be 4 digits"
            elif not (cleaned |> Seq.forall Char.IsDigit) then
                Error "Bank code must contain only digits"
            else
                Ok (BankCode cleaned)

        let value (BankCode v) = v

    /// Account type
    type AccountType =
        | Ordinary      // 普通預金
        | Checking      // 当座預金
        | Savings       // 貯蓄預金
        | TimeDeposit   // 定期預金

    /// Complete bank account information
    type BankAccount = {
        BankCode: BankCode
        BankName: string
        BranchCode: BranchCode
        BranchName: string
        AccountType: AccountType
        AccountNumber: BankAccountNumber
        AccountHolder: string
    }
