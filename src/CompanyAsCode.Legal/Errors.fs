namespace CompanyAsCode.Legal

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Domain errors for Legal & Corporate Governance context
module Errors =

    // ============================================
    // Company Errors
    // ============================================

    /// Errors related to company operations
    type CompanyError =
        // Validation errors
        | InvalidCorporateNumber of message: string
        | InvalidCompanyName of message: string
        | InvalidCapital of message: string

        // Business rule violations
        | InsufficientCapital of required: Money * actual: Money
        | CapitalBelowMinimum of entityType: Japanese.EntityType * minimum: decimal * actual: decimal
        | NoRepresentativeDirector
        | RepresentativeNotOnBoard of directorId: DirectorId
        | CompanyNotActive

        // State transition errors
        | CannotIncorporate of reason: string
        | CannotDissolve of reason: string
        | AlreadyDissolved
        | UnderLiquidation

        // Dividend errors
        | InsufficientNetAssets of required: Money * actual: Money
        | InsufficientRetainedEarnings
        | ReserveRequirementNotMet of requiredReserve: decimal * actualReserve: decimal

    module CompanyError =

        let toString = function
            | InvalidCorporateNumber msg -> $"Invalid corporate number: {msg}"
            | InvalidCompanyName msg -> $"Invalid company name: {msg}"
            | InvalidCapital msg -> $"Invalid capital: {msg}"
            | InsufficientCapital (required, actual) ->
                $"Insufficient capital: required {Money.format required}, actual {Money.format actual}"
            | CapitalBelowMinimum (entityType, min, actual) ->
                $"Capital ¥{actual:N0} is below minimum ¥{min:N0} for {Japanese.EntityType.toJapanese entityType}"
            | NoRepresentativeDirector -> "Company must have at least one representative director"
            | RepresentativeNotOnBoard dirId ->
                $"Representative director {DirectorId.toString dirId} is not on the board"
            | CompanyNotActive -> "Company is not in active status"
            | CannotIncorporate reason -> $"Cannot incorporate: {reason}"
            | CannotDissolve reason -> $"Cannot dissolve: {reason}"
            | AlreadyDissolved -> "Company has already been dissolved"
            | UnderLiquidation -> "Company is under liquidation"
            | InsufficientNetAssets (required, actual) ->
                $"Net assets {Money.format actual} below required minimum {Money.format required}"
            | InsufficientRetainedEarnings -> "Insufficient retained earnings for dividend"
            | ReserveRequirementNotMet (required, actual) ->
                $"Legal reserve requirement not met: required {required:P0}, actual {actual:P0}"

    // ============================================
    // Director Errors
    // ============================================

    /// Errors related to director operations
    type DirectorError =
        // Validation errors
        | InvalidDirectorName of message: string
        | InvalidTerm of message: string

        // Business rule violations
        | TermExceedsMaximum of maxYears: int * requestedYears: int
        | DirectorAlreadyOnBoard of directorId: DirectorId
        | DirectorNotOnBoard of directorId: DirectorId
        | CannotRemoveLastDirector
        | CannotRemoveRepresentativeDirector
        | OutsideDirectorRequired

        // State errors
        | DirectorTermExpired of directorId: DirectorId * expiredOn: Date
        | DirectorNotActive of directorId: DirectorId
        | NoRepresentativeDirector

    module DirectorError =

        let toString = function
            | InvalidDirectorName msg -> $"Invalid director name: {msg}"
            | InvalidTerm msg -> $"Invalid term: {msg}"
            | TermExceedsMaximum (max, requested) ->
                $"Term {requested} years exceeds maximum {max} years"
            | DirectorAlreadyOnBoard dirId ->
                $"Director {DirectorId.toString dirId} is already on the board"
            | DirectorNotOnBoard dirId ->
                $"Director {DirectorId.toString dirId} is not on the board"
            | CannotRemoveLastDirector -> "Cannot remove the last director"
            | CannotRemoveRepresentativeDirector -> "Must designate new representative before removing current one"
            | OutsideDirectorRequired -> "At least one outside director is required"
            | DirectorTermExpired (dirId, date) ->
                $"Director {DirectorId.toString dirId} term expired on {Date.format date}"
            | DirectorNotActive dirId ->
                $"Director {DirectorId.toString dirId} is not active"
            | NoRepresentativeDirector -> "Board must have at least one representative director"

    // ============================================
    // Board Errors
    // ============================================

    /// Errors related to board operations
    type BoardError =
        // Structure errors
        | InsufficientDirectors of required: int * actual: int
        | InsufficientOutsideDirectors of required: int * actual: int
        | BoardAlreadyExists
        | BoardNotEstablished

        // Meeting errors
        | QuorumNotMet of required: int * present: int
        | InvalidResolution of message: string
        | MeetingNotConvened

        // Director errors
        | DirectorError of DirectorError

    module BoardError =

        let toString = function
            | InsufficientDirectors (required, actual) ->
                $"Insufficient directors: required {required}, actual {actual}"
            | InsufficientOutsideDirectors (required, actual) ->
                $"Insufficient outside directors: required {required}, actual {actual}"
            | BoardAlreadyExists -> "Board of directors already exists"
            | BoardNotEstablished -> "Board of directors has not been established"
            | QuorumNotMet (required, present) ->
                $"Quorum not met: required {required}, present {present}"
            | InvalidResolution msg -> $"Invalid resolution: {msg}"
            | MeetingNotConvened -> "Board meeting has not been convened"
            | DirectorError err -> DirectorError.toString err

    // ============================================
    // Shareholder Errors
    // ============================================

    /// Errors related to shareholder operations
    type ShareholderError =
        // Validation errors
        | InvalidShareCount of message: string
        | InvalidShareTransfer of message: string

        // Business rule violations
        | InsufficientShares of required: int64 * available: int64
        | ShareholderNotFound of shareholderId: ShareholderId
        | TransferRequiresApproval
        | TransferNotApproved
        | CannotTransferToSelf

        // State errors
        | SharesAlreadyIssued of shareCount: int64
        | NoSharesIssued
        | ExceedsAuthorizedShares of authorized: int64 * requested: int64

    module ShareholderError =

        let toString = function
            | InvalidShareCount msg -> $"Invalid share count: {msg}"
            | InvalidShareTransfer msg -> $"Invalid share transfer: {msg}"
            | InsufficientShares (required, available) ->
                $"Insufficient shares: required {required}, available {available}"
            | ShareholderNotFound shId ->
                $"Shareholder {ShareholderId.value shId} not found"
            | TransferRequiresApproval -> "Share transfer requires board approval"
            | TransferNotApproved -> "Share transfer has not been approved"
            | CannotTransferToSelf -> "Cannot transfer shares to self"
            | SharesAlreadyIssued count -> $"Shares already issued: {count}"
            | NoSharesIssued -> "No shares have been issued"
            | ExceedsAuthorizedShares (auth, req) ->
                $"Requested {req} shares exceeds authorized {auth}"

    // ============================================
    // Seal Errors
    // ============================================

    /// Errors related to corporate seal operations
    type SealError =
        | SealNotRegistered of sealType: Japanese.SealType
        | SealAlreadyRegistered of sealType: Japanese.SealType
        | InvalidSealRegistration of message: string
        | RepresentativeSealRequired

    module SealError =

        let toString = function
            | SealNotRegistered sealType -> $"Seal not registered: {sealType}"
            | SealAlreadyRegistered sealType -> $"Seal already registered: {sealType}"
            | InvalidSealRegistration msg -> $"Invalid seal registration: {msg}"
            | RepresentativeSealRequired -> "Representative seal (実印) is required"

    // ============================================
    // Unified Legal Context Error
    // ============================================

    /// Union of all legal context errors
    type LegalError =
        | Company of CompanyError
        | Director of DirectorError
        | Board of BoardError
        | Shareholder of ShareholderError
        | Seal of SealError
        | Validation of message: string

    module LegalError =

        let toString = function
            | Company err -> CompanyError.toString err
            | Director err -> DirectorError.toString err
            | Board err -> BoardError.toString err
            | Shareholder err -> ShareholderError.toString err
            | Seal err -> SealError.toString err
            | Validation msg -> $"Validation error: {msg}"

        let fromCompany (err: CompanyError) = Company err
        let fromDirector (err: DirectorError) = Director err
        let fromBoard (err: BoardError) = Board err
        let fromShareholder (err: ShareholderError) = Shareholder err
        let fromSeal (err: SealError) = Seal err
