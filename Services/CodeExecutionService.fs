namespace ZapEditor.Services

open System
open System.Diagnostics
open System.IO
open System.Security
open System.Threading.Tasks

type CodeExecutionResult =
    { Success: bool
      Output: string
      Error: string
      ExitCode: int }

type SecurityConfig = {
    AllowedPaths: string list
    AllowedDirectories: string list
    MaxCodeSize: int
    TempDir: string option
}

type CodeExecutionService(?securityConfig: SecurityConfig) =

    static let defaultConfig = {
        AllowedPaths = [
            "/usr/bin"
            "/usr/local/bin"
            "/opt/homebrew/bin"
        ]
        AllowedDirectories = [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools")
        ]
        MaxCodeSize = 50000
        TempDir = None
    }

    let securityConfig = defaultArg securityConfig defaultConfig

    static member private ValidateExecutablePath(executable: string, config: SecurityConfig) =
        try
            let path =
                if Path.IsPathRooted(executable) then
                    executable
                else
                    match Environment.GetEnvironmentVariable("PATH") with
                    | null -> executable
                    | pathEnv ->
                        pathEnv.Split(Path.PathSeparator)
                        |> Array.tryFind (fun dir ->
                            let fullPath = Path.Combine(dir, executable)
                            File.Exists(fullPath))
                        |> function
                            | Some dir -> Path.Combine(dir, executable)
                            | None -> executable

            if not (File.Exists(path)) then
                Error (ExecutableNotFound executable)
            else
                let fullPath = Path.GetFullPath(path)

                // Check if executable is in allowed paths
                let isInAllowedPaths = config.AllowedPaths |> List.exists (fun allowedPath -> fullPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))

                // Check if executable is in allowed directories
                let isInAllowedDirectories = config.AllowedDirectories |> List.exists (fun allowedDir -> fullPath.StartsWith(allowedDir, StringComparison.OrdinalIgnoreCase))

                if isInAllowedPaths || isInAllowedDirectories then
                    Ok path
                else
                    Error (UnsafeExecutionPath fullPath)
        with
        | :? SecurityException -> Error (AccessDenied executable)
        | ex -> Error (ValidationFailed ex.Message)

    member private this.CreateSecureTempFile(extension: string, content: string) =
        let tempDir = defaultArg securityConfig.TempDir (Path.GetTempPath())
        let fileName = Guid.NewGuid().ToString("N") + extension
        let tempFile = Path.Combine(tempDir, fileName)

        try
            File.WriteAllText(tempFile, content)
            tempFile
        with
        | ex ->
            if File.Exists(tempFile) then
                try File.Delete(tempFile) with | _ -> ()
            reraise()

    member private this.ExecuteProcessSafely(executable: string, arguments: string, ?workingDirectory: string) =
        task {
            match CodeExecutionService.ValidateExecutablePath(executable, securityConfig) with
            | Error err -> return { Success = false; Output = ""; Error = this.GetErrorMessage(err); ExitCode = -1 }
            | Ok validatedPath ->
                let startInfo = ProcessStartInfo(
                    FileName = validatedPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
                )

                startInfo.RedirectStandardInput <- true

                use proc = new Process()
                proc.StartInfo <- startInfo

                try
                    let started = proc.Start()
                    if not started then
                        return { Success = false; Output = ""; Error = this.GetErrorMessage(ProcessStartFailed); ExitCode = -1 }

                    let output = proc.StandardOutput.ReadToEnd()
                    let error = proc.StandardError.ReadToEnd()

                    do! proc.WaitForExitAsync()

                    return
                        { Success = proc.ExitCode = 0
                          Output = output
                          Error = error
                          ExitCode = proc.ExitCode }
                with
                | ex ->
                    try proc.Kill() with | _ -> ()
                    return { Success = false; Output = ""; Error = ex.Message; ExitCode = -1 }
        }

    member private this.GetErrorMessage(error: CodeExecutionError) =
        match error with
        | ExecutableNotFound exe -> $"Executable not found: {exe}"
        | UnsafeExecutionPath path -> $"Unsafe execution path: {path}"
        | AccessDenied exe -> $"Access denied to executable: {exe}"
        | ProcessStartFailed -> "Failed to start process"
        | ValidationFailed msg -> $"Validation failed: {msg}"
        | UnknownExecutionError msg -> $"Unknown error: {msg}"

    member this.ExecutePythonCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            if code.Length > securityConfig.MaxCodeSize then
                return { Success = false; Output = ""; Error = $"Code too large (max {securityConfig.MaxCodeSize} characters)"; ExitCode = -1 }

            let tempFile = this.CreateSecureTempFile(".py", code)
            try
                return! this.ExecuteProcessSafely("python3", tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    member this.ExecuteFSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            if code.Length > securityConfig.MaxCodeSize then
                return { Success = false; Output = ""; Error = $"Code too large (max {securityConfig.MaxCodeSize} characters)"; ExitCode = -1 }

            let tempFile = this.CreateSecureTempFile(".fsx", code)
            try
                return! this.ExecuteProcessSafely("dotnet", sprintf "fsi \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    member this.ExecuteCSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            if code.Length > securityConfig.MaxCodeSize then
                return { Success = false; Output = ""; Error = $"Code too large (max {securityConfig.MaxCodeSize} characters)"; ExitCode = -1 }

            let tempFile = this.CreateSecureTempFile(".cs",
                sprintf "using System;\n\npublic class Program\n{\n    public static void Main()\n    {\n        %s\n    }\n}" code)
            try
                return! this.ExecuteProcessSafely("dotnet", sprintf "run --project \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    member this.ExecuteJavaScriptCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            if code.Length > securityConfig.MaxCodeSize then
                return { Success = false; Output = ""; Error = $"Code too large (max {securityConfig.MaxCodeSize} characters)"; ExitCode = -1 }

            let tempFile = this.CreateSecureTempFile(".js", code)
            try
                return! this.ExecuteProcessSafely("node", sprintf "\"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    // Static members for backward compatibility
    static member ExecutePythonCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        let service = CodeExecutionService()
        service.ExecutePythonCode(code, ?workingDirectory = workingDirectory)

    static member ExecuteFSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        let service = CodeExecutionService()
        service.ExecuteFSharpCode(code, ?workingDirectory = workingDirectory)

    static member ExecuteCSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        let service = CodeExecutionService()
        service.ExecuteCSharpCode(code, ?workingDirectory = workingDirectory)

    static member ExecuteJavaScriptCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        let service = CodeExecutionService()
        service.ExecuteJavaScriptCode(code, ?workingDirectory = workingDirectory)
