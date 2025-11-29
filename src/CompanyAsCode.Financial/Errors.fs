namespace CompanyAsCode.Financial

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Domain errors for Financial & Accounting context
module Errors =

    // ============================================
    // Account Errors
    // ============================================

    /// Errors related to account operations
    type AccountError =
        | InvalidAccountCode of message: string
        | InvalidAccountName of message: string
        | AccountNotFound of accountCode: string
        | AccountAlreadyExists of accountCode: string
        | AccountNotActive of accountCode: string
        | CannotDeleteAccountWithBalance of accountCode: string * balance: Money
        | InvalidAccountType of message: string

    module AccountError =

        let toString = function
            | InvalidAccountCode msg -> $"Invalid account code: {msg}"
            | InvalidAccountName msg -> $"Invalid account name: {msg}"
            | AccountNotFound code -> $"Account not found: {code}"
            | AccountAlreadyExists code -> $"Account already exists: {code}"
            | AccountNotActive code -> $"Account is not active: {code}"
            | CannotDeleteAccountWithBalance (code, balance) ->
                $"Cannot delete account {code} with balance {Money.format balance}"
            | InvalidAccountType msg -> $"Invalid account type: {msg}"

    // ============================================
    // Journal Entry Errors
    // ============================================

    /// Errors related to journal entry operations
    type JournalEntryError =
        | InvalidEntryDescription of message: string
        | UnbalancedEntry of debits: Money * credits: Money
        | EmptyEntry
        | InvalidLineItem of message: string
        | EntryNotFound of entryId: string
        | EntryAlreadyPosted of entryId: string
        | CannotModifyPostedEntry of entryId: string
        | InvalidPostingDate of message: string
        | AccountError of AccountError

    module JournalEntryError =

        let toString = function
            | InvalidEntryDescription msg -> $"Invalid entry description: {msg}"
            | UnbalancedEntry (debits, credits) ->
                $"Entry is unbalanced: debits {Money.format debits} != credits {Money.format credits}"
            | EmptyEntry -> "Journal entry must have at least one line item"
            | InvalidLineItem msg -> $"Invalid line item: {msg}"
            | EntryNotFound id -> $"Journal entry not found: {id}"
            | EntryAlreadyPosted id -> $"Journal entry already posted: {id}"
            | CannotModifyPostedEntry id -> $"Cannot modify posted entry: {id}"
            | InvalidPostingDate msg -> $"Invalid posting date: {msg}"
            | AccountError err -> AccountError.toString err

    // ============================================
    // Ledger Errors
    // ============================================

    /// Errors related to general ledger operations
    type LedgerError =
        | LedgerNotFound of ledgerId: string
        | LedgerAlreadyClosed of fiscalYear: int
        | LedgerNotClosed of fiscalYear: int
        | InvalidFiscalYear of message: string
        | PeriodNotOpen of period: string
        | PeriodAlreadyClosed of period: string
        | CannotPostToClosedPeriod of period: string
        | TrialBalanceNotBalanced of difference: Money
        | JournalEntryError of JournalEntryError
        | AccountError of AccountError

    module LedgerError =

        let toString = function
            | LedgerNotFound id -> $"Ledger not found: {id}"
            | LedgerAlreadyClosed year -> $"Ledger already closed for fiscal year {year}"
            | LedgerNotClosed year -> $"Ledger not closed for fiscal year {year}"
            | InvalidFiscalYear msg -> $"Invalid fiscal year: {msg}"
            | PeriodNotOpen period -> $"Period not open: {period}"
            | PeriodAlreadyClosed period -> $"Period already closed: {period}"
            | CannotPostToClosedPeriod period -> $"Cannot post to closed period: {period}"
            | TrialBalanceNotBalanced diff ->
                $"Trial balance not balanced, difference: {Money.format diff}"
            | JournalEntryError err -> JournalEntryError.toString err
            | AccountError err -> AccountError.toString err

    // ============================================
    // Tax Errors
    // ============================================

    /// Errors related to tax operations
    type TaxError =
        | InvalidTaxRate of message: string
        | InvalidTaxPeriod of message: string
        | TaxFilingNotFound of filingId: string
        | TaxFilingAlreadySubmitted of filingId: string
        | InsufficientDataForFiling of missing: string list
        | InvalidInvoiceNumber of message: string
        | DuplicateInvoiceNumber of invoiceNumber: string

    module TaxError =

        let toString = function
            | InvalidTaxRate msg -> $"Invalid tax rate: {msg}"
            | InvalidTaxPeriod msg -> $"Invalid tax period: {msg}"
            | TaxFilingNotFound id -> $"Tax filing not found: {id}"
            | TaxFilingAlreadySubmitted id -> $"Tax filing already submitted: {id}"
            | InsufficientDataForFiling missing ->
                let missingStr = String.concat ", " missing
                $"Insufficient data for filing, missing: {missingStr}"
            | InvalidInvoiceNumber msg -> $"Invalid invoice number: {msg}"
            | DuplicateInvoiceNumber num -> $"Duplicate invoice number: {num}"

    // ============================================
    // Invoice Errors
    // ============================================

    /// Errors related to invoice operations
    type InvoiceError =
        | InvalidInvoice of message: string
        | InvoiceNotFound of invoiceId: string
        | InvoiceAlreadyPaid of invoiceId: string
        | InvoiceAlreadyCancelled of invoiceId: string
        | InvalidPayment of message: string
        | PaymentExceedsBalance of payment: Money * balance: Money
        | InvoiceNotDue of invoiceId: string
        | TaxError of TaxError

    module InvoiceError =

        let toString = function
            | InvalidInvoice msg -> $"Invalid invoice: {msg}"
            | InvoiceNotFound id -> $"Invoice not found: {id}"
            | InvoiceAlreadyPaid id -> $"Invoice already paid: {id}"
            | InvoiceAlreadyCancelled id -> $"Invoice already cancelled: {id}"
            | InvalidPayment msg -> $"Invalid payment: {msg}"
            | PaymentExceedsBalance (payment, balance) ->
                $"Payment {Money.format payment} exceeds balance {Money.format balance}"
            | InvoiceNotDue id -> $"Invoice not due: {id}"
            | TaxError err -> TaxError.toString err

    // ============================================
    // Unified Financial Error
    // ============================================

    /// Union of all financial context errors
    type FinancialError =
        | Account of AccountError
        | JournalEntry of JournalEntryError
        | Ledger of LedgerError
        | Tax of TaxError
        | Invoice of InvoiceError
        | Validation of message: string

    module FinancialError =

        let toString = function
            | Account err -> AccountError.toString err
            | JournalEntry err -> JournalEntryError.toString err
            | Ledger err -> LedgerError.toString err
            | Tax err -> TaxError.toString err
            | Invoice err -> InvoiceError.toString err
            | Validation msg -> $"Validation error: {msg}"

        let fromAccount (err: AccountError) = Account err
        let fromJournalEntry (err: JournalEntryError) = JournalEntry err
        let fromLedger (err: LedgerError) = Ledger err
        let fromTax (err: TaxError) = Tax err
        let fromInvoice (err: InvoiceError) = Invoice err
