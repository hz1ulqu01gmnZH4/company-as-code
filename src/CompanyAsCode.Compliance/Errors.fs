namespace CompanyAsCode.Compliance

open CompanyAsCode.SharedKernel.Temporal

/// Domain errors for Compliance context
module Errors =

    // ============================================
    // Compliance Requirement Errors
    // ============================================

    /// Errors related to compliance requirements
    type ComplianceError =
        | InvalidRequirement of message: string
        | RequirementNotFound of requirementId: string
        | RequirementExpired of requirementCode: string * expiredOn: Date
        | MissingRequiredDocument of documentType: string
        | ComplianceCheckFailed of reason: string
        | InsufficientEvidence of requirementCode: string
        | DuplicateRequirementCode of code: string

    module ComplianceError =

        let message (error: ComplianceError) =
            match error with
            | InvalidRequirement msg -> $"Invalid requirement: {msg}"
            | RequirementNotFound id -> $"Requirement not found: {id}"
            | RequirementExpired (code, date) -> $"Requirement {code} expired on {date}"
            | MissingRequiredDocument docType -> $"Missing required document: {docType}"
            | ComplianceCheckFailed reason -> $"Compliance check failed: {reason}"
            | InsufficientEvidence code -> $"Insufficient evidence for requirement: {code}"
            | DuplicateRequirementCode code -> $"Duplicate requirement code: {code}"

    // ============================================
    // Filing Errors
    // ============================================

    /// Errors related to regulatory filings
    type FilingError =
        | InvalidFiling of message: string
        | FilingNotFound of filingId: string
        | FilingAlreadySubmitted of filingNumber: string
        | FilingDeadlinePassed of filingType: string * deadline: Date
        | MissingRequiredAttachment of attachmentType: string
        | InvalidFilingPeriod of message: string
        | FilingRejected of reason: string

    module FilingError =

        let message (error: FilingError) =
            match error with
            | InvalidFiling msg -> $"Invalid filing: {msg}"
            | FilingNotFound id -> $"Filing not found: {id}"
            | FilingAlreadySubmitted num -> $"Filing already submitted: {num}"
            | FilingDeadlinePassed (filingType, deadline) -> $"{filingType} deadline passed: {deadline}"
            | MissingRequiredAttachment attachmentType -> $"Missing required attachment: {attachmentType}"
            | InvalidFilingPeriod msg -> $"Invalid filing period: {msg}"
            | FilingRejected reason -> $"Filing rejected: {reason}"

    // ============================================
    // Audit Errors
    // ============================================

    /// Errors related to audit operations
    type AuditError =
        | InvalidAudit of message: string
        | AuditNotFound of auditId: string
        | AuditAlreadyComplete of auditId: string
        | FindingNotResolved of findingId: string
        | InvalidAuditPeriod of message: string
        | AuditorNotQualified of message: string

    module AuditError =

        let message (error: AuditError) =
            match error with
            | InvalidAudit msg -> $"Invalid audit: {msg}"
            | AuditNotFound id -> $"Audit not found: {id}"
            | AuditAlreadyComplete id -> $"Audit already complete: {id}"
            | FindingNotResolved id -> $"Finding not resolved: {id}"
            | InvalidAuditPeriod msg -> $"Invalid audit period: {msg}"
            | AuditorNotQualified msg -> $"Auditor not qualified: {msg}"

    // ============================================
    // Privacy & Data Protection Errors
    // ============================================

    /// Errors related to privacy compliance (個人情報保護法)
    type PrivacyError =
        | InvalidConsent of message: string
        | ConsentExpired of dataSubjectId: string
        | DataRetentionViolation of message: string
        | UnauthorizedDataAccess of message: string
        | DataBreachNotReported of message: string
        | InvalidDataProcessingPurpose of message: string

    module PrivacyError =

        let message (error: PrivacyError) =
            match error with
            | InvalidConsent msg -> $"Invalid consent: {msg}"
            | ConsentExpired id -> $"Consent expired for data subject: {id}"
            | DataRetentionViolation msg -> $"Data retention violation: {msg}"
            | UnauthorizedDataAccess msg -> $"Unauthorized data access: {msg}"
            | DataBreachNotReported msg -> $"Data breach not reported: {msg}"
            | InvalidDataProcessingPurpose msg -> $"Invalid data processing purpose: {msg}"
