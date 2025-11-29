module CompanyAsCode.Tests.FinancialContextTests

open System
open Xunit
open FsUnit.Xunit
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.Financial
open CompanyAsCode.Financial.Account
open CompanyAsCode.Financial.JournalEntry
open CompanyAsCode.Financial.Tax
open CompanyAsCode.Financial.Invoice
open CompanyAsCode.Financial.Events

// ============================================
// Account Tests
// ============================================

module AccountTests =

    [<Fact>]
    let ``Create account with valid code should succeed`` () =
        let today = Date.today()
        let result = Account.Create "110" "Accounts Receivable" "売掛金" Asset today

        match result with
        | Ok account ->
            account.Name |> should equal "Accounts Receivable"
            account.NameJapanese |> should equal "売掛金"
            account.AccountType |> should equal Asset
            account.NormalBalance |> should equal Debit
            account.IsActive |> should equal true
        | Error _ -> Assert.True(false, "Account creation should succeed")

    [<Fact>]
    let ``Create account with invalid code should fail`` () =
        let today = Date.today()
        let result = Account.Create "AB" "Test" "テスト" Asset today

        match result with
        | Ok _ -> Assert.True(false, "Should fail with invalid code")
        | Error (Errors.InvalidAccountCode _) -> Assert.True(true)
        | Error _ -> Assert.True(false, "Wrong error type")

    [<Fact>]
    let ``Post debit to asset account increases balance`` () =
        let today = Date.today()
        let accountResult = Account.Create "100" "Cash" "現金" Asset today

        match accountResult with
        | Ok account ->
            let amount = Money.yen 10000m
            let postResult = account.PostDebit amount today

            match postResult with
            | Ok updatedAccount ->
                Money.amount updatedAccount.CurrentBalance |> should equal 10000m
            | Error _ -> Assert.True(false, "Post should succeed")
        | Error _ -> Assert.True(false, "Account creation should succeed")

    [<Fact>]
    let ``Post credit to liability account increases balance`` () =
        let today = Date.today()
        let accountResult = Account.Create "200" "Accounts Payable" "買掛金" Liability today

        match accountResult with
        | Ok account ->
            let amount = Money.yen 50000m
            let postResult = account.PostCredit amount today

            match postResult with
            | Ok updatedAccount ->
                Money.amount updatedAccount.CurrentBalance |> should equal 50000m
            | Error _ -> Assert.True(false, "Post should succeed")
        | Error _ -> Assert.True(false, "Account creation should succeed")

    [<Fact>]
    let ``Account code type detection works correctly`` () =
        let assetCode = AccountCode.create "100" |> Result.toOption
        let liabilityCode = AccountCode.create "200" |> Result.toOption
        let equityCode = AccountCode.create "300" |> Result.toOption
        let revenueCode = AccountCode.create "400" |> Result.toOption
        let expenseCode = AccountCode.create "500" |> Result.toOption

        assetCode |> Option.map AccountCode.getAccountType |> should equal (Some (Some Asset))
        liabilityCode |> Option.map AccountCode.getAccountType |> should equal (Some (Some Liability))
        equityCode |> Option.map AccountCode.getAccountType |> should equal (Some (Some Equity))
        revenueCode |> Option.map AccountCode.getAccountType |> should equal (Some (Some Revenue))
        expenseCode |> Option.map AccountCode.getAccountType |> should equal (Some (Some Expense))

// ============================================
// Journal Entry Tests
// ============================================

