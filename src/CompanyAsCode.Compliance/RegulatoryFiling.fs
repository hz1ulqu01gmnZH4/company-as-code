namespace CompanyAsCode.Compliance

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Temporal

/// Regulatory Filing aggregate
module RegulatoryFiling =

    open Events
    open Errors

    // ============================================
    // Filing Number (Value Object)
    // ============================================

    /// Filing number/reference
    type FilingNumber = private FilingNumber of string

    module FilingNumber =

        let create (number: string) : Result<FilingNumber, string> =
            if System.String.IsNullOrWhiteSpace(number) then
                Error "Filing number cannot be empty"
            elif number.Length > 50 then
                Error "Filing number too long"
            else
                Ok (FilingNumber (number.Trim()))

        let value (FilingNumber num) = num

        /// Generate a filing number
        let generate (prefix: string) (year: int) (sequence: int) : FilingNumber =
            FilingNumber $"{prefix}-{year}-{sequence:D6}"

    // ============================================
    // Attachment (Value Object)
    // ============================================

    /// Filing attachment
    type FilingAttachment = {
        AttachmentId: Guid
        FileName: string
        FileType: string
        Description: string option
        IsRequired: bool
        UploadedAt: DateTimeOffset option
    }

    module FilingAttachment =

        let create (fileName: string) (fileType: string) (isRequired: bool) : FilingAttachment =
            {
                AttachmentId = Guid.NewGuid()
                FileName = fileName
                FileType = fileType
                Description = None
                IsRequired = isRequired
                UploadedAt = None
            }

        let markUploaded (attachment: FilingAttachment) : FilingAttachment =
            { attachment with UploadedAt = Some DateTimeOffset.UtcNow }

        let isUploaded (attachment: FilingAttachment) =
            attachment.UploadedAt.IsSome

    // ============================================
    // Filing Period (Value Object)
    // ============================================

    /// Filing period
    type FilingPeriod = {
        StartDate: Date
        EndDate: Date
        FiscalYear: int option
        Quarter: int option
    }

    module FilingPeriod =

        let createAnnual (fiscalYear: int) (startDate: Date) (endDate: Date) : FilingPeriod =
            {
                StartDate = startDate
                EndDate = endDate
                FiscalYear = Some fiscalYear
                Quarter = None
            }

        let createQuarterly (fiscalYear: int) (quarter: int) (startDate: Date) (endDate: Date) : FilingPeriod =
            {
                StartDate = startDate
                EndDate = endDate
                FiscalYear = Some fiscalYear
                Quarter = Some quarter
            }

    // ============================================
    // Regulatory Filing State
    // ============================================

    /// Filing state (immutable)
    type FilingState = {
        Id: FilingId
        CompanyId: CompanyId
        FilingNumber: FilingNumber
        FilingType: FilingType
        RegulatoryBody: RegulatoryBody

        // Period and deadline
        FilingPeriod: FilingPeriod option
        DueDate: Date
        SubmissionDeadline: Date

        // Status
        Status: FilingStatus
        SubmittedDate: Date option
        AcknowledgedDate: Date option
        AcceptedDate: Date option
        ReferenceNumber: string option

        // Attachments
        Attachments: FilingAttachment list

        // Preparer information
        PreparedBy: string option
        ReviewedBy: string option

        // Notes
        Notes: string option

        // Metadata
        CreatedAt: DateTimeOffset
        UpdatedAt: DateTimeOffset
    }

    module FilingState =

        let create
            (id: FilingId)
            (companyId: CompanyId)
            (filingNumber: FilingNumber)
            (filingType: FilingType)
            (regulatoryBody: RegulatoryBody)
            (dueDate: Date)
            : FilingState =

            {
                Id = id
                CompanyId = companyId
                FilingNumber = filingNumber
                FilingType = filingType
                RegulatoryBody = regulatoryBody
                FilingPeriod = None
                DueDate = dueDate
                SubmissionDeadline = dueDate
                Status = FilingStatus.Draft
                SubmittedDate = None
                AcknowledgedDate = None
                AcceptedDate = None
                ReferenceNumber = None
                Attachments = []
                PreparedBy = None
                ReviewedBy = None
                Notes = None
                CreatedAt = DateTimeOffset.UtcNow
                UpdatedAt = DateTimeOffset.UtcNow
            }

        let isSubmitted (state: FilingState) =
            match state.Status with
            | Submitted | Acknowledged | Accepted -> true
            | _ -> false

        let isComplete (state: FilingState) =
            state.Status = Accepted

        let isOverdue (asOfDate: Date) (state: FilingState) =
            not (isSubmitted state) && Date.isAfter asOfDate state.DueDate

    // ============================================
    // Regulatory Filing Aggregate
    // ============================================

    /// Regulatory filing aggregate root
    type RegulatoryFiling private (state: FilingState) =

        member _.State = state
        member _.Id = state.Id
        member _.CompanyId = state.CompanyId
        member _.FilingNumber = state.FilingNumber
        member _.FilingType = state.FilingType
        member _.RegulatoryBody = state.RegulatoryBody
        member _.DueDate = state.DueDate
        member _.Status = state.Status
        member _.SubmittedDate = state.SubmittedDate
        member _.AcceptedDate = state.AcceptedDate
        member _.Attachments = state.Attachments
        member _.IsSubmitted = FilingState.isSubmitted state
        member _.IsComplete = FilingState.isComplete state

        member this.IsOverdue(asOfDate: Date) = FilingState.isOverdue asOfDate state

        // ============================================
        // Commands
        // ============================================

        /// Set filing period
        member this.SetFilingPeriod(period: FilingPeriod)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (state.Status = FilingStatus.Draft || state.Status = FilingStatus.UnderPreparation)
                        (InvalidFiling "Cannot change period of submitted filing")

                return RegulatoryFiling({
                    state with
                        FilingPeriod = Some period
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Add attachment
        member this.AddAttachment(attachment: FilingAttachment)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (not (FilingState.isSubmitted state))
                        (FilingAlreadySubmitted (FilingNumber.value state.FilingNumber))

                return RegulatoryFiling({
                    state with
                        Attachments = state.Attachments @ [attachment]
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark attachment as uploaded
        member this.MarkAttachmentUploaded(attachmentId: Guid)
            : Result<RegulatoryFiling, FilingError> =

            result {
                let updated =
                    state.Attachments
                    |> List.map (fun a ->
                        if a.AttachmentId = attachmentId then
                            FilingAttachment.markUploaded a
                        else a)

                return RegulatoryFiling({
                    state with
                        Attachments = updated
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark as under preparation
        member this.StartPreparation(preparedBy: string)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (state.Status = FilingStatus.Draft)
                        (InvalidFiling "Filing must be in draft status")

                return RegulatoryFiling({
                    state with
                        Status = UnderPreparation
                        PreparedBy = Some preparedBy
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark as ready for submission
        member this.MarkReady(reviewedBy: string)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (state.Status = UnderPreparation)
                        (InvalidFiling "Filing must be under preparation")

                // Check all required attachments are uploaded
                let missingRequired =
                    state.Attachments
                    |> List.filter (fun a -> a.IsRequired && not (FilingAttachment.isUploaded a))

                do! Result.require
                        (missingRequired.IsEmpty)
                        (MissingRequiredAttachment "Required attachments not uploaded")

                return RegulatoryFiling({
                    state with
                        Status = ReadyForSubmission
                        ReviewedBy = Some reviewedBy
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Submit the filing
        member this.Submit(submittedDate: Date)
            : Result<RegulatoryFiling * ComplianceEvent, FilingError> =

            result {
                do! Result.require
                        (state.Status = ReadyForSubmission)
                        (InvalidFiling "Filing must be ready for submission")

                let newState = {
                    state with
                        Status = Submitted
                        SubmittedDate = Some submittedDate
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = FilingSubmitted {
                    Meta = ComplianceEventMeta.create state.CompanyId
                    FilingId = state.Id
                    FilingType = state.FilingType
                    FilingNumber = FilingNumber.value state.FilingNumber
                    SubmittedDate = submittedDate
                    Deadline = state.DueDate
                    RegulatoryBody = state.RegulatoryBody
                }

                return (RegulatoryFiling(newState), event)
            }

        /// Mark as acknowledged
        member this.MarkAcknowledged(acknowledgedDate: Date)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (state.Status = Submitted)
                        (InvalidFiling "Filing must be submitted")

                return RegulatoryFiling({
                    state with
                        Status = Acknowledged
                        AcknowledgedDate = Some acknowledgedDate
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark as accepted
        member this.MarkAccepted(acceptedDate: Date) (referenceNumber: string)
            : Result<RegulatoryFiling * ComplianceEvent, FilingError> =

            result {
                do! Result.require
                        (state.Status = Submitted || state.Status = Acknowledged)
                        (InvalidFiling "Filing must be submitted or acknowledged")

                let newState = {
                    state with
                        Status = Accepted
                        AcceptedDate = Some acceptedDate
                        ReferenceNumber = Some referenceNumber
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = FilingAccepted {
                    Meta = ComplianceEventMeta.create state.CompanyId
                    FilingId = state.Id
                    AcceptedDate = acceptedDate
                    Reference = referenceNumber
                }

                return (RegulatoryFiling(newState), event)
            }

        /// Mark as rejected
        member this.MarkRejected(reason: string)
            : Result<RegulatoryFiling, FilingError> =

            result {
                do! Result.require
                        (state.Status = Submitted || state.Status = Acknowledged)
                        (InvalidFiling "Filing must be submitted")

                return RegulatoryFiling({
                    state with
                        Status = Rejected reason
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new filing
        static member Create
            (companyId: CompanyId)
            (filingNumber: string)
            (filingType: FilingType)
            (regulatoryBody: RegulatoryBody)
            (dueDate: Date)
            : Result<RegulatoryFiling, FilingError> =

            result {
                let! number =
                    FilingNumber.create filingNumber
                    |> Result.mapError InvalidFiling

                let filingId = FilingId.create()
                let state = FilingState.create filingId companyId number filingType regulatoryBody dueDate

                return RegulatoryFiling(state)
            }

        /// Reconstitute from state
        static member FromState(state: FilingState) : RegulatoryFiling =
            RegulatoryFiling(state)

    // ============================================
    // Filing Logic
    // ============================================

    module FilingLogic =

        /// Get overdue filings
        let overdueFilings (asOfDate: Date) (filings: RegulatoryFiling list) =
            filings |> List.filter (fun f -> f.IsOverdue asOfDate)

        /// Get upcoming filings within days
        let upcomingFilings (days: int) (asOfDate: Date) (filings: RegulatoryFiling list) =
            let futureDate = Date.addDays days asOfDate
            filings
            |> List.filter (fun f ->
                not f.IsSubmitted &&
                Date.isOnOrBefore asOfDate f.DueDate &&
                Date.isOnOrBefore f.DueDate futureDate)

        /// Group by regulatory body
        let byRegulatoryBody (filings: RegulatoryFiling list) =
            filings |> List.groupBy (fun f -> f.RegulatoryBody) |> Map.ofList

        /// Group by filing type
        let byFilingType (filings: RegulatoryFiling list) =
            filings |> List.groupBy (fun f -> f.FilingType) |> Map.ofList

        /// Get filing deadlines for a regulatory body
        let filingDeadlines (regulatoryBody: RegulatoryBody) (filingType: FilingType) : string =
            match regulatoryBody, filingType with
            | TaxOffice, CorporateTaxReturn ->
                "決算日から2ヶ月以内（申告期限の延長が認められた場合は3ヶ月以内）"
            | TaxOffice, ConsumptionTaxReturn ->
                "課税期間終了後2ヶ月以内"
            | LaborStandardsOffice, OvertimeAgreement ->
                "有効期間満了前に更新届出"
            | LegalAffairsBureau, DirectorChangeFiling ->
                "変更から2週間以内"
            | _ -> "規制当局の指定する期限"
