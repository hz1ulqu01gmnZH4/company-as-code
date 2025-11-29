namespace CompanyAsCode.Financial

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Domain events for Financial & Accounting context
module Events =

    // ============================================
    // Event Metadata
    // ============================================

    /// Common metadata for all financial events
    type FinancialEventMeta = {
        EventId: Guid
        OccurredAt: DateTimeOffset
        CompanyId: CompanyId
        FiscalYearId: FiscalYearId option
        CorrelationId: Guid option
        UserId: string option
    }

    module FinancialEventMeta =

        let create (companyId: CompanyId) = {
            EventId = Guid.NewGuid()
            OccurredAt = DateTimeOffset.UtcNow
            CompanyId = companyId
            FiscalYearId = None
            CorrelationId = None
            UserId = None
        }

        let withFiscalYear (fyId: FiscalYearId) (meta: FinancialEventMeta) =
            { meta with FiscalYearId = Some fyId }

        let withUser (userId: string) (meta: FinancialEventMeta) =
            { meta with UserId = Some userId }

    // ============================================
    // Account Events
    // ============================================

    /// Account type classification
    type AccountType =
        | Asset           // 資産
        | Liability       // 負債
        | Equity          // 純資産
        | Revenue         // 収益
        | Expense         // 費用

    /// Account sub-types for Japanese accounting
    type AccountSubType =
        // Assets
        | CurrentAsset        // 流動資産
        | FixedAsset          // 固定資産
        | DeferredAsset       // 繰延資産
        // Liabilities
        | CurrentLiability    // 流動負債
        | LongTermLiability   // 固定負債
        // Equity
        | ShareCapital        // 資本金
        | Reserves            // 準備金
        | RetainedEarnings    // 利益剰余金
        // Revenue
        | OperatingRevenue    // 営業収益
        | NonOperatingRevenue // 営業外収益
        | ExtraordinaryIncome // 特別利益
        // Expense
        | CostOfSales         // 売上原価
        | OperatingExpense    // 販売費及び一般管理費
        | NonOperatingExpense // 営業外費用
        | ExtraordinaryLoss   // 特別損失

    /// Account created event
    type AccountCreatedEvent = {
        Meta: FinancialEventMeta
        AccountId: AccountId
        AccountCode: string
        AccountName: string
        AccountNameJapanese: string
        AccountType: AccountType
        SubType: AccountSubType option
        ParentAccountId: AccountId option
        IsControlAccount: bool
    }

    /// Account deactivated event
    type AccountDeactivatedEvent = {
        Meta: FinancialEventMeta
        AccountId: AccountId
        Reason: string
        DeactivatedDate: Date
    }

    // ============================================
    // Journal Entry Events
    // ============================================

    /// Debit/Credit indicator
    type DebitCredit =
        | Debit   // 借方
        | Credit  // 貸方

    /// Journal entry line
    type JournalEntryLine = {
        AccountId: AccountId
        DebitCredit: DebitCredit
        Amount: Money
        Description: string option
        TaxCode: string option
    }

    /// Journal entry created event
    type JournalEntryCreatedEvent = {
        Meta: FinancialEventMeta
        EntryId: JournalEntryId
        EntryNumber: string
        TransactionDate: Date
        Description: string
        Lines: JournalEntryLine list
        SourceDocument: string option
    }

    /// Journal entry posted event
    type JournalEntryPostedEvent = {
        Meta: FinancialEventMeta
        EntryId: JournalEntryId
        PostedDate: Date
        PostedBy: string
    }

    /// Journal entry reversed event
    type JournalEntryReversedEvent = {
        Meta: FinancialEventMeta
        OriginalEntryId: JournalEntryId
        ReversalEntryId: JournalEntryId
        ReversalDate: Date
        Reason: string
    }

    // ============================================
    // Fiscal Period Events
    // ============================================

    /// Fiscal year opened event
    type FiscalYearOpenedEvent = {
        Meta: FinancialEventMeta
        FiscalYearId: FiscalYearId
        FiscalYearNumber: int
        StartDate: Date
        EndDate: Date
        PeriodCount: int
    }

    /// Accounting period status
    type PeriodStatus =
        | NotStarted
        | Open
        | SoftClosed    // Adjustments only
        | HardClosed    // No changes allowed

    /// Accounting period opened event
    type AccountingPeriodOpenedEvent = {
        Meta: FinancialEventMeta
        PeriodId: Guid
        FiscalYearId: FiscalYearId
        PeriodNumber: int
        StartDate: Date
        EndDate: Date
    }

    /// Accounting period closed event
    type AccountingPeriodClosedEvent = {
        Meta: FinancialEventMeta
        PeriodId: Guid
        ClosedDate: Date
        ClosedBy: string
        IsSoftClose: bool
    }

    /// Fiscal year closed event
    type FiscalYearClosedEvent = {
        Meta: FinancialEventMeta
        FiscalYearId: FiscalYearId
        ClosedDate: Date
        ClosingEntryId: JournalEntryId option
        NetIncome: Money
        RetainedEarningsCarryForward: Money
    }

    // ============================================
    // Tax Events
    // ============================================

    /// Tax type
    type TaxType =
        | CorporateTax           // 法人税
        | LocalCorporateTax      // 地方法人税
        | InhabitantTax          // 住民税
        | EnterpriseeTax         // 事業税
        | ConsumptionTax         // 消費税
        | LocalConsumptionTax    // 地方消費税
        | WithholdingTax         // 源泉所得税

    /// Tax filing status
    type FilingStatus =
        | Draft
        | UnderReview
        | Submitted
        | Accepted
        | Rejected of reason: string
        | Amended

    /// Tax calculation completed event
    type TaxCalculationCompletedEvent = {
        Meta: FinancialEventMeta
        TaxType: TaxType
        TaxPeriod: DateRange
        TaxableBase: Money
        TaxAmount: Money
        DeductibleAmount: Money
        NetTaxDue: Money
    }

    /// Tax filing submitted event
    type TaxFilingSubmittedEvent = {
        Meta: FinancialEventMeta
        FilingId: Guid
        TaxType: TaxType
        TaxPeriod: DateRange
        FilingDate: Date
        DueDate: Date
        TaxAmount: Money
        FilingMethod: string
    }

    // ============================================
    // Invoice Events (Qualified Invoice System)
    // ============================================

    /// Invoice type
    type InvoiceType =
        | SalesInvoice        // 売上請求書
        | PurchaseInvoice     // 仕入請求書
        | CreditNote          // 返品・値引き
        | DebitNote           // 追加請求

    /// Invoice status
    type InvoiceStatus =
        | Draft
        | Issued
        | Sent
        | PartiallyPaid
        | Paid
        | Overdue
        | Cancelled
        | Disputed

    /// Invoice created event
    type InvoiceCreatedEvent = {
        Meta: FinancialEventMeta
        InvoiceId: InvoiceId
        InvoiceNumber: string
        InvoiceType: InvoiceType
        IssueDate: Date
        DueDate: Date
        CustomerOrVendorId: string
        CustomerOrVendorName: string
        SubTotal: Money
        TaxAmount: Money
        TotalAmount: Money
        QualifiedInvoiceNumber: string option  // インボイス登録番号
    }

    /// Invoice payment received event
    type InvoicePaymentReceivedEvent = {
        Meta: FinancialEventMeta
        InvoiceId: InvoiceId
        PaymentId: Guid
        PaymentDate: Date
        PaymentAmount: Money
        PaymentMethod: string
        RemainingBalance: Money
    }

    // ============================================
    // Unified Event Type
    // ============================================

    /// Union of all financial context events
    type FinancialEvent =
        // Account events
        | AccountCreated of AccountCreatedEvent
        | AccountDeactivated of AccountDeactivatedEvent

        // Journal entry events
        | JournalEntryCreated of JournalEntryCreatedEvent
        | JournalEntryPosted of JournalEntryPostedEvent
        | JournalEntryReversed of JournalEntryReversedEvent

        // Fiscal period events
        | FiscalYearOpened of FiscalYearOpenedEvent
        | AccountingPeriodOpened of AccountingPeriodOpenedEvent
        | AccountingPeriodClosed of AccountingPeriodClosedEvent
        | FiscalYearClosed of FiscalYearClosedEvent

        // Tax events
        | TaxCalculationCompleted of TaxCalculationCompletedEvent
        | TaxFilingSubmitted of TaxFilingSubmittedEvent

        // Invoice events
        | InvoiceCreated of InvoiceCreatedEvent
        | InvoicePaymentReceived of InvoicePaymentReceivedEvent

    module FinancialEvent =

        let getCompanyId = function
            | AccountCreated e -> e.Meta.CompanyId
            | AccountDeactivated e -> e.Meta.CompanyId
            | JournalEntryCreated e -> e.Meta.CompanyId
            | JournalEntryPosted e -> e.Meta.CompanyId
            | JournalEntryReversed e -> e.Meta.CompanyId
            | FiscalYearOpened e -> e.Meta.CompanyId
            | AccountingPeriodOpened e -> e.Meta.CompanyId
            | AccountingPeriodClosed e -> e.Meta.CompanyId
            | FiscalYearClosed e -> e.Meta.CompanyId
            | TaxCalculationCompleted e -> e.Meta.CompanyId
            | TaxFilingSubmitted e -> e.Meta.CompanyId
            | InvoiceCreated e -> e.Meta.CompanyId
            | InvoicePaymentReceived e -> e.Meta.CompanyId

        let getEventId = function
            | AccountCreated e -> e.Meta.EventId
            | AccountDeactivated e -> e.Meta.EventId
            | JournalEntryCreated e -> e.Meta.EventId
            | JournalEntryPosted e -> e.Meta.EventId
            | JournalEntryReversed e -> e.Meta.EventId
            | FiscalYearOpened e -> e.Meta.EventId
            | AccountingPeriodOpened e -> e.Meta.EventId
            | AccountingPeriodClosed e -> e.Meta.EventId
            | FiscalYearClosed e -> e.Meta.EventId
            | TaxCalculationCompleted e -> e.Meta.EventId
            | TaxFilingSubmitted e -> e.Meta.EventId
            | InvoiceCreated e -> e.Meta.EventId
            | InvoicePaymentReceived e -> e.Meta.EventId

        let getOccurredAt = function
            | AccountCreated e -> e.Meta.OccurredAt
            | AccountDeactivated e -> e.Meta.OccurredAt
            | JournalEntryCreated e -> e.Meta.OccurredAt
            | JournalEntryPosted e -> e.Meta.OccurredAt
            | JournalEntryReversed e -> e.Meta.OccurredAt
            | FiscalYearOpened e -> e.Meta.OccurredAt
            | AccountingPeriodOpened e -> e.Meta.OccurredAt
            | AccountingPeriodClosed e -> e.Meta.OccurredAt
            | FiscalYearClosed e -> e.Meta.OccurredAt
            | TaxCalculationCompleted e -> e.Meta.OccurredAt
            | TaxFilingSubmitted e -> e.Meta.OccurredAt
            | InvoiceCreated e -> e.Meta.OccurredAt
            | InvoicePaymentReceived e -> e.Meta.OccurredAt
