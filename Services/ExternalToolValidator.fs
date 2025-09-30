namespace ZapEditor.Services

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks

type ToolDependency =
    { Name: string
      ExecutableName: string
      VersionArgument: string
      VersionPattern: string
      Required: bool
      Description: string }

type ToolValidationResult =
    { Tool: ToolDependency
      IsAvailable: bool
      Version: string option
      ExecutablePath: string option
      Error: string option }

type IExternalToolValidator =
    abstract member ValidateTool: ToolDependency -> Task<ToolValidationResult>
    abstract member ValidateAllTools: ToolDependency list -> Task<ToolValidationResult list>

type ExternalToolValidator() =
    interface IExternalToolValidator with
        member this.ValidateTool(tool: ToolDependency) : Task<ToolValidationResult> =
            task {
                try
                    let processStartInfo = ProcessStartInfo(
                        FileName = tool.ExecutableName,
                        Arguments = tool.VersionArgument,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    )

                    use process = new Process()
                    process.StartInfo <- processStartInfo

                    try
                        let started = process.Start()
                        if not started then
                            return { Tool = tool; IsAvailable = false; Version = None; ExecutablePath = None; Error = Some $"Failed to start {tool.ExecutableName}" }
                        else
                            let! output = process.StandardOutput.ReadToEndAsync()
                            let! error = process.StandardError.ReadToEndAsync()
                            do! process.WaitForExitAsync()

                            if process.ExitCode = 0 then
                                let version = this.ExtractVersion(output, tool.VersionPattern)
                                let fullPath = this.GetExecutablePath(tool.ExecutableName)
                                return { Tool = tool; IsAvailable = true; Version = version; ExecutablePath = fullPath; Error = None }
                            else
                                return { Tool = tool; IsAvailable = false; Version = None; ExecutablePath = None; Error = Some $"Process failed with exit code {process.ExitCode}: {error}" }
                    with
                    | ex ->
                        return { Tool = tool; IsAvailable = false; Version = None; ExecutablePath = None; Error = Some ex.Message }
                with
                | ex ->
                    return { Tool = tool; IsAvailable = false; Version = None; ExecutablePath = None; Error = Some ex.Message }
            }

        member this.ValidateAllTools(tools: ToolDependency list) : Task<ToolValidationResult list> =
            task {
                let! results =
                    tools
                    |> List.map (this.ValidateTool >> Async.AwaitTask)
                    |> Task.WhenAll

                return List.ofArray results
            }

    member private this.ExtractVersion(output: string, pattern: string) : string option =
        try
            let regex = System.Text.RegularExpressions.Regex(pattern)
            let match' = regex.Match(output)
            if match'.Success then
                Some match'.Value
            else
                Some output.Trim()
        with
        | _ -> Some output.Trim()

    member private this.GetExecutablePath(executableName: string) : string option =
        try
            let processStartInfo = ProcessStartInfo(
                FileName = if Environment.OSVersion.Platform = PlatformID.Win32NT then "where" else "which",
                Arguments = executableName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            )

            use process = new Process()
            process.StartInfo <- processStartInfo

            if process.Start() then
                let output = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                if process.ExitCode = 0 then
                    Some output.Trim()
                else
                    None
            else
                None
        with
        | _ -> None

    // 定義済みツール依存性
    static member DefaultToolDependencies =
        [ { Name = "Python"
            ExecutableName = "python3"
            VersionArgument = "--version"
            VersionPattern = @"Python \d+\.\d+\.\d+"
            Required = false
            Description = "Python 3.x for Python code execution" }
          { Name = "Node.js"
            ExecutableName = "node"
            VersionArgument = "--version"
            VersionPattern = @"v\d+\.\d+\.\d+"
            Required = false
            Description = "Node.js for JavaScript code execution" }
          { Name = ".NET SDK"
            ExecutableName = "dotnet"
            VersionArgument = "--version"
            VersionPattern = @"\d+\.\d+\.\d+"
            Required = true
            Description = ".NET SDK for F# and C# code execution" } ]

module ToolValidationHelper =
    let GetToolStatusMessage(result: ToolValidationResult) =
        if result.IsAvailable then
            match result.Version with
            | Some version -> $"✅ {result.Tool.Name} v{version} - Available"
            | None -> $"✅ {result.Tool.Name} - Available (version unknown)"
        else
            match result.Error with
            | Some error -> $"❌ {result.Tool.Name} - Not available: {error}"
            | None -> $"❌ {result.Tool.Name} - Not available"

    let ValidateRequiredToolsOnly(tools: ToolDependency list, validator: IExternalToolValidator) : Task<bool> =
        task {
            let requiredTools = tools |> List.filter (fun t -> t.Required)
            let! results = validator.ValidateAllTools(requiredTools)
            return results |> List.forall (fun r -> r.IsAvailable)
        }