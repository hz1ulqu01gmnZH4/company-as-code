namespace CompanyAsCode.Operations

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal

/// Project aggregate
module Project =

    open Events
    open Errors

    // ============================================
    // Project Code (Value Object)
    // ============================================

    /// Project code - unique identifier for projects
    type ProjectCode = private ProjectCode of string

    module ProjectCode =

        let create (code: string) : Result<ProjectCode, string> =
            if System.String.IsNullOrWhiteSpace(code) then
                Error "Project code cannot be empty"
            elif code.Length < 3 || code.Length > 30 then
                Error "Project code must be 3-30 characters"
            elif not (code |> Seq.forall (fun c -> System.Char.IsLetterOrDigit(c) || c = '-' || c = '_')) then
                Error "Project code must contain only letters, digits, hyphens, and underscores"
            else
                Ok (ProjectCode (code.ToUpperInvariant()))

        let value (ProjectCode code) = code

    // ============================================
    // Milestone (Value Object)
    // ============================================

    /// Project milestone
    type Milestone = {
        Name: string
        Description: string option
        PlannedDate: Date
        ActualDate: Date option
        IsComplete: bool
    }

    module Milestone =

        let create (name: string) (plannedDate: Date) : Milestone =
            {
                Name = name
                Description = None
                PlannedDate = plannedDate
                ActualDate = None
                IsComplete = false
            }

        let complete (completedDate: Date) (milestone: Milestone) : Milestone =
            { milestone with ActualDate = Some completedDate; IsComplete = true }

        let isOverdue (asOfDate: Date) (milestone: Milestone) : bool =
            not milestone.IsComplete && Date.isAfter asOfDate milestone.PlannedDate

    // ============================================
    // Project Budget (Value Object)
    // ============================================

    /// Project budget tracking
    type ProjectBudget = {
        TotalBudget: Money
        LaborBudget: Money
        ExpenseBudget: Money
        ActualLabor: Money
        ActualExpenses: Money
        CommittedAmount: Money
    }

    module ProjectBudget =

        let create (total: Money) (labor: Money) (expense: Money) : ProjectBudget =
            {
                TotalBudget = total
                LaborBudget = labor
                ExpenseBudget = expense
                ActualLabor = Money.yen 0m
                ActualExpenses = Money.yen 0m
                CommittedAmount = Money.yen 0m
            }

        let totalActual (budget: ProjectBudget) : Money =
            Money.add budget.ActualLabor budget.ActualExpenses
            |> Result.defaultValue budget.ActualLabor

        let remainingBudget (budget: ProjectBudget) : Money =
            let actual = totalActual budget
            Money.subtract budget.TotalBudget actual
            |> Result.defaultValue (Money.yen 0m)

        let isOverBudget (budget: ProjectBudget) : bool =
            Money.amount (totalActual budget) > Money.amount budget.TotalBudget

        let utilizationRate (budget: ProjectBudget) : decimal =
            let total = Money.amount budget.TotalBudget
            if total > 0m then
                Money.amount (totalActual budget) / total * 100m
            else
                0m

    // ============================================
    // Project State
    // ============================================

    /// Project state (immutable)
    type ProjectState = {
        Id: ProjectId
        CompanyId: CompanyId
        Code: ProjectCode
        Name: string
        Description: string option
        ProjectType: ProjectType
        Status: ProjectStatus

        // Timeline
        StartDate: Date
        PlannedEndDate: Date
        ActualEndDate: Date option

        // Budget
        Budget: ProjectBudget

        // Milestones
        Milestones: Milestone list

        // Team
        ProjectManagerId: string option
        TeamMemberIds: string list

        // Related entities
        CustomerId: BusinessPartnerId option
        ContractId: ContractId option

        // Metadata
        CreatedAt: DateTimeOffset
        UpdatedAt: DateTimeOffset
    }

    module ProjectState =

        let create
            (id: ProjectId)
            (companyId: CompanyId)
            (code: ProjectCode)
            (name: string)
            (projectType: ProjectType)
            (startDate: Date)
            (plannedEndDate: Date)
            (budget: ProjectBudget)
            : ProjectState =

            {
                Id = id
                CompanyId = companyId
                Code = code
                Name = name
                Description = None
                ProjectType = projectType
                Status = ProjectStatus.Planning
                StartDate = startDate
                PlannedEndDate = plannedEndDate
                ActualEndDate = None
                Budget = budget
                Milestones = []
                ProjectManagerId = None
                TeamMemberIds = []
                CustomerId = None
                ContractId = None
                CreatedAt = DateTimeOffset.UtcNow
                UpdatedAt = DateTimeOffset.UtcNow
            }

        let isActive (state: ProjectState) =
            state.Status = ProjectStatus.Active

        let isClosed (state: ProjectState) =
            match state.Status with
            | Completed | Cancelled -> true
            | _ -> false

        let durationDays (state: ProjectState) : int =
            let endDate = state.ActualEndDate |> Option.defaultValue state.PlannedEndDate
            Date.daysBetween state.StartDate endDate

    // ============================================
    // Project Aggregate
    // ============================================

    /// Project aggregate root
    type Project private (state: ProjectState) =

        member _.State = state
        member _.Id = state.Id
        member _.CompanyId = state.CompanyId
        member _.Code = state.Code
        member _.Name = state.Name
        member _.ProjectType = state.ProjectType
        member _.Status = state.Status
        member _.StartDate = state.StartDate
        member _.PlannedEndDate = state.PlannedEndDate
        member _.ActualEndDate = state.ActualEndDate
        member _.Budget = state.Budget
        member _.Milestones = state.Milestones
        member _.IsActive = ProjectState.isActive state
        member _.IsClosed = ProjectState.isClosed state
        member _.DurationDays = ProjectState.durationDays state

        // ============================================
        // Commands
        // ============================================

        /// Start the project
        member this.Start()
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                do! Result.require
                        (state.Status = ProjectStatus.Planning)
                        (InvalidProjectPhase "Project must be in planning phase to start")

                let newState = {
                    state with
                        Status = ProjectStatus.Active
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProjectStatusChanged {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProjectId = state.Id
                    OldStatus = ProjectStatus.Planning
                    NewStatus = ProjectStatus.Active
                    Reason = Some "Project started"
                }

                return (Project(newState), event)
            }

        /// Put project on hold
        member this.PutOnHold(reason: string)
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                do! Result.require
                        (state.Status = ProjectStatus.Active)
                        (InvalidProjectPhase "Only active projects can be put on hold")

                let newState = {
                    state with
                        Status = ProjectStatus.OnHold
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProjectStatusChanged {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProjectId = state.Id
                    OldStatus = ProjectStatus.Active
                    NewStatus = ProjectStatus.OnHold
                    Reason = Some reason
                }

                return (Project(newState), event)
            }

        /// Resume project from hold
        member this.Resume()
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                do! Result.require
                        (state.Status = ProjectStatus.OnHold)
                        (InvalidProjectPhase "Only on-hold projects can be resumed")

                let newState = {
                    state with
                        Status = ProjectStatus.Active
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProjectStatusChanged {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProjectId = state.Id
                    OldStatus = ProjectStatus.OnHold
                    NewStatus = ProjectStatus.Active
                    Reason = Some "Project resumed"
                }

                return (Project(newState), event)
            }

        /// Complete the project
        member this.Complete(completedDate: Date)
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                do! Result.require
                        (state.Status = ProjectStatus.Active)
                        (InvalidProjectPhase "Only active projects can be completed")

                let actualCost = ProjectBudget.totalActual state.Budget
                let withinBudget = not (ProjectBudget.isOverBudget state.Budget)

                let newState = {
                    state with
                        Status = ProjectStatus.Completed
                        ActualEndDate = Some completedDate
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProjectCompleted {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProjectId = state.Id
                    CompletedDate = completedDate
                    ActualCost = actualCost
                    WithinBudget = withinBudget
                }

                return (Project(newState), event)
            }

        /// Cancel the project
        member this.Cancel(reason: string)
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                do! Result.require
                        (not (ProjectState.isClosed state))
                        (ProjectAlreadyClosed (ProjectCode.value state.Code))

                let newState = {
                    state with
                        Status = ProjectStatus.Cancelled
                        UpdatedAt = DateTimeOffset.UtcNow
                }

                let event = ProjectStatusChanged {
                    Meta = OperationsEventMeta.create state.CompanyId
                    ProjectId = state.Id
                    OldStatus = state.Status
                    NewStatus = ProjectStatus.Cancelled
                    Reason = Some reason
                }

                return (Project(newState), event)
            }

        /// Add a milestone
        member this.AddMilestone(name: string) (plannedDate: Date)
            : Result<Project, ProjectError> =

            result {
                do! Result.require
                        (not (ProjectState.isClosed state))
                        (CannotModifyClosedProject (ProjectCode.value state.Code))

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidMilestone "Milestone name cannot be empty")

                let milestone = Milestone.create name plannedDate

                return Project({
                    state with
                        Milestones = state.Milestones @ [milestone]
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Complete a milestone
        member this.CompleteMilestone(milestoneName: string) (completedDate: Date)
            : Result<Project, ProjectError> =

            result {
                do! Result.require
                        (not (ProjectState.isClosed state))
                        (CannotModifyClosedProject (ProjectCode.value state.Code))

                let milestoneExists = state.Milestones |> List.exists (fun m -> m.Name = milestoneName)
                do! Result.require milestoneExists (InvalidMilestone $"Milestone not found: {milestoneName}")

                let updatedMilestones =
                    state.Milestones
                    |> List.map (fun m ->
                        if m.Name = milestoneName then
                            Milestone.complete completedDate m
                        else m)

                return Project({
                    state with
                        Milestones = updatedMilestones
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Record labor cost
        member this.RecordLaborCost(amount: Money)
            : Result<Project, ProjectError> =

            result {
                do! Result.require
                        (not (ProjectState.isClosed state))
                        (CannotModifyClosedProject (ProjectCode.value state.Code))

                let newActual =
                    Money.add state.Budget.ActualLabor amount
                    |> Result.defaultValue state.Budget.ActualLabor

                let newBudget = { state.Budget with ActualLabor = newActual }

                return Project({
                    state with
                        Budget = newBudget
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Record expense
        member this.RecordExpense(amount: Money)
            : Result<Project, ProjectError> =

            result {
                do! Result.require
                        (not (ProjectState.isClosed state))
                        (CannotModifyClosedProject (ProjectCode.value state.Code))

                let newActual =
                    Money.add state.Budget.ActualExpenses amount
                    |> Result.defaultValue state.Budget.ActualExpenses

                let newBudget = { state.Budget with ActualExpenses = newActual }

                return Project({
                    state with
                        Budget = newBudget
                        UpdatedAt = DateTimeOffset.UtcNow
                })
            }

        /// Assign project manager
        member this.AssignProjectManager(managerId: string)
            : Project =

            Project({
                state with
                    ProjectManagerId = Some managerId
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        /// Add team member
        member this.AddTeamMember(memberId: string)
            : Project =

            let newMembers =
                if state.TeamMemberIds |> List.contains memberId then
                    state.TeamMemberIds
                else
                    state.TeamMemberIds @ [memberId]

            Project({
                state with
                    TeamMemberIds = newMembers
                    UpdatedAt = DateTimeOffset.UtcNow
            })

        /// Link to customer
        member this.LinkToCustomer(customerId: BusinessPartnerId)
            : Project =
            Project({ state with CustomerId = Some customerId; UpdatedAt = DateTimeOffset.UtcNow })

        /// Link to contract
        member this.LinkToContract(contractId: ContractId)
            : Project =
            Project({ state with ContractId = Some contractId; UpdatedAt = DateTimeOffset.UtcNow })

        // ============================================
        // Factory Methods
        // ============================================

        /// Create a new project
        static member Create
            (companyId: CompanyId)
            (code: string)
            (name: string)
            (projectType: ProjectType)
            (startDate: Date)
            (plannedEndDate: Date)
            (totalBudget: Money)
            : Result<Project * OperationsEvent, ProjectError> =

            result {
                let! projectCode =
                    ProjectCode.create code
                    |> Result.mapError InvalidProjectCode

                do! Result.require
                        (not (System.String.IsNullOrWhiteSpace(name)))
                        (InvalidProjectName "Project name cannot be empty")

                do! Result.require
                        (Date.isOnOrBefore startDate plannedEndDate)
                        (InvalidProjectPhase "Planned end date must be on or after start date")

                // Split budget 70/30 between labor and expenses by default
                let laborBudget = Money.multiply 0.7m totalBudget
                let expenseBudget = Money.multiply 0.3m totalBudget
                let budget = ProjectBudget.create totalBudget laborBudget expenseBudget

                let projectId = ProjectId.create()
                let state = ProjectState.create projectId companyId projectCode name projectType startDate plannedEndDate budget

                let event = ProjectCreated {
                    Meta = OperationsEventMeta.create companyId
                    ProjectId = projectId
                    ProjectCode = code
                    ProjectName = name
                    ProjectType = projectType
                    StartDate = startDate
                    PlannedEndDate = plannedEndDate
                    Budget = totalBudget
                }

                return (Project(state), event)
            }

        /// Reconstitute from state
        static member FromState(state: ProjectState) : Project =
            Project(state)

    // ============================================
    // Project Logic
    // ============================================

    module ProjectLogic =

        /// Get overdue milestones
        let overdueMilestones (asOfDate: Date) (project: Project) : Milestone list =
            project.Milestones
            |> List.filter (Milestone.isOverdue asOfDate)

        /// Calculate schedule variance (positive = ahead, negative = behind)
        let scheduleVariance (asOfDate: Date) (project: Project) : int =
            let plannedDays = Date.daysBetween project.StartDate project.PlannedEndDate
            let elapsedDays = Date.daysBetween project.StartDate asOfDate

            let completedMilestones =
                project.Milestones |> List.filter (fun m -> m.IsComplete)

            let totalMilestones = project.Milestones.Length
            if totalMilestones = 0 then 0
            else
                let expectedProgress = float elapsedDays / float plannedDays
                let actualProgress = float completedMilestones.Length / float totalMilestones
                int ((actualProgress - expectedProgress) * float plannedDays)

        /// Calculate cost variance (positive = under budget)
        let costVariance (project: Project) : Money =
            ProjectBudget.remainingBudget project.Budget
