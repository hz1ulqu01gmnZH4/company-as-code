namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact
open CompanyAsCode.SharedKernel.Japanese

/// Company aggregate - the core aggregate root for legal entity
module Company =

    open Director
    open Board
    open Shareholder
    open Events
    open Errors

    // ============================================
    // Company Status
    // ============================================

    /// Company operational status
    type CompanyStatus =
        | Incorporating     // 設立中
        | Active            // 活動中
        | Suspended         // 休眠中
        | UnderLiquidation  // 清算中
        | Dissolved         // 解散済

    // ============================================
    // Corporate Seals State
    // ============================================

    /// Collection of corporate seals
    type CorporateSealsState = {
        Jituin: CorporateSeal option       // 実印 (Representative seal)
        Ginkoin: CorporateSeal option      // 銀行印 (Bank seal)
        Kakuin: CorporateSeal option       // 角印 (Acknowledgment seal)
    }

    module CorporateSealsState =

        let empty = {
            Jituin = None
            Ginkoin = None
            Kakuin = None
        }

        let hasRegisteredSeal (state: CorporateSealsState) : bool =
            state.Jituin
            |> Option.map CorporateSeal.isRegistered
            |> Option.defaultValue false

    // ============================================
    // Company State (Immutable)
    // ============================================

    /// Internal state of company aggregate
    type CompanyState = {
        Id: CompanyId
        CorporateNumber: CorporateNumber
        LegalName: BilingualName
        EntityType: EntityType
        Status: CompanyStatus
        RegisteredCapital: RegisteredCapital
        FiscalYearEnd: FiscalYearEnd
        Headquarters: Address
        CorporateSeals: CorporateSealsState
        EstablishmentDate: Date
        BoardId: BoardId option
        ShareholderRegisterId: ShareholderId option
        // Financial state for dividend validation
        NetAssets: Money option
        LegalReserve: Money option
    }

    module CompanyState =

        let isActive (state: CompanyState) =
            state.Status = Active

        let canPayDividend (state: CompanyState) : bool =
            match state.NetAssets with
            | Some netAssets ->
                // Companies Act: Cannot pay dividend if net assets < ¥3,000,000
                Money.amount netAssets >= 3_000_000m
            | None -> false

        let capitalAmount (state: CompanyState) : decimal =
            RegisteredCapital.amount state.RegisteredCapital

    // ============================================
    // Company Aggregate Root
    // ============================================

    /// Company aggregate root - encapsulates company state and business logic
    type Company private (state: CompanyState) =

        // ============================================
        // Properties (read-only access to state)
        // ============================================

        member _.State = state
        member _.Id = state.Id
        member _.CorporateNumber = state.CorporateNumber
        member _.LegalName = state.LegalName
        member _.EntityType = state.EntityType
        member _.Status = state.Status
        member _.RegisteredCapital = state.RegisteredCapital
        member _.FiscalYearEnd = state.FiscalYearEnd
        member _.Headquarters = state.Headquarters
        member _.CorporateSeals = state.CorporateSeals
        member _.EstablishmentDate = state.EstablishmentDate
        member _.BoardId = state.BoardId
        member _.NetAssets = state.NetAssets

        member _.IsActive = CompanyState.isActive state
        member _.CanPayDividend = CompanyState.canPayDividend state
        member _.CapitalAmount = CompanyState.capitalAmount state

        member _.HasRegisteredSeal = CorporateSealsState.hasRegisteredSeal state.CorporateSeals

        // ============================================
        // Commands (return Result with new Company + Events)
        // ============================================

        /// Increase registered capital
        member this.IncreaseCapital(amount: Money) (effectiveDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        CompanyNotActive

                do! Result.require
                        (Money.currency amount = Currency.JPY)
                        (InvalidCapital "Capital must be in Japanese Yen")

                do! Result.require
                        (Money.isPositive amount)
                        (InvalidCapital "Capital increase must be positive")

                let currentCapital = RegisteredCapital.value state.RegisteredCapital
                let! newCapitalMoney = Money.add currentCapital amount
                                       |> Result.mapError InvalidCapital

                let! newCapital = RegisteredCapital.create newCapitalMoney
                                  |> Result.mapError InvalidCapital

                let newState = { state with RegisteredCapital = newCapital }

                let event = CapitalIncreased {
                    Meta = LegalEventMeta.create state.Id
                    PreviousCapital = currentCapital
                    NewCapital = newCapitalMoney
                    IncreaseAmount = amount
                    EffectiveDate = effectiveDate
                }

                return (Company(newState), event)
            }

        /// Change company name
        member this.ChangeName(newName: BilingualName) (effectiveDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        CompanyNotActive

                do! Result.require
                        (newName.Japanese <> state.LegalName.Japanese)
                        (InvalidCompanyName "New name must be different from current name")

                let newState = { state with LegalName = newName }

                let event = CompanyNameChanged {
                    Meta = LegalEventMeta.create state.Id
                    PreviousName = state.LegalName
                    NewName = newName
                    EffectiveDate = effectiveDate
                }

                return (Company(newState), event)
            }

        /// Change headquarters address
        member this.ChangeHeadquarters(newAddress: Address) (effectiveDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        CompanyNotActive

                let newState = { state with Headquarters = newAddress }

                let event = HeadquartersChanged {
                    Meta = LegalEventMeta.create state.Id
                    PreviousAddress = state.Headquarters
                    NewAddress = newAddress
                    EffectiveDate = effectiveDate
                }

                return (Company(newState), event)
            }

        /// Change fiscal year end
        member this.ChangeFiscalYearEnd(newFiscalYearEnd: FiscalYearEnd) (effectiveDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        CompanyNotActive

                let newState = { state with FiscalYearEnd = newFiscalYearEnd }

                let event = FiscalYearEndChanged {
                    Meta = LegalEventMeta.create state.Id
                    PreviousFiscalYearEnd = state.FiscalYearEnd
                    NewFiscalYearEnd = newFiscalYearEnd
                    EffectiveDate = effectiveDate
                }

                return (Company(newState), event)
            }

        /// Register corporate seal
        member this.RegisterSeal
            (sealType: SealType)
            (registrationDate: Date)
            (legalAffairsBureau: string)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                let seal = CorporateSeal.createRegistered sealType (Date.toDateTimeOffset registrationDate) legalAffairsBureau

                let newSeals =
                    match sealType with
                    | SealType.Jituin ->
                        { state.CorporateSeals with Jituin = Some seal }
                    | SealType.Ginkoin ->
                        { state.CorporateSeals with Ginkoin = Some seal }
                    | SealType.Kakuin ->
                        { state.CorporateSeals with Kakuin = Some seal }
                    | SealType.Mitomein ->
                        state.CorporateSeals  // Mitomein not tracked at company level

                let newState = { state with CorporateSeals = newSeals }

                let event = Events.CorporateSealRegistered {
                    Meta = LegalEventMeta.create state.Id
                    SealType = sealType
                    RegistrationDate = registrationDate
                    LegalAffairsBureau = legalAffairsBureau
                }

                return (Company(newState), event)
            }

        /// Update net assets (for dividend validation)
        member this.UpdateNetAssets(netAssets: Money)
            : Company =
            Company({ state with NetAssets = Some netAssets })

        /// Update legal reserve
        member this.UpdateLegalReserve(reserve: Money)
            : Company =
            Company({ state with LegalReserve = Some reserve })

        /// Associate board with company
        member this.AssociateBoard(boardId: BoardId)
            : Company =
            Company({ state with BoardId = Some boardId })

        /// Initiate liquidation
        member this.InitiateLiquidation(reason: string) (initiatedDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active || state.Status = Suspended)
                        (CannotDissolve $"Cannot initiate liquidation in {state.Status} status")

                let newState = { state with Status = UnderLiquidation }

                let event = LiquidationInitiated {
                    Meta = LegalEventMeta.create state.Id
                    InitiatedDate = initiatedDate
                    Reason = reason
                    Liquidator = None
                }

                return (Company(newState), event)
            }

        /// Complete dissolution
        member this.Dissolve(reason: string) (dissolutionDate: Date)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                do! Result.require
                        (state.Status = UnderLiquidation)
                        (CannotDissolve "Company must be under liquidation to dissolve")

                let newState = { state with Status = Dissolved }

                let event = CompanyDissolved {
                    Meta = LegalEventMeta.create state.Id
                    DissolutionDate = dissolutionDate
                    Reason = reason
                }

                return (Company(newState), event)
            }

        /// Suspend company operations
        member this.Suspend()
            : Result<Company, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        CompanyNotActive

                return Company({ state with Status = Suspended })
            }

        /// Resume company operations
        member this.Resume()
            : Result<Company, CompanyError> =

            result {
                do! Result.require
                        (state.Status = Suspended)
                        (CannotIncorporate "Company must be suspended to resume")

                return Company({ state with Status = Active })
            }

        // ============================================
        // Validation
        // ============================================

        /// Validate company state
        member this.Validate() : Result<unit, Errors.CompanyError> =
            // Check capital meets minimum
            let minCapital = EntityType.minimumCapital state.EntityType
            if CompanyState.capitalAmount state < minCapital then
                Error (Errors.CompanyError.CapitalBelowMinimum (state.EntityType, minCapital, CompanyState.capitalAmount state))
            // Active company must have registered seal
            elif state.Status = CompanyStatus.Active && not (CorporateSealsState.hasRegisteredSeal state.CorporateSeals) then
                Error (Errors.CompanyError.InvalidCapital "Active company must have registered seal")
            else
                Ok ()

        /// Check if company can pay specified dividend
        member this.CanPayDividendOf(amount: Money) : Result<unit, Errors.CompanyError> =
            if state.Status <> CompanyStatus.Active then
                Error Errors.CompanyError.CompanyNotActive
            else
                let minNetAssets = Money.yen 3_000_000m
                match state.NetAssets with
                | Some netAssets when Money.amount netAssets >= 3_000_000m ->
                    Ok ()
                | Some netAssets ->
                    Error (Errors.CompanyError.InsufficientNetAssets (minNetAssets, netAssets))
                | None ->
                    Error (Errors.CompanyError.InsufficientNetAssets (minNetAssets, Money.yen 0m))

        // ============================================
        // Factory Method
        // ============================================

        /// Reconstitute company from state (for persistence)
        static member FromState(state: CompanyState) : Company =
            Company(state)

        /// Internal constructor for factory
        static member internal CreateFromState(state: CompanyState) : Company =
            Company(state)

    // ============================================
    // Pure Business Logic (Module)
    // ============================================

    /// Pure business logic functions for company operations
    module CompanyLogic =

        /// Minimum practical capital for K.K. (for credibility)
        let recommendedMinimumKK = 10_000_000m

        /// Minimum practical capital for G.K.
        let recommendedMinimumGK = 3_000_000m

        /// Minimum net assets for dividend distribution
        let minimumNetAssetsForDividend = 3_000_000m

        /// Legal reserve requirement ratio (25% of capital)
        let legalReserveRatio = 0.25m

        /// Required contribution to legal reserve per dividend
        let dividendReserveContribution = 0.10m

        /// Validate corporate number
        let validateCorporateNumber (value: string) : Result<CorporateNumber, string> =
            CorporateNumber.create value

        /// Check if entity type allows simplified governance
        let allowsSimplifiedGovernance (entityType: EntityType) : bool =
            match entityType with
            | GodoKaisha -> true
            | GomeiKaisha -> true
            | GoshiKaisha -> true
            | KabushikiKaisha -> false  // K.K. has more requirements

        /// Calculate registration fee
        let calculateRegistrationFee (entityType: EntityType) (capital: decimal) : decimal =
            EntityType.registrationFee capital entityType

        /// Calculate required legal reserve
        let calculateRequiredLegalReserve (capital: decimal) : decimal =
            capital * legalReserveRatio

        /// Calculate dividend reserve contribution
        let calculateDividendReserveContribution (dividendAmount: decimal) : decimal =
            dividendAmount * dividendReserveContribution

        /// Check if company meets capital recommendation
        let meetsCapitalRecommendation (entityType: EntityType) (capital: decimal) : bool =
            capital >= EntityType.recommendedCapital entityType

        /// Get fiscal year for date
        let getFiscalYearForDate (fiscalYearEnd: FiscalYearEnd) (date: Date) : FiscalYear =
            FiscalYear.forDate fiscalYearEnd date

        /// Format company name with entity type
        let formatCompanyName (name: BilingualName) (entityType: EntityType) : string =
            let entityPrefix = EntityType.toJapanese entityType
            $"{entityPrefix}{name.Japanese}"
