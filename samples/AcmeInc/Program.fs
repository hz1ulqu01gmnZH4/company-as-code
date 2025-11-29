open System
open AcmeInc
open AcmeInc.OllamaDriver
open AcmeInc.AcmeCompany

let printHeader () =
    printfn ""
    printfn "╔══════════════════════════════════════════════════════════════╗"
    printfn "║           ACME Inc. (アクメ株式会社) Management              ║"
    printfn "║                  Powered by Ollama LLM                       ║"
    printfn "╠══════════════════════════════════════════════════════════════╣"
    printfn "║  Commands:                                                   ║"
    printfn "║    /status   - Show company status                          ║"
    printfn "║    /directors - List all directors                          ║"
    printfn "║    /events   - Show event history                           ║"
    printfn "║    /model    - Change LLM model                             ║"
    printfn "║    /help     - Show this help                               ║"
    printfn "║    /quit     - Exit                                         ║"
    printfn "║                                                              ║"
    printfn "║  Or ask anything in natural language!                       ║"
    printfn "╚══════════════════════════════════════════════════════════════╝"
    printfn ""

let printStatus (acme: AcmeManager) =
    printfn "%s" (acme.GetStatus())

let printDirectors (acme: AcmeManager) =
    printfn "%s" (acme.GetDirectors())

let printEvents (acme: AcmeManager) =
    printfn "%s" (acme.GetEvents())

let rec repl (driver: CompanyDriver) (acme: AcmeManager) =
    printf "\n[ACME] > "
    let input = Console.ReadLine()

    if String.IsNullOrWhiteSpace(input) then
        repl driver acme
    else
        match input.Trim().ToLowerInvariant() with
        | "/quit" | "/exit" | "/q" ->
            printfn "Goodbye! お疲れ様でした!"
        | "/help" | "/h" ->
            printHeader ()
            repl driver acme
        | "/status" | "/s" ->
            printStatus acme
            repl driver acme
        | "/directors" | "/d" ->
            printDirectors acme
            repl driver acme
        | "/events" | "/e" ->
            printEvents acme
            repl driver acme
        | "/model" | "/m" ->
            printfn "Current model: %s" driver.Config.Model
            printf "Enter new model name (or press Enter to keep current): "
            let newModel = Console.ReadLine()
            if not (String.IsNullOrWhiteSpace(newModel)) then
                let newDriver = createDriverWithModel (newModel.Trim())
                printfn "Switched to model: %s" newModel
                repl newDriver acme
            else
                repl driver acme
        | "/clear" ->
            driver.ClearHistory()
            printfn "Conversation history cleared."
            repl driver acme
        | _ ->
            // Send to LLM
            printfn "Thinking..."
            let result = driver.ProcessCommand(input) |> Async.RunSynchronously
            match result with
            | Ok response ->
                printfn "\n%s" response
            | Error err ->
                printfn "\n[Error] %s" err
            repl driver acme

[<EntryPoint>]
let main argv =
    // Parse command line args
    let model =
        match Array.tryFindIndex ((=) "--model") argv with
        | Some i when i + 1 < argv.Length -> argv.[i + 1]
        | _ -> "gemma3:27b"  // Default model

    let url =
        match Array.tryFindIndex ((=) "--url") argv with
        | Some i when i + 1 < argv.Length -> argv.[i + 1]
        | _ -> "http://localhost:11434"

    printHeader ()

    // Initialize ACME Inc
    printfn "Initializing ACME Inc. (アクメ株式会社)..."
    let acme = createDefault ()
    printStatus acme

    // Create Ollama driver
    let config = {
        OllamaConfig.defaultConfig with
            Model = model
            BaseUrl = url
    }
    let driver = createDriverWithConfig config

    printfn "LLM Model: %s" model
    printfn "Ollama URL: %s" url

    // Check Ollama connection
    printfn "\nChecking Ollama connection..."
    let connectionResult = driver.CheckConnection() |> Async.RunSynchronously
    match connectionResult with
    | Ok msg -> printfn "%s" msg
    | Error err ->
        printfn "[Warning] %s" err
        printfn "You can still use local commands (/status, /directors, etc.)"

    printfn "\nReady! Type your command or ask a question.\n"

    // Start REPL
    repl driver acme

    0
