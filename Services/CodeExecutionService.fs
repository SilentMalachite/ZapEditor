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
                failwithf "実行可能ファイルが見つかりません: %s" executable
            
            let fullPath = Path.GetFullPath(path)
            if not (fullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) ||
                    fullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) ||
                    fullPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) ||
                    fullPath.StartsWith("/usr/bin") || fullPath.StartsWith("/usr/local/bin")) then
                failwithf "安全でない実行パスです: %s" fullPath
            
            path
        with
        | :? SecurityException -> failwithf "実行可能ファイルへのアクセス権限がありません: %s" executable
        | ex -> failwithf "実行可能ファイルの検証に失敗しました: %s" ex.Message

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
                try File.Delete(tempFile) with | _ -> ()
            reraise()

    static member private ExecuteProcessSafely(executable: string, arguments: string, ?workingDirectory: string) =
        task {
            let validatedPath = CodeExecutionService.ValidateExecutablePath(executable)
            
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
                    failwith "プロセスを開始できませんでした"
                
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
                return! Task.FromException<CodeExecutionResult>(ex)
        }

    static member ExecutePythonCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".py", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("python3", tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    static member ExecuteFSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".fsx", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("dotnet", sprintf "fsi \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    static member ExecuteCSharpCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".cs", 
                sprintf "using System;\n\npublic class Program\n{\n    public static void Main()\n    {\n        %s\n    }\n}" code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("dotnet", sprintf "run --project \"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }

    static member ExecuteJavaScriptCode(code: string, ?workingDirectory: string) : Task<CodeExecutionResult> =
        task {
            let tempFile = CodeExecutionService.CreateSecureTempFile(".js", code)
            try
                return! CodeExecutionService.ExecuteProcessSafely("node", sprintf "\"%s\"" tempFile, ?workingDirectory = workingDirectory)
            finally
                if File.Exists(tempFile) then
                    try File.Delete(tempFile) with | _ -> ()
        }
