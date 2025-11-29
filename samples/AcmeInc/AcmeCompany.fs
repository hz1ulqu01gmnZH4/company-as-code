namespace AcmeInc

open System
open CompanyAsCode.SharedKernel
open CompanyAsCode.SharedKernel.Financial
open CompanyAsCode.SharedKernel.Temporal
open CompanyAsCode.SharedKernel.Contact
open CompanyAsCode.SharedKernel.Japanese
open CompanyAsCode.Legal.Company
open CompanyAsCode.Legal.Director
open CompanyAsCode.Legal.Events
open CompanyAsCode.Legal.CompanyFactory

/// ACME Inc - Sample Japanese Company Implementation
module AcmeCompany =

    /// ACME company state holder
    type AcmeState = {
        Company: Company option
        Directors: Director list
        Events: LegalEvent list
    }

    module AcmeState =
        let empty = {
            Company = None
            Directors = []
            Events = []
        }

        let addEvent event state = { state with Events = event :: state.Events }
        let setCompany company state = { state with Company = Some company }
        let addDirector director state = { state with Directors = director :: state.Directors }

    /// ACME Inc management operations
    type AcmeManager() =
        let mutable state = AcmeState.empty

        /// Initialize ACME Inc as a Japanese KK (株式会社)
        member _.Initialize() =
            // Create Tokyo address for headquarters
            let postalCode =
                PostalCode.create "100-0001"
                |> Result.defaultValue (Unchecked.defaultof<PostalCode>)

            let hqAddress =
                Address.create postalCode Tokyo "Chiyoda-ku" "Marunouchi 1-1-1"
                |> Result.map (Address.withBuilding "ACME Building 10F")
                |> Result.defaultValue (Unchecked.defaultof<Address>)

            // Use the factory to create the company
            let result =
                QuickIncorporate.withBilingualName
                    "1234567890123"  // Corporate number
                    "アクメ株式会社"
                    "ACME Inc."
                    KabushikiKaisha
                    10_000_000m  // Initial capital: 10 million yen
                    hqAddress

            match result with
            | Ok (company, event) ->
                state <- state |> AcmeState.setCompany company |> AcmeState.addEvent event
                Ok $"ACME Inc. (アクメ株式会社) incorporated with capital ¥{company.CapitalAmount:N0}"
            | Error err ->
                Error $"Failed to create company: {err}"

        /// Appoint a director
        member _.AppointDirector(name: string, nameJapanese: string, isRepresentative: bool) =
            match state.Company with
            | None -> Error "Company not initialized"
            | Some company ->
                let personName: PersonName = {
                    FamilyName = name.Split(' ') |> Array.tryLast |> Option.defaultValue name
                    GivenName = name.Split(' ') |> Array.tryHead |> Option.defaultValue ""
                    FamilyNameKana = None
                    GivenNameKana = None
                }

                let position = if isRepresentative then DirectorPosition.President else DirectorPosition.Director

                let result =
                    Director.CreateInsideDirector
                        personName
                        position
                        2  // 2 year term
                        (Date.today())

                match result with
                | Ok director ->
                    let director =
                        if isRepresentative then
                            match director.DesignateAsRepresentative() with
                            | Ok d -> d
                            | Error _ -> director
                        else
                            director
                    state <- state |> AcmeState.addDirector director
                    let positionStr = if isRepresentative then "Representative Director (代表取締役)" else "Director (取締役)"
                    Ok $"Appointed {name} ({nameJapanese}) as {positionStr}"
                | Error err ->
                    Error $"Failed to appoint director: {err}"

        /// Get company status
        member _.GetStatus() =
            match state.Company with
            | None -> "Company not initialized. Call Initialize() first."
            | Some company ->
                let directorCount = state.Directors.Length
                let eventCount = state.Events.Length
                let japaneseName = company.LegalName.Japanese
                let englishName = company.LegalName.English |> Option.defaultValue "N/A"
                $"""
ACME Inc. (アクメ株式会社) Status
================================
Trade Name: {englishName}
Japanese Name: {japaneseName}
Type: 株式会社 (Kabushiki Kaisha)
Status: {company.Status}
Capital: ¥{company.CapitalAmount:N0}
Directors: {directorCount}
Fiscal Year End: {company.FiscalYearEnd}
Events Recorded: {eventCount}
"""

        /// Get director list
        member _.GetDirectors() =
            if state.Directors.IsEmpty then
                "No directors appointed yet."
            else
                let directorLines =
                    state.Directors
                    |> List.mapi (fun i d ->
                        let pos = match d.Position with
                                  | DirectorPosition.President -> "President"
                                  | DirectorPosition.Director -> "Director"
                                  | DirectorPosition.Chairman -> "Chairman"
                                  | DirectorPosition.VicePresident -> "Vice President"
                                  | DirectorPosition.SeniorManagingDirector -> "Senior Managing Director"
                                  | DirectorPosition.ManagingDirector -> "Managing Director"
                                  | DirectorPosition.OutsideDirector -> "Outside Director"
                        let repStr = if d.IsRepresentative then " (Representative)" else ""
                        $"{i+1}. {d.Name.FamilyName} {d.Name.GivenName} - {pos}{repStr}")
                    |> String.concat "\n"
                $"Directors:\n{directorLines}"

        /// Get event history
        member _.GetEvents() =
            if state.Events.IsEmpty then
                "No events recorded."
            else
                state.Events
                |> List.rev
                |> List.mapi (fun i e ->
                    let desc = match e with
                               | CompanyIncorporated _ -> "Company Incorporated"
                               | DirectorAppointed evt -> $"Director Appointed: {evt.Name.FamilyName}"
                               | CapitalIncreased _ -> "Capital Increased"
                               | _ -> "Other Event"
                    $"{i+1}. {desc}")
                |> String.concat "\n"

        /// Current state
        member _.State = state

    /// Create and initialize ACME Inc with default setup
    let createDefault () =
        let manager = AcmeManager()

        // Initialize company
        manager.Initialize() |> ignore

        // Appoint initial directors
        manager.AppointDirector("Taro Yamada", "山田太郎", true) |> ignore
        manager.AppointDirector("Hanako Suzuki", "鈴木花子", false) |> ignore
        manager.AppointDirector("John Smith", "ジョン・スミス", false) |> ignore

        manager
