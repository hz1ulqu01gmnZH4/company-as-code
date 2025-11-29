namespace CompanyAsCode.Financial

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Journal Entry aggregate
module JournalEntry =

    open Events
    open Errors

    // ============================================
    // Journal Entry Line (Value Object)
    // ============================================

    /// Line item in a journal entry
    type EntryLine = {
        AccountId: AccountId
        AccountCode: string
        AccountName: string
        DebitCredit: DebitCredit
        Amount: Money
        Description: string option
        TaxCode: string option
        DepartmentCode: string option
    }

    module EntryLine =

        let createDebit
            (accountId: AccountId)
            (accountCode: string)
            (accountName: string)
            (amount: Money)
            : EntryLine =
            {
                AccountId = accountId
                AccountCode = accountCode
                AccountName = accountName
                DebitCredit = Debit
                Amount = amount
                Description = None
                TaxCode = None
                DepartmentCode = None
            }

        let createCredit
            (accountId: AccountId)
            (accountCode: string)
            (accountName: string)
            (amount: Money)
            : EntryLine =
            {
                AccountId = accountId
                AccountCode = accountCode
                AccountName = accountName
                DebitCredit = Credit
                Amount = amount
                Description = None
                TaxCode = None
                DepartmentCode = None
            }

        let withDescription (desc: string) (line: EntryLine) =
            { line with Description = Some desc }

        let withTaxCode (code: string) (line: EntryLine) =
            { line with TaxCode = Some code }

        let withDepartment (dept: string) (line: EntryLine) =
            { line with DepartmentCode = Some dept }

        let isDebit (line: EntryLine) = line.DebitCredit = Debit
        let isCredit (line: EntryLine) = line.DebitCredit = Credit

    // ============================================
    // Journal Entry Status
    // ============================================

    /// Entry status
    type EntryStatus =
        | Draft           // 下書き
        | Pending         // 承認待ち
        | Approved        // 承認済
        | Posted          // 転記済
        | Reversed        // 取消済

    // ============================================
    // Journal Entry State
    // ============================================

    /// Journal entry state (immutable)
    type JournalEntryState = {
        Id: JournalEntryId
        EntryNumber: string
        FiscalYearId: FiscalYearId
        TransactionDate: Date
        PostingDate: Date option
        Description: string
        Lines: EntryLine list
        Status: EntryStatus
        SourceDocument: string option
        SourceDocumentNumber: string option
        CreatedBy: string
        CreatedAt: DateTimeOffset
        ApprovedBy: string option
        ApprovedAt: DateTimeOffset option
        PostedBy: string option
        PostedAt: DateTimeOffset option
        ReversalEntryId: JournalEntryId option
    }

    module JournalEntryState =

        let totalDebits (state: JournalEntryState) : Money =
            state.Lines
            |> List.filter EntryLine.isDebit
            |> List.map (fun l -> l.Amount)
            |> List.fold (fun acc m ->
                Money.add acc m |> Result.defaultValue acc
            ) (Money.yen 0m)

        let totalCredits (state: JournalEntryState) : Money =
            state.Lines
            |> List.filter EntryLine.isCredit
            |> List.map (fun l -> l.Amount)
            |> List.fold (fun acc m ->
                Money.add acc m |> Result.defaultValue acc
            ) (Money.yen 0m)

        let isBalanced (state: JournalEntryState) : bool =
            let debits = totalDebits state
            let credits = totalCredits state
            Money.amount debits = Money.amount credits

        let isPosted (state: JournalEntryState) : bool =
            state.Status = Posted

        let canModify (state: JournalEntryState) : bool =
            match state.Status with
            | Draft | Pending -> true
            | Approved | Posted | Reversed -> false

    // ============================================
    // Journal Entry Aggregate
    // ============================================

    /// Journal entry aggregate root
    type JournalEntry private (state: JournalEntryState) =

        member _.State = state
        member _.Id = state.Id
        member _.EntryNumber = state.EntryNumber
        member _.FiscalYearId = state.FiscalYearId
        member _.TransactionDate = state.TransactionDate
        member _.PostingDate = state.PostingDate
        member _.Description = state.Description
        member _.Lines = state.Lines
        member _.Status = state.Status

        member _.TotalDebits = JournalEntryState.totalDebits state
        member _.TotalCredits = JournalEntryState.totalCredits state
        member _.IsBalanced = JournalEntryState.isBalanced state
        member _.IsPosted = JournalEntryState.isPosted state
        member _.CanModify = JournalEntryState.canModify state

        // ============================================
        // Commands
        // ============================================

        /// Add a line to the entry
        member this.AddLine(line: EntryLine)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (JournalEntryState.canModify state)
                        (CannotModifyPostedEntry state.EntryNumber)

                do! Result.require
                        (Money.isPositive line.Amount)
                        (InvalidLineItem "Amount must be positive")

                return JournalEntry({
                    state with Lines = state.Lines @ [line]
                })
            }

        /// Remove a line from the entry
        member this.RemoveLine(accountId: AccountId) (debitCredit: DebitCredit)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (JournalEntryState.canModify state)
                        (CannotModifyPostedEntry state.EntryNumber)

                let newLines =
                    state.Lines
                    |> List.filter (fun l ->
                        not (l.AccountId = accountId && l.DebitCredit = debitCredit))

                return JournalEntry({ state with Lines = newLines })
            }

        /// Update description
        member this.UpdateDescription(description: string)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (JournalEntryState.canModify state)
                        (CannotModifyPostedEntry state.EntryNumber)

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(description)))
                        (InvalidEntryDescription "Description cannot be empty")

                return JournalEntry({ state with Description = description })
            }

        /// Submit for approval
        member this.SubmitForApproval()
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (state.Status = Draft)
                        (InvalidEntryDescription "Entry must be in Draft status to submit")

                do! Result.require
                        (state.Lines.Length > 0)
                        EmptyEntry

                do! Result.require
                        (JournalEntryState.isBalanced state)
                        (UnbalancedEntry (this.TotalDebits, this.TotalCredits))

                return JournalEntry({ state with Status = Pending })
            }

        /// Approve the entry
        member this.Approve(approvedBy: string)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (state.Status = Pending)
                        (InvalidEntryDescription "Entry must be Pending to approve")

                return JournalEntry({
                    state with
                        Status = Approved
                        ApprovedBy = Some approvedBy
                        ApprovedAt = Some DateTimeOffset.UtcNow
                })
            }

        /// Post the entry to the ledger
        member this.Post(postingDate: Date) (postedBy: string)
            : Result<JournalEntry * FinancialEvent, JournalEntryError> =

            result {
                do! Result.require
                        (state.Status = Approved || state.Status = Draft)
                        (InvalidEntryDescription "Entry must be Approved or Draft to post")

                do! Result.require
                        (state.Lines.Length > 0)
                        EmptyEntry

                do! Result.require
                        (JournalEntryState.isBalanced state)
                        (UnbalancedEntry (this.TotalDebits, this.TotalCredits))

                let newState = {
                    state with
                        Status = Posted
                        PostingDate = Some postingDate
                        PostedBy = Some postedBy
                        PostedAt = Some DateTimeOffset.UtcNow
                }

                let event = JournalEntryPosted {
                    Meta = FinancialEventMeta.create (CompanyId.create())  // TODO: get from context
                    EntryId = state.Id
                    PostedDate = postingDate
                    PostedBy = postedBy
                }

                return (JournalEntry(newState), event)
            }

        /// Reverse the entry
        member this.CreateReversal(reversalDate: Date) (reversalNumber: string) (reversedBy: string)
            : Result<JournalEntry * JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (state.Status = Posted)
                        (InvalidEntryDescription "Only posted entries can be reversed")

                // Create reversal entry with opposite debits/credits
                let reversalLines =
                    state.Lines
                    |> List.map (fun l ->
                        { l with
                            DebitCredit =
                                match l.DebitCredit with
                                | Debit -> Credit
                                | Credit -> Debit
                        })

                let reversalState = {
                    Id = JournalEntryId.create()
                    EntryNumber = reversalNumber
                    FiscalYearId = state.FiscalYearId
                    TransactionDate = reversalDate
                    PostingDate = Some reversalDate
                    Description = $"Reversal of {state.EntryNumber}: {state.Description}"
                    Lines = reversalLines
                    Status = Posted
                    SourceDocument = Some "Reversal"
                    SourceDocumentNumber = Some state.EntryNumber
                    CreatedBy = reversedBy
                    CreatedAt = DateTimeOffset.UtcNow
                    ApprovedBy = Some reversedBy
                    ApprovedAt = Some DateTimeOffset.UtcNow
                    PostedBy = Some reversedBy
                    PostedAt = Some DateTimeOffset.UtcNow
                    ReversalEntryId = Some state.Id
                }

                let originalUpdated = {
                    state with
                        Status = Reversed
                        ReversalEntryId = Some reversalState.Id
                }

                return (JournalEntry(originalUpdated), JournalEntry(reversalState))
            }

        /// Set source document reference
        member this.SetSourceDocument(docType: string) (docNumber: string)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (JournalEntryState.canModify state)
                        (CannotModifyPostedEntry state.EntryNumber)

                return JournalEntry({
                    state with
                        SourceDocument = Some docType
                        SourceDocumentNumber = Some docNumber
                })
            }

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new journal entry
        static member Create
            (entryNumber: string)
            (fiscalYearId: FiscalYearId)
            (transactionDate: Date)
            (description: string)
            (createdBy: string)
            : Result<JournalEntry, JournalEntryError> =

            result {
                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(entryNumber)))
                        (InvalidEntryDescription "Entry number cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(description)))
                        (InvalidEntryDescription "Description cannot be empty")

                let state = {
                    Id = JournalEntryId.create()
                    EntryNumber = entryNumber
                    FiscalYearId = fiscalYearId
                    TransactionDate = transactionDate
                    PostingDate = None
                    Description = description
                    Lines = []
                    Status = Draft
                    SourceDocument = None
                    SourceDocumentNumber = None
                    CreatedBy = createdBy
                    CreatedAt = DateTimeOffset.UtcNow
                    ApprovedBy = None
                    ApprovedAt = None
                    PostedBy = None
                    PostedAt = None
                    ReversalEntryId = None
                }

                return JournalEntry(state)
            }

        /// Create entry with lines
        static member CreateWithLines
            (entryNumber: string)
            (fiscalYearId: FiscalYearId)
            (transactionDate: Date)
            (description: string)
            (lines: EntryLine list)
            (createdBy: string)
            : Result<JournalEntry, JournalEntryError> =

            result {
                let! entry = JournalEntry.Create entryNumber fiscalYearId transactionDate description createdBy

                do! Result.require
                        (lines.Length > 0)
                        EmptyEntry

                let state = { entry.State with Lines = lines }

                do! Result.require
                        (JournalEntryState.isBalanced state)
                        (UnbalancedEntry (
                            JournalEntryState.totalDebits state,
                            JournalEntryState.totalCredits state))

                return JournalEntry(state)
            }

        /// Reconstitute from state
        static member FromState(state: JournalEntryState) : JournalEntry =
            JournalEntry(state)

    // ============================================
    // Journal Entry Logic
    // ============================================

    module JournalEntryLogic =

        /// Create a simple two-line entry (debit one account, credit another)
        let createSimpleEntry
            (debitAccountId: AccountId)
            (debitAccountCode: string)
            (debitAccountName: string)
            (creditAccountId: AccountId)
            (creditAccountCode: string)
            (creditAccountName: string)
            (amount: Money)
            : EntryLine list =

            [
                EntryLine.createDebit debitAccountId debitAccountCode debitAccountName amount
                EntryLine.createCredit creditAccountId creditAccountCode creditAccountName amount
            ]

        /// Calculate net impact on account
        let accountNetImpact (accountId: AccountId) (lines: EntryLine list) : Money =
            let debits =
                lines
                |> List.filter (fun l -> l.AccountId = accountId && l.DebitCredit = Debit)
                |> List.sumBy (fun l -> Money.amount l.Amount)

            let credits =
                lines
                |> List.filter (fun l -> l.AccountId = accountId && l.DebitCredit = Credit)
                |> List.sumBy (fun l -> Money.amount l.Amount)

            Money.yen (debits - credits)

        /// Validate entry balance
        let validateBalance (lines: EntryLine list) : Result<unit, JournalEntryError> =
            let totalDebits =
                lines
                |> List.filter EntryLine.isDebit
                |> List.sumBy (fun l -> Money.amount l.Amount)

            let totalCredits =
                lines
                |> List.filter EntryLine.isCredit
                |> List.sumBy (fun l -> Money.amount l.Amount)

            if totalDebits = totalCredits then
                Ok ()
            else
                Error (UnbalancedEntry (Money.yen totalDebits, Money.yen totalCredits))

        /// Group lines by account
        let groupByAccount (lines: EntryLine list) : Map<AccountId, EntryLine list> =
            lines
            |> List.groupBy (fun l -> l.AccountId)
            |> Map.ofList
