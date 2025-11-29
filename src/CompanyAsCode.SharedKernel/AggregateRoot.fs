namespace CompanyAsCode.SharedKernel

open System

/// Base types and interfaces for DDD aggregates
module AggregateRoot =

    // ============================================
    // Domain Events
    // ============================================

    /// Marker interface for domain events
    type IDomainEvent =
        abstract member OccurredAt: DateTimeOffset
        abstract member EventId: Guid

    /// Base record for domain events
    type DomainEventBase = {
        EventId: Guid
        OccurredAt: DateTimeOffset
        CorrelationId: Guid option
        CausationId: Guid option
    }

    module DomainEventBase =

        let create () = {
            EventId = Guid.NewGuid()
            OccurredAt = DateTimeOffset.UtcNow
            CorrelationId = None
            CausationId = None
        }

        let withCorrelation (correlationId: Guid) (evt: DomainEventBase) =
            { evt with CorrelationId = Some correlationId }

        let withCausation (causationId: Guid) (evt: DomainEventBase) =
            { evt with CausationId = Some causationId }

    // ============================================
    // Aggregate Version
    // ============================================

    /// Version for optimistic concurrency
    [<Struct>]
    type AggregateVersion = AggregateVersion of int64

    module AggregateVersion =

        let initial = AggregateVersion 0L

        let increment (AggregateVersion v) = AggregateVersion (v + 1L)

        let value (AggregateVersion v) = v

    // ============================================
    // Aggregate State
    // ============================================

    /// Base interface for aggregate state
    type IAggregateState<'TId> =
        abstract member Id: 'TId
        abstract member Version: AggregateVersion

    /// Generic aggregate state wrapper
    type AggregateState<'TId, 'TState> = {
        Id: 'TId
        Version: AggregateVersion
        State: 'TState
        PendingEvents: IDomainEvent list
    }

    module AggregateState =

        let create (id: 'TId) (initialState: 'TState) = {
            Id = id
            Version = AggregateVersion.initial
            State = initialState
            PendingEvents = []
        }

        let addEvent (event: IDomainEvent) (agg: AggregateState<'TId, 'TState>) = {
            agg with
                PendingEvents = event :: agg.PendingEvents
                Version = AggregateVersion.increment agg.Version
        }

        let clearEvents (agg: AggregateState<'TId, 'TState>) = {
            agg with PendingEvents = []
        }

        let updateState (newState: 'TState) (agg: AggregateState<'TId, 'TState>) = {
            agg with State = newState
        }

    // ============================================
    // Aggregate Root Base Class
    // ============================================

    /// Abstract base class for aggregate roots (OOP style)
    [<AbstractClass>]
    type AggregateRoot<'TId, 'TEvent when 'TEvent :> IDomainEvent>
        (id: 'TId, version: AggregateVersion) =

        let mutable _version = version
        let mutable _pendingEvents: 'TEvent list = []

        /// Aggregate identifier
        member _.Id = id

        /// Current version for optimistic concurrency
        member _.Version = _version

        /// Events raised during command execution
        member _.PendingEvents = _pendingEvents |> List.rev

        /// Clear pending events after persistence
        member _.ClearPendingEvents() =
            _pendingEvents <- []

        /// Raise a domain event
        member internal this.RaiseEvent(event: 'TEvent) =
            _pendingEvents <- event :: _pendingEvents
            _version <- AggregateVersion.increment _version

        /// Apply event to update state (for event sourcing)
        abstract member Apply: 'TEvent -> unit

    // ============================================
    // Command Result
    // ============================================

    /// Result of executing a command on an aggregate
    type CommandResult<'TAggregate, 'TEvent, 'TError> =
        Result<'TAggregate * 'TEvent list, 'TError>

    module CommandResult =

        let success (aggregate: 'TAggregate) (events: 'TEvent list) : CommandResult<'TAggregate, 'TEvent, 'TError> =
            Ok (aggregate, events)

        let successWithEvent (aggregate: 'TAggregate) (event: 'TEvent) : CommandResult<'TAggregate, 'TEvent, 'TError> =
            Ok (aggregate, [event])

        let failure (error: 'TError) : CommandResult<'TAggregate, 'TEvent, 'TError> =
            Error error

        let map (f: 'TAggregate -> 'TAggregate2) (result: CommandResult<'TAggregate, 'TEvent, 'TError>) =
            result |> Result.map (fun (agg, events) -> (f agg, events))

        let bind (f: 'TAggregate * 'TEvent list -> CommandResult<'TAggregate2, 'TEvent, 'TError>)
                 (result: CommandResult<'TAggregate, 'TEvent, 'TError>) =
            result |> Result.bind f

    // ============================================
    // Repository Interface
    // ============================================

    /// Generic repository interface
    type IRepository<'TId, 'TAggregate> =
        abstract member GetById: 'TId -> Async<'TAggregate option>
        abstract member Save: 'TAggregate -> Async<unit>
        abstract member Delete: 'TId -> Async<unit>

    /// Repository with optimistic concurrency
    type IVersionedRepository<'TId, 'TAggregate> =
        inherit IRepository<'TId, 'TAggregate>
        abstract member SaveWithVersion: 'TAggregate -> AggregateVersion -> Async<Result<unit, string>>

    // ============================================
    // Unit of Work
    // ============================================

    /// Unit of work for transaction management
    type IUnitOfWork =
        abstract member Commit: unit -> Async<Result<unit, string>>
        abstract member Rollback: unit -> Async<unit>

    /// Unit of work with event publishing
    type IUnitOfWorkWithEvents =
        inherit IUnitOfWork
        abstract member RegisterEvent: IDomainEvent -> unit
        abstract member GetPendingEvents: unit -> IDomainEvent list

    // ============================================
    // Specification Pattern
    // ============================================

    /// Specification for querying aggregates
    type ISpecification<'T> =
        abstract member IsSatisfiedBy: 'T -> bool

    /// Composable specification
    type Specification<'T> =
        | Spec of ('T -> bool)
        | And of Specification<'T> * Specification<'T>
        | Or of Specification<'T> * Specification<'T>
        | Not of Specification<'T>

    module Specification =

        let isSatisfiedBy (spec: Specification<'T>) (item: 'T) : bool =
            let rec eval = function
                | Spec f -> f item
                | And (left, right) -> eval left && eval right
                | Or (left, right) -> eval left || eval right
                | Not inner -> not (eval inner)
            eval spec

        let create (predicate: 'T -> bool) : Specification<'T> =
            Spec predicate

        let andSpec (left: Specification<'T>) (right: Specification<'T>) : Specification<'T> =
            And (left, right)

        let orSpec (left: Specification<'T>) (right: Specification<'T>) : Specification<'T> =
            Or (left, right)

        let notSpec (spec: Specification<'T>) : Specification<'T> =
            Not spec

        /// Infix operators
        let (&&.) = andSpec
        let (||.) = orSpec
        let (!.) = notSpec

    // ============================================
    // Domain Service Interface
    // ============================================

    /// Marker interface for domain services
    type IDomainService = interface end

    // ============================================
    // Factory Interface
    // ============================================

    /// Factory for creating aggregates
    type IFactory<'TCommand, 'TAggregate, 'TEvent, 'TError> =
        abstract member Create: 'TCommand -> CommandResult<'TAggregate, 'TEvent, 'TError>

    // ============================================
    // Event Handler
    // ============================================

    /// Event handler interface
    type IEventHandler<'TEvent when 'TEvent :> IDomainEvent> =
        abstract member Handle: 'TEvent -> Async<unit>

    /// Synchronous event handler
    type ISyncEventHandler<'TEvent when 'TEvent :> IDomainEvent> =
        abstract member Handle: 'TEvent -> unit
