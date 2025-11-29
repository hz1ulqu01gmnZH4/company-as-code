namespace CompanyAsCode.HR

open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Employee aggregate
module Employee =

    open Employment

    // ============================================
    // Employee Status
    // ============================================

    /// Employee status
    type EmployeeStatus =
        | Probation         // 試用期間中
        | Active            // 在籍中
        | OnLeave           // 休職中
        | Suspended         // 停職中
        | Resigned          // 退職
        | Terminated        // 解雇
        | Retired           // 定年退職

    // ============================================
    // Employee State
    // ============================================

    /// Employee state
    type EmployeeState = {
        Id: EmployeeId
        EmployeeNumber: EmployeeNumber
        CompanyId: CompanyId
        PersonName: PersonName
        Contact: ContactInfo
        Contract: EmploymentContract
        Status: EmployeeStatus
        DepartmentId: DepartmentId option
        PositionId: PositionId option
        ManagerId: EmployeeId option
        HireDate: Date
        TerminationDate: Date option
        SalaryStructure: SalaryStructure
        LeaveBalances: Map<LeaveType, LeaveBalance>
        BankAccount: BankAccount option
    }

    module EmployeeState =

        let tenure (asOfDate: Date) (state: EmployeeState) : Age =
            Age.calculate state.HireDate asOfDate

        let tenureYears (asOfDate: Date) (state: EmployeeState) : int =
            (tenure asOfDate state).Years

        let isActive (state: EmployeeState) : bool =
            match state.Status with
            | Active | Probation | OnLeave -> true
            | _ -> false

        let monthlySalary (state: EmployeeState) : Money =
            SalaryStructure.totalMonthlyGross state.SalaryStructure

    // ============================================
    // Employee Aggregate
    // ============================================

    /// Employee aggregate root
    type Employee private (state: EmployeeState) =

        member _.State = state
        member _.Id = state.Id
        member _.EmployeeNumber = state.EmployeeNumber
        member _.CompanyId = state.CompanyId
        member _.Name = state.PersonName
        member _.Status = state.Status
        member _.Contract = state.Contract
        member _.HireDate = state.HireDate
        member _.DepartmentId = state.DepartmentId
        member _.PositionId = state.PositionId

        member _.IsActive = EmployeeState.isActive state
        member _.TenureYears (asOf: Date) = EmployeeState.tenureYears asOf state
        member _.MonthlySalary = EmployeeState.monthlySalary state

        /// Get leave balance
        member _.GetLeaveBalance(leaveType: LeaveType) : LeaveBalance option =
            Map.tryFind leaveType state.LeaveBalances

        // ============================================
        // Commands
        // ============================================

        /// Complete probation period
        member this.CompleteProbation() : Result<Employee, EmploymentError> =
            result {
                do! Result.require
                        (state.Status = Probation)
                        (InvalidContract "Employee is not on probation")

                return Employee({ state with Status = Active })
            }

        /// Request leave
        member this.RequestLeave(leaveType: LeaveType) (days: decimal)
            : Result<Employee, EmploymentError> =

            result {
                do! Result.require
                        (EmployeeState.isActive state)
                        (InvalidContract "Employee is not active")

                let! balance =
                    Map.tryFind leaveType state.LeaveBalances
                    |> Result.ofOption (InsufficientLeaveBalance (leaveType, days, 0m))

                do! Result.require
                        (balance.Remaining >= days)
                        (InsufficientLeaveBalance (leaveType, days, balance.Remaining))

                let newBalance = {
                    balance with
                        Used = balance.Used + days
                        Remaining = balance.Remaining - days
                }

                let newBalances = Map.add leaveType newBalance state.LeaveBalances
                return Employee({ state with LeaveBalances = newBalances })
            }

        /// Update salary
        member this.UpdateSalary(newSalary: SalaryStructure)
            : Result<Employee, EmploymentError> =

            result {
                do! Result.require
                        (EmployeeState.isActive state)
                        (InvalidContract "Cannot update salary for inactive employee")

                return Employee({ state with SalaryStructure = newSalary })
            }

        /// Transfer to department
        member this.TransferToDepartment(departmentId: DepartmentId)
            : Employee =
            Employee({ state with DepartmentId = Some departmentId })

        /// Assign manager
        member this.AssignManager(managerId: EmployeeId)
            : Employee =
            Employee({ state with ManagerId = Some managerId })

        /// Update bank account
        member this.UpdateBankAccount(account: BankAccount)
            : Employee =
            Employee({ state with BankAccount = Some account })

        /// Update contact info
        member this.UpdateContact(contact: ContactInfo)
            : Employee =
            Employee({ state with Contact = contact })

        /// Start leave of absence
        member this.StartLeaveOfAbsence()
            : Result<Employee, EmploymentError> =

            result {
                do! Result.require
                        (state.Status = Active)
                        (InvalidContract "Only active employees can take leave of absence")

                return Employee({ state with Status = OnLeave })
            }

        /// Return from leave of absence
        member this.ReturnFromLeave()
            : Result<Employee, EmploymentError> =

            result {
                do! Result.require
                        (state.Status = OnLeave)
                        (InvalidContract "Employee is not on leave")

                return Employee({ state with Status = Active })
            }

        /// Resign
        member this.Resign(resignationDate: Date)
            : Employee =
            Employee({
                state with
                    Status = Resigned
                    TerminationDate = Some resignationDate
            })

        /// Terminate employment
        member this.Terminate(terminationDate: Date)
            : Employee =
            Employee({
                state with
                    Status = Terminated
                    TerminationDate = Some terminationDate
            })

        /// Retire
        member this.Retire(retirementDate: Date)
            : Employee =
            Employee({
                state with
                    Status = Retired
                    TerminationDate = Some retirementDate
            })

        /// Renew annual leave (typically at fiscal year start)
        member this.RenewAnnualLeave(asOfDate: Date)
            : Employee =

            let tenureYears = EmployeeState.tenureYears asOfDate state
            let monthlyWorkDays = state.Contract.WorkingHours.WeeklyHours / 5m * 4m // Rough estimate

            let entitledDays = decimal (calculateAnnualLeave tenureYears monthlyWorkDays)

            let currentBalance =
                Map.tryFind AnnualPaid state.LeaveBalances
                |> Option.defaultValue {
                    LeaveType = AnnualPaid
                    Entitled = 0m
                    Used = 0m
                    Remaining = 0m
                    CarryOver = 0m
                    ExpiryDate = None
                }

            // Carry over up to 20 days
            let carryOver = min 20m currentBalance.Remaining

            let newBalance = {
                LeaveType = AnnualPaid
                Entitled = entitledDays
                Used = 0m
                Remaining = entitledDays + carryOver
                CarryOver = carryOver
                ExpiryDate = Some (asOfDate |> Date.addYears 2)  // Expires in 2 years
            }

            let newBalances = Map.add AnnualPaid newBalance state.LeaveBalances
            Employee({ state with LeaveBalances = newBalances })

        // ============================================
        // Factory
        // ============================================

        /// Create new employee
        static member Create
            (companyId: CompanyId)
            (employeeNumber: EmployeeNumber)
            (name: PersonName)
            (contract: EmploymentContract)
            (salary: SalaryStructure)
            (hireDate: Date)
            : Employee =

            let initialStatus =
                match contract.Probation with
                | ProbationPeriod.NoProbation -> EmployeeStatus.Active
                | ProbationPeriod.Probation _ -> EmployeeStatus.Probation

            let initialLeaveBalances =
                // Initial annual leave based on employment type
                if EmploymentType.isRegular contract.EmploymentType then
                    Map.ofList [
                        (AnnualPaid, {
                            LeaveType = AnnualPaid
                            Entitled = 10m  // Initial 10 days after 6 months
                            Used = 0m
                            Remaining = 10m
                            CarryOver = 0m
                            ExpiryDate = Some (hireDate |> Date.addYears 2)
                        })
                    ]
                else
                    Map.empty

            let state = {
                Id = EmployeeId.create()
                EmployeeNumber = employeeNumber
                CompanyId = companyId
                PersonName = name
                Contact = ContactInfo.empty
                Contract = contract
                Status = initialStatus
                DepartmentId = None
                PositionId = None
                ManagerId = None
                HireDate = hireDate
                TerminationDate = None
                SalaryStructure = salary
                LeaveBalances = initialLeaveBalances
                BankAccount = None
            }

            Employee(state)

        /// Reconstitute from state
        static member FromState(state: EmployeeState) : Employee =
            Employee(state)

    // ============================================
    // Pure Logic
    // ============================================

    module EmployeeLogic =

        /// Standard retirement age in Japan
        let standardRetirementAge = 65

        /// Check if employee is due for retirement
        let isDueForRetirement (birthDate: Date) (asOfDate: Date) : bool =
            let age = Age.calculate birthDate asOfDate
            age.Years >= standardRetirementAge

        /// Calculate severance pay (simple formula)
        let calculateSeverancePay
            (monthlyBaseSalary: Money)
            (tenureYears: int)
            : Money =
            // Common formula: monthly salary * tenure * coefficient
            let coefficient = 1.0m  // Varies by company
            Money.multiply (decimal tenureYears * coefficient) monthlyBaseSalary

        /// Check if contract renewal is needed
        let needsContractRenewal (employee: Employee) (asOfDate: Date) : bool =
            EmploymentContract.isExpiring asOfDate employee.Contract
