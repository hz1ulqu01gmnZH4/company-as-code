namespace CompanyAsCode.Operations

open CompanyAsCode.SharedKernel.Financial

/// Domain errors for Operations context
module Errors =

    // ============================================
    // Project Errors
    // ============================================

    /// Errors related to project operations
    type ProjectError =
        | InvalidProjectCode of message: string
        | InvalidProjectName of message: string
        | ProjectNotFound of projectId: string
        | ProjectAlreadyClosed of projectCode: string
        | ProjectBudgetExceeded of projectCode: string * budget: Money * actual: Money
        | InvalidProjectPhase of message: string
        | DuplicateProjectCode of code: string
        | InvalidMilestone of message: string
        | CannotModifyClosedProject of projectCode: string

    module ProjectError =

        let message (error: ProjectError) =
            match error with
            | InvalidProjectCode msg -> $"Invalid project code: {msg}"
            | InvalidProjectName msg -> $"Invalid project name: {msg}"
            | ProjectNotFound id -> $"Project not found: {id}"
            | ProjectAlreadyClosed code -> $"Project already closed: {code}"
            | ProjectBudgetExceeded (code, budget, actual) ->
                $"Project {code} budget exceeded: budget {Money.amount budget}, actual {Money.amount actual}"
            | InvalidProjectPhase msg -> $"Invalid project phase: {msg}"
            | DuplicateProjectCode code -> $"Duplicate project code: {code}"
            | InvalidMilestone msg -> $"Invalid milestone: {msg}"
            | CannotModifyClosedProject code -> $"Cannot modify closed project: {code}"

    // ============================================
    // Contract Errors
    // ============================================

    /// Errors related to contract operations
    type ContractError =
        | InvalidContractNumber of message: string
        | InvalidContractTerm of message: string
        | ContractNotFound of contractId: string
        | ContractAlreadySigned of contractNumber: string
        | ContractExpired of contractNumber: string
        | InvalidContractParty of message: string
        | ContractAmendmentError of message: string
        | MissingRequiredClause of clauseType: string
        | CannotTerminateActiveContract of message: string

    module ContractError =

        let message (error: ContractError) =
            match error with
            | InvalidContractNumber msg -> $"Invalid contract number: {msg}"
            | InvalidContractTerm msg -> $"Invalid contract term: {msg}"
            | ContractNotFound id -> $"Contract not found: {id}"
            | ContractAlreadySigned num -> $"Contract already signed: {num}"
            | ContractExpired num -> $"Contract expired: {num}"
            | InvalidContractParty msg -> $"Invalid contract party: {msg}"
            | ContractAmendmentError msg -> $"Contract amendment error: {msg}"
            | MissingRequiredClause clauseType -> $"Missing required clause: {clauseType}"
            | CannotTerminateActiveContract msg -> $"Cannot terminate active contract: {msg}"

    // ============================================
    // Customer/Vendor Errors
    // ============================================

    /// Errors related to business partner operations
    type BusinessPartnerError =
        | InvalidPartnerCode of message: string
        | InvalidPartnerName of message: string
        | PartnerNotFound of partnerId: string
        | DuplicatePartnerCode of code: string
        | InvalidCreditLimit of message: string
        | PartnerAlreadyInactive of partnerCode: string
        | InvalidPaymentTerms of message: string

    module BusinessPartnerError =

        let message (error: BusinessPartnerError) =
            match error with
            | InvalidPartnerCode msg -> $"Invalid partner code: {msg}"
            | InvalidPartnerName msg -> $"Invalid partner name: {msg}"
            | PartnerNotFound id -> $"Business partner not found: {id}"
            | DuplicatePartnerCode code -> $"Duplicate partner code: {code}"
            | InvalidCreditLimit msg -> $"Invalid credit limit: {msg}"
            | PartnerAlreadyInactive code -> $"Partner already inactive: {code}"
            | InvalidPaymentTerms msg -> $"Invalid payment terms: {msg}"

    // ============================================
    // Product/Service Errors
    // ============================================

    /// Errors related to product/service operations
    type ProductServiceError =
        | InvalidProductCode of message: string
        | InvalidProductName of message: string
        | ProductNotFound of productId: string
        | DuplicateProductCode of code: string
        | InvalidPrice of message: string
        | ProductAlreadyDiscontinued of productCode: string
        | InvalidServiceDuration of message: string

    module ProductServiceError =

        let message (error: ProductServiceError) =
            match error with
            | InvalidProductCode msg -> $"Invalid product code: {msg}"
            | InvalidProductName msg -> $"Invalid product name: {msg}"
            | ProductNotFound id -> $"Product not found: {id}"
            | DuplicateProductCode code -> $"Duplicate product code: {code}"
            | InvalidPrice msg -> $"Invalid price: {msg}"
            | ProductAlreadyDiscontinued code -> $"Product already discontinued: {code}"
            | InvalidServiceDuration msg -> $"Invalid service duration: {msg}"
