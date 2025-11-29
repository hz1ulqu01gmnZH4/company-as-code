namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact
open CompanyAsCode.SharedKernel.Japanese
open CompanyAsCode.SharedKernel.AggregateRoot

/// Factory for creating Company aggregates
module CompanyFactory =

    open Company
    open Events
    open Errors

    // ============================================
    // Incorporation Command
    // ============================================

    /// Command to incorporate a new company
    type IncorporateCompanyCommand = {
        CorporateNumber: string
        LegalName: BilingualName
        EntityType: EntityType
        InitialCapital: Money
        FiscalYearEnd: FiscalYearEnd
        HeadquartersAddress: Address
        EstablishmentDate: Date
    }

    module IncorporateCompanyCommand =

        /// Validate incorporation command
        let validate (cmd: IncorporateCompanyCommand) : Result<unit, CompanyError> =
            result {
                // Validate corporate number
                let! _ = CorporateNumber.create cmd.CorporateNumber
                         |> Result.mapError InvalidCorporateNumber

                // Validate capital
                do! Result.require
                        (Money.currency cmd.InitialCapital = Currency.JPY)
                        (InvalidCapital "Capital must be in Japanese Yen")

                do! Result.require
                        (Money.isPositive cmd.InitialCapital || Money.isZero cmd.InitialCapital |> not)
                        (InvalidCapital "Initial capital must be positive")

                // Validate minimum capital for entity type
                let minCapital = EntityType.minimumCapital cmd.EntityType
                do! Result.require
                        (Money.amount cmd.InitialCapital >= minCapital)
                        (CapitalBelowMinimum (cmd.EntityType, minCapital, Money.amount cmd.InitialCapital))

                // Validate company name
                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(cmd.LegalName.Japanese)))
                        (InvalidCompanyName "Japanese company name is required")

                return ()
            }

    // ============================================
    // Company Factory
    // ============================================

    /// Factory for creating new Company aggregates
    type CompanyFactory() =

        /// Incorporate a new company
        member _.Incorporate(cmd: IncorporateCompanyCommand)
            : Result<Company * LegalEvent, CompanyError> =

            result {
                // Validate command
                do! IncorporateCompanyCommand.validate cmd

                // Create corporate number
                let! corporateNumber =
                    CorporateNumber.create cmd.CorporateNumber
                    |> Result.mapError InvalidCorporateNumber

                // Create registered capital
                let! registeredCapital =
                    RegisteredCapital.create cmd.InitialCapital
                    |> Result.mapError InvalidCapital

                // Create company state
                let companyId = CompanyId.create()

                let state: CompanyState = {
                    Id = companyId
                    CorporateNumber = corporateNumber
                    LegalName = cmd.LegalName
                    EntityType = cmd.EntityType
                    Status = Active
                    RegisteredCapital = registeredCapital
                    FiscalYearEnd = cmd.FiscalYearEnd
                    Headquarters = cmd.HeadquartersAddress
                    CorporateSeals = CorporateSealsState.empty
                    EstablishmentDate = cmd.EstablishmentDate
                    BoardId = None
                    ShareholderRegisterId = None
                    NetAssets = Some cmd.InitialCapital  // Initial net assets = capital
                    LegalReserve = Some (Money.yen 0m)
                }

                let company = Company.CreateFromState(state)

                // Create incorporation event
                let event = CompanyIncorporated {
                    Meta = LegalEventMeta.create companyId
                    CorporateNumber = corporateNumber
                    LegalName = cmd.LegalName
                    EntityType = cmd.EntityType
                    InitialCapital = cmd.InitialCapital
                    FiscalYearEnd = cmd.FiscalYearEnd
                    HeadquartersAddress = cmd.HeadquartersAddress
                    EstablishmentDate = cmd.EstablishmentDate
                }

                return (company, event)
            }

        interface IFactory<IncorporateCompanyCommand, Company, LegalEvent, Errors.CompanyError> with
            member this.Create(cmd) =
                match this.Incorporate(cmd) with
                | Ok (c, e) -> Ok (c, [e])
                | Error err -> Error err

    // ============================================
    // Quick Factory Functions
    // ============================================

    /// Quick incorporation functions for common scenarios
    module QuickIncorporate =

        let private factory = CompanyFactory()

        /// Create a Kabushiki Kaisha (K.K.) with standard settings
        let kabushikiKaisha
            (corporateNumber: string)
            (japaneseName: string)
            (capital: decimal)
            (address: Address)
            : Result<Company * LegalEvent, CompanyError> =

            let cmd = {
                CorporateNumber = corporateNumber
                LegalName = {
                    Japanese = japaneseName
                    JapaneseKana = None
                    English = None
                }
                EntityType = KabushikiKaisha
                InitialCapital = Money.yen capital
                FiscalYearEnd = FiscalYearEnd.march31
                HeadquartersAddress = address
                EstablishmentDate = Date.today()
            }

            factory.Incorporate(cmd)

        /// Create a Godo Kaisha (G.K.) with standard settings
        let godoKaisha
            (corporateNumber: string)
            (japaneseName: string)
            (capital: decimal)
            (address: Address)
            : Result<Company * LegalEvent, CompanyError> =

            let cmd = {
                CorporateNumber = corporateNumber
                LegalName = {
                    Japanese = japaneseName
                    JapaneseKana = None
                    English = None
                }
                EntityType = GodoKaisha
                InitialCapital = Money.yen capital
                FiscalYearEnd = FiscalYearEnd.march31
                HeadquartersAddress = address
                EstablishmentDate = Date.today()
            }

            factory.Incorporate(cmd)

        /// Create a company with bilingual name
        let withBilingualName
            (corporateNumber: string)
            (japaneseName: string)
            (englishName: string)
            (entityType: EntityType)
            (capital: decimal)
            (address: Address)
            : Result<Company * LegalEvent, CompanyError> =

            let cmd = {
                CorporateNumber = corporateNumber
                LegalName = {
                    Japanese = japaneseName
                    JapaneseKana = None
                    English = Some englishName
                }
                EntityType = entityType
                InitialCapital = Money.yen capital
                FiscalYearEnd = FiscalYearEnd.march31
                HeadquartersAddress = address
                EstablishmentDate = Date.today()
            }

            factory.Incorporate(cmd)

        /// Create a company with December fiscal year end
        let withDecemberFiscalYear
            (corporateNumber: string)
            (japaneseName: string)
            (entityType: EntityType)
            (capital: decimal)
            (address: Address)
            : Result<Company * LegalEvent, CompanyError> =

            let cmd = {
                CorporateNumber = corporateNumber
                LegalName = {
                    Japanese = japaneseName
                    JapaneseKana = None
                    English = None
                }
                EntityType = entityType
                InitialCapital = Money.yen capital
                FiscalYearEnd = FiscalYearEnd.december31
                HeadquartersAddress = address
                EstablishmentDate = Date.today()
            }

            factory.Incorporate(cmd)

    // ============================================
    // Builder Pattern (Alternative)
    // ============================================

    /// Builder for creating incorporation commands
    type IncorporationBuilder() =
        let mutable corporateNumber = ""
        let mutable japaneseName = ""
        let mutable englishName: string option = None
        let mutable kanaName: string option = None
        let mutable entityType = KabushikiKaisha
        let mutable capital = 0m
        let mutable fiscalYearEnd = FiscalYearEnd.march31
        let mutable address: Address option = None
        let mutable establishmentDate = Date.today()

        member this.WithCorporateNumber(number: string) =
            corporateNumber <- number
            this

        member this.WithJapaneseName(name: string) =
            japaneseName <- name
            this

        member this.WithEnglishName(name: string) =
            englishName <- Some name
            this

        member this.WithKanaName(name: string) =
            kanaName <- Some name
            this

        member this.AsKabushikiKaisha() =
            entityType <- KabushikiKaisha
            this

        member this.AsGodoKaisha() =
            entityType <- GodoKaisha
            this

        member this.WithEntityType(et: EntityType) =
            entityType <- et
            this

        member this.WithCapital(amount: decimal) =
            capital <- amount
            this

        member this.WithFiscalYearEnd(fye: FiscalYearEnd) =
            fiscalYearEnd <- fye
            this

        member this.WithMarchFiscalYear() =
            fiscalYearEnd <- FiscalYearEnd.march31
            this

        member this.WithDecemberFiscalYear() =
            fiscalYearEnd <- FiscalYearEnd.december31
            this

        member this.WithAddress(addr: Address) =
            address <- Some addr
            this

        member this.WithEstablishmentDate(date: Date) =
            establishmentDate <- date
            this

        member this.Build() : Result<IncorporateCompanyCommand, string> =
            result {
                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(corporateNumber)))
                        "Corporate number is required"

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(japaneseName)))
                        "Japanese name is required"

                do! Result.require
                        (capital > 0m)
                        "Capital must be positive"

                let! addr =
                    address
                    |> Result.ofOption "Address is required"

                return {
                    CorporateNumber = corporateNumber
                    LegalName = {
                        Japanese = japaneseName
                        JapaneseKana = kanaName
                        English = englishName
                    }
                    EntityType = entityType
                    InitialCapital = Money.yen capital
                    FiscalYearEnd = fiscalYearEnd
                    HeadquartersAddress = addr
                    EstablishmentDate = establishmentDate
                }
            }

        member this.Incorporate() : Result<Company * LegalEvent, CompanyError> =
            match this.Build() with
            | Ok cmd -> CompanyFactory().Incorporate(cmd)
            | Error msg -> Error (InvalidCompanyName msg)

    /// Start building an incorporation command
    let incorporate() = IncorporationBuilder()
