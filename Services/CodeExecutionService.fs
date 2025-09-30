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

type CodeExecutionService() =

    static member private ValidateExecutablePath(executable: string) =
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
                failwith (ResourceManager.FormatString("Error_ExecutableNotFound", [| executable :> obj |]))
            
            let fullPath = Path.GetFullPath(path)
            let safePaths = [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                "/usr/bin"
                "/usr/local/bin"
                "/opt/homebrew/bin"
            ]
            let isSafe = safePaths |> List.exists (fun safePath -> 
                not (String.IsNullOrEmpty(safePath)) && fullPath.StartsWith(safePath))
            if not isSafe then
                failwith (ResourceManager.FormatString("Error_UnsafePath", [| fullPath :> obj |]))
            
            path
        with
        | :? SecurityException -> failwith (ResourceManager.FormatString("Error_AccessDenied", [| executable :> obj |]))
        | ex -> failwith (ResourceManager.FormatString("Error_ValidationFailed", [| ex.Message :> obj |]))

    static member private CreateSecureTempFile(extension: string, content: string) =
        let tempDir = Path.GetTempPath()
        let fileName = Guid.NewGuid().ToString("N") + extension
        let tempFile = Path.Combine(tempDir, fileName)
        
        try
            File.WriteAllText(tempFile, content)
            tempFile
        with
        | ex ->
            if File.Exists(tempFile) then
                try 
                    File.Delete(tempFile)
                with 
                | deleteEx -> 
                    // Log the delete failure but don't suppress the original exception
                    eprintfn "%s" (ResourceManager.FormatString("Error_TempFileDeleteFailed", [| tempFile :> obj; deleteEx.Message :> obj |]))
            reraise()
    
    static member private DeleteTempFileSafely(tempFile: string) =
        if File.Exists(tempFile) then
            try 
                File.Delete(tempFile)
            with 
            | ex -> 
                eprintfn "一時ファイルの削除に失敗: %s - %s" tempFile ex.Message

    static member private ExecuteProcessSafely(executable: string, arguments: string, ?workingDirectory: string, ?timeoutMs: int) =
        task {
            let validatedPath = CodeExecutionService.ValidateExecutablePath(executable)
            let timeout = defaultArg timeoutMs 30000 // 30 seconds default
            
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
                    failwith (ResourceManager.GetString("Error_ProcessStartFailed"))
                
                // Read output asynchronously to prevent deadlocks
                let outputTask = proc.StandardOutput.ReadToEndAsync()
                let errorTask = proc.StandardError.ReadToEndAsync()
                
                let! completed = proc.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(float timeout))
                
                let! output = outputTask
                let! error = errorTask
                
                if proc.HasExited then
                    return
                        { Success = proc.ExitCode = 0
                          Output = output
                          Error = error
                          ExitCode = proc.ExitCode }
                else
                    // Timeout occurred
                    try proc.Kill(true) with | _ -> ()
                    return
                        { Success = false
                          Output = output
                          Error = ResourceManager.GetString("Error_ExecutionTimeout")
                          ExitCode = -1 }
            with
            | :? TimeoutException ->
                try proc.Kill(true) with | _ -> ()
                return
                    { Success = false
                      Output = ""
                      Error = "実行がタイムアウトしました"
                      ExitCode = -1 }
            | ex ->
                try proc.Kill(true) with | _ -> ()
                return! Task.FromException<CodeExecutionResult>(ex)
        }

    static member ExecutePythonCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".py", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("python3", tempFile, ?workingDirectory = workingDirectory)
            finally
                CodeExecutionService.DeleteTempFileSafely(tempFile)
        }

    static member ExecuteFSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".fsx", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("dotnet", sprintf "fsi \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                CodeExecutionService.DeleteTempFileSafely(tempFile)
        }

    static member ExecuteCSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            // Wrap code in a complete program with top-level statements
            let wrappedCode = sprintf "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\n\n%s" code
            let tempFile = CodeExecutionService.CreateSecureTempFile(".csx", wrappedCode)
            try
                // Use dotnet-script for C# script execution
                return! CodeExecutionService.ExecuteProcessSafely("dotnet", sprintf "script \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                CodeExecutionService.DeleteTempFileSafely(tempFile)
        }

    static member ExecuteJavaScriptCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".js", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("node", sprintf "\"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                CodeExecutionService.DeleteTempFileSafely(tempFile)
        }
