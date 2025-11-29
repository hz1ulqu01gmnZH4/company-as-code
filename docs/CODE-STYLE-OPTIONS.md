# F# Code Style Options for Company-as-Code

## Overview

F# supports multiple paradigms. Here are concrete examples showing how the Japanese Company domain would look in each style.

---

## Option 1: OOP + Monadic (Railway-Oriented Programming)

**Characteristics**: Classes for aggregates, `Result<'T, 'E>` for error handling, computation expressions for composition.

```fsharp
// ============================================
// Value Objects (Immutable Records)
// ============================================
type CorporateNumber = private CorporateNumber of string

module CorporateNumber =
    let create (value: string) : Result<CorporateNumber, string> =
        if String.length value = 13 && value |> Seq.forall Char.IsDigit
        then Ok (CorporateNumber value)
        else Error "Corporate number must be 13 digits"

    let value (CorporateNumber v) = v

type Money = { Amount: decimal; Currency: Currency }
and Currency = JPY | USD | EUR

// ============================================
// Domain Errors (Discriminated Union)
// ============================================
type CompanyError =
    | InvalidCorporateNumber of string
    | InsufficientCapital of required: Money * actual: Money
    | NoRepresentativeDirector
    | DirectorTermExceeded of maxYears: int
    | DividendViolation of netAssets: Money

// ============================================
// Aggregate Root (Class with private state)
// ============================================
type Company private (state: CompanyState) =

    member _.Id = state.Id
    member _.LegalName = state.LegalName
    member _.EntityType = state.EntityType
    member _.Capital = state.Capital
    member _.Status = state.Status

    // Command: Increase Capital
    member _.IncreaseCapital(amount: Money) : Result<Company * CompanyEvent, CompanyError> =
        result {
            let newCapital = { state.Capital with Amount = state.Capital.Amount + amount.Amount }
            let newState = { state with Capital = newCapital }
            let event = CapitalIncreased {
                CompanyId = state.Id
                PreviousAmount = state.Capital
                NewAmount = newCapital
                Date = DateTime.UtcNow
            }
            return (Company(newState), event)
        }

    // Command: Appoint Representative Director
    member _.AppointRepresentativeDirector(director: Director) : Result<Company * CompanyEvent, CompanyError> =
        result {
            do! director.Term |> validateDirectorTerm
            let newState = { state with RepresentativeDirector = Some director.Id }
            let event = RepresentativeDirectorAppointed {
                CompanyId = state.Id
                DirectorId = director.Id
                AppointmentDate = DateTime.UtcNow
            }
            return (Company(newState), event)
        }

    // Factory: Incorporate new company
    static member Incorporate(cmd: IncorporateCompanyCommand) : Result<Company * CompanyEvent, CompanyError> =
        result {
            let! corpNumber = CorporateNumber.create cmd.CorporateNumber
                              |> Result.mapError InvalidCorporateNumber
            do! validateMinimumCapital cmd.EntityType cmd.InitialCapital

            let state = {
                Id = CompanyId.create()
                CorporateNumber = corpNumber
                LegalName = cmd.LegalName
                EntityType = cmd.EntityType
                Capital = cmd.InitialCapital
                Status = Active
                RepresentativeDirector = None
            }
            let event = CompanyIncorporated {
                CompanyId = state.Id
                CorporateNumber = corpNumber
                EntityType = cmd.EntityType
                EstablishmentDate = DateTime.UtcNow
            }
            return (Company(state), event)
        }

// ============================================
// Result Computation Expression (Railway)
// ============================================
type ResultBuilder() =
    member _.Bind(x, f) = Result.bind f x
    member _.Return(x) = Ok x
    member _.ReturnFrom(x) = x
    member _.Zero() = Ok ()

let result = ResultBuilder()

// ============================================
// Usage Example
// ============================================
let incorporateAndSetupCompany() =
    result {
        let! (company, evt1) =
            Company.Incorporate {
                CorporateNumber = "1234567890123"
                LegalName = { Japanese = "株式会社テスト"; English = Some "Test Corp." }
                EntityType = KabushikiKaisha
                InitialCapital = { Amount = 10_000_000m; Currency = JPY }
            }

        let director = { Id = DirectorId.create(); Name = "田中太郎"; Position = President }
        let! (company, evt2) = company.AppointRepresentativeDirector(director)

        return (company, [evt1; evt2])
    }
```

