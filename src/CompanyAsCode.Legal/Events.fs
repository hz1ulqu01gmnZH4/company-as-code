namespace CompanyAsCode.Legal

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.AggregateRoot
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact
open CompanyAsCode.SharedKernel.Japanese

/// Domain events for Legal & Corporate Governance context
module Events =

    // ============================================
    // Event Metadata
    // ============================================

    /// Common metadata for all legal events
    type LegalEventMeta = {
        EventId: Guid
        OccurredAt: DateTimeOffset
        CompanyId: CompanyId
        CorrelationId: Guid option
        CausationId: Guid option
        UserId: string option
    }

    module LegalEventMeta =

        let create (companyId: CompanyId) = {
            EventId = Guid.NewGuid()
            OccurredAt = DateTimeOffset.UtcNow
            CompanyId = companyId
            CorrelationId = None
            CausationId = None
            UserId = None
        }

        let withUser (userId: string) (meta: LegalEventMeta) =
            { meta with UserId = Some userId }

    // ============================================
    // Company Events
    // ============================================

    /// Company incorporated event
    type CompanyIncorporatedEvent = {
        Meta: LegalEventMeta
        CorporateNumber: CorporateNumber
        LegalName: BilingualName
        EntityType: EntityType
        InitialCapital: Money
        FiscalYearEnd: FiscalYearEnd
        HeadquartersAddress: Address
        EstablishmentDate: Date
    }

    /// Capital increased event
    type CapitalIncreasedEvent = {
        Meta: LegalEventMeta
        PreviousCapital: Money
        NewCapital: Money
        IncreaseAmount: Money
        EffectiveDate: Date
    }

    /// Capital decreased event
    type CapitalDecreasedEvent = {
        Meta: LegalEventMeta
        PreviousCapital: Money
        NewCapital: Money
        DecreaseAmount: Money
        Reason: string
        EffectiveDate: Date
    }

    /// Company name changed event
    type CompanyNameChangedEvent = {
        Meta: LegalEventMeta
        PreviousName: BilingualName
        NewName: BilingualName
        EffectiveDate: Date
    }

    /// Headquarters address changed event
    type HeadquartersChangedEvent = {
        Meta: LegalEventMeta
        PreviousAddress: Address
        NewAddress: Address
        EffectiveDate: Date
    }

    /// Fiscal year end changed event
    type FiscalYearEndChangedEvent = {
        Meta: LegalEventMeta
        PreviousFiscalYearEnd: FiscalYearEnd
        NewFiscalYearEnd: FiscalYearEnd
        EffectiveDate: Date
    }

    /// Liquidation initiated event
    type LiquidationInitiatedEvent = {
        Meta: LegalEventMeta
        InitiatedDate: Date
        Reason: string
        Liquidator: PersonName option
    }

    /// Company dissolved event
    type CompanyDissolvedEvent = {
        Meta: LegalEventMeta
        DissolutionDate: Date
        Reason: string
    }

    // ============================================
    // Director Events
    // ============================================

    /// Director appointed event
    type DirectorAppointedEvent = {
        Meta: LegalEventMeta
        DirectorId: DirectorId
        Name: PersonName
        Position: DirectorPosition
        IsOutsideDirector: bool
        AppointmentDate: Date
        TermExpiry: Date
    }

    /// Director position
    and DirectorPosition =
        | Chairman              // 会長
        | President             // 社長
        | VicePresident         // 副社長
        | SeniorManagingDirector // 専務取締役
        | ManagingDirector      // 常務取締役
        | Director              // 取締役
        | OutsideDirector       // 社外取締役

    /// Representative director designated event
    type RepresentativeDirectorDesignatedEvent = {
        Meta: LegalEventMeta
        DirectorId: DirectorId
        PreviousRepresentativeId: DirectorId option
        DesignationDate: Date
    }

    /// Director removed event
    type DirectorRemovedEvent = {
        Meta: LegalEventMeta
        DirectorId: DirectorId
        RemovalDate: Date
        Reason: DirectorRemovalReason
    }

    /// Reason for director removal
    and DirectorRemovalReason =
        | TermExpired
        | Resignation
        | Dismissal
        | Death
        | Disqualification
        | Other of string

    /// Director term renewed event
    type DirectorTermRenewedEvent = {
        Meta: LegalEventMeta
        DirectorId: DirectorId
        PreviousTermExpiry: Date
        NewTermExpiry: Date
        RenewalDate: Date
    }

    // ============================================
    // Board Events
    // ============================================

    /// Board established event
    type BoardEstablishedEvent = {
        Meta: LegalEventMeta
        BoardId: BoardId
        Structure: BoardStructure
        InitialDirectors: DirectorId list
        EstablishmentDate: Date
    }

    /// Board structure types
    and BoardStructure =
        | WithStatutoryAuditors         // 監査役設置会社
        | WithAuditCommittee            // 監査等委員会設置会社
        | WithThreeCommittees           // 指名委員会等設置会社
        | WithoutBoard                  // 取締役会非設置会社

    /// Board meeting held event
    type BoardMeetingHeldEvent = {
        Meta: LegalEventMeta
        BoardId: BoardId
        MeetingDate: Date
        AttendeesCount: int
        ResolutionsPassed: int
    }

    /// Board resolution passed event
    type BoardResolutionPassedEvent = {
        Meta: LegalEventMeta
        BoardId: BoardId
        ResolutionType: ResolutionType
        Description: string
        VotesFor: int
        VotesAgainst: int
        Abstentions: int
        PassedDate: Date
    }

    /// Types of board resolutions
    and ResolutionType =
        | DirectorAppointment
        | DirectorRemoval
        | RepresentativeDesignation
        | DividendDeclaration
        | CapitalIncrease
        | CapitalDecrease
        | ShareIssuance
        | MajorTransaction
        | AmendmentOfArticles
        | Other of string

    // ============================================
    // Shareholder Events
    // ============================================

    /// Shares issued event
    type SharesIssuedEvent = {
        Meta: LegalEventMeta
        ShareholderId: ShareholderId
        ShareholderName: string
        ShareCount: int64
        ParValue: Money
        TotalValue: Money
        IssueDate: Date
    }

    /// Shares transferred event
    type SharesTransferredEvent = {
        Meta: LegalEventMeta
        FromShareholderId: ShareholderId
        ToShareholderId: ShareholderId
        ShareCount: int64
        TransferPrice: Money option
        TransferDate: Date
        BoardApprovalDate: Date option
    }

    /// Shareholder meeting held event
    type ShareholderMeetingHeldEvent = {
        Meta: LegalEventMeta
        MeetingType: ShareholderMeetingType
        MeetingDate: Date
        SharesRepresented: int64
        TotalIssuedShares: int64
        QuorumMet: bool
    }

    /// Types of shareholder meetings
    and ShareholderMeetingType =
        | AnnualGeneralMeeting      // 定時株主総会
        | ExtraordinaryMeeting      // 臨時株主総会

    /// Shareholder resolution passed event
    type ShareholderResolutionPassedEvent = {
        Meta: LegalEventMeta
        ResolutionType: ShareholderResolutionType
        Description: string
        VotesFor: int64
        VotesAgainst: int64
        RequiredMajority: decimal
        PassedDate: Date
    }

    /// Types of shareholder resolutions
    and ShareholderResolutionType =
        | OrdinaryResolution        // 普通決議
        | SpecialResolution         // 特別決議
        | SuperSpecialResolution    // 特殊決議

    // ============================================
    // Seal Events
    // ============================================

    /// Corporate seal registered event
    type CorporateSealRegisteredEvent = {
        Meta: LegalEventMeta
        SealType: SealType
        RegistrationDate: Date
        LegalAffairsBureau: string
    }

    /// Corporate seal retired event
    type CorporateSealRetiredEvent = {
        Meta: LegalEventMeta
        SealType: SealType
        RetirementDate: Date
        Reason: string
    }

    // ============================================
    // Unified Event Type
    // ============================================

    /// Union of all legal context events
    type LegalEvent =
        // Company events
        | CompanyIncorporated of CompanyIncorporatedEvent
        | CapitalIncreased of CapitalIncreasedEvent
        | CapitalDecreased of CapitalDecreasedEvent
        | CompanyNameChanged of CompanyNameChangedEvent
        | HeadquartersChanged of HeadquartersChangedEvent
        | FiscalYearEndChanged of FiscalYearEndChangedEvent
        | LiquidationInitiated of LiquidationInitiatedEvent
        | CompanyDissolved of CompanyDissolvedEvent

        // Director events
        | DirectorAppointed of DirectorAppointedEvent
        | RepresentativeDirectorDesignated of RepresentativeDirectorDesignatedEvent
        | DirectorRemoved of DirectorRemovedEvent
        | DirectorTermRenewed of DirectorTermRenewedEvent

        // Board events
        | BoardEstablished of BoardEstablishedEvent
        | BoardMeetingHeld of BoardMeetingHeldEvent
        | BoardResolutionPassed of BoardResolutionPassedEvent

        // Shareholder events
        | SharesIssued of SharesIssuedEvent
        | SharesTransferred of SharesTransferredEvent
        | ShareholderMeetingHeld of ShareholderMeetingHeldEvent
        | ShareholderResolutionPassed of ShareholderResolutionPassedEvent

        // Seal events
        | CorporateSealRegistered of CorporateSealRegisteredEvent
        | CorporateSealRetired of CorporateSealRetiredEvent

        interface IDomainEvent with
            member this.OccurredAt =
                match this with
                | CompanyIncorporated e -> e.Meta.OccurredAt
                | CapitalIncreased e -> e.Meta.OccurredAt
                | CapitalDecreased e -> e.Meta.OccurredAt
                | CompanyNameChanged e -> e.Meta.OccurredAt
                | HeadquartersChanged e -> e.Meta.OccurredAt
                | FiscalYearEndChanged e -> e.Meta.OccurredAt
                | LiquidationInitiated e -> e.Meta.OccurredAt
                | CompanyDissolved e -> e.Meta.OccurredAt
                | DirectorAppointed e -> e.Meta.OccurredAt
                | RepresentativeDirectorDesignated e -> e.Meta.OccurredAt
                | DirectorRemoved e -> e.Meta.OccurredAt
                | DirectorTermRenewed e -> e.Meta.OccurredAt
                | BoardEstablished e -> e.Meta.OccurredAt
                | BoardMeetingHeld e -> e.Meta.OccurredAt
                | BoardResolutionPassed e -> e.Meta.OccurredAt
                | SharesIssued e -> e.Meta.OccurredAt
                | SharesTransferred e -> e.Meta.OccurredAt
                | ShareholderMeetingHeld e -> e.Meta.OccurredAt
                | ShareholderResolutionPassed e -> e.Meta.OccurredAt
                | CorporateSealRegistered e -> e.Meta.OccurredAt
                | CorporateSealRetired e -> e.Meta.OccurredAt

            member this.EventId =
                match this with
                | CompanyIncorporated e -> e.Meta.EventId
                | CapitalIncreased e -> e.Meta.EventId
                | CapitalDecreased e -> e.Meta.EventId
                | CompanyNameChanged e -> e.Meta.EventId
                | HeadquartersChanged e -> e.Meta.EventId
                | FiscalYearEndChanged e -> e.Meta.EventId
                | LiquidationInitiated e -> e.Meta.EventId
                | CompanyDissolved e -> e.Meta.EventId
                | DirectorAppointed e -> e.Meta.EventId
                | RepresentativeDirectorDesignated e -> e.Meta.EventId
                | DirectorRemoved e -> e.Meta.EventId
                | DirectorTermRenewed e -> e.Meta.EventId
                | BoardEstablished e -> e.Meta.EventId
                | BoardMeetingHeld e -> e.Meta.EventId
                | BoardResolutionPassed e -> e.Meta.EventId
                | SharesIssued e -> e.Meta.EventId
                | SharesTransferred e -> e.Meta.EventId
                | ShareholderMeetingHeld e -> e.Meta.EventId
                | ShareholderResolutionPassed e -> e.Meta.EventId
                | CorporateSealRegistered e -> e.Meta.EventId
                | CorporateSealRetired e -> e.Meta.EventId

    module LegalEvent =

        let getCompanyId = function
            | CompanyIncorporated e -> e.Meta.CompanyId
            | CapitalIncreased e -> e.Meta.CompanyId
            | CapitalDecreased e -> e.Meta.CompanyId
            | CompanyNameChanged e -> e.Meta.CompanyId
            | HeadquartersChanged e -> e.Meta.CompanyId
            | FiscalYearEndChanged e -> e.Meta.CompanyId
            | LiquidationInitiated e -> e.Meta.CompanyId
            | CompanyDissolved e -> e.Meta.CompanyId
            | DirectorAppointed e -> e.Meta.CompanyId
            | RepresentativeDirectorDesignated e -> e.Meta.CompanyId
            | DirectorRemoved e -> e.Meta.CompanyId
            | DirectorTermRenewed e -> e.Meta.CompanyId
            | BoardEstablished e -> e.Meta.CompanyId
            | BoardMeetingHeld e -> e.Meta.CompanyId
            | BoardResolutionPassed e -> e.Meta.CompanyId
            | SharesIssued e -> e.Meta.CompanyId
            | SharesTransferred e -> e.Meta.CompanyId
            | ShareholderMeetingHeld e -> e.Meta.CompanyId
            | ShareholderResolutionPassed e -> e.Meta.CompanyId
            | CorporateSealRegistered e -> e.Meta.CompanyId
            | CorporateSealRetired e -> e.Meta.CompanyId