module JournalEntryTests =

    let createTestFiscalYearId () = FiscalYearId.create()

    [<Fact>]
    let ``Create journal entry with valid data should succeed`` () =
        let fiscalYearId = createTestFiscalYearId()
        let today = Date.today()

        let result = JournalEntry.Create
                        "JE-2024-001"
                        fiscalYearId
                        today
                        "Test entry"
                        "testuser"

        match result with
        | Ok entry ->
            entry.EntryNumber |> should equal "JE-2024-001"
            entry.Description |> should equal "Test entry"
            entry.Status |> should equal EntryStatus.Draft
            entry.Lines |> should be Empty
        | Error _ -> Assert.True(false, "Entry creation should succeed")

    [<Fact>]
    let ``Add line to draft entry should succeed`` () =
        let fiscalYearId = createTestFiscalYearId()
        let today = Date.today()

        let entryResult = JournalEntry.Create
                            "JE-2024-002"
                            fiscalYearId
                            today
                            "Test entry"
                            "testuser"

        match entryResult with
        | Ok entry ->
            let accountId = AccountId.create()
            let line = EntryLine.createDebit accountId "100" "Cash" (Money.yen 10000m)
            let addResult = entry.AddLine line

            match addResult with
            | Ok updatedEntry ->
                updatedEntry.Lines |> List.length |> should equal 1
            | Error _ -> Assert.True(false, "AddLine should succeed")
        | Error _ -> Assert.True(false, "Entry creation should succeed")

    [<Fact>]
    let ``Balanced entry should report as balanced`` () =
        let fiscalYearId = createTestFiscalYearId()
        let today = Date.today()
        let debitAccountId = AccountId.create()
        let creditAccountId = AccountId.create()
        let amount = Money.yen 10000m

        let lines = [
            EntryLine.createDebit debitAccountId "100" "Cash" amount
            EntryLine.createCredit creditAccountId "200" "Payables" amount
        ]

        let result = JournalEntry.CreateWithLines
                        "JE-2024-003"
                        fiscalYearId
                        today
                        "Balanced entry"
                        lines
                        "testuser"

        match result with
        | Ok entry ->
            entry.IsBalanced |> should equal true
            Money.amount entry.TotalDebits |> should equal 10000m
            Money.amount entry.TotalCredits |> should equal 10000m
        | Error _ -> Assert.True(false, "Entry creation should succeed")

    [<Fact>]
    let ``Unbalanced entry creation should fail`` () =
        let fiscalYearId = createTestFiscalYearId()
        let today = Date.today()
        let debitAccountId = AccountId.create()
        let creditAccountId = AccountId.create()

        let lines = [
            EntryLine.createDebit debitAccountId "100" "Cash" (Money.yen 10000m)
            EntryLine.createCredit creditAccountId "200" "Payables" (Money.yen 5000m)
        ]

        let result = JournalEntry.CreateWithLines
                        "JE-2024-004"
                        fiscalYearId
                        today
                        "Unbalanced entry"
                        lines
                        "testuser"

        match result with
        | Ok _ -> Assert.True(false, "Should fail for unbalanced entry")
        | Error (Errors.UnbalancedEntry _) -> Assert.True(true)
        | Error _ -> Assert.True(false, "Wrong error type")

// ============================================
// Tax Calculation Tests
// ============================================

module TaxTests =

    [<Fact>]
    let ``Corporate tax calculation for SME should use reduced rate`` () =
        let taxableIncome = TaxableIncome.calculate (Money.yen 5_000_000m) (Money.yen 0m)
        let calculation = CorporateTaxCalculation.calculate taxableIncome

        // SME rate is 15% for income <= ¥8M
        // National tax = 5,000,000 * 15% = 750,000
        Money.amount calculation.NationalTax |> should equal 750_000m

    [<Fact>]
    let ``Corporate tax calculation for large income should use progressive rates`` () =
        let taxableIncome = TaxableIncome.calculate (Money.yen 20_000_000m) (Money.yen 0m)
        let calculation = CorporateTaxCalculation.calculate taxableIncome

        // First ¥8M at 15% = 1,200,000
        // Remaining ¥12M at 23.2% = 2,784,000
        // Total = 3,984,000
        let expectedNational = 8_000_000m * 0.15m + 12_000_000m * 0.232m
        Money.amount calculation.NationalTax |> should equal expectedNational

    [<Fact>]
    let ``Consumption tax calculation should compute net payable`` () =
        let sales = Money.yen 1_000_000m
        let purchases = Money.yen 600_000m

        let calculation = ConsumptionTaxCalculation.calculate sales purchases 10.0m

        // Output tax = 1,000,000 * 10% = 100,000
        // Input tax = 600,000 * 10% = 60,000
        // Net = 40,000
        Money.amount calculation.OutputTax |> should equal 100_000m
        Money.amount calculation.InputTax |> should equal 60_000m
        Money.amount calculation.NetTaxPayable |> should equal 40_000m

    [<Fact>]
    let ``Consumption tax with excess input should result in credit`` () =
        let sales = Money.yen 500_000m
        let purchases = Money.yen 800_000m

        let calculation = ConsumptionTaxCalculation.calculate sales purchases 10.0m

        // Output tax = 50,000
        // Input tax = 80,000
        // Net = -30,000 (credit)
        Money.amount calculation.NetTaxPayable |> should equal 0m
        Money.amount calculation.TaxCredit |> should equal 30_000m

    [<Fact>]
    let ``Withholding tax on dividends should be calculated correctly`` () =
        let grossDividend = Money.yen 100_000m
        let withholding = TaxLogic.dividendWithholdingTax grossDividend

        // 20.42% rate
        Money.amount withholding |> should equal 20420m

    [<Fact>]
    let ``Professional fee withholding under 1M should use lower rate`` () =
        let fee = Money.yen 500_000m
        let withholding = TaxLogic.professionalFeeWithholdingTax fee

        // 10.21% for fees <= ¥1M
        Money.amount withholding |> should equal 51050m

    [<Fact>]
    let ``Professional fee withholding over 1M should use progressive rates`` () =
        let fee = Money.yen 2_000_000m
        let withholding = TaxLogic.professionalFeeWithholdingTax fee

        // First ¥1M at 10.21% = 102,100
        // Remaining ¥1M at 20.42% = 204,200
        // Total = 306,300
        let expectedWithholding = 1_000_000m * 0.1021m + 1_000_000m * 0.2042m
        Money.amount withholding |> should equal expectedWithholding

