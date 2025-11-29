namespace CompanyAsCode.Operations

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Contract aggregate
module Contract =

    open Events
    open Errors

    // ============================================
    // Contract Number (Value Object)
    // ============================================

    /// Contract number - unique identifier for contracts
    type ContractNumber = private ContractNumber of string

    module ContractNumber =

        let create (number: string) : Result<ContractNumber, string> =
            if System.String.IsNullOrWhiteSpace(number) then
                Error "Contract number cannot be empty"
            elif number.Length < 5 || number.Length > 30 then
                Error "Contract number must be 5-30 characters"
            else
                Ok (ContractNumber (number.ToUpperInvariant()))

        let value (ContractNumber num) = num

        /// Generate contract number with prefix and date
        let generate (prefix: string) (year: int) (sequence: int) : ContractNumber =
            ContractNumber $"{prefix}-{year}-{sequence:D5}"

    // ============================================
    // Contract Term (Value Object)
    // ============================================

    /// Contract term/duration
    type ContractTerm = {
        StartDate: Date
        EndDate: Date
        AutoRenewal: bool
        RenewalNoticeDays: int      // 更新拒否通知期限（日数）
        TerminationNoticeDays: int  // 解約予告期間（日数）
    }

    module ContractTerm =

        let create
            (startDate: Date)
            (endDate: Date)
            (autoRenewal: bool)
            : Result<ContractTerm, string> =

            if Date.isAfter startDate endDate then
                Error "End date must be after start date"
            else
                Ok {
                    StartDate = startDate
                    EndDate = endDate
                    AutoRenewal = autoRenewal
                    RenewalNoticeDays = 30  // Default 30 days
                    TerminationNoticeDays = 30
                }

        let durationMonths (term: ContractTerm) : int =
            let months = (Date.year term.EndDate - Date.year term.StartDate) * 12
                         + (Date.month term.EndDate - Date.month term.StartDate)
            max 1 months

        let isExpired (asOfDate: Date) (term: ContractTerm) : bool =
            Date.isAfter asOfDate term.EndDate

        let daysUntilExpiration (asOfDate: Date) (term: ContractTerm) : int =
            Date.daysBetween asOfDate term.EndDate

    // ============================================
    // Contract Clause (Value Object)
    // ============================================

    /// Standard contract clause types
    type ClauseType =
        | Scope                   // 契約範囲
        | Compensation            // 報酬
        | PaymentTerms            // 支払条件
        | Confidentiality         // 秘密保持
        | IntellectualProperty    // 知的財産権
        | Warranty                // 保証
        | Limitation              // 責任制限
        | Termination             // 解約
        | DisputeResolution       // 紛争解決
        | GoverningLaw            // 準拠法
        | Custom of name: string

    /// Contract clause
    type ContractClause = {
        ClauseType: ClauseType
        Title: string
        Content: string
        IsRequired: bool
    }

    // ============================================
    // Contract Amendment (Value Object)
    // ============================================

    /// Contract amendment record
    type ContractAmendment = {
        AmendmentNumber: int
        AmendmentDate: Date
        Description: string
        ChangedClauses: ClauseType list
        SignedBy: string
        CounterpartySignatory: string
    }

    // ============================================
    // Contract State
    // ============================================

    /// Contract state (immutable)
    type ContractState = {
        Id: ContractId
        CompanyId: CompanyId
        ContractNumber: ContractNumber
        ContractType: ContractType
        Title: string
        Description: string option

        // Parties
        CounterpartyId: BusinessPartnerId
        CounterpartyName: string
        CompanySignatory: string option
        CounterpartySignatory: string option

        // Terms
        Term: ContractTerm
        Value: Money
        Currency: string

        // Content
        Clauses: ContractClause list
        Amendments: ContractAmendment list

        // Status
        Status: ContractStatus
        SignedDate: Date option

        // Linked entities
        ProjectId: ProjectId option

        // Metadata
        CreatedAt: DateTimeOffset
        UpdatedAt: DateTimeOffset
    }

    module ContractState =

        let create
            (id: ContractId)
            (companyId: CompanyId)
            (contractNumber: ContractNumber)
            (contractType: ContractType)
            (title: string)
            (counterpartyId: BusinessPartnerId)
            (counterpartyName: string)
            (term: ContractTerm)
            (value: Money)
            : ContractState =

            {
                Id = id
                CompanyId = companyId
                ContractNumber = contractNumber
                ContractType = contractType
                Title = title
                Description = None
                CounterpartyId = counterpartyId
                CounterpartyName = counterpartyName
                CompanySignatory = None
                CounterpartySignatory = None
                Term = term
                Value = value
                Currency = "JPY"
                Clauses = []
                Amendments = []
                Status = ContractStatus.Draft
                SignedDate = None
                ProjectId = None
                CreatedAt = DateTimeOffset.UtcNow
                UpdatedAt = DateTimeOffset.UtcNow
            }

        let isActive (state: ContractState) =
            state.Status = ContractStatus.Active

        let isSigned (state: ContractState) =
            match state.Status with
            | ContractStatus.Signed | ContractStatus.Active | ContractStatus.Renewed -> true
            | _ -> false

        let canModify (state: ContractState) =
            match state.Status with
            | ContractStatus.Draft | ContractStatus.UnderReview | ContractStatus.Negotiating -> true
            | _ -> false

    // ============================================
    // Contract Aggregate
    // ============================================

    /// Contract aggregate root
    type Contract private (state: ContractState) =

        member _.State = state
        member _.Id = state.Id
        member _.CompanyId = state.CompanyId
        member _.ContractNumber = state.ContractNumber
        member _.ContractType = state.ContractType
        member _.Title = state.Title
        member _.CounterpartyId = state.CounterpartyId
        member _.CounterpartyName = state.CounterpartyName
        member _.Term = state.Term
        member _.Value = state.Value
        member _.Status = state.Status
        member _.SignedDate = state.SignedDate
        member _.Clauses = state.Clauses
        member _.Amendments = state.Amendments
        member _.IsActive = ContractState.isActive state
        member _.IsSigned = ContractState.isSigned state
        member _.CanModify = ContractState.canModify state

        // ============================================
        // Commands
        // ============================================

        /// Add a clause
        member this.AddClause(clause: ContractClause)
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (ContractState.canModify state)
                        (ContractAlreadySigned (ContractNumber.value state.ContractNumber))

                return Contract({
                    state with
                        Clauses = state.Clauses @ [clause]
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Submit for review
        member this.SubmitForReview()
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Draft)
                        (InvalidContractTerm "Contract must be in draft status")

                do! Result.require
                        (state.Clauses.Length > 0)
                        (InvalidContractTerm "Contract must have at least one clause")

                return Contract({ state with Status = ContractStatus.UnderReview; UpdatedAt = DateTimeOffset.UtcNow })
            }

        /// Start negotiation
        member this.StartNegotiation()
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.UnderReview)
                        (InvalidContractTerm "Contract must be under review")

                return Contract({ state with Status = ContractStatus.Negotiating; UpdatedAt = DateTimeOffset.UtcNow })
            }

        /// Sign the contract
        member this.Sign
            (signedDate: Date)
            (companySignatory: string)
            (counterpartySignatory: string)
            : Result<Contract * OperationsEvent, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Negotiating || state.Status = ContractStatus.UnderReview || state.Status = ContractStatus.Draft)
                        (ContractAlreadySigned (ContractNumber.value state.ContractNumber))

                // Check required clauses for non-NDA contracts
                let needsConfidentiality = state.ContractType <> NDA
                let hasConfidentiality =
                    state.Clauses |> List.exists (fun c -> c.ClauseType = Confidentiality)

                do! Result.require
                        (not needsConfidentiality || hasConfidentiality)
                        (MissingRequiredClause "Confidentiality")

                let newState = {
                    state with
                        Status = ContractStatus.Signed
                        SignedDate = Some signedDate
                        CompanySignatory = Some companySignatory
                        CounterpartySignatory = Some counterpartySignatory
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ContractSigned {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ContractId = state.Id
                    SignedDate = signedDate
                    SignedBy = companySignatory
                    CounterpartySignatory = counterpartySignatory
                }

                return (Contract(newState), event)
            }

        /// Activate the contract
        member this.Activate()
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Signed)
                        (InvalidContractTerm "Contract must be signed to activate")

                return Contract({ state with Status = ContractStatus.Active; UpdatedAt = DateTimeOffset.UtcNow })
            }

        /// Terminate the contract
        member this.Terminate(terminationDate: Date) (reason: string) (terminatedBy: string)
            : Result<Contract * OperationsEvent, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Active || state.Status = ContractStatus.Signed)
                        (CannotTerminateActiveContract "Contract must be active or signed")

                let newState = {
                    state with
                        Status = ContractStatus.Terminated
                        Term = { state.Term with EndDate = terminationDate }
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ContractTerminated {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ContractId = state.Id
                    TerminationDate = terminationDate
                    Reason = reason
                    TerminatedBy = terminatedBy
                }

                return (Contract(newState), event)
            }

        /// Amend the contract
        member this.Amend
            (description: string)
            (changedClauses: ClauseType list)
            (amendmentDate: Date)
            (signedBy: string)
            (counterpartySignatory: string)
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Active || state.Status = ContractStatus.Signed)
                        (ContractAmendmentError "Only active or signed contracts can be amended")

                let amendment = {
                    AmendmentNumber = state.Amendments.Length + 1
                    AmendmentDate = amendmentDate
                    Description = description
                    ChangedClauses = changedClauses
                    SignedBy = signedBy
                    CounterpartySignatory = counterpartySignatory
                }

                return Contract({
                    state with
                        Amendments = state.Amendments @ [amendment]
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Renew the contract
        member this.Renew(newEndDate: Date)
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Active || state.Status = ContractStatus.Expired)
                        (InvalidContractTerm "Only active or expired contracts can be renewed")

                do! Result.require
                        (Date.isAfter newEndDate state.Term.EndDate)
                        (InvalidContractTerm "New end date must be after current end date")

                let newTerm = { state.Term with EndDate = newEndDate }

                return Contract({
                    state with
                        Status = ContractStatus.Renewed
                        Term = newTerm
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Mark as expired
        member this.MarkExpired()
            : Result<Contract, ContractError> =

            result {
                do! Result.require
                        (state.Status = ContractStatus.Active || state.Status = ContractStatus.Renewed)
                        (InvalidContractTerm "Only active contracts can expire")

                return Contract({ state with Status = ContractStatus.Expired; UpdatedAt = DateTimeOffset.UtcNow })
            }

        /// Link to project
        member this.LinkToProject(projectId: ProjectId)
            : Contract =
            Contract({ state with ProjectId = Some projectId; UpdatedAt = DateTimeOffset.UtcNow })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new contract
        static member Create
            (companyId: CompanyId)
            (contractNumber: string)
            (contractType: ContractType)
            (title: string)
            (counterpartyId: BusinessPartnerId)
            (counterpartyName: string)
            (startDate: Date)
            (endDate: Date)
            (value: Money)
            : Result<Contract * OperationsEvent, ContractError> =

            result {
                let! contractNum =
                    ContractNumber.create contractNumber
                    |> Result.mapError InvalidContractNumber

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(title)))
                        (InvalidContractNumber "Contract title cannot be empty")

                let! term =
                    ContractTerm.create startDate endDate false
                    |> Result.mapError InvalidContractTerm

                let contractId = ContractId.create()
                let state = ContractState.create
                                contractId
                                companyId
                                contractNum
                                contractType
                                title
                                counterpartyId
                                counterpartyName
                                term
                                value

                let event = ContractCreated {
                    Meta = OperationsEventMeta.create companyId
                    ContractId = contractId
                    ContractNumber = contractNumber
                    ContractType = contractType
                    CounterpartyId = counterpartyId.Id.ToString()
                    ContractValue = value
                    StartDate = startDate
                    EndDate = endDate
                }

                return (Contract(state), event)
            }

        /// Reconstitute from state
        static member FromState(state: ContractState) : Contract =
            Contract(state)

    // ============================================
    // Contract Logic
    // ============================================

    module ContractLogic =

        /// Get contracts expiring within days
        let expiringWithin (days: int) (asOfDate: Date) (contracts: Contract list) : Contract list =
            contracts
            |> List.filter (fun c ->
                c.IsActive &&
                ContractTerm.daysUntilExpiration asOfDate c.Term <= days &&
                ContractTerm.daysUntilExpiration asOfDate c.Term > 0)

        /// Calculate total contract value
        let totalValue (contracts: Contract list) : Money =
            contracts
            |> List.fold (fun acc c ->
                Money.add acc c.Value |> Result.defaultValue acc
            ) (Money.yen 0m)

        /// Get standard clause template
        let standardClause (clauseType: ClauseType) : ContractClause =
            match clauseType with
            | Confidentiality ->
                {
                    ClauseType = Confidentiality
                    Title = "秘密保持"
                    Content = "両当事者は、本契約に関連して知り得た相手方の秘密情報を、事前の書面による同意なく第三者に開示しないものとする。"
                    IsRequired = true
                }
            | GoverningLaw ->
                {
                    ClauseType = GoverningLaw
                    Title = "準拠法"
                    Content = "本契約は、日本法に準拠し、日本法に従って解釈されるものとする。"
                    IsRequired = true
                }
            | DisputeResolution ->
                {
                    ClauseType = DisputeResolution
                    Title = "紛争解決"
                    Content = "本契約に関する紛争については、東京地方裁判所を第一審の専属的合意管轄裁判所とする。"
                    IsRequired = true
                }
            | _ ->
                {
                    ClauseType = clauseType
                    Title = $"{clauseType}"
                    Content = ""
                    IsRequired = false
                }
