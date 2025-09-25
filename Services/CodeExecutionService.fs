namespace ZapEditor.Services

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks

type CodeExecutionResult =
    { Success: bool
      Output: string
      Error: string
      ExitCode: int }

type CodeExecutionService() =

    static member ExecutePythonCode(code: string, ?workingDirectory: string) =
        task {
            let tempFile = Path.GetTempFileName() + ".py"
            try
                File.WriteAllText(tempFile, code)

                let startInfo = ProcessStartInfo(
                    FileName = "python3",
                    Arguments = tempFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
                )

                use proc = new Process()
                proc.StartInfo <- startInfo
                proc.Start() |> ignore

                let output = proc.StandardOutput.ReadToEnd()
                let error = proc.StandardError.ReadToEnd()

                do! proc.WaitForExitAsync()

                return
                    { Success = proc.ExitCode = 0
                      Output = output
                      Error = error
                      ExitCode = proc.ExitCode }
            finally
                if File.Exists(tempFile) then
                    File.Delete(tempFile)
        }

    static member ExecuteFSharpCode(code: string, ?workingDirectory: string) =
        task {
            let tempFile = Path.GetTempFileName() + ".fsx"
            try
                File.WriteAllText(tempFile, code)

                let startInfo = ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = sprintf "fsi %s" tempFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
                )

                use proc = new Process()
                proc.StartInfo <- startInfo
                proc.Start() |> ignore

                let output = proc.StandardOutput.ReadToEnd()
                let error = proc.StandardError.ReadToEnd()

                do! proc.WaitForExitAsync()

                return
                    { Success = proc.ExitCode = 0
                      Output = output
                      Error = error
                      ExitCode = proc.ExitCode }
            finally
                if File.Exists(tempFile) then
                    File.Delete(tempFile)
        }

    static member ExecuteCSharpCode(code: string, ?workingDirectory: string) =
        task {
            let tempFile = Path.GetTempFileName() + ".cs"
            try
                let csharpCode = sprintf "using System;\n\npublic class Program\n{\n    public static void Main()\n    {\n        %s\n    }\n}" code

                File.WriteAllText(tempFile, csharpCode)

                let startInfo = ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = sprintf "run --project %s" tempFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
                )

                use proc = new Process()
                proc.StartInfo <- startInfo
                proc.Start() |> ignore

                let output = proc.StandardOutput.ReadToEnd()
                let error = proc.StandardError.ReadToEnd()

                do! proc.WaitForExitAsync()

                return
                    { Success = proc.ExitCode = 0
                      Output = output
                      Error = error
                      ExitCode = proc.ExitCode }
            finally
                if File.Exists(tempFile) then
                    File.Delete(tempFile)
        }

    static member ExecuteJavaScriptCode(code: string, ?workingDirectory: string) =
        task {
            let tempFile = Path.GetTempFileName() + ".js"
            try
                File.WriteAllText(tempFile, code)

                let startInfo = ProcessStartInfo(
                    FileName = "node",
                    Arguments = tempFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
                )

                use proc = new Process()
                proc.StartInfo <- startInfo
                proc.Start() |> ignore

                let output = proc.StandardOutput.ReadToEnd()
                let error = proc.StandardError.ReadToEnd()

                do! proc.WaitForExitAsync()

                return
                    { Success = proc.ExitCode = 0
                      Output = output
                      Error = error
                      ExitCode = proc.ExitCode }
            finally
                if File.Exists(tempFile) then
                    File.Delete(tempFile)
        }
