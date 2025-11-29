namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact
open CompanyAsCode.SharedKernel.Japanese

/// Shareholder entities and share management
module Shareholder =

    open Errors

    // ============================================
    // Share Types
    // ============================================

    /// Share class
    type ShareClass =
        | Common                    // 普通株式
        | PreferredDividend         // 配当優先株式
        | PreferredLiquidation      // 残余財産分配優先株式
        | NonVoting                 // 無議決権株式
        | Restricted                // 譲渡制限株式
        | Convertible               // 転換株式
        | Redeemable                // 償還株式

    /// Share transfer restriction
    type TransferRestriction =
        | NoRestriction
        | RequiresBoardApproval
        | RequiresShareholderApproval
        | Prohibited

    /// Share certificate status
    type CertificateStatus =
        | Issued
        | NotIssued
        | Electronic

    // ============================================
    // Shareholder Types
    // ============================================

    /// Shareholder type
    type ShareholderType =
        | Individual of PersonName * Contact.Address option
        | Domestic of CompanyId * BilingualName * CorporateNumber
        | Foreign of name: string * country: string * identifier: string option

    // ============================================
    // Shareholding State
    // ============================================

    /// Individual shareholding record
    type ShareholdingState = {
        ShareholderId: ShareholderId
        ShareholderType: ShareholderType
        ShareCount: ShareCount
        ShareClass: ShareClass
        AcquisitionDate: Date
        AcquisitionPrice: Money option
        CertificateStatus: CertificateStatus
        VotingRightsPerShare: int
    }

    module ShareholdingState =

        let create
            (shareholderId: ShareholderId)
            (shareholderType: ShareholderType)
            (shareCount: ShareCount)
            (shareClass: ShareClass)
            (acquisitionDate: Date)
            : ShareholdingState =
            {
                ShareholderId = shareholderId
                ShareholderType = shareholderType
                ShareCount = shareCount
                ShareClass = shareClass
                AcquisitionDate = acquisitionDate
                AcquisitionPrice = None
                CertificateStatus = NotIssued
                VotingRightsPerShare = 1
            }

        let totalVotingRights (state: ShareholdingState) : int64 =
            (ShareCount.value state.ShareCount) * int64 state.VotingRightsPerShare

        let hasVotingRights (state: ShareholdingState) : bool =
            state.VotingRightsPerShare > 0

    // ============================================
    // Shareholder Register State
    // ============================================

    /// Shareholder register aggregate state
    type ShareholderRegisterState = {
        CompanyId: CompanyId
        AuthorizedShares: ShareCount
        IssuedShares: ShareCount
        ParValue: ParValue option
        Shareholdings: Map<ShareholderId, ShareholdingState>
        TransferRestriction: TransferRestriction
        ShareClasses: ShareClass Set
    }

    module ShareholderRegisterState =

        let create
            (companyId: CompanyId)
            (authorizedShares: ShareCount)
            (transferRestriction: TransferRestriction)
            : ShareholderRegisterState =
            {
                CompanyId = companyId
                AuthorizedShares = authorizedShares
                IssuedShares = ShareCount.zero
                ParValue = None
                Shareholdings = Map.empty
                TransferRestriction = transferRestriction
                ShareClasses = Set.singleton Common
            }

        let totalIssuedShares (state: ShareholderRegisterState) : int64 =
            ShareCount.value state.IssuedShares

        let totalVotingRights (state: ShareholderRegisterState) : int64 =
            state.Shareholdings
            |> Map.toSeq
            |> Seq.sumBy (fun (_, sh) -> ShareholdingState.totalVotingRights sh)

        let shareholderCount (state: ShareholderRegisterState) : int =
            state.Shareholdings.Count

        let getShareholding (shareholderId: ShareholderId) (state: ShareholderRegisterState) =
            Map.tryFind shareholderId state.Shareholdings

    // ============================================
    // Shareholder Register Aggregate
    // ============================================

    /// Shareholder register aggregate root
    type ShareholderRegister private (state: ShareholderRegisterState) =

        member _.State = state
        member _.CompanyId = state.CompanyId
        member _.AuthorizedShares = state.AuthorizedShares
        member _.IssuedShares = state.IssuedShares
        member _.ParValue = state.ParValue
        member _.TransferRestriction = state.TransferRestriction

        member _.TotalIssuedShares = ShareholderRegisterState.totalIssuedShares state
        member _.TotalVotingRights = ShareholderRegisterState.totalVotingRights state
        member _.ShareholderCount = ShareholderRegisterState.shareholderCount state

        /// Get shareholding by ID
        member _.GetShareholding(shareholderId: ShareholderId) =
            ShareholderRegisterState.getShareholding shareholderId state

        /// Get all shareholdings
        member _.GetAllShareholdings() =
            state.Shareholdings |> Map.toList |> List.map snd

        /// Get shareholdings by type
        member _.GetShareholdingsByType(shareholderType: ShareholderType -> bool) =
            state.Shareholdings
            |> Map.toList
            |> List.filter (fun (_, sh) -> shareholderType sh.ShareholderType)
            |> List.map snd

        // ============================================
        // Commands
        // ============================================

        /// Issue new shares
        member this.IssueShares
            (shareholderType: ShareholderType)
            (shareCount: ShareCount)
            (shareClass: ShareClass)
            (issueDate: Date)
            : Result<ShareholderRegister * ShareholderId, ShareholderError> =

            result {
                let requestedCount = ShareCount.value shareCount
                let currentIssued = ShareCount.value state.IssuedShares
                let authorized = ShareCount.value state.AuthorizedShares

                do! Result.require
                        (currentIssued + requestedCount <= authorized)
                        (ExceedsAuthorizedShares (authorized, currentIssued + requestedCount))

                let shareholderId = ShareholderId.create()

                let shareholding = ShareholdingState.create
                                    shareholderId
                                    shareholderType
                                    shareCount
                                    shareClass
                                    issueDate

                let! newIssuedShares =
                    ShareCount.add state.IssuedShares shareCount
                    |> Ok

                let newState = {
                    state with
                        IssuedShares = newIssuedShares
                        Shareholdings = Map.add shareholderId shareholding state.Shareholdings
                        ShareClasses = Set.add shareClass state.ShareClasses
                }

                return (ShareholderRegister(newState), shareholderId)
            }

        /// Transfer shares between shareholders
        member this.TransferShares
            (fromShareholderId: ShareholderId)
            (toShareholderId: ShareholderId)
            (transferCount: ShareCount)
            (transferDate: Date)
            (boardApproved: bool)
            : Result<ShareholderRegister, ShareholderError> =

            result {
                // Validate transfer restriction
                do! match state.TransferRestriction with
                    | RequiresBoardApproval when not boardApproved ->
                        Error TransferNotApproved
                    | Prohibited ->
                        Error (InvalidShareTransfer "Share transfers are prohibited")
                    | _ -> Ok ()

                do! Result.require
                        (fromShareholderId <> toShareholderId)
                        CannotTransferToSelf

                // Get source shareholding
                let! fromShareholding =
                    Map.tryFind fromShareholderId state.Shareholdings
                    |> Result.ofOption (ShareholderNotFound fromShareholderId)

                let requestedCount = ShareCount.value transferCount
                let availableCount = ShareCount.value fromShareholding.ShareCount

                do! Result.require
                        (requestedCount <= availableCount)
                        (InsufficientShares (requestedCount, availableCount))

                // Update or remove source shareholding
                let! newFromCount =
                    ShareCount.subtract fromShareholding.ShareCount transferCount
                    |> Result.mapError (fun _ -> InsufficientShares (requestedCount, availableCount))

                let updatedShareholdings =
                    if ShareCount.value newFromCount = 0L then
                        Map.remove fromShareholderId state.Shareholdings
                    else
                        Map.add fromShareholderId
                            { fromShareholding with ShareCount = newFromCount }
                            state.Shareholdings

                // Update or create target shareholding
                let finalShareholdings =
                    match Map.tryFind toShareholderId updatedShareholdings with
                    | Some existing ->
                        let newCount = ShareCount.add existing.ShareCount transferCount
                        Map.add toShareholderId
                            { existing with ShareCount = newCount }
                            updatedShareholdings
                    | None ->
                        // Create new shareholding for recipient
                        // Note: In real implementation, would need shareholder info
                        let newShareholding = {
                            fromShareholding with
                                ShareholderId = toShareholderId
                                ShareCount = transferCount
                                AcquisitionDate = transferDate
                        }
                        Map.add toShareholderId newShareholding updatedShareholdings

                return ShareholderRegister({
                    state with Shareholdings = finalShareholdings
                })
            }

        /// Increase authorized shares
        member this.IncreaseAuthorizedShares(additionalShares: ShareCount)
            : Result<ShareholderRegister, ShareholderError> =
            let newAuthorized = ShareCount.add state.AuthorizedShares additionalShares
            Ok (ShareholderRegister({ state with AuthorizedShares = newAuthorized }))

        /// Set par value
        member this.SetParValue(parValue: ParValue)
            : ShareholderRegister =
            ShareholderRegister({ state with ParValue = Some parValue })

        /// Update transfer restriction
        member this.SetTransferRestriction(restriction: TransferRestriction)
            : ShareholderRegister =
            ShareholderRegister({ state with TransferRestriction = restriction })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create new shareholder register
        static member Create
            (companyId: CompanyId)
            (authorizedShares: ShareCount)
            (transferRestriction: TransferRestriction)
            : ShareholderRegister =

            let state = ShareholderRegisterState.create
                            companyId
                            authorizedShares
                            transferRestriction

            ShareholderRegister(state)

        /// Reconstitute from state
        static member FromState(state: ShareholderRegisterState) : ShareholderRegister =
            ShareholderRegister(state)

    // ============================================
    // Pure Logic Functions
    // ============================================

    module ShareholderLogic =

        /// Calculate ownership percentage
        let ownershipPercentage
            (shareholding: ShareholdingState)
            (totalIssued: ShareCount)
            : decimal =
            let holding = ShareCount.value shareholding.ShareCount
            let total = ShareCount.value totalIssued
            if total = 0L then 0m
            else (decimal holding / decimal total) * 100m

        /// Check if shareholder has controlling interest (>50%)
        let hasControllingInterest
            (shareholding: ShareholdingState)
            (totalIssued: ShareCount)
            : bool =
            ownershipPercentage shareholding totalIssued > 50m

        /// Check if shareholder has blocking minority (>33.3%)
        let hasBlockingMinority
            (shareholding: ShareholdingState)
            (totalIssued: ShareCount)
            : bool =
            ownershipPercentage shareholding totalIssued > 33.33m

        /// Calculate dividend amount per share
        let dividendPerShare
            (totalDividend: Money)
            (totalShares: ShareCount)
            : Result<Money, string> =
            let count = ShareCount.value totalShares
            if count = 0L then
                Error "No shares issued"
            else
                Money.divide (decimal count) totalDividend

        /// Group shareholdings by shareholder type
        let groupByType
            (shareholdings: ShareholdingState list)
            : Map<string, ShareholdingState list> =
            shareholdings
            |> List.groupBy (fun sh ->
                match sh.ShareholderType with
                | Individual _ -> "Individual"
                | Domestic _ -> "Domestic Corporate"
                | Foreign _ -> "Foreign")
            |> Map.ofList

        /// Calculate quorum for shareholder meeting
        let calculateQuorum
            (presentShares: int64)
            (totalShares: int64)
            : decimal =
            if totalShares = 0L then 0m
            else (decimal presentShares / decimal totalShares)

        /// Check if ordinary resolution passes
        let ordinaryResolutionPasses
            (votesFor: int64)
            (votesAgainst: int64)
            : bool =
            votesFor > votesAgainst

        /// Check if special resolution passes (2/3 majority)
        let specialResolutionPasses
            (votesFor: int64)
            (totalVotes: int64)
            : bool =
            if totalVotes = 0L then false
            else (decimal votesFor / decimal totalVotes) >= (2m / 3m)