---

## Option 2: Pure Functional (Module-Based + Result)

**Characteristics**: Records for state, modules for behavior, pure functions, no classes.

```fsharp
// ============================================
// Types (All Records and DUs)
// ============================================
type CompanyId = CompanyId of Guid
type CorporateNumber = private CorporateNumber of string

type EntityType =
    | KabushikiKaisha   // 株式会社
    | GodoKaisha        // 合同会社
    | GomeiKaisha       // 合名会社
    | GoshiKaisha       // 合資会社

type CompanyStatus =
    | Active
    | Suspended
    | UnderLiquidation
    | Dissolved

type Company = {
    Id: CompanyId
    CorporateNumber: CorporateNumber
    LegalName: BilingualName
    EntityType: EntityType
    Capital: Money
    Status: CompanyStatus
    RepresentativeDirectorId: DirectorId option
    EstablishedAt: DateTimeOffset
}

type CompanyEvent =
    | Incorporated of IncorporatedEvent
    | CapitalIncreased of CapitalIncreasedEvent
    | RepresentativeChanged of RepresentativeChangedEvent
    | Dissolved of DissolvedEvent

// ============================================
// Pure Functions in Modules
// ============================================
module Company =

    let private minCapitalFor = function
        | KabushikiKaisha -> 1m  // Legal min ¥1, practical ¥10M
        | GodoKaisha -> 1m
        | GomeiKaisha -> 0m
        | GoshiKaisha -> 0m

    let incorporate (cmd: IncorporateCommand) : Result<Company * CompanyEvent, CompanyError> =
        CorporateNumber.create cmd.CorporateNumber
        |> Result.bind (fun corpNum ->
            if cmd.Capital.Amount < minCapitalFor cmd.EntityType then
                Error (InsufficientCapital (minCapitalFor cmd.EntityType))
            else
                let company = {
                    Id = CompanyId (Guid.NewGuid())
                    CorporateNumber = corpNum
                    LegalName = cmd.LegalName
                    EntityType = cmd.EntityType
                    Capital = cmd.Capital
                    Status = Active
                    RepresentativeDirectorId = None
                    EstablishedAt = DateTimeOffset.UtcNow
                }
                let event = Incorporated {
                    CompanyId = company.Id
                    CorporateNumber = corpNum
                    EntityType = cmd.EntityType
                    Timestamp = DateTimeOffset.UtcNow
                }
                Ok (company, event))

    let increaseCapital (amount: Money) (company: Company) : Result<Company * CompanyEvent, CompanyError> =
        let newCapital = { company.Capital with Amount = company.Capital.Amount + amount.Amount }
        let updated = { company with Capital = newCapital }
        let event = CapitalIncreased {
            CompanyId = company.Id
            Previous = company.Capital
            New = newCapital
            Timestamp = DateTimeOffset.UtcNow
        }
        Ok (updated, event)

    let canPayDividend (company: Company) (netAssets: Money) : bool =
        netAssets.Amount >= 3_000_000m  // ¥3M minimum per Companies Act

// ============================================
// Composition with Pipe Operator
// ============================================
let workflow =
    Company.incorporate {
        CorporateNumber = "1234567890123"
        LegalName = { Japanese = "合同会社サンプル"; English = None }
        EntityType = GodoKaisha
        Capital = { Amount = 5_000_000m; Currency = JPY }
    }
    |> Result.bind (fun (company, _) ->
        Company.increaseCapital { Amount = 5_000_000m; Currency = JPY } company)
```

