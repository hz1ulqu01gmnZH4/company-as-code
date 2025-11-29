namespace CompanyAsCode.Operations

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Business Partner (Customer/Vendor) aggregate
module BusinessPartner =

    open Events
    open Errors

    // ============================================
    // Partner Code (Value Object)
    // ============================================

    /// Partner code - unique identifier for business partners
    type PartnerCode = private PartnerCode of string

    module PartnerCode =

        let create (code: string) : Result<PartnerCode, string> =
            if System.String.IsNullOrWhiteSpace(code) then
                Error "Partner code cannot be empty"
            elif code.Length < 3 || code.Length > 20 then
                Error "Partner code must be 3-20 characters"
            elif not (code |> Seq.forall (fun c -> System.Char.IsLetterOrDigit(c) || c = '-')) then
                Error "Partner code must contain only letters, digits, and hyphens"
            else
                Ok (PartnerCode (code.ToUpperInvariant()))

        let value (PartnerCode code) = code

    // ============================================
    // Payment Terms (Value Object)
    // ============================================

    /// Standard payment terms in Japan
    type PaymentTerms = {
        DaysUntilDue: int          // 支払い期日までの日数
        ClosingDay: int            // 締め日 (e.g., 20 = 20日締め)
        PaymentDay: int            // 支払い日 (e.g., 10 = 翌月10日払い)
        PaymentMethod: string      // 支払い方法
    }

    module PaymentTerms =

        /// Default: End of month closing, payment on 20th of next month
        let defaultTerms : PaymentTerms =
            {
                DaysUntilDue = 30
                ClosingDay = 0   // Month end
                PaymentDay = 20
                PaymentMethod = "Bank Transfer"
            }

        /// 20th closing, 10th of next month payment (20日締め翌月10日払い)
        let standard20_10 : PaymentTerms =
            {
                DaysUntilDue = 20
                ClosingDay = 20
                PaymentDay = 10
                PaymentMethod = "Bank Transfer"
            }

        /// End of month closing, end of next month payment (末締め翌月末払い)
        let monthEnd : PaymentTerms =
            {
                DaysUntilDue = 30
                ClosingDay = 0
                PaymentDay = 0
                PaymentMethod = "Bank Transfer"
            }

        /// Calculate due date from invoice date
        let calculateDueDate (terms: PaymentTerms) (invoiceDate: Date) : Date =
            // Simplified calculation - add days until due
            Date.addDays terms.DaysUntilDue invoiceDate

    // ============================================
    // Business Partner State
    // ============================================

    /// Business partner state (immutable)
    type BusinessPartnerState = {
        Id: BusinessPartnerId
        Code: PartnerCode
        Name: string
        NameJapanese: string
        PartnerType: BusinessPartnerType
        Status: BusinessPartnerStatus

        // Contact information
        Address: Address option
        Phone: PhoneNumber option
        Email: Email option
        Website: string option

        // Business terms
        PaymentTerms: PaymentTerms
        CreditLimit: Money option
        TaxRegistrationNumber: string option    // インボイス登録番号

        // Banking
        BankName: string option
        BankBranch: string option
        AccountNumber: string option
        AccountType: string option

        // Metadata
        Industry: string option
        RepresentativeName: string option
        Notes: string option
        CreatedAt: DateTimeOffset
        LastTransactionDate: Date option
    }

    module BusinessPartnerState =

        let create
            (id: BusinessPartnerId)
            (code: PartnerCode)
            (name: string)
            (nameJapanese: string)
            (partnerType: BusinessPartnerType)
            : BusinessPartnerState =

            {
                Id = id
                Code = code
                Name = name
                NameJapanese = nameJapanese
                PartnerType = partnerType
                Status = BusinessPartnerStatus.Active
                Address = None
                Phone = None
                Email = None
                Website = None
                PaymentTerms = PaymentTerms.defaultTerms
                CreditLimit = None
                TaxRegistrationNumber = None
                BankName = None
                BankBranch = None
                AccountNumber = None
                AccountType = None
                Industry = None
                RepresentativeName = None
                Notes = None
                CreatedAt = DateTimeOffset.UtcNow
                LastTransactionDate = None
            }

        let isActive (state: BusinessPartnerState) =
            state.Status = BusinessPartnerStatus.Active

        let isCustomer (state: BusinessPartnerState) =
            match state.PartnerType with
            | Customer | Both -> true
            | _ -> false

        let isVendor (state: BusinessPartnerState) =
            match state.PartnerType with
            | Vendor | Both -> true
            | _ -> false

    // ============================================
    // Business Partner Aggregate
    // ============================================

    /// Business partner aggregate root
    type BusinessPartner private (state: BusinessPartnerState) =

        member _.State = state
        member _.Id = state.Id
        member _.Code = state.Code
        member _.Name = state.Name
        member _.NameJapanese = state.NameJapanese
        member _.PartnerType = state.PartnerType
        member _.Status = state.Status
        member _.PaymentTerms = state.PaymentTerms
        member _.CreditLimit = state.CreditLimit
        member _.TaxRegistrationNumber = state.TaxRegistrationNumber
        member _.IsActive = BusinessPartnerState.isActive state
        member _.IsCustomer = BusinessPartnerState.isCustomer state
        member _.IsVendor = BusinessPartnerState.isVendor state

        // ============================================
        // Commands
        // ============================================

        /// Update contact information
        member this.UpdateContactInfo
            (address: Address option)
            (phone: PhoneNumber option)
            (email: Email option)
            : BusinessPartner =

            BusinessPartner({
                state with
                    Address = address
                    Phone = phone
                    Email = email
            })

        /// Set payment terms
        member this.SetPaymentTerms(terms: PaymentTerms)
            : BusinessPartner =
            BusinessPartner({ state with PaymentTerms = terms })

        /// Set credit limit
        member this.SetCreditLimit(limit: Money)
            : Result<BusinessPartner, BusinessPartnerError> =

            result {
                do! Result.require
                        (Money.isPositive limit || Money.isZero limit)
                        (InvalidCreditLimit "Credit limit must be non-negative")

                return BusinessPartner({ state with CreditLimit = Some limit })
            }

        /// Set tax registration number (インボイス登録番号)
        member this.SetTaxRegistrationNumber(regNumber: string)
            : Result<BusinessPartner, BusinessPartnerError> =

            result {
                let cleaned = regNumber.Trim().ToUpperInvariant()

                do! Result.require
                        (cleaned.StartsWith("T") && cleaned.Length = 14)
                        (InvalidPartnerCode "Tax registration number must be T + 13 digits")

                return BusinessPartner({ state with TaxRegistrationNumber = Some cleaned })
            }

        /// Set bank account information
        member this.SetBankAccount
            (bankName: string)
            (branchName: string)
            (accountNumber: string)
            (accountType: string)
            : BusinessPartner =

            BusinessPartner({
                state with
                    BankName = Some bankName
                    BankBranch = Some branchName
                    AccountNumber = Some accountNumber
                    AccountType = Some accountType
            })

        /// Suspend the partner
        member this.Suspend(reason: string)
            : Result<BusinessPartner * OperationsEvent, BusinessPartnerError> =

            result {
                do! Result.require
                        (state.Status = BusinessPartnerStatus.Active)
                        (PartnerAlreadyInactive (PartnerCode.value state.Code))

                let newState = { state with Status = BusinessPartnerStatus.Suspended }

                let event = BusinessPartnerStatusChanged {
                    Meta = OperationsEventMeta.create state.Id.CompanyId
                    PartnerId = state.Id
                    OldStatus = BusinessPartnerStatus.Active
                    NewStatus = BusinessPartnerStatus.Suspended
                    Reason = reason
                }

                return (BusinessPartner(newState), event)
            }

        /// Deactivate the partner
        member this.Deactivate(reason: string)
            : Result<BusinessPartner * OperationsEvent, BusinessPartnerError> =

            result {
                do! Result.require
                        (state.Status <> BusinessPartnerStatus.Inactive)
                        (PartnerAlreadyInactive (PartnerCode.value state.Code))

                let newState = { state with Status = BusinessPartnerStatus.Inactive }

                let event = BusinessPartnerStatusChanged {
                    Meta = OperationsEventMeta.create state.Id.CompanyId
                    PartnerId = state.Id
                    OldStatus = state.Status
                    NewStatus = BusinessPartnerStatus.Inactive
                    Reason = reason
                }

                return (BusinessPartner(newState), event)
            }

        /// Reactivate the partner
        member this.Reactivate()
            : Result<BusinessPartner * OperationsEvent, BusinessPartnerError> =

            result {
                do! Result.require
                        (state.Status = BusinessPartnerStatus.Inactive || state.Status = BusinessPartnerStatus.Suspended)
                        (InvalidPartnerCode "Partner must be inactive or suspended to reactivate")

                let newState = { state with Status = BusinessPartnerStatus.Active }

                let event = BusinessPartnerStatusChanged {
                    Meta = OperationsEventMeta.create state.Id.CompanyId
                    PartnerId = state.Id
                    OldStatus = state.Status
                    NewStatus = BusinessPartnerStatus.Active
                    Reason = "Reactivated"
                }

                return (BusinessPartner(newState), event)
            }

        /// Record a transaction date
        member this.RecordTransaction(transactionDate: Date)
            : BusinessPartner =
            BusinessPartner({ state with LastTransactionDate = Some transactionDate })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a customer
        static member CreateCustomer
            (companyId: CompanyId)
            (code: string)
            (name: string)
            (nameJapanese: string)
            : Result<BusinessPartner * OperationsEvent, BusinessPartnerError> =

            result {
                let! partnerCode =
                    PartnerCode.create code
                    |> Result.mapError InvalidPartnerCode

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidPartnerName "Partner name cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(nameJapanese)))
                        (InvalidPartnerName "Japanese name cannot be empty")

                let partnerId = BusinessPartnerId.create companyId
                let state = BusinessPartnerState.create partnerId partnerCode name nameJapanese Customer

                let event = BusinessPartnerCreated {
                    Meta = OperationsEventMeta.create companyId
                    PartnerId = partnerId
                    PartnerCode = code
                    PartnerName = name
                    PartnerType = Customer
                    CreditLimit = None
                }

                return (BusinessPartner(state), event)
            }

        /// Create a vendor
        static member CreateVendor
            (companyId: CompanyId)
            (code: string)
            (name: string)
            (nameJapanese: string)
            : Result<BusinessPartner * OperationsEvent, BusinessPartnerError> =

            result {
                let! partnerCode =
                    PartnerCode.create code
                    |> Result.mapError InvalidPartnerCode

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidPartnerName "Partner name cannot be empty")

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(nameJapanese)))
                        (InvalidPartnerName "Japanese name cannot be empty")

                let partnerId = BusinessPartnerId.create companyId
                let state = BusinessPartnerState.create partnerId partnerCode name nameJapanese Vendor

                let event = BusinessPartnerCreated {
                    Meta = OperationsEventMeta.create companyId
                    PartnerId = partnerId
                    PartnerCode = code
                    PartnerName = name
                    PartnerType = Vendor
                    CreditLimit = None
                }

                return (BusinessPartner(state), event)
            }

        /// Reconstitute from state
        static member FromState(state: BusinessPartnerState) : BusinessPartner =
            BusinessPartner(state)

    // ============================================
    // Business Partner Logic
    // ============================================

    module BusinessPartnerLogic =

        /// Check if transaction amount exceeds credit limit
        let exceedsCreditLimit (partner: BusinessPartner) (amount: Money) : bool =
            match partner.CreditLimit with
            | None -> false  // No limit set
            | Some limit -> Money.amount amount > Money.amount limit

        /// Get partner code prefix based on type
        let codePrefix (partnerType: BusinessPartnerType) : string =
            match partnerType with
            | Customer -> "CUST"
            | Vendor -> "VEND"
            | Both -> "PART"
            | Affiliate -> "AFFL"
            | Partner -> "PRTN"
