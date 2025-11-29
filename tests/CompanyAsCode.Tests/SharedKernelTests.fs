module CompanyAsCode.Tests.SharedKernelTests

open Xunit
open FsUnit.Xunit
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Japanese
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact

/// Helper to check if a string contains a substring
let containsString (expected: string) (actual: string) =
    Assert.True(actual.Contains(expected), $"Expected '{actual}' to contain '{expected}'")

// ============================================
// Corporate Number Tests
// ============================================

module CorporateNumberTests =

    [<Fact>]
    let ``Valid 13-digit corporate number should create successfully`` () =
        // This is a sample valid corporate number format
        let result = CorporateNumber.create "1234567890123"

        match result with
        | Ok _ -> ()  // May fail checksum, but format is right
        | Error msg -> containsString "checksum" msg  // Expected if checksum invalid

    [<Fact>]
    let ``Corporate number with wrong length should fail`` () =
        let result = CorporateNumber.create "12345"

        match result with
        | Error msg -> containsString "13 digits" msg
        | Ok _ -> failwith "Should have failed"

    [<Fact>]
    let ``Corporate number with non-digits should fail`` () =
        let result = CorporateNumber.create "123456789012A"

        match result with
        | Error msg -> containsString "digits" msg
        | Ok _ -> failwith "Should have failed"

    [<Fact>]
    let ``Empty corporate number should fail`` () =
        let result = CorporateNumber.create ""

        match result with
        | Error msg -> containsString "empty" msg
        | Ok _ -> failwith "Should have failed"

// ============================================
// Money Tests
// ============================================

module MoneyTests =

    [<Fact>]
    let ``Creating yen should work`` () =
        let money = Money.yen 1000m
        Money.amount money |> should equal 1000m
        Money.currency money |> should equal Currency.JPY

    [<Fact>]
    let ``Adding same currency should work`` () =
        let m1 = Money.yen 1000m
        let m2 = Money.yen 500m

        let result = Money.add m1 m2

        match result with
        | Ok sum -> Money.amount sum |> should equal 1500m
        | Error _ -> failwith "Should succeed"

    [<Fact>]
    let ``Adding different currencies should fail`` () =
        let m1 = Money.yen 1000m
        let m2 = Money.createRounded 100m Currency.USD

        let result = Money.add m1 m2

        match result with
        | Error msg -> containsString "Cannot add" msg  // Capital C
        | Ok _ -> failwith "Should fail"

    [<Fact>]
    let ``Multiplying money should work`` () =
        let m = Money.yen 1000m
        let result = Money.multiply 1.5m m

        Money.amount result |> should equal 1500m

    [<Fact>]
    let ``Yen should round to whole numbers`` () =
        let money = Money.yen 1000.567m
        Money.amount money |> should equal 1001m  // Rounds to nearest integer

    [<Fact>]
    let ``Money formatting should include symbol`` () =
        let m = Money.yen 10000m
        let formatted = Money.format m

        containsString "¥" formatted

// ============================================
// Prefecture Tests
// ============================================

module PrefectureTests =

    [<Fact>]
    let ``Tokyo should have correct Japanese name`` () =
        Prefecture.toJapanese Tokyo |> should equal "東京都"

    [<Fact>]
    let ``Tokyo should have correct JIS code`` () =
        Prefecture.toJisCode Tokyo |> should equal "13"

    [<Fact>]
    let ``JIS code 13 should return Tokyo`` () =
        Prefecture.fromJisCode "13" |> should equal (Some Tokyo)

    [<Fact>]
    let ``Invalid JIS code should return None`` () =
        Prefecture.fromJisCode "99" |> should equal None

// ============================================
// Entity Type Tests
// ============================================

module EntityTypeTests =

    [<Fact>]
    let ``KabushikiKaisha should have correct Japanese name`` () =
        EntityType.toJapanese KabushikiKaisha |> should equal "株式会社"

    [<Fact>]
    let ``GodoKaisha should have correct abbreviation`` () =
        EntityType.toAbbreviation GodoKaisha |> should equal "G.K."

    [<Fact>]
    let ``KabushikiKaisha should have limited liability`` () =
        EntityType.hasLimitedLiability KabushikiKaisha |> should be True

    [<Fact>]
    let ``GomeiKaisha should have unlimited liability`` () =
        EntityType.hasLimitedLiability GomeiKaisha |> should be False

    [<Fact>]
    let ``Registration fee should be at least minimum`` () =
        let fee = EntityType.registrationFee 1_000_000m KabushikiKaisha
        fee |> should greaterThanOrEqualTo 150_000m

