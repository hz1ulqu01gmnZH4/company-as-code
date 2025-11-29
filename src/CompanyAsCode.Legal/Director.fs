namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Director entity and related types
module Director =

    open Events
    open Errors

    // ============================================
    // Director Types
    // ============================================

    /// Director status
    type DirectorStatus =
        | Active
        | TermExpired
        | Resigned
        | Dismissed
        | Deceased

    /// Director classification
    type DirectorClassification =
        | Inside            // 社内取締役
        | Outside           // 社外取締役
        | Independent       // 独立社外取締役

    /// Director compensation
    type DirectorCompensation = {
        BaseSalary: Financial.Money option
        Bonus: Financial.Money option
        StockOptions: int64 option
        OtherBenefits: string list
    }

    module DirectorCompensation =

        let empty = {
            BaseSalary = None
            Bonus = None
            StockOptions = None
            OtherBenefits = []
        }

    // ============================================
    // Director State (Immutable)
    // ============================================

    /// Internal state of a director
    type DirectorState = {
        Id: DirectorId
        PersonName: PersonName
        Position: DirectorPosition
        Classification: DirectorClassification
        Term: TermPeriod
        Status: DirectorStatus
        IsRepresentative: bool
        Contact: ContactInfo
        Compensation: DirectorCompensation
        AppointedAt: Date
        RegistrationStatus: RegistrationStatus
    }

    /// Registration status with Legal Affairs Bureau
    and RegistrationStatus =
        | Pending
        | Registered of registrationDate: Date
        | Deregistered of deregistrationDate: Date

    module DirectorState =

        let create
            (id: DirectorId)
            (name: PersonName)
            (position: DirectorPosition)
            (classification: DirectorClassification)
            (term: TermPeriod)
            (appointedAt: Date)
            : DirectorState =
            {
                Id = id
                PersonName = name
                Position = position
                Classification = classification
                Term = term
                Status = Active
                IsRepresentative = false
                Contact = ContactInfo.empty
                Compensation = DirectorCompensation.empty
                AppointedAt = appointedAt
                RegistrationStatus = Pending
            }

        let isActive (state: DirectorState) =
            state.Status = Active

        let isOutside (state: DirectorState) =
            match state.Classification with
            | Outside | Independent -> true
            | Inside -> false

        let isTermExpired (asOfDate: Date) (state: DirectorState) =
            TermPeriod.isExpired asOfDate state.Term

    // ============================================
    // Director Entity (Class with encapsulated state)
    // ============================================

    /// Director entity
    type Director private (state: DirectorState) =

        /// Current state (read-only)
        member _.State = state

        // Expose key properties
        member _.Id = state.Id
        member _.Name = state.PersonName
        member _.Position = state.Position
        member _.Classification = state.Classification
        member _.Term = state.Term
        member _.Status = state.Status
        member _.IsRepresentative = state.IsRepresentative
        member _.IsActive = DirectorState.isActive state
        member _.IsOutsideDirector = DirectorState.isOutside state

        /// Check if term is expired as of date
        member _.IsTermExpiredOn(date: Date) =
            DirectorState.isTermExpired date state

        /// Days remaining in term
        member _.DaysRemainingInTerm(asOfDate: Date) =
            TermPeriod.daysRemaining asOfDate state.Term

        // ============================================
        // Commands (return Result with new Director)
        // ============================================

        /// Designate as representative director
        member this.DesignateAsRepresentative()
            : Result<Director, DirectorError> =
            result {
                do! Result.require
                        (state.Status = Active)
                        (DirectorNotActive state.Id)

                return Director({ state with IsRepresentative = true })
            }

        /// Remove representative designation
        member this.RemoveRepresentativeDesignation()
            : Result<Director, DirectorError> =
            result {
                return Director({ state with IsRepresentative = false })
            }

        /// Update contact information
        member this.UpdateContact(contact: ContactInfo)
            : Director =
            Director({ state with Contact = contact })

        /// Update compensation
        member this.UpdateCompensation(compensation: DirectorCompensation)
            : Director =
            Director({ state with Compensation = compensation })

        /// Mark as registered with Legal Affairs Bureau
        member this.MarkAsRegistered(registrationDate: Date)
            : Director =
            Director({ state with RegistrationStatus = Registered registrationDate })

        /// Renew term
        member this.RenewTerm(newTermYears: int) (renewalDate: Date)
            : Result<Director, DirectorError> =
            result {
                let maxYears =
                    match state.Position with
                    | DirectorPosition.OutsideDirector -> 2
                    | _ -> 2  // Standard max is 2 years per Companies Act

                do! Result.require
                        (newTermYears <= maxYears)
                        (TermExceedsMaximum (maxYears, newTermYears))

                let! newTerm =
                    TermPeriod.directorTerm renewalDate newTermYears
                    |> Result.mapError (fun msg -> InvalidTerm msg)

                return Director({
                    state with
                        Term = newTerm
                        Status = Active
                })
            }

        /// Resign from position
        member this.Resign(resignationDate: Date)
            : Director =
            Director({
                state with
                    Status = Resigned
                    RegistrationStatus = Deregistered resignationDate
            })

        /// Dismiss from position
        member this.Dismiss(dismissalDate: Date)
            : Director =
            Director({
                state with
                    Status = Dismissed
                    RegistrationStatus = Deregistered dismissalDate
            })

        /// Mark term as expired
        member this.ExpireTerm()
            : Director =
            Director({ state with Status = TermExpired })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new director
        static member Create
            (id: DirectorId)
            (name: PersonName)
            (position: DirectorPosition)
            (classification: DirectorClassification)
            (termYears: int)
            (appointmentDate: Date)
            : Result<Director, DirectorError> =

            result {
                let maxYears = 2  // Companies Act maximum

                do! Result.require
                        (termYears <= maxYears)
                        (TermExceedsMaximum (maxYears, termYears))

                let! term =
                    TermPeriod.directorTerm appointmentDate termYears
                    |> Result.mapError (fun msg -> InvalidTerm msg)

                let state = DirectorState.create id name position classification term appointmentDate
                return Director(state)
            }

        /// Create an inside director
        static member CreateInsideDirector
            (name: PersonName)
            (position: DirectorPosition)
            (termYears: int)
            (appointmentDate: Date)
            : Result<Director, DirectorError> =

            Director.Create
                (DirectorId.create())
                name
                position
                Inside
                termYears
                appointmentDate

        /// Create an outside director
        static member CreateOutsideDirector
            (name: PersonName)
            (termYears: int)
            (appointmentDate: Date)
            : Result<Director, DirectorError> =

            Director.Create
                (DirectorId.create())
                name
                DirectorPosition.OutsideDirector
                Outside
                termYears
                appointmentDate

        /// Create an independent outside director
        static member CreateIndependentDirector
            (name: PersonName)
            (termYears: int)
            (appointmentDate: Date)
            : Result<Director, DirectorError> =

            Director.Create
                (DirectorId.create())
                name
                DirectorPosition.OutsideDirector
                Independent
                termYears
                appointmentDate

        /// Reconstitute from state (for persistence)
        static member FromState(state: DirectorState) : Director =
            Director(state)

    // ============================================
    // Pure Logic Functions (Module)
    // ============================================

    /// Pure business logic for director operations
    module DirectorLogic =

        /// Maximum director term in years (Companies Act)
        let maxTermYears = 2

        /// Maximum auditor term in years (Companies Act)
        let maxAuditorTermYears = 4

        /// Check if director can be appointed to position
        let canAppointToPosition
            (existingDirectors: Director list)
            (position: DirectorPosition)
            : Result<unit, DirectorError> =

            // Business rule: Only one President
            match position with
            | DirectorPosition.President ->
                let hasPresident =
                    existingDirectors
                    |> List.exists (fun d -> d.Position = DirectorPosition.President && d.IsActive)

                if hasPresident then
                    Error (InvalidDirectorName "Company already has a President")
                else
                    Ok ()
            | _ -> Ok ()

        /// Validate director term
        let validateTerm (termYears: int) : Result<unit, DirectorError> =
            if termYears <= 0 then
                Error (InvalidTerm "Term must be at least 1 year")
            elif termYears > maxTermYears then
                Error (TermExceedsMaximum (maxTermYears, termYears))
            else
                Ok ()

        /// Check if outside director requirement is met
        let meetsOutsideDirectorRequirement
            (directors: Director list)
            (requiredCount: int)
            : bool =

            directors
            |> List.filter (fun d -> d.IsActive && d.IsOutsideDirector)
            |> List.length
            |> (>=) requiredCount

        /// Get active directors
        let getActiveDirectors (directors: Director list) : Director list =
            directors |> List.filter (fun d -> d.IsActive)

        /// Get representative directors
        let getRepresentativeDirectors (directors: Director list) : Director list =
            directors |> List.filter (fun d -> d.IsActive && d.IsRepresentative)

        /// Count outside directors
        let countOutsideDirectors (directors: Director list) : int =
            directors
            |> List.filter (fun d -> d.IsActive && d.IsOutsideDirector)
            |> List.length

        /// Find directors with expiring terms
        let findExpiringTerms (withinDays: int) (asOfDate: Date) (directors: Director list) : Director list =
            directors
            |> List.filter (fun d ->
                d.IsActive && d.DaysRemainingInTerm(asOfDate) <= withinDays)
