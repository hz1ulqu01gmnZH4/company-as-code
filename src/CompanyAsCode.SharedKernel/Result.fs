namespace CompanyAsCode.SharedKernel

/// Result computation expression for railway-oriented programming
[<AutoOpen>]
module ResultBuilder =

    /// Computation expression builder for Result<'T, 'E>
    type ResultBuilder() =
        member _.Return(x) = Ok x
        member _.ReturnFrom(x: Result<'a, 'e>) = x
        member _.Bind(x: Result<'a, 'e>, f: 'a -> Result<'b, 'e>) = Result.bind f x
        member _.Zero() = Ok ()

        member _.Combine(a: Result<unit, 'e>, b: Result<'b, 'e>) =
            match a with
            | Ok () -> b
            | Error e -> Error e

        member _.Delay(f: unit -> Result<'a, 'e>) = f
        member _.Run(f: unit -> Result<'a, 'e>) = f()

        member this.While(guard: unit -> bool, body: unit -> Result<unit, 'e>) =
            if not (guard()) then
                Ok ()
            else
                match body() with
                | Ok () -> this.While(guard, body)
                | Error e -> Error e

        member this.For(sequence: seq<'a>, body: 'a -> Result<unit, 'e>) =
            use enumerator = sequence.GetEnumerator()
            this.While(
                enumerator.MoveNext,
                fun () -> body enumerator.Current)

        member _.TryWith(body: unit -> Result<'a, 'e>, handler: exn -> Result<'a, 'e>) =
            try body()
            with ex -> handler ex

        member _.TryFinally(body: unit -> Result<'a, 'e>, compensation: unit -> unit) =
            try body()
            finally compensation()

        member this.Using(resource: 'a :> System.IDisposable, body: 'a -> Result<'b, 'e>) =
            this.TryFinally(
                (fun () -> body resource),
                (fun () -> if not (obj.ReferenceEquals(resource, null)) then resource.Dispose()))

    /// Global result computation expression instance
    let result = ResultBuilder()

/// Extensions for Result type
[<RequireQualifiedAccess>]
module Result =

    /// Apply a function wrapped in Result to a value wrapped in Result
    let apply (fResult: Result<'a -> 'b, 'e>) (xResult: Result<'a, 'e>) : Result<'b, 'e> =
        match fResult, xResult with
        | Ok f, Ok x -> Ok (f x)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Map over the error type
    let mapError (f: 'e1 -> 'e2) (result: Result<'a, 'e1>) : Result<'a, 'e2> =
        match result with
        | Ok x -> Ok x
        | Error e -> Error (f e)

    /// Combine two results, returning the first error if any
    let combine (r1: Result<'a, 'e>) (r2: Result<'b, 'e>) : Result<'a * 'b, 'e> =
        match r1, r2 with
        | Ok a, Ok b -> Ok (a, b)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Sequence a list of Results into a Result of list
    let sequence (results: Result<'a, 'e> list) : Result<'a list, 'e> =
        let folder acc item =
            match acc, item with
            | Ok list, Ok x -> Ok (x :: list)
            | Error e, _ -> Error e
            | _, Error e -> Error e
        results |> List.fold folder (Ok []) |> Result.map List.rev

    /// Traverse a list with a function returning Result
    let traverse (f: 'a -> Result<'b, 'e>) (list: 'a list) : Result<'b list, 'e> =
        list |> List.map f |> sequence

    /// Convert Option to Result with specified error
    let ofOption (error: 'e) (option: 'a option) : Result<'a, 'e> =
        match option with
        | Some x -> Ok x
        | None -> Error error

    /// Convert Result to Option, discarding error
    let toOption (result: Result<'a, 'e>) : 'a option =
        match result with
        | Ok x -> Some x
        | Error _ -> None

    /// Require a condition to be true, or return error
    let require (condition: bool) (error: 'e) : Result<unit, 'e> =
        if condition then Ok () else Error error

    /// Ensure a condition on the value, or return error
    let ensure (predicate: 'a -> bool) (error: 'e) (value: 'a) : Result<'a, 'e> =
        if predicate value then Ok value else Error error

    /// Tap into a successful result for side effects
    let tap (f: 'a -> unit) (result: Result<'a, 'e>) : Result<'a, 'e> =
        match result with
        | Ok x -> f x; Ok x
        | Error e -> Error e

    /// Tap into an error result for side effects
    let tapError (f: 'e -> unit) (result: Result<'a, 'e>) : Result<'a, 'e> =
        match result with
        | Ok x -> Ok x
        | Error e -> f e; Error e

/// Validation result that accumulates errors
[<RequireQualifiedAccess>]
module Validation =

    /// Validation type that accumulates errors
    type Validation<'a, 'e> = Result<'a, 'e list>

    /// Create a success validation
    let success (x: 'a) : Validation<'a, 'e> = Ok x

    /// Create a failure validation
    let failure (e: 'e) : Validation<'a, 'e> = Error [e]

    /// Apply a function wrapped in Validation to a value wrapped in Validation
    /// Accumulates errors from both sides
    let apply (fVal: Validation<'a -> 'b, 'e>) (xVal: Validation<'a, 'e>) : Validation<'b, 'e> =
        match fVal, xVal with
        | Ok f, Ok x -> Ok (f x)
        | Error e1, Error e2 -> Error (e1 @ e2)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Map over validation
    let map (f: 'a -> 'b) (v: Validation<'a, 'e>) : Validation<'b, 'e> =
        Result.map f v

    /// Combine two validations, accumulating errors
    let combine (v1: Validation<'a, 'e>) (v2: Validation<'b, 'e>) : Validation<'a * 'b, 'e> =
        match v1, v2 with
        | Ok a, Ok b -> Ok (a, b)
        | Error e1, Error e2 -> Error (e1 @ e2)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Convert single-error Result to Validation
    let ofResult (r: Result<'a, 'e>) : Validation<'a, 'e> =
        Result.mapError List.singleton r

    /// Convert Validation to single-error Result (takes first error)
    let toResult (v: Validation<'a, 'e>) : Result<'a, 'e> =
        Result.mapError List.head v

/// Operators for Result and Validation
[<AutoOpen>]
module Operators =

    /// Infix bind operator for Result
    let (>>=) result f = Result.bind f result

    /// Infix map operator for Result
    let (<!>) f result = Result.map f result

    /// Infix apply operator for Result
    let (<*>) fResult xResult = Result.apply fResult xResult

    /// Kleisli composition (compose two Result-returning functions)
    let (>=>) f g x = f x >>= g
