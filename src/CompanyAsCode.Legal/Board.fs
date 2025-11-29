namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Temporal

/// Board of Directors aggregate
module Board =

    open Director
    open Events
    open Errors

    // ============================================
    // Board Types
    // ============================================

    /// Board meeting type
    type MeetingType =
        | Regular           // 定例取締役会
        | Extraordinary     // 臨時取締役会
        | Written           // 書面決議

    /// Meeting attendance record
    type AttendanceRecord = {
        DirectorId: DirectorId
        Present: bool
        ProxyId: DirectorId option
    }

    /// Board meeting record
    type BoardMeeting = {
        MeetingDate: Date
        MeetingType: MeetingType
        Attendees: AttendanceRecord list
        QuorumMet: bool
        Minutes: string option
    }

    /// Resolution status
    type ResolutionStatus =
        | Proposed
        | Passed
        | Rejected
        | Withdrawn

    /// Board resolution
    type BoardResolution = {
        ResolutionType: ResolutionType
        Description: string
        ProposedDate: Date
        VotesFor: int
        VotesAgainst: int
        Abstentions: int
        Status: ResolutionStatus
        PassedDate: Date option
    }

    // ============================================
    // Board State
    // ============================================

    /// Board state
    type BoardState = {
        BoardId: BoardId
        CompanyId: CompanyId
        Structure: BoardStructure
        Directors: Map<DirectorId, Director>
        RepresentativeDirectorId: DirectorId option
        Meetings: BoardMeeting list
        Resolutions: BoardResolution list
        EstablishedDate: Date
    }

    module BoardState =

        let create
            (companyId: CompanyId)
            (structure: BoardStructure)
            (establishedDate: Date)
            : BoardState =
            {
                BoardId = BoardId.create()
                CompanyId = companyId
                Structure = structure
                Directors = Map.empty
                RepresentativeDirectorId = None
                Meetings = []
                Resolutions = []
                EstablishedDate = establishedDate
            }

        let directorCount (state: BoardState) : int =
            state.Directors
            |> Map.toSeq
            |> Seq.filter (fun (_, d) -> d.IsActive)
            |> Seq.length

        let outsideDirectorCount (state: BoardState) : int =
            state.Directors
            |> Map.toSeq
            |> Seq.filter (fun (_, d) -> d.IsActive && d.IsOutsideDirector)
            |> Seq.length

        let hasRepresentativeDirector (state: BoardState) : bool =
            state.RepresentativeDirectorId.IsSome

        let minimumDirectorsFor (structure: BoardStructure) : int =
            match structure with
            | BoardStructure.WithoutBoard -> 1
            | BoardStructure.WithStatutoryAuditors -> 3
            | BoardStructure.WithAuditCommittee -> 3
            | BoardStructure.WithThreeCommittees -> 3

        let quorumFor (totalDirectors: int) : int =
            (totalDirectors + 1) / 2  // Majority

    // ============================================
    // Board Aggregate
    // ============================================

    /// Board of Directors aggregate root
    type Board private (state: BoardState) =

        member _.State = state
        member _.BoardId = state.BoardId
        member _.CompanyId = state.CompanyId
        member _.Structure = state.Structure
        member _.EstablishedDate = state.EstablishedDate
        member _.RepresentativeDirectorId = state.RepresentativeDirectorId

        member _.DirectorCount = BoardState.directorCount state
        member _.OutsideDirectorCount = BoardState.outsideDirectorCount state
        member _.HasRepresentativeDirector = BoardState.hasRepresentativeDirector state
        member _.MinimumDirectors = BoardState.minimumDirectorsFor state.Structure

        /// Get all active directors
        member _.GetActiveDirectors() : Director list =
            state.Directors
            |> Map.toList
            |> List.map snd
            |> List.filter (fun d -> d.IsActive)

        /// Get director by ID
        member _.GetDirector(directorId: DirectorId) : Director option =
            Map.tryFind directorId state.Directors

        /// Get representative director
        member _.GetRepresentativeDirector() : Director option =
            state.RepresentativeDirectorId
            |> Option.bind (fun id -> Map.tryFind id state.Directors)

        /// Calculate quorum
        member this.Quorum = BoardState.quorumFor this.DirectorCount

        // ============================================
        // Commands
        // ============================================

        /// Add a director to the board
        member this.AddDirector(director: Director)
            : Result<Board, BoardError> =

            result {
                do! Result.require
                        (not (Map.containsKey director.Id state.Directors))
                        (DirectorError (DirectorAlreadyOnBoard director.Id))

                let newDirectors = Map.add director.Id director state.Directors
                return Board({ state with Directors = newDirectors })
            }

        /// Remove a director from the board
        member this.RemoveDirector(directorId: DirectorId)
            : Result<Board, BoardError> =

            result {
                do! Result.require
                        (Map.containsKey directorId state.Directors)
                        (DirectorError (DirectorNotOnBoard directorId))

                let activeCount = BoardState.directorCount state

                do! Result.require
                        (activeCount > 1)
                        (DirectorError CannotRemoveLastDirector)

                // Cannot remove representative without designating new one
                do! match state.RepresentativeDirectorId with
                    | Some repId when repId = directorId ->
                        Error (DirectorError CannotRemoveRepresentativeDirector)
                    | _ -> Ok ()

                // Mark director as resigned/removed instead of deleting
                let! director =
                    Map.tryFind directorId state.Directors
                    |> Result.ofOption (DirectorError (DirectorNotOnBoard directorId))

                let removedDirector = director.Resign(Date.today())
                let newDirectors = Map.add directorId removedDirector state.Directors

                return Board({ state with Directors = newDirectors })
            }

        /// Designate representative director
        member this.DesignateRepresentativeDirector(directorId: DirectorId)
            : Result<Board, BoardError> =

            result {
                let! director =
                    Map.tryFind directorId state.Directors
                    |> Result.ofOption (DirectorError (DirectorNotOnBoard directorId))

                do! Result.require
                        director.IsActive
                        (DirectorError (DirectorNotActive directorId))

                // Update previous representative if exists
                let updatedDirectors =
                    match state.RepresentativeDirectorId with
                    | Some prevId when prevId <> directorId ->
                        match Map.tryFind prevId state.Directors with
                        | Some prev ->
                            let updated = prev.RemoveRepresentativeDesignation() |> Result.defaultValue prev
                            Map.add prevId updated state.Directors
                        | None -> state.Directors
                    | _ -> state.Directors

                // Designate new representative
                let! newRepresentative = director.DesignateAsRepresentative()
                                         |> Result.mapError DirectorError

                let finalDirectors = Map.add directorId newRepresentative updatedDirectors

                return Board({
                    state with
                        Directors = finalDirectors
                        RepresentativeDirectorId = Some directorId
                })
            }

        /// Record a board meeting
        member this.RecordMeeting
            (meetingDate: Date)
            (meetingType: MeetingType)
            (attendees: AttendanceRecord list)
            : Result<Board, BoardError> =

            result {
                let presentCount = attendees |> List.filter (fun a -> a.Present) |> List.length
                let quorum = this.Quorum

                let meeting = {
                    MeetingDate = meetingDate
                    MeetingType = meetingType
                    Attendees = attendees
                    QuorumMet = presentCount >= quorum
                    Minutes = None
                }

                return Board({
                    state with Meetings = meeting :: state.Meetings
                })
            }

        /// Pass a resolution
        member this.PassResolution
            (resolutionType: ResolutionType)
            (description: string)
            (votesFor: int)
            (votesAgainst: int)
            (abstentions: int)
            (date: Date)
            : Result<Board, BoardError> =

            result {
                // Check that a meeting was held (quorum met)
                let recentMeeting =
                    state.Meetings
                    |> List.tryHead
                    |> Option.filter (fun m -> m.QuorumMet)

                do! Result.require
                        recentMeeting.IsSome
                        (QuorumNotMet (this.Quorum, 0))

                // Resolution passes with majority
                let totalVotes = votesFor + votesAgainst
                let passed = votesFor > votesAgainst && totalVotes > 0

                let resolution = {
                    ResolutionType = resolutionType
                    Description = description
                    ProposedDate = date
                    VotesFor = votesFor
                    VotesAgainst = votesAgainst
                    Abstentions = abstentions
                    Status = if passed then Passed else Rejected
                    PassedDate = if passed then Some date else None
                }

                return Board({
                    state with Resolutions = resolution :: state.Resolutions
                })
            }

        /// Renew director term
        member this.RenewDirectorTerm
            (directorId: DirectorId)
            (newTermYears: int)
            (renewalDate: Date)
            : Result<Board, BoardError> =

            result {
                let! director =
                    Map.tryFind directorId state.Directors
                    |> Result.ofOption (DirectorError (DirectorNotOnBoard directorId))

                let! renewedDirector =
                    director.RenewTerm newTermYears renewalDate
                    |> Result.mapError DirectorError

                let newDirectors = Map.add directorId renewedDirector state.Directors
                return Board({ state with Directors = newDirectors })
            }

        // ============================================
        // Validation
        // ============================================

        /// Validate board meets legal requirements
        member this.Validate() : Result<unit, BoardError> =
            let activeCount = this.DirectorCount
            let minRequired = this.MinimumDirectors

            if activeCount < minRequired then
                Error (InsufficientDirectors (minRequired, activeCount))
            elif not this.HasRepresentativeDirector && activeCount > 0 then
                Error (DirectorError NoRepresentativeDirector)
            else
                // For certain structures, outside directors are required
                match state.Structure with
                | BoardStructure.WithAuditCommittee ->
                    if this.OutsideDirectorCount < 2 then
                        Error (InsufficientOutsideDirectors (2, this.OutsideDirectorCount))
                    else Ok ()
                | BoardStructure.WithThreeCommittees ->
                    // Each committee needs majority outside directors
                    let outsideCount = this.OutsideDirectorCount
                    let totalCount = this.DirectorCount
                    if outsideCount * 2 < totalCount then
                        Error (InsufficientOutsideDirectors (totalCount / 2 + 1, outsideCount))
                    else Ok ()
                | _ -> Ok ()

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new board
        static member Create
            (companyId: CompanyId)
            (structure: BoardStructure)
            (establishedDate: Date)
            : Board =

            let state = BoardState.create companyId structure establishedDate
            Board(state)

        /// Create board with initial directors
        static member CreateWithDirectors
            (companyId: CompanyId)
            (structure: BoardStructure)
            (directors: Director list)
            (representativeId: DirectorId)
            (establishedDate: Date)
            : Result<Board, BoardError> =

            let addDirectors (board: Board) (dirs: Director list) : Result<Board, BoardError> =
                dirs |> List.fold (fun acc dir ->
                    match acc with
                    | Error e -> Error e
                    | Ok b -> b.AddDirector(dir)
                ) (Ok board)

            result {
                let initialBoard = Board.Create companyId structure establishedDate

                // Add all directors
                let! boardWithDirectors = addDirectors initialBoard directors

                // Designate representative
                let! finalBoard = boardWithDirectors.DesignateRepresentativeDirector(representativeId)

                return finalBoard
            }

        /// Reconstitute from state
        static member FromState(state: BoardState) : Board =
            Board(state)

    // ============================================
    // Pure Logic Functions
    // ============================================

    module BoardLogic =

        /// Check if board structure allows simplified governance
        let allowsSimplifiedGovernance (structure: BoardStructure) : bool =
            match structure with
            | BoardStructure.WithoutBoard -> true
            | _ -> false

        /// Get required outside director ratio
        let requiredOutsideDirectorRatio (structure: BoardStructure) : decimal option =
            match structure with
            | BoardStructure.WithThreeCommittees -> Some 0.5m  // Majority
            | BoardStructure.WithAuditCommittee -> Some 0.0m   // At least 2, no ratio
            | _ -> None

        /// Calculate meeting quorum percentage
        let quorumPercentage (present: int) (total: int) : decimal =
            if total = 0 then 0m
            else (decimal present / decimal total)

        /// Check if resolution requires special majority
        let requiresSpecialMajority (resType: ResolutionType) : bool =
            match resType with
            | ResolutionType.AmendmentOfArticles -> true
            | ResolutionType.CapitalDecrease -> true
            | _ -> false

        /// Get expiring terms within days
        let getExpiringTerms
            (board: Board)
            (withinDays: int)
            : Director list =
            let asOf = Date.today()
            board.GetActiveDirectors()
            |> List.filter (fun d -> d.DaysRemainingInTerm(asOf) <= withinDays)
