namespace CompanyAsCode.Financial

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Invoice aggregate (Qualified Invoice System - インボイス制度)
module Invoice =

    open Events
    open Errors

    // ============================================
    // Invoice Number (Value Object)
    // ============================================

    /// Qualified invoice registration number (T + 13 digits)
    type QualifiedInvoiceNumber = private QualifiedInvoiceNumber of string

    module QualifiedInvoiceNumber =

        /// Format: T + 13-digit corporate number
        let create (number: string) : Result<QualifiedInvoiceNumber, string> =
            let cleaned = number.Trim().ToUpperInvariant()
            if System.String.IsNullOrWhiteSpace(cleaned) then
                Error "Invoice number cannot be empty"
            elif not (cleaned.StartsWith("T")) then
                Error "Qualified invoice number must start with 'T'"
            elif cleaned.Length <> 14 then
                Error "Qualified invoice number must be 14 characters (T + 13 digits)"
            elif not (cleaned.Substring(1) |> Seq.forall System.Char.IsDigit) then
                Error "Characters after 'T' must be digits"
            else
                Ok (QualifiedInvoiceNumber cleaned)

        let value (QualifiedInvoiceNumber n) = n

        /// Create from corporate number
        let fromCorporateNumber (corpNum: string) : Result<QualifiedInvoiceNumber, string> =
            create ("T" + corpNum)

    // ============================================
    // Invoice Line Item
    // ============================================

    /// Tax rate category for invoice
    type TaxRateCategory =
        | Standard      // 10%
        | Reduced       // 8%
        | Exempt        // 0% (non-taxable)

    /// Invoice line item
    type InvoiceLineItem = {
        LineNumber: int
        Description: string
        Quantity: decimal
        UnitPrice: Money
        TaxCategory: TaxRateCategory
        TaxRate: decimal
        Amount: Money
        TaxAmount: Money
        TotalAmount: Money
    }

    module InvoiceLineItem =

        let create
            (lineNumber: int)
            (description: string)
            (quantity: decimal)
            (unitPrice: Money)
            (taxCategory: TaxRateCategory)
            : InvoiceLineItem =

            let amount = Money.multiply quantity unitPrice
            let taxRate =
                match taxCategory with
                | Standard -> 10.0m
                | Reduced -> 8.0m
                | Exempt -> 0.0m

            let taxAmount = Money.multiply (taxRate / 100m) amount
            let totalAmount =
                Money.add amount taxAmount
                |> Result.defaultValue amount

            {
                LineNumber = lineNumber
                Description = description
                Quantity = quantity
                UnitPrice = unitPrice
                TaxCategory = taxCategory
                TaxRate = taxRate
                Amount = amount
                TaxAmount = taxAmount
                TotalAmount = totalAmount
            }

    // ============================================
    // Payment Record
    // ============================================

    /// Payment method
    type PaymentMethod =
        | BankTransfer of bankName: string * accountNumber: string
        | CreditCard of lastFourDigits: string
        | DirectDebit
        | Check of checkNumber: string
        | Cash
        | Other of description: string

    /// Payment record
    type PaymentRecord = {
        PaymentId: Guid
        PaymentDate: Date
        Amount: Money
        Method: PaymentMethod
        Reference: string option
        RecordedAt: DateTimeOffset
    }

    module PaymentRecord =

        let create
            (paymentDate: Date)
            (amount: Money)
            (method: PaymentMethod)
            : PaymentRecord =
            {
                PaymentId = Guid.NewGuid()
                PaymentDate = paymentDate
                Amount = amount
                Method = method
                Reference = None
                RecordedAt = DateTimeOffset.UtcNow
            }

    // ============================================
    // Invoice State
    // ============================================

    /// Invoice state (immutable)
    type InvoiceState = {
        Id: InvoiceId
        InvoiceNumber: string
        InvoiceType: InvoiceType
        CompanyId: CompanyId
        QualifiedInvoiceNumber: QualifiedInvoiceNumber option

        // Counterparty
        CounterpartyId: string
        CounterpartyName: string
        CounterpartyAddress: Address option
        CounterpartyInvoiceNumber: QualifiedInvoiceNumber option

        // Dates
        IssueDate: Date
        DueDate: Date
        TransactionDate: Date option

        // Line items
        LineItems: InvoiceLineItem list

        // Amounts
        SubTotal: Money
        TaxAmountStandard: Money
        TaxAmountReduced: Money
        TotalTaxAmount: Money
        TotalAmount: Money

        // Status and payments
        Status: InvoiceStatus
        Payments: PaymentRecord list
        PaidAmount: Money
        BalanceDue: Money

        // Metadata
        Notes: string option
        CreatedAt: DateTimeOffset
        SentAt: DateTimeOffset option
    }

    module InvoiceState =

        let calculateTotals (lineItems: InvoiceLineItem list) =
            let subTotal =
                lineItems
                |> List.sumBy (fun l -> Money.amount l.Amount)
                |> Money.yen

            let taxStandard =
                lineItems
                |> List.filter (fun l -> l.TaxCategory = Standard)
                |> List.sumBy (fun l -> Money.amount l.TaxAmount)
                |> Money.yen

            let taxReduced =
                lineItems
                |> List.filter (fun l -> l.TaxCategory = Reduced)
                |> List.sumBy (fun l -> Money.amount l.TaxAmount)
                |> Money.yen

            let totalTax =
                Money.add taxStandard taxReduced
                |> Result.defaultValue taxStandard

            let total =
                Money.add subTotal totalTax
                |> Result.defaultValue subTotal

            (subTotal, taxStandard, taxReduced, totalTax, total)

        let isPaid (state: InvoiceState) =
            state.Status = Paid

        let isOverdue (asOfDate: Date) (state: InvoiceState) =
            Date.isAfter asOfDate state.DueDate &&
            state.Status <> Paid &&
            state.Status <> Cancelled

    // ============================================
    // Invoice Aggregate
    // ============================================

    /// Invoice aggregate root
    type Invoice private (state: InvoiceState) =

        member _.State = state
        member _.Id = state.Id
        member _.InvoiceNumber = state.InvoiceNumber
        member _.InvoiceType = state.InvoiceType
        member _.QualifiedInvoiceNumber = state.QualifiedInvoiceNumber
        member _.CounterpartyId = state.CounterpartyId
        member _.CounterpartyName = state.CounterpartyName
        member _.IssueDate = state.IssueDate
        member _.DueDate = state.DueDate
        member _.LineItems = state.LineItems
        member _.SubTotal = state.SubTotal
        member _.TotalTaxAmount = state.TotalTaxAmount
        member _.TotalAmount = state.TotalAmount
        member _.Status = state.Status
        member _.PaidAmount = state.PaidAmount
        member _.BalanceDue = state.BalanceDue
        member _.IsPaid = InvoiceState.isPaid state

        member this.IsOverdue(asOfDate: Date) = InvoiceState.isOverdue asOfDate state

        // ============================================
        // Commands
        // ============================================

        /// Add a line item
        member this.AddLineItem(item: InvoiceLineItem)
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (state.Status = Draft)
                        (InvalidInvoice "Can only add items to draft invoices")

                let newItems = state.LineItems @ [item]
                let (subTotal, taxStd, taxRed, totalTax, total) =
                    InvoiceState.calculateTotals newItems

                return Invoice({
                    state with
                        LineItems = newItems
                        SubTotal = subTotal
                        TaxAmountStandard = taxStd
                        TaxAmountReduced = taxRed
                        TotalTaxAmount = totalTax
                        TotalAmount = total
                        BalanceDue = total
                })
            }

        /// Issue the invoice
        member this.Issue()
            : Result<Invoice * FinancialEvent, InvoiceError> =

            result {
                do! Result.require
                        (state.Status = Draft)
                        (InvalidInvoice "Invoice is not in draft status")

                do! Result.require
                        (state.LineItems.Length > 0)
                        (InvalidInvoice "Invoice must have at least one line item")

                let newState = { state with Status = Issued }

                let event = InvoiceCreated {
                    Meta = FinancialEventMeta.create state.CompanyId
                    InvoiceId = state.Id
                    InvoiceNumber = state.InvoiceNumber
                    InvoiceType = state.InvoiceType
                    IssueDate = state.IssueDate
                    DueDate = state.DueDate
                    CustomerOrVendorId = state.CounterpartyId
                    CustomerOrVendorName = state.CounterpartyName
                    SubTotal = state.SubTotal
                    TaxAmount = state.TotalTaxAmount
                    TotalAmount = state.TotalAmount
                    QualifiedInvoiceNumber = state.QualifiedInvoiceNumber |> Option.map QualifiedInvoiceNumber.value
                }

                return (Invoice(newState), event)
            }

        /// Mark as sent
        member this.MarkSent()
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (state.Status = Issued)
                        (InvalidInvoice "Invoice must be issued before sending")

                return Invoice({
                    state with
                        Status = Sent
                        SentAt = Some DateTimeOffset.UtcNow
                })
            }

        /// Record a payment
        member this.RecordPayment(payment: PaymentRecord)
            : Result<Invoice * FinancialEvent, InvoiceError> =

            result {
                do! Result.require
                        (state.Status <> Cancelled)
                        (InvoiceAlreadyCancelled state.InvoiceNumber)

                do! Result.require
                        (state.Status <> Paid)
                        (InvoiceAlreadyPaid state.InvoiceNumber)

                do! Result.require
                        (Money.amount payment.Amount <= Money.amount state.BalanceDue)
                        (PaymentExceedsBalance (payment.Amount, state.BalanceDue))

                let newPaidAmount =
                    Money.add state.PaidAmount payment.Amount
                    |> Result.defaultValue state.PaidAmount

                let newBalance =
                    Money.subtract state.TotalAmount newPaidAmount
                    |> Result.defaultValue (Money.yen 0m)

                let newStatus =
                    if Money.isZero newBalance then Paid
                    elif state.Status = Sent then PartiallyPaid
                    else state.Status

                let newState = {
                    state with
                        Payments = state.Payments @ [payment]
                        PaidAmount = newPaidAmount
                        BalanceDue = newBalance
                        Status = newStatus
                }

                let event = InvoicePaymentReceived {
                    Meta = FinancialEventMeta.create state.CompanyId
                    InvoiceId = state.Id
                    PaymentId = payment.PaymentId
                    PaymentDate = payment.PaymentDate
                    PaymentAmount = payment.Amount
                    PaymentMethod =
                        match payment.Method with
                        | BankTransfer _ -> "Bank Transfer"
                        | CreditCard _ -> "Credit Card"
                        | DirectDebit -> "Direct Debit"
                        | Check _ -> "Check"
                        | Cash -> "Cash"
                        | Other desc -> desc
                    RemainingBalance = newBalance
                }

                return (Invoice(newState), event)
            }

        /// Cancel the invoice
        member this.Cancel(reason: string)
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (state.Status <> Paid)
                        (InvoiceAlreadyPaid state.InvoiceNumber)

                do! Result.require
                        (state.Status <> Cancelled)
                        (InvoiceAlreadyCancelled state.InvoiceNumber)

                return Invoice({
                    state with
                        Status = Cancelled
                        Notes = Some (state.Notes |> Option.map (fun n -> n + "\n" + reason) |> Option.defaultValue reason)
                })
            }

        /// Mark as disputed
        member this.MarkDisputed(reason: string)
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (state.Status <> Cancelled && state.Status <> Paid)
                        (InvalidInvoice "Cannot dispute cancelled or paid invoices")

                return Invoice({
                    state with
                        Status = Disputed
                        Notes = Some (state.Notes |> Option.map (fun n -> n + "\nDisputed: " + reason) |> Option.defaultValue ("Disputed: " + reason))
                })
            }

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a sales invoice
        static member CreateSalesInvoice
            (companyId: CompanyId)
            (invoiceNumber: string)
            (customerId: string)
            (customerName: string)
            (issueDate: Date)
            (dueDate: Date)
            (qualifiedInvoiceNumber: QualifiedInvoiceNumber option)
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(invoiceNumber)))
                        (InvalidInvoice "Invoice number cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(customerName)))
                        (InvalidInvoice "Customer name cannot be empty")

                do! Result.require
                        (Date.isOnOrBefore issueDate dueDate)
                        (InvalidInvoice "Due date must be on or after issue date")

                let state = {
                    Id = InvoiceId.create()
                    InvoiceNumber = invoiceNumber
                    InvoiceType = SalesInvoice
                    CompanyId = companyId
                    QualifiedInvoiceNumber = qualifiedInvoiceNumber
                    CounterpartyId = customerId
                    CounterpartyName = customerName
                    CounterpartyAddress = None
                    CounterpartyInvoiceNumber = None
                    IssueDate = issueDate
                    DueDate = dueDate
                    TransactionDate = Some issueDate
                    LineItems = []
                    SubTotal = Money.yen 0m
                    TaxAmountStandard = Money.yen 0m
                    TaxAmountReduced = Money.yen 0m
                    TotalTaxAmount = Money.yen 0m
                    TotalAmount = Money.yen 0m
                    Status = Draft
                    Payments = []
                    PaidAmount = Money.yen 0m
                    BalanceDue = Money.yen 0m
                    Notes = None
                    CreatedAt = DateTimeOffset.UtcNow
                    SentAt = None
                }

                return Invoice(state)
            }

        /// Create a purchase invoice
        static member CreatePurchaseInvoice
            (companyId: CompanyId)
            (invoiceNumber: string)
            (vendorId: string)
            (vendorName: string)
            (issueDate: Date)
            (dueDate: Date)
            (vendorInvoiceNumber: QualifiedInvoiceNumber option)
            : Result<Invoice, InvoiceError> =

            result {
                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(invoiceNumber)))
                        (InvalidInvoice "Invoice number cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(vendorName)))
                        (InvalidInvoice "Vendor name cannot be empty")

                let state = {
                    Id = InvoiceId.create()
                    InvoiceNumber = invoiceNumber
                    InvoiceType = PurchaseInvoice
                    CompanyId = companyId
                    QualifiedInvoiceNumber = None
                    CounterpartyId = vendorId
                    CounterpartyName = vendorName
                    CounterpartyAddress = None
                    CounterpartyInvoiceNumber = vendorInvoiceNumber
                    IssueDate = issueDate
                    DueDate = dueDate
                    TransactionDate = Some issueDate
                    LineItems = []
                    SubTotal = Money.yen 0m
                    TaxAmountStandard = Money.yen 0m
                    TaxAmountReduced = Money.yen 0m
                    TotalTaxAmount = Money.yen 0m
                    TotalAmount = Money.yen 0m
                    Status = Draft
                    Payments = []
                    PaidAmount = Money.yen 0m
                    BalanceDue = Money.yen 0m
                    Notes = None
                    CreatedAt = DateTimeOffset.UtcNow
                    SentAt = None
                }

                return Invoice(state)
            }

        /// Reconstitute from state
        static member FromState(state: InvoiceState) : Invoice =
            Invoice(state)

    // ============================================
    // Invoice Logic
    // ============================================

    module InvoiceLogic =

        /// Standard payment terms in Japan
        let standardPaymentTermsDays = 30

        /// Calculate due date based on issue date and terms
        let calculateDueDate (issueDate: Date) (termsDays: int) : Date =
            Date.addDays termsDays issueDate

        /// Calculate age of invoice
        let invoiceAge (invoice: Invoice) (asOfDate: Date) : int =
            Date.daysBetween invoice.IssueDate asOfDate

        /// Calculate days overdue
        let daysOverdue (invoice: Invoice) (asOfDate: Date) : int =
            if Date.isAfter asOfDate invoice.DueDate then
                Date.daysBetween invoice.DueDate asOfDate
            else
                0

        /// Group invoices by status
        let groupByStatus (invoices: Invoice list) : Map<InvoiceStatus, Invoice list> =
            invoices
            |> List.groupBy (fun i -> i.Status)
            |> Map.ofList

        /// Calculate total outstanding
        let totalOutstanding (invoices: Invoice list) : Money =
            invoices
            |> List.filter (fun i ->
                match i.Status with
                | Draft | Cancelled | Paid -> false
                | _ -> true)
            |> List.sumBy (fun i -> Money.amount i.BalanceDue)
            |> Money.yen

        /// Calculate total overdue
        let totalOverdue (invoices: Invoice list) (asOfDate: Date) : Money =
            invoices
            |> List.filter (fun i -> i.IsOverdue asOfDate)
            |> List.sumBy (fun i -> Money.amount i.BalanceDue)
            |> Money.yen

        /// Validate qualified invoice requirements
        let validateQualifiedInvoice (invoice: Invoice) : Result<unit, string list> =
            let errors = ResizeArray<string>()

            // Must have qualified invoice number
            if invoice.QualifiedInvoiceNumber.IsNone then
                errors.Add("Qualified invoice number is required")

            // Must have issue date
            if Date.year invoice.IssueDate < 2023 then
                errors.Add("Qualified invoice system started October 2023")

            // Must have line items with proper tax breakdown
            let hasStandardRate = invoice.LineItems |> List.exists (fun l -> l.TaxCategory = Standard)
            let hasReducedRate = invoice.LineItems |> List.exists (fun l -> l.TaxCategory = Reduced)

            if hasStandardRate && Money.isZero invoice.State.TaxAmountStandard then
                errors.Add("Standard rate items must show tax amount")

            if hasReducedRate && Money.isZero invoice.State.TaxAmountReduced then
                errors.Add("Reduced rate items must show tax amount")

            if errors.Count > 0 then
                Error (errors |> Seq.toList)
            else
                Ok ()
