namespace AcmeInc

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks

/// Ollama API request/response types
module OllamaTypes =

    type OllamaRequest = {
        model: string
        prompt: string
        stream: bool
    }

    type OllamaResponse = {
        model: string
        response: string
        ``done``: bool
    }

/// LLM Driver for interacting with company operations via Ollama
module OllamaDriver =

    open OllamaTypes

    /// Configuration for the Ollama driver
    type OllamaConfig = {
        BaseUrl: string
        Model: string
        TimeoutSeconds: int
    }

    module OllamaConfig =
        let defaultConfig = {
            BaseUrl = "http://localhost:11434"
            Model = "gemma3:27b"
            TimeoutSeconds = 120
        }

        let withModel model config = { config with Model = model }
        let withUrl url config = { config with BaseUrl = url }
        let withTimeout seconds config = { config with TimeoutSeconds = seconds }

    /// Available company operations
    type CompanyOperation =
        | CreateCompany of name: string * capital: decimal
        | AppointDirector of name: string * position: string
        | RemoveDirector of name: string
        | HireEmployee of name: string * position: string * salary: decimal
        | TerminateEmployee of name: string
        | CreateInvoice of customer: string * amount: decimal
        | RecordPayment of invoiceId: string * amount: decimal
        | GetCompanyStatus
        | GetFinancialSummary
        | GetEmployeeList
        | GetDirectorList
        | Help
        | Unknown of input: string

    /// Parse natural language input into company operations
    let parseOperation (input: string) : CompanyOperation =
        let lower = input.ToLowerInvariant().Trim()

        // Simple pattern matching - LLM will enhance this
        if lower.Contains("help") || lower.Contains("?") then
            Help
        elif lower.Contains("status") || lower.Contains("overview") then
            GetCompanyStatus
        elif lower.Contains("financial") || lower.Contains("money") || lower.Contains("balance") then
            GetFinancialSummary
        elif lower.Contains("employee") && lower.Contains("list") then
            GetEmployeeList
        elif lower.Contains("director") && lower.Contains("list") then
            GetDirectorList
        else
            Unknown input

    /// System prompt for the LLM to understand company operations
    let systemPrompt = """You are an AI assistant helping manage a Japanese company (株式会社) called ACME Inc.
You can help with:
- Corporate governance (directors, board meetings, shareholders)
- HR management (employees, contracts, payroll)
- Financial operations (invoices, payments, accounting)
- Compliance and regulatory filings

When the user asks to perform an action, respond with a JSON object in this format:
{
  "action": "action_name",
  "params": { ... },
  "explanation": "Brief explanation of what will happen"
}

Available actions:
- create_company: params { name, capital_yen }
- appoint_director: params { name, position, is_representative }
- remove_director: params { name }
- hire_employee: params { name, position, department, salary_yen }
- terminate_employee: params { employee_id }
- create_invoice: params { customer, amount_yen, description }
- record_payment: params { invoice_id, amount_yen }
- get_status: no params
- get_financials: no params
- list_employees: no params
- list_directors: no params

If the user's request is unclear, ask for clarification.
If the request cannot be fulfilled, explain why based on Japanese Companies Act requirements."""

    /// Driver state
    type DriverState = {
        Config: OllamaConfig
        ConversationHistory: (string * string) list
        LastError: string option
    }

    module DriverState =
        let create config = {
            Config = config
            ConversationHistory = []
            LastError = None
        }

        let addExchange user assistant state = {
            state with ConversationHistory = (user, assistant) :: state.ConversationHistory
        }

        let setError error state = { state with LastError = Some error }
        let clearError state = { state with LastError = None }

    /// Send a prompt to Ollama and get a response
    let queryOllama (config: OllamaConfig) (prompt: string) : Async<Result<string, string>> =
        async {
            use client = new HttpClient()
            client.Timeout <- TimeSpan.FromSeconds(float config.TimeoutSeconds)

            let request = {
                model = config.Model
                prompt = $"{systemPrompt}\n\nUser: {prompt}"
                stream = false
            }

            let json = JsonSerializer.Serialize(request)
            let content = new StringContent(json, Encoding.UTF8, "application/json")

            try
                let! response = client.PostAsync($"{config.BaseUrl}/api/generate", content) |> Async.AwaitTask

                if response.IsSuccessStatusCode then
                    let! body = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    let result = JsonSerializer.Deserialize<OllamaResponse>(body)
                    return Ok result.response
                else
                    let! error = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    return Error $"Ollama API error: {response.StatusCode} - {error}"
            with
            | :? HttpRequestException as ex ->
                return Error $"Connection error: {ex.Message}. Is Ollama running at {config.BaseUrl}?"
            | :? TaskCanceledException ->
                return Error $"Request timed out after {config.TimeoutSeconds} seconds"
            | ex ->
                return Error $"Unexpected error: {ex.Message}"
        }

    /// Interactive REPL for company management
    type CompanyDriver(config: OllamaConfig) =
        let mutable state = DriverState.create config

        member _.Config = config
        member _.State = state

        /// Process a user command
        member this.ProcessCommand(input: string) : Async<Result<string, string>> =
            async {
                let! result = queryOllama config input
                match result with
                | Ok response ->
                    state <- DriverState.addExchange input response state |> DriverState.clearError
                    return Ok response
                | Error err ->
                    state <- DriverState.setError err state
                    return Error err
            }

        /// Get conversation history
        member _.GetHistory() = state.ConversationHistory |> List.rev

        /// Clear conversation history
        member _.ClearHistory() =
            state <- { state with ConversationHistory = [] }

        /// Check if Ollama is available
        member _.CheckConnection() : Async<Result<string, string>> =
            async {
                use client = new HttpClient()
                client.Timeout <- TimeSpan.FromSeconds(5.0)
                try
                    let! response = client.GetAsync($"{config.BaseUrl}/api/tags") |> Async.AwaitTask
                    if response.IsSuccessStatusCode then
                        return Ok $"Connected to Ollama at {config.BaseUrl}"
                    else
                        return Error $"Ollama returned status {response.StatusCode}"
                with
                | ex -> return Error $"Cannot connect to Ollama: {ex.Message}"
            }

    /// Create a driver with default configuration
    let createDriver () = CompanyDriver(OllamaConfig.defaultConfig)

    /// Create a driver with custom model
    let createDriverWithModel model =
        CompanyDriver(OllamaConfig.defaultConfig |> OllamaConfig.withModel model)

    /// Create a driver with full custom config
    let createDriverWithConfig config = CompanyDriver(config)
