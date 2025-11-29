namespace CompanyAsCode.SharedKernel

open System

/// Strongly-typed identifiers for domain entities
[<AutoOpen>]
module Identity =

    // ============================================
    // Base Identity Types
    // ============================================

    /// Generic entity identifier wrapper
    [<Struct>]
    type EntityId<'T> = private EntityId of Guid

    module EntityId =
        let create<'T> () : EntityId<'T> = EntityId (Guid.NewGuid())
        let fromGuid<'T> (guid: Guid) : EntityId<'T> = EntityId guid
        let value (EntityId guid) = guid
        let toString (EntityId guid) = guid.ToString("N")

        let tryParse<'T> (s: string) : EntityId<'T> option =
            match Guid.TryParse(s) with
            | true, guid -> Some (EntityId guid)
            | false, _ -> None

    // ============================================
    // Legal Context Identifiers
    // ============================================

    /// Company aggregate identifier
    type CompanyId = CompanyId of Guid

    module CompanyId =
        let create () = CompanyId (Guid.NewGuid())
        let fromGuid guid = CompanyId guid
        let value (CompanyId guid) = guid
        let toString (CompanyId guid) = guid.ToString("N")

        let tryParse (s: string) =
            match Guid.TryParse(s) with
            | true, guid -> Some (CompanyId guid)
            | false, _ -> None

    /// Director identifier
    type DirectorId = DirectorId of Guid

    module DirectorId =
        let create () = DirectorId (Guid.NewGuid())
        let fromGuid guid = DirectorId guid
        let value (DirectorId guid) = guid
        let toString (DirectorId guid) = guid.ToString("N")

    /// Auditor identifier
    type AuditorId = AuditorId of Guid

    module AuditorId =
        let create () = AuditorId (Guid.NewGuid())
        let value (AuditorId guid) = guid

    /// Shareholder identifier
    type ShareholderId = ShareholderId of Guid

    module ShareholderId =
        let create () = ShareholderId (Guid.NewGuid())
        let value (ShareholderId guid) = guid

    /// Board identifier
    type BoardId = BoardId of Guid

    module BoardId =
        let create () = BoardId (Guid.NewGuid())
        let value (BoardId guid) = guid

    // ============================================
    // HR Context Identifiers
    // ============================================

    /// Employee identifier
    type EmployeeId = EmployeeId of Guid

    module EmployeeId =
        let create () = EmployeeId (Guid.NewGuid())
        let fromGuid guid = EmployeeId guid
        let value (EmployeeId guid) = guid
        let toString (EmployeeId guid) = guid.ToString("N")

    /// Employee number (社員番号) - business identifier
    type EmployeeNumber = private EmployeeNumber of string

    module EmployeeNumber =
        let create (value: string) : Result<EmployeeNumber, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Employee number cannot be empty"
            elif value.Length > 20 then
                Error "Employee number cannot exceed 20 characters"
            else
                Ok (EmployeeNumber value)

        let value (EmployeeNumber v) = v

    /// Department identifier
    type DepartmentId = DepartmentId of Guid

    module DepartmentId =
        let create () = DepartmentId (Guid.NewGuid())
        let value (DepartmentId guid) = guid

    /// Position identifier
    type PositionId = PositionId of Guid

    module PositionId =
        let create () = PositionId (Guid.NewGuid())
        let value (PositionId guid) = guid

    /// Contract identifier
    type ContractId = ContractId of Guid

    module ContractId =
        let create () = ContractId (Guid.NewGuid())
        let value (ContractId guid) = guid

    // ============================================
    // Financial Context Identifiers
    // ============================================

    /// Ledger identifier
    type LedgerId = LedgerId of Guid

    module LedgerId =
        let create () = LedgerId (Guid.NewGuid())
        let value (LedgerId guid) = guid

    /// Account identifier
    type AccountId = AccountId of Guid

    module AccountId =
        let create () = AccountId (Guid.NewGuid())
        let value (AccountId guid) = guid

    /// Account code (勘定科目コード)
    type AccountCode = private AccountCode of string

    module AccountCode =
        let create (value: string) : Result<AccountCode, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Account code cannot be empty"
            elif not (value |> Seq.forall Char.IsLetterOrDigit) then
                Error "Account code must be alphanumeric"
            else
                Ok (AccountCode value)

        let value (AccountCode v) = v

    /// Journal entry identifier
    type JournalEntryId = JournalEntryId of Guid

    module JournalEntryId =
        let create () = JournalEntryId (Guid.NewGuid())
        let value (JournalEntryId guid) = guid

    /// Fiscal year identifier
    type FiscalYearId = FiscalYearId of Guid

    module FiscalYearId =
        let create () = FiscalYearId (Guid.NewGuid())
        let value (FiscalYearId guid) = guid

    /// Invoice identifier
    type InvoiceId = InvoiceId of Guid

    module InvoiceId =
        let create () = InvoiceId (Guid.NewGuid())
        let value (InvoiceId guid) = guid

    /// Invoice number (請求書番号)
    type InvoiceNumber = private InvoiceNumber of string

    module InvoiceNumber =
        let create (value: string) : Result<InvoiceNumber, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Invoice number cannot be empty"
            else
                Ok (InvoiceNumber value)

        let value (InvoiceNumber v) = v

    // ============================================
    // Operations Context Identifiers
    // ============================================

    /// Customer identifier
    type CustomerId = CustomerId of Guid

    module CustomerId =
        let create () = CustomerId (Guid.NewGuid())
        let fromGuid guid = CustomerId guid
        let value (CustomerId guid) = guid

    /// Customer code (取引先コード)
    type CustomerCode = private CustomerCode of string

    module CustomerCode =
        let create (value: string) : Result<CustomerCode, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Customer code cannot be empty"
            elif value.Length > 20 then
                Error "Customer code cannot exceed 20 characters"
            else
                Ok (CustomerCode value)

        let value (CustomerCode v) = v

    /// Order identifier
    type OrderId = OrderId of Guid

    module OrderId =
        let create () = OrderId (Guid.NewGuid())
        let value (OrderId guid) = guid

    /// Order number (注文番号)
    type OrderNumber = private OrderNumber of string

    module OrderNumber =
        let create (value: string) : Result<OrderNumber, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Order number cannot be empty"
            else
                Ok (OrderNumber value)

        let value (OrderNumber v) = v

    /// Product identifier
    type ProductId = ProductId of Guid

    module ProductId =
        let create () = ProductId (Guid.NewGuid())
        let value (ProductId guid) = guid

    /// Product code (商品コード)
    type ProductCode = private ProductCode of string

    module ProductCode =
        let create (value: string) : Result<ProductCode, string> =
            if String.IsNullOrWhiteSpace(value) then
                Error "Product code cannot be empty"
            else
                Ok (ProductCode value)

        let value (ProductCode v) = v

    /// Vendor identifier
    type VendorId = VendorId of Guid

    module VendorId =
        let create () = VendorId (Guid.NewGuid())
        let value (VendorId guid) = guid

    // ============================================
    // Compliance Context Identifiers
    // ============================================

    /// Regulatory requirement identifier
    type RequirementId = RequirementId of Guid

    module RequirementId =
        let create () = RequirementId (Guid.NewGuid())
        let value (RequirementId guid) = guid

    /// Filing identifier
    type FilingId = FilingId of Guid

    module FilingId =
        let create () = FilingId (Guid.NewGuid())
        let value (FilingId guid) = guid

    /// Audit entry identifier
    type AuditEntryId = AuditEntryId of Guid

    module AuditEntryId =
        let create () = AuditEntryId (Guid.NewGuid())
        let value (AuditEntryId guid) = guid

    // ============================================
    // Operations Context Identifiers
    // ============================================

    /// Project identifier
    type ProjectId = ProjectId of Guid

    module ProjectId =
        let create () = ProjectId (Guid.NewGuid())
        let value (ProjectId guid) = guid

    /// Business partner identifier (customer/vendor)
    type BusinessPartnerId = {
        Id: Guid
        CompanyId: CompanyId
    }

    module BusinessPartnerId =
        let create (companyId: CompanyId) : BusinessPartnerId =
            { Id = Guid.NewGuid(); CompanyId = companyId }

        let value (bp: BusinessPartnerId) = bp.Id