// ============================================
// Invoice Tests
// ============================================

module InvoiceTests =

    [<Fact>]
    let ``Create qualified invoice number with valid format should succeed`` () =
        let result = QualifiedInvoiceNumber.create "T1234567890123"

        match result with
        | Ok qin ->
            QualifiedInvoiceNumber.value qin |> should equal "T1234567890123"
        | Error _ -> Assert.True(false, "Should succeed with valid format")

    [<Fact>]
    let ``Qualified invoice number without T prefix should fail`` () =
        let result = QualifiedInvoiceNumber.create "1234567890123"

        match result with
        | Ok _ -> Assert.True(false, "Should fail without T prefix")
        | Error msg ->
            Assert.True(msg.Contains("T"), "Error should mention T prefix")

    [<Fact>]
    let ``Invoice line item tax calculation should be correct`` () =
        let lineItem = InvoiceLineItem.create
                          1
                          "Consulting service"
                          1.0m
                          (Money.yen 100_000m)
                          TaxRateCategory.Standard

        // Standard rate is 10%
        Money.amount lineItem.TaxAmount |> should equal 10_000m
        Money.amount lineItem.TotalAmount |> should equal 110_000m

    [<Fact>]
    let ``Invoice line item with reduced rate should calculate correctly`` () =
        let lineItem = InvoiceLineItem.create
                          1
                          "Food items"
                          1.0m
                          (Money.yen 50_000m)
                          TaxRateCategory.Reduced

        // Reduced rate is 8%
        Money.amount lineItem.TaxAmount |> should equal 4_000m
        Money.amount lineItem.TotalAmount |> should equal 54_000m

    [<Fact>]
    let ``Invoice line item with exempt category should have zero tax`` () =
        let lineItem = InvoiceLineItem.create
                          1
                          "Insurance"
                          1.0m
                          (Money.yen 30_000m)
                          TaxRateCategory.Exempt

        Money.amount lineItem.TaxAmount |> should equal 0m
        Money.amount lineItem.TotalAmount |> should equal 30_000m

    [<Fact>]
    let ``Create sales invoice with valid data should succeed`` () =
        let companyId = CompanyId.create()
        let today = Date.today()
        let dueDate = Date.addDays 30 today
        let qinResult = QualifiedInvoiceNumber.create "T1234567890123"

        match qinResult with
        | Ok qin ->
            let invoiceResult = Invoice.CreateSalesInvoice
                                    companyId
                                    "INV-001"
                                    "CUST-001"
                                    "Customer A"
                                    today
                                    dueDate
                                    (Some qin)

            match invoiceResult with
            | Ok invoice ->
                invoice.InvoiceNumber |> should equal "INV-001"
                invoice.CounterpartyName |> should equal "Customer A"
                invoice.Status |> should equal InvoiceStatus.Draft
                invoice.QualifiedInvoiceNumber.IsSome |> should equal true
                Money.amount invoice.SubTotal |> should equal 0m
                Money.amount invoice.TotalTaxAmount |> should equal 0m
                Money.amount invoice.TotalAmount |> should equal 0m
            | Error e -> Assert.True(false, $"Invoice creation should succeed: {e}")
        | Error e -> Assert.True(false, $"QIN creation should succeed: {e}")

    [<Fact>]
    let ``Add line item to invoice and issue should work`` () =
        let companyId = CompanyId.create()
        let today = Date.today()
        let dueDate = Date.addDays 30 today
        let qinResult = QualifiedInvoiceNumber.create "T1234567890123"

        match qinResult with
        | Ok qin ->
            let invoiceResult = Invoice.CreateSalesInvoice
                                    companyId
                                    "INV-002"
                                    "CUST-002"
                                    "Customer B"
                                    today
                                    dueDate
                                    (Some qin)

            match invoiceResult with
            | Ok invoice ->
                // Add a line item
                let lineItem = InvoiceLineItem.create 1 "Test item" 2.0m (Money.yen 5_000m) TaxRateCategory.Standard
                let withItemResult = invoice.AddLineItem lineItem

                match withItemResult with
                | Ok invoiceWithItem ->
                    // Verify totals (2 * 5000 = 10000 + 1000 tax = 11000)
                    Money.amount invoiceWithItem.SubTotal |> should equal 10_000m
                    Money.amount invoiceWithItem.TotalTaxAmount |> should equal 1_000m
                    Money.amount invoiceWithItem.TotalAmount |> should equal 11_000m

                    // Issue the invoice
                    let issueResult = invoiceWithItem.Issue()
                    match issueResult with
                    | Ok (issuedInvoice, _) ->
                        issuedInvoice.Status |> should equal InvoiceStatus.Issued
                    | Error e -> Assert.True(false, $"Issue should succeed: {e}")
                | Error e -> Assert.True(false, $"AddLineItem should succeed: {e}")
            | Error e -> Assert.True(false, $"Invoice creation should succeed: {e}")
        | Error e -> Assert.True(false, $"QIN creation should succeed: {e}")