---

## Option 3: Effect System (Monadic with Custom Effects)

**Characteristics**: Reader monad for dependencies, custom effect types, maximum composability.

```fsharp
// ============================================
// Effect Types
// ============================================
type CompanyEffect<'T> =
    | Pure of 'T
    | Bind of obj * (obj -> CompanyEffect<'T>)
    | GetTime of (DateTimeOffset -> CompanyEffect<'T>)
    | GenerateId of (Guid -> CompanyEffect<'T>)
    | LogEvent of CompanyEvent * (unit -> CompanyEffect<'T>)
    | RaiseError of CompanyError

module Effect =
    let pure' x = Pure x
    let bind f = function
        | Pure x -> f x
        | other -> Bind (box other, fun o -> f (unbox o))

    let getTime() = GetTime Pure
    let generateId() = GenerateId Pure
    let logEvent evt = LogEvent (evt, Pure)
    let fail err = RaiseError err

    let map f eff = bind (f >> pure') eff

// ============================================
// Effect Computation Expression
// ============================================
type EffectBuilder() =
    member _.Return(x) = Effect.pure' x
    member _.Bind(eff, f) = Effect.bind f eff
    member _.ReturnFrom(eff) = eff
    member _.Zero() = Effect.pure' ()

let effect = EffectBuilder()

// ============================================
// Domain Logic with Effects
// ============================================
module Company =

    let incorporate (cmd: IncorporateCommand) : CompanyEffect<Company> =
        effect {
            match CorporateNumber.create cmd.CorporateNumber with
            | Error e -> return! Effect.fail (InvalidCorporateNumber e)
            | Ok corpNum ->
                let! id = Effect.generateId()
                let! now = Effect.getTime()

                let company = {
                    Id = CompanyId id
                    CorporateNumber = corpNum
                    LegalName = cmd.LegalName
                    EntityType = cmd.EntityType
                    Capital = cmd.Capital
                    Status = Active
                    RepresentativeDirectorId = None
                    EstablishedAt = now
                }

                do! Effect.logEvent (Incorporated {
                    CompanyId = company.Id
                    CorporateNumber = corpNum
                    EntityType = cmd.EntityType
                    Timestamp = now
                })

                return company
        }

// ============================================
// Interpreter (Runs Effects)
// ============================================
let rec interpret (eventStore: IEventStore) (eff: CompanyEffect<'T>) : Result<'T, CompanyError> =
    match eff with
    | Pure x -> Ok x
    | GetTime next -> interpret eventStore (next DateTimeOffset.UtcNow)
    | GenerateId next -> interpret eventStore (next (Guid.NewGuid()))
    | LogEvent (evt, next) ->
        eventStore.Append(evt)
        interpret eventStore (next ())
    | RaiseError err -> Error err
    | Bind (eff', f) ->
        match interpret eventStore (unbox eff') with
        | Ok x -> interpret eventStore (f x)
        | Error e -> Error e
```

---

## Option 4: Tagless Final (Advanced Monadic)

**Characteristics**: Abstract over effect type, maximum testability, complex but powerful.

