namespace CompanyAsCode.Operations

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Domain events for Operations context
module Events =

    // ============================================
    // Event Metadata
    // ============================================

    /// Operations event metadata
    type OperationsEventMeta = {
        EventId: Guid
        CompanyId: CompanyId
        OccurredAt: DateTimeOffset
    }

    module OperationsEventMeta =

        let create (companyId: CompanyId) : OperationsEventMeta =
            {
                EventId = Guid.NewGuid()
                CompanyId = companyId
                OccurredAt = DateTimeOffset.UtcNow
            }

    // ============================================
    // Project Events
    // ============================================

    /// Project status
    type ProjectStatus =
        | Planning        // 企画中
        | Active          // 進行中
        | OnHold          // 一時停止
        | Completed       // 完了
        | Cancelled       // 中止

    /// Project type
    type ProjectType =
        | Internal            // 社内プロジェクト
        | ClientProject       // 顧客プロジェクト
        | Research            // 研究開発
        | Maintenance         // 保守

    /// Project created event
    type ProjectCreatedEvent = {
        Meta: OperationsEventMeta
        ProjectId: ProjectId
        ProjectCode: string
        ProjectName: string
        ProjectType: ProjectType
        StartDate: Date
        PlannedEndDate: Date
        Budget: Money
    }

    /// Project status changed event
    type ProjectStatusChangedEvent = {
        Meta: OperationsEventMeta
        ProjectId: ProjectId
        OldStatus: ProjectStatus
        NewStatus: ProjectStatus
        Reason: string option
    }

    /// Project completed event
    type ProjectCompletedEvent = {
        Meta: OperationsEventMeta
        ProjectId: ProjectId
        CompletedDate: Date
        ActualCost: Money
        WithinBudget: bool
    }

    // ============================================
    // Contract Events
    // ============================================

    /// Contract type
    type ContractType =
        | ServiceAgreement        // サービス契約
        | SalesContract           // 売買契約
        | LicenseAgreement        // ライセンス契約
        | MaintenanceContract     // 保守契約
        | ConsultingAgreement     // コンサルティング契約
        | PartnershipAgreement    // 業務提携契約
        | NDA                     // 秘密保持契約
        | EmploymentRelated       // 雇用関連

    /// Contract status
    type ContractStatus =
        | Draft           // 下書き
        | UnderReview     // 審査中
        | Negotiating     // 交渉中
        | Signed          // 締結済
        | Active          // 有効
        | Expired         // 期限切れ
        | Terminated      // 解約
        | Renewed         // 更新済

    /// Contract created event
    type ContractCreatedEvent = {
        Meta: OperationsEventMeta
        ContractId: ContractId
        ContractNumber: string
        ContractType: ContractType
        CounterpartyId: string
        ContractValue: Money
        StartDate: Date
        EndDate: Date
    }

    /// Contract signed event
    type ContractSignedEvent = {
        Meta: OperationsEventMeta
        ContractId: ContractId
        SignedDate: Date
        SignedBy: string
        CounterpartySignatory: string
    }

    /// Contract terminated event
    type ContractTerminatedEvent = {
        Meta: OperationsEventMeta
        ContractId: ContractId
        TerminationDate: Date
        Reason: string
        TerminatedBy: string
    }

    // ============================================
    // Business Partner Events
    // ============================================

    /// Business partner type
    type BusinessPartnerType =
        | Customer        // 顧客
        | Vendor          // 仕入先
        | Both            // 顧客兼仕入先
        | Affiliate       // 関係会社
        | Partner         // 提携先

    /// Business partner status
    type BusinessPartnerStatus =
        | Active          // 取引中
        | Inactive        // 取引停止
        | Prospect        // 見込み
        | Suspended       // 一時停止

    /// Business partner created event
    type BusinessPartnerCreatedEvent = {
        Meta: OperationsEventMeta
        PartnerId: BusinessPartnerId
        PartnerCode: string
        PartnerName: string
        PartnerType: BusinessPartnerType
        CreditLimit: Money option
    }

    /// Business partner status changed event
    type BusinessPartnerStatusChangedEvent = {
        Meta: OperationsEventMeta
        PartnerId: BusinessPartnerId
        OldStatus: BusinessPartnerStatus
        NewStatus: BusinessPartnerStatus
        Reason: string
    }

    // ============================================
    // Product/Service Events
    // ============================================

    /// Product category
    type ProductCategory =
        | Software            // ソフトウェア
        | Hardware            // ハードウェア
        | Service             // サービス
        | Subscription        // サブスクリプション
        | License             // ライセンス
        | Consulting          // コンサルティング
        | Training            // 研修
        | Support             // サポート

    /// Product status
    type ProductStatus =
        | Development         // 開発中
        | Active              // 販売中
        | Discontinued        // 販売終了
        | EndOfLife           // サポート終了

    /// Product created event
    type ProductCreatedEvent = {
        Meta: OperationsEventMeta
        ProductId: ProductId
        ProductCode: string
        ProductName: string
        Category: ProductCategory
        UnitPrice: Money
    }

    /// Price changed event
    type PriceChangedEvent = {
        Meta: OperationsEventMeta
        ProductId: ProductId
        OldPrice: Money
        NewPrice: Money
        EffectiveDate: Date
    }

    // ============================================
    // Combined Event Type
    // ============================================

    /// All operations events
    type OperationsEvent =
        | ProjectCreated of ProjectCreatedEvent
        | ProjectStatusChanged of ProjectStatusChangedEvent
        | ProjectCompleted of ProjectCompletedEvent
        | ContractCreated of ContractCreatedEvent
        | ContractSigned of ContractSignedEvent
        | ContractTerminated of ContractTerminatedEvent
        | BusinessPartnerCreated of BusinessPartnerCreatedEvent
        | BusinessPartnerStatusChanged of BusinessPartnerStatusChangedEvent
        | ProductCreated of ProductCreatedEvent
        | PriceChanged of PriceChangedEvent