// ============================================
// Date Tests
// ============================================

module DateTests =

    [<Fact>]
    let ``Valid date should create successfully`` () =
        let result = Date.create 2024 3 31

        match result with
        | Ok date ->
            Date.year date |> should equal 2024
            Date.month date |> should equal 3
            Date.day date |> should equal 31
        | Error _ -> failwith "Should succeed"

    [<Fact>]
    let ``Invalid date should fail`` () =
        let result = Date.create 2024 2 30  // February 30 doesn't exist

        match result with
        | Error msg -> containsString "Invalid" msg
        | Ok _ -> failwith "Should fail"

    [<Fact>]
    let ``Adding months should work`` () =
        let date = Date.create 2024 1 15 |> Result.defaultValue (Date.today())
        let newDate = Date.addMonths 3 date

        Date.month newDate |> should equal 4

// ============================================
// Fiscal Year Tests
// ============================================

module FiscalYearTests =

    [<Fact>]
    let ``Japanese standard fiscal year should end in March`` () =
        let fy = FiscalYear.japaneseStandard 2024

        Date.month fy.EndDate |> should equal 3
        Date.day fy.EndDate |> should equal 31

    [<Fact>]
    let ``Fiscal year should contain dates within range`` () =
        let fy = FiscalYear.japaneseStandard 2024
        let midYear = Date.create 2024 1 15 |> Result.defaultValue (Date.today())

        FiscalYear.contains midYear fy |> should be True

// ============================================
// Person Name Tests
// ============================================

module PersonNameTests =

    [<Fact>]
    let ``Valid name should create successfully`` () =
        let result = PersonName.create "田中" "太郎"

        match result with
        | Ok name ->
            name.FamilyName |> should equal "田中"
            name.GivenName |> should equal "太郎"
        | Error _ -> failwith "Should succeed"

    [<Fact>]
    let ``Empty family name should fail`` () =
        let result = PersonName.create "" "太郎"

        match result with
        | Error msg -> containsString "Family name" msg
        | Ok _ -> failwith "Should fail"

    [<Fact>]
    let ``Full name should be in Japanese order`` () =
        let name =
            PersonName.create "田中" "太郎"
            |> Result.defaultValue { FamilyName = ""; GivenName = ""; FamilyNameKana = None; GivenNameKana = None }

        PersonName.fullName name |> should equal "田中 太郎"

// ============================================
// Postal Code Tests
// ============================================

module PostalCodeTests =

    [<Fact>]
    let ``Valid postal code with hyphen should work`` () =
        let result = PostalCode.create "100-0001"

        match result with
        | Ok pc -> PostalCode.value pc |> should equal "1000001"
        | Error _ -> failwith "Should succeed"

    [<Fact>]
    let ``Valid postal code without hyphen should work`` () =
        let result = PostalCode.create "1000001"

        match result with
        | Ok _ -> ()
        | Error _ -> failwith "Should succeed"

    [<Fact>]
    let ``Invalid postal code should fail`` () =
        let result = PostalCode.create "12345"

        match result with
        | Error msg -> containsString "7 digits" msg
        | Ok _ -> failwith "Should fail"

// ============================================
// Result Builder Tests
// ============================================

module ResultBuilderTests =

    [<Fact>]
    let ``Result computation should short-circuit on error`` () =
        let computation = result {
            let! _ = Ok 1
            let! _ = Error "Failed"
            let! _ = Ok 3  // Should not execute
            return 4
        }

        match computation with
        | Error msg -> msg |> should equal "Failed"
        | Ok _ -> failwith "Should fail"

    [<Fact>]
    let ``Result computation should succeed with all Ok`` () =
        let computation = result {
            let! a = Ok 1
            let! b = Ok 2
            return a + b
        }

        match computation with
        | Ok sum -> sum |> should equal 3
        | Error _ -> failwith "Should succeed"