```fsharp
// ============================================
// Abstract Effect Interface
// ============================================
type ICompanyEffects<'F> =
    abstract member Pure: 'a -> 'F<'a>
    abstract member Bind: 'F<'a> -> ('a -> 'F<'b>) -> 'F<'b>
    abstract member GetTime: unit -> 'F<DateTimeOffset>
    abstract member GenerateId: unit -> 'F<Guid>
    abstract member SaveEvent: CompanyEvent -> 'F<unit>
    abstract member Fail: CompanyError -> 'F<'a>

// ============================================
// Domain Logic (Polymorphic over Effect)
// ============================================
module Company =
    let incorporate<'F> (E: ICompanyEffects<'F>) (cmd: IncorporateCommand) : 'F<Company> =
        E.Bind (E.GenerateId()) (fun id ->
        E.Bind (E.GetTime()) (fun now ->
            match CorporateNumber.create cmd.CorporateNumber with
            | Error e -> E.Fail (InvalidCorporateNumber e)
            | Ok corpNum ->
                let company = {
                    Id = CompanyId id
                    CorporateNumber = corpNum
                    LegalName = cmd.LegalName
                    EntityType = cmd.EntityType
                    Capital = cmd.Capital
                    Status = Active
                    RepresentativeDirectorId = None
                    EstablishedAt = now
                }
                E.Bind (E.SaveEvent (Incorporated { ... })) (fun () ->
                E.Pure company)))

// ============================================
// Production Interpreter
// ============================================
type ProductionEffects(store: IEventStore) =
    interface ICompanyEffects<Async> with
        member _.Pure x = async.Return x
        member _.Bind m f = async.Bind(m, f)
        member _.GetTime() = async.Return DateTimeOffset.UtcNow
        member _.GenerateId() = async.Return (Guid.NewGuid())
        member _.SaveEvent evt = async { do! store.AppendAsync(evt) }
        member _.Fail err = async { return raise (CompanyException err) }

// ============================================
// Test Interpreter (Pure, Deterministic)
// ============================================
type TestEffects(fixedTime: DateTimeOffset, fixedId: Guid) =
    let mutable events = []
    interface ICompanyEffects<Result<_, CompanyError>> with
        member _.Pure x = Ok x
        member _.Bind m f = Result.bind f m
        member _.GetTime() = Ok fixedTime
        member _.GenerateId() = Ok fixedId
        member _.SaveEvent evt = events <- evt :: events; Ok ()
        member _.Fail err = Error err
    member _.RecordedEvents = events |> List.rev
```

---

## Recommendation: Hybrid Approach

Based on your domain (legal compliance, auditing, complex business rules), I recommend:

### **OOP for Aggregates + Pure Functions for Logic + Result for Errors**

```fsharp
// Best of all worlds:
// 1. Classes encapsulate aggregate state and enforce invariants
// 2. Pure functions in modules for business logic
// 3. Result<'T, 'E> for explicit error handling
// 4. Computation expressions for clean composition
// 5. DUs for modeling domain choices

type Company private (state: CompanyState) =
    // Encapsulated state
    member _.State = state

    // Commands return Result with events
    member this.Execute(cmd: CompanyCommand) : Result<Company * CompanyEvent list, CompanyError> =
        match cmd with
        | IncreaseCapital amount -> CompanyLogic.increaseCapital amount state
        | AppointDirector dir -> CompanyLogic.appointDirector dir state
        | PayDividend amount -> CompanyLogic.payDividend amount state
        |> Result.map (fun (newState, events) -> (Company(newState), events))

// Pure business logic in module
module CompanyLogic =
    let increaseCapital amount state =
        // Pure validation and transformation
        ...

    let canPayDividend netAssets =
        netAssets.Amount >= 3_000_000m
```

---

## Summary Comparison

| Style | Complexity | Testability | Type Safety | Best For |
|-------|------------|-------------|-------------|----------|
| **OOP + Monadic** | Medium | High | High | Enterprise DDD, familiar to .NET devs |
| **Pure Functional** | Low | Very High | High | Simple domains, FP purists |
| **Effect System** | High | Very High | Very High | Complex side effects, event sourcing |
| **Tagless Final** | Very High | Maximum | Maximum | Library code, advanced teams |
| **Hybrid (Recommended)** | Medium | Very High | Very High | This project |

---

## Your Choice?

1. **OOP + Monadic** - Classes + Result + Computation Expressions
2. **Pure Functional** - Records + Modules + Pipes
3. **Effect System** - Custom effect types + Interpreters
4. **Tagless Final** - Maximum abstraction
5. **Hybrid** - Recommended blend (Classes for aggregates, modules for logic)

