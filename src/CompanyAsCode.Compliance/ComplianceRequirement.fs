namespace CompanyAsCode.Compliance

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Temporal

/// Compliance Requirement aggregate
module ComplianceRequirement =

    open Events
    open Errors

    // ============================================
    // Requirement Code (Value Object)
    // ============================================

    /// Unique requirement code
    type RequirementCode = private RequirementCode of string

    module RequirementCode =

        let create (code: string) : Result<RequirementCode, string> =
            if System.String.IsNullOrWhiteSpace(code) then
                Error "Requirement code cannot be empty"
            elif code.Length < 3 || code.Length > 20 then
                Error "Requirement code must be 3-20 characters"
            else
                Ok (RequirementCode (code.ToUpperInvariant()))

        let value (RequirementCode code) = code

    // ============================================
    // Compliance Check Record (Value Object)
    // ============================================

    /// Record of a compliance check
    type ComplianceCheck = {
        CheckDate: Date
        CheckedBy: string
        Status: ComplianceStatus
        Findings: string option
        Evidence: string list
        NextCheckDue: Date option
    }

    module ComplianceCheck =

        let create
            (checkDate: Date)
            (checkedBy: string)
            (status: ComplianceStatus)
            : ComplianceCheck =
            {
                CheckDate = checkDate
                CheckedBy = checkedBy
                Status = status
                Findings = None
                Evidence = []
                NextCheckDue = None
            }

        let isCompliant (check: ComplianceCheck) =
            match check.Status with
            | Compliant | NotApplicable -> true
            | _ -> false

    // ============================================
    // Compliance Requirement State
    // ============================================

    /// Requirement state (immutable)
    type RequirementState = {
        Id: RequirementId
        CompanyId: CompanyId
        Code: RequirementCode
        Title: string
        Description: string

        // Classification
        Category: RequirementCategory
        RegulatoryBody: RegulatoryBody
        Frequency: ComplianceFrequency

        // Legal reference
        LegalBasis: string option           // 法的根拠
        Penalties: string option            // 罰則規定

        // Status
        CurrentStatus: ComplianceStatus
        LastCheckDate: Date option
        NextDueDate: Date option

        // History
        CheckHistory: ComplianceCheck list

        // Metadata
        IsActive: bool
        CreatedAt: DateTimeOffset
        UpdatedAt: DateTimeOffset
    }

    module RequirementState =

        let create
            (id: RequirementId)
            (companyId: CompanyId)
            (code: RequirementCode)
            (title: string)
            (description: string)
            (category: RequirementCategory)
            (regulatoryBody: RegulatoryBody)
            (frequency: ComplianceFrequency)
            : RequirementState =

            {
                Id = id
                CompanyId = companyId
                Code = code
                Title = title
                Description = description
                Category = category
                RegulatoryBody = regulatoryBody
                Frequency = frequency
                LegalBasis = None
                Penalties = None
                CurrentStatus = Unknown
                LastCheckDate = None
                NextDueDate = None
                CheckHistory = []
                IsActive = true
                CreatedAt = DateTimeOffset.UtcNow
                UpdatedAt = DateTimeOffset.UtcNow
            }

        let isCompliant (state: RequirementState) =
            match state.CurrentStatus with
            | Compliant | NotApplicable -> true
            | _ -> false

        let isOverdue (asOfDate: Date) (state: RequirementState) =
            match state.NextDueDate with
            | Some dueDate -> Date.isAfter asOfDate dueDate
            | None -> false

    // ============================================
    // Compliance Requirement Aggregate
    // ============================================

    /// Compliance requirement aggregate root
    type ComplianceRequirement private (state: RequirementState) =

        member _.State = state
        member _.Id = state.Id
        member _.CompanyId = state.CompanyId
        member _.Code = state.Code
        member _.Title = state.Title
        member _.Description = state.Description
        member _.Category = state.Category
        member _.RegulatoryBody = state.RegulatoryBody
        member _.Frequency = state.Frequency
        member _.CurrentStatus = state.CurrentStatus
        member _.LastCheckDate = state.LastCheckDate
        member _.NextDueDate = state.NextDueDate
        member _.IsCompliant = RequirementState.isCompliant state
        member _.IsActive = state.IsActive

        member this.IsOverdue(asOfDate: Date) = RequirementState.isOverdue asOfDate state

        // ============================================
        // Commands
        // ============================================

        /// Record a compliance check
        member this.RecordCheck
            (check: ComplianceCheck)
            : Result<ComplianceRequirement * ComplianceEvent, ComplianceError> =

            result {
                do! Result.require
                        state.IsActive
                        (InvalidRequirement "Cannot check inactive requirement")

                let nextDue = this.CalculateNextDueDate check.CheckDate

                let newState = {
                    state with
                        CurrentStatus = check.Status
                        LastCheckDate = Some check.CheckDate
                        NextDueDate = nextDue
                        CheckHistory = state.CheckHistory @ [check]
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ComplianceCheckCompleted {
                    Meta = ComplianceEventMeta.create state.CompanyId
                    RequirementId = state.Id
                    CheckDate = check.CheckDate
                    Status = check.Status
                    Findings = check.Findings
                    NextCheckDue = nextDue
                }

                return (ComplianceRequirement(newState), event)
            }

        /// Mark as compliant
        member this.MarkCompliant(checkedBy: string) (checkDate: Date)
            : Result<ComplianceRequirement * ComplianceEvent, ComplianceError> =

            let check = ComplianceCheck.create checkDate checkedBy Compliant
            this.RecordCheck check

        /// Mark as non-compliant
        member this.MarkNonCompliant(checkedBy: string) (checkDate: Date) (findings: string)
            : Result<ComplianceRequirement * ComplianceEvent, ComplianceError> =

            let check = { ComplianceCheck.create checkDate checkedBy NonCompliant with Findings = Some findings }
            this.RecordCheck check

        /// Set legal basis
        member this.SetLegalBasis(legalBasis: string)
            : ComplianceRequirement =

            ComplianceRequirement({
                state with
                    LegalBasis = Some legalBasis
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        /// Set penalties information
        member this.SetPenalties(penalties: string)
            : ComplianceRequirement =

            ComplianceRequirement({
                state with
                    Penalties = Some penalties
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        /// Deactivate the requirement
        member this.Deactivate()
            : ComplianceRequirement =

            ComplianceRequirement({
                state with
                    IsActive = false
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        /// Reactivate the requirement
        member this.Reactivate()
            : ComplianceRequirement =

            ComplianceRequirement({
                state with
                    IsActive = true
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        // ============================================
        // Private Helpers
        // ============================================

        member private this.CalculateNextDueDate(fromDate: Date) : Date option =
            match state.Frequency with
            | OneTime -> None
            | Annual -> Some (Date.addYears 1 fromDate)
            | SemiAnnual -> Some (Date.addMonths 6 fromDate)
            | Quarterly -> Some (Date.addMonths 3 fromDate)
            | Monthly -> Some (Date.addMonths 1 fromDate)
            | AsNeeded -> None
            | OnChange -> None

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new compliance requirement
        static member Create
            (companyId: CompanyId)
            (code: string)
            (title: string)
            (description: string)
            (category: RequirementCategory)
            (regulatoryBody: RegulatoryBody)
            (frequency: ComplianceFrequency)
            : Result<ComplianceRequirement * ComplianceEvent, ComplianceError> =

            result {
                let! reqCode =
                    RequirementCode.create code
                    |> Result.mapError InvalidRequirement

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(title)))
                        (InvalidRequirement "Title cannot be empty")

                let reqId = RequirementId.create()
                let state = RequirementState.create
                                reqId
                                companyId
                                reqCode
                                title
                                description
                                category
                                regulatoryBody
                                frequency

                let event = RequirementCreated {
                    Meta = ComplianceEventMeta.create companyId
                    RequirementId = reqId
                    RequirementCode = code
                    Description = description
                    Category = category
                    RegulatoryBody = regulatoryBody
                    Frequency = frequency
                }

                return (ComplianceRequirement(state), event)
            }

        /// Reconstitute from state
        static member FromState(state: RequirementState) : ComplianceRequirement =
            ComplianceRequirement(state)

    // ============================================
    // Compliance Logic
    // ============================================

    module ComplianceLogic =

        /// Get overdue requirements
        let overdueRequirements (asOfDate: Date) (requirements: ComplianceRequirement list) =
            requirements |> List.filter (fun r -> r.IsOverdue asOfDate)

        /// Get non-compliant requirements
        let nonCompliantRequirements (requirements: ComplianceRequirement list) =
            requirements |> List.filter (fun r -> not r.IsCompliant && r.IsActive)

        /// Group by category
        let byCategory (requirements: ComplianceRequirement list) =
            requirements |> List.groupBy (fun r -> r.Category) |> Map.ofList

        /// Group by regulatory body
        let byRegulatoryBody (requirements: ComplianceRequirement list) =
            requirements |> List.groupBy (fun r -> r.RegulatoryBody) |> Map.ofList
