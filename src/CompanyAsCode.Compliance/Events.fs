namespace CompanyAsCode.Compliance

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Temporal

/// Domain events for Compliance context
module Events =

    // ============================================
    // Event Metadata
    // ============================================

    /// Compliance event metadata
    type ComplianceEventMeta = {
        EventId: Guid
        CompanyId: CompanyId
        OccurredAt: DateTimeOffset
    }

    module ComplianceEventMeta =

        let create (companyId: CompanyId) : ComplianceEventMeta =
            {
                EventId = Guid.NewGuid()
                CompanyId = companyId
                OccurredAt = DateTimeOffset.UtcNow
            }

    // ============================================
    // Compliance Requirement Types
    // ============================================

    /// Japanese regulatory body
    type RegulatoryBody =
        | LegalAffairsBureau           // 法務局
        | TaxOffice                    // 税務署
        | LaborStandardsOffice         // 労働基準監督署
        | PensionOffice                // 年金事務所
        | FSA                          // 金融庁
        | FairTradeCommission          // 公正取引委員会
        | PersonalInfoProtection       // 個人情報保護委員会
        | StockExchange                // 証券取引所
        | MinistryOfEconomy            // 経済産業省
        | LocalGovernment              // 地方自治体
        | Other of name: string

    /// Compliance requirement category
    type RequirementCategory =
        | CorporateRegistry            // 商業登記
        | TaxCompliance                // 税務コンプライアンス
        | LaborCompliance              // 労務コンプライアンス
        | FinancialReporting           // 財務報告
        | PrivacyProtection            // 個人情報保護
        | AntitTrust                   // 独占禁止法
        | InternalControl              // 内部統制
        | EnvironmentalCompliance      // 環境規制
        | IndustrySpecific of string   // 業界固有規制

    /// Compliance frequency
    type ComplianceFrequency =
        | OneTime           // 一回限り
        | Annual            // 年次
        | SemiAnnual        // 半期
        | Quarterly         // 四半期
        | Monthly           // 月次
        | AsNeeded          // 随時
        | OnChange          // 変更時

    /// Compliance status
    type ComplianceStatus =
        | Compliant             // 適合
        | NonCompliant          // 不適合
        | PartiallyCompliant    // 部分適合
        | PendingReview         // 審査中
        | NotApplicable         // 該当なし
        | Unknown               // 不明

    // ============================================
    // Filing Types
    // ============================================

    /// Japanese regulatory filing type
    type FilingType =
        // Corporate registry filings (商業登記)
        | IncorporationFiling                  // 設立登記
        | DirectorChangeFiling                 // 役員変更登記
        | AddressChangeFiling                  // 本店移転登記
        | CapitalChangeFiling                  // 資本金変更登記
        | ArticlesAmendmentFiling              // 定款変更登記
        | DissolutionFiling                    // 解散登記

        // Tax filings (税務申告)
        | CorporateTaxReturn                   // 法人税申告
        | ConsumptionTaxReturn                 // 消費税申告
        | WithholdingTaxReport                 // 源泉徴収報告
        | FixedAssetTaxReturn                  // 固定資産税申告
        | BusinessTaxReturn                    // 事業税申告

        // Labor filings (労務届出)
        | LaborInsuranceReport                 // 労働保険申告
        | SocialInsuranceReport                // 社会保険届出
        | WorkRulesNotification                // 就業規則届出
        | OvertimeAgreement                    // 36協定届出
        | WorkersCompReport                    // 労災報告

        // Financial filings
        | AnnualSecuritiesReport               // 有価証券報告書
        | QuarterlySecuritiesReport            // 四半期報告書
        | InternalControlReport                // 内部統制報告書

        | Other of name: string

    /// Filing status
    type FilingStatus =
        | Draft
        | UnderPreparation
        | ReadyForSubmission
        | Submitted
        | Acknowledged
        | Accepted
        | Rejected of reason: string
        | RequiresAmendment

    // ============================================
    // Audit Types
    // ============================================

    /// Audit type
    type AuditType =
        | InternalAudit                // 内部監査
        | ExternalAudit                // 外部監査
        | TaxAudit                     // 税務調査
        | RegulatoryInspection         // 規制当局検査
        | QualityAudit                 // 品質監査
        | InformationSecurityAudit     // 情報セキュリティ監査
        | EnvironmentalAudit           // 環境監査

    /// Audit status
    type AuditStatus =
        | Planned
        | InProgress
        | FindingsReported
        | CorrectiveActionsRequired
        | Closed
        | Cancelled

    /// Finding severity
    type FindingSeverity =
        | Critical          // 重大
        | Major             // 重要
        | Minor             // 軽微
        | Observation       // 観察事項

    // ============================================
    // Compliance Events
    // ============================================

    /// Requirement created event
    type RequirementCreatedEvent = {
        Meta: ComplianceEventMeta
        RequirementId: RequirementId
        RequirementCode: string
        Description: string
        Category: RequirementCategory
        RegulatoryBody: RegulatoryBody
        Frequency: ComplianceFrequency
    }

    /// Compliance check completed event
    type ComplianceCheckCompletedEvent = {
        Meta: ComplianceEventMeta
        RequirementId: RequirementId
        CheckDate: Date
        Status: ComplianceStatus
        Findings: string option
        NextCheckDue: Date option
    }

    /// Filing submitted event
    type FilingSubmittedEvent = {
        Meta: ComplianceEventMeta
        FilingId: FilingId
        FilingType: FilingType
        FilingNumber: string
        SubmittedDate: Date
        Deadline: Date
        RegulatoryBody: RegulatoryBody
    }

    /// Filing accepted event
    type FilingAcceptedEvent = {
        Meta: ComplianceEventMeta
        FilingId: FilingId
        AcceptedDate: Date
        Reference: string
    }

    /// Audit started event
    type AuditStartedEvent = {
        Meta: ComplianceEventMeta
        AuditId: AuditEntryId
        AuditType: AuditType
        StartDate: Date
        AuditorName: string
        Scope: string
    }

    /// Audit completed event
    type AuditCompletedEvent = {
        Meta: ComplianceEventMeta
        AuditId: AuditEntryId
        CompletedDate: Date
        FindingsCount: int
        CriticalFindings: int
        Opinion: string option
    }

    // ============================================
    // Combined Event Type
    // ============================================

    /// All compliance events
    type ComplianceEvent =
        | RequirementCreated of RequirementCreatedEvent
        | ComplianceCheckCompleted of ComplianceCheckCompletedEvent
        | FilingSubmitted of FilingSubmittedEvent
        | FilingAccepted of FilingAcceptedEvent
        | AuditStarted of AuditStartedEvent
        | AuditCompleted of AuditCompletedEvent
