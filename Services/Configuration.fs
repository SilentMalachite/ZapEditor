namespace ZapEditor.Services

open System
open System.IO
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.Json

type ApplicationSettings =
    { Name: string
      Version: string
      MaxFileSize: int64
      MaxCodeSize: int
      MaxOutputLength: int
      DefaultLanguage: string }

type EditorSettings =
    { IndentationSize: int
      ConvertTabsToSpaces: bool
      ShowLineNumbers: bool
      EnableHyperlinks: bool
      EnableTextDragDrop: bool
      Theme: string }

type SecuritySettings =
    { AllowedExecutionPaths: string list
      AllowedDirectories: string list
      TempDirectory: string option
      ValidateExecutables: bool }

type LoggingSettings =
    { LogLevel: string
      FileEnabled: bool
      FilePath: string
      MaxFileSize: int64
      MaxFiles: int }

type CodeExecutionSettings =
    { TimeoutSeconds: int
      MaxConcurrentExecutions: int
      EnableDebug: bool }

type UISettings =
    { WindowWidth: int
      WindowHeight: int
      RememberWindowSize: bool
      RememberLanguage: bool }

type AppConfig =
    { Application: ApplicationSettings
      Editor: EditorSettings
      Security: SecuritySettings
      Logging: LoggingSettings
      CodeExecution: CodeExecutionSettings
      UI: UISettings }

type IConfigurationService =
    abstract member GetConfig: unit -> AppConfig
    abstract member Reload: unit -> unit

type ConfigurationService(?configPath: string) =
    let configPath = defaultArg configPath "appsettings.json"
    let mutable config: AppConfig option = None
    let reloadLock = obj()

    let buildConfiguration() =
        let builder = ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional = true, reloadOnChange = true)
            .AddEnvironmentVariables()

        builder.Build()

    let loadConfig() =
        lock reloadLock (fun () ->
            try
                let configuration = buildConfiguration()

                let appSettings = {
                    Name = configuration["Application:Name"] |> Option.ofObj |> Option.defaultValue "ZapEditor"
                    Version = configuration["Application:Version"] |> Option.ofObj |> Option.defaultValue "1.0.0"
                    MaxFileSize = configuration.GetValue<int64>("Application:MaxFileSize", 10485760L)
                    MaxCodeSize = configuration.GetValue<int>("Application:MaxCodeSize", 50000)
                    MaxOutputLength = configuration.GetValue<int>("Application:MaxOutputLength", 200)
                    DefaultLanguage = configuration["Application:DefaultLanguage"] |> Option.ofObj |> Option.defaultValue "ja"
                }

                let editorSettings = {
                    IndentationSize = configuration.GetValue<int>("Editor:IndentationSize", 4)
                    ConvertTabsToSpaces = configuration.GetValue<bool>("Editor:ConvertTabsToSpaces", true)
                    ShowLineNumbers = configuration.GetValue<bool>("Editor:ShowLineNumbers", true)
                    EnableHyperlinks = configuration.GetValue<bool>("Editor:EnableHyperlinks", false)
                    EnableTextDragDrop = configuration.GetValue<bool>("Editor:EnableTextDragDrop", true)
                    Theme = configuration["Editor:Theme"] |> Option.ofObj |> Option.defaultValue "DarkPlus"
                }

                let securitySettings = {
                    AllowedExecutionPaths =
                        configuration.GetSection("Security:AllowedExecutionPaths").GetChildren()
                        |> Seq.map (fun c -> c.Value)
                        |> Seq.choose id
                        |> List.ofSeq
                    AllowedDirectories =
                        configuration.GetSection("Security:AllowedDirectories").GetChildren()
                        |> Seq.map (fun c -> c.Value)
                        |> Seq.choose id
                        |> List.ofSeq
                    TempDirectory = configuration["Security:TempDirectory"] |> Option.ofObj
                    ValidateExecutables = configuration.GetValue<bool>("Security:ValidateExecutables", true)
                }

                let loggingSettings = {
                    LogLevel = configuration["Logging:LogLevel:Default"] |> Option.ofObj |> Option.defaultValue "Information"
                    FileEnabled = configuration.GetValue<bool>("Logging:File:Enabled", true)
                    FilePath = configuration["Logging:File:Path"] |> Option.ofObj |> Option.defaultValue "logs/zapeditor.log"
                    MaxFileSize = configuration.GetValue<int64>("Logging:File:MaxFileSize", 1048576L)
                    MaxFiles = configuration.GetValue<int>("Logging:File:MaxFiles", 5)
                }

                let codeExecutionSettings = {
                    TimeoutSeconds = configuration.GetValue<int>("CodeExecution:TimeoutSeconds", 30)
                    MaxConcurrentExecutions = configuration.GetValue<int>("CodeExecution:MaxConcurrentExecutions", 1)
                    EnableDebug = configuration.GetValue<bool>("CodeExecution:EnableDebug", true)
                }

                let uiSettings = {
                    WindowWidth = configuration.GetValue<int>("UI:WindowWidth", 1200)
                    WindowHeight = configuration.GetValue<int>("UI:WindowHeight", 800)
                    RememberWindowSize = configuration.GetValue<bool>("UI:RememberWindowSize", true)
                    RememberLanguage = configuration.GetValue<bool>("UI:RememberLanguage", true)
                }

                config <- Some {
                    Application = appSettings
                    Editor = editorSettings
                    Security = securitySettings
                    Logging = loggingSettings
                    CodeExecution = codeExecutionSettings
                    UI = uiSettings
                }

            with
            | ex ->
                // デフォルト設定をフォールバックとして使用
                config <- Some this.GetDefaultConfig()
                printfn $"Configuration loading failed, using defaults: {ex.Message}")

    member this.GetDefaultConfig() =
        {
            Application = {
                Name = "ZapEditor"
                Version = "1.1.0"
                MaxFileSize = 10485760L
                MaxCodeSize = 50000
                MaxOutputLength = 200
                DefaultLanguage = "ja"
            }
            Editor = {
                IndentationSize = 4
                ConvertTabsToSpaces = true
                ShowLineNumbers = true
                EnableHyperlinks = false
                EnableTextDragDrop = true
                Theme = "DarkPlus"
            }
            Security = {
                AllowedExecutionPaths = [
                    "/usr/bin"
                    "/usr/local/bin"
                    "/opt/homebrew/bin"
                ]
                AllowedDirectories = [
                    "ProgramFiles"
                    "ProgramFilesX86"
                    "LocalApplicationData"
                    "UserProfile/.dotnet/tools"
                ]
                TempDirectory = None
                ValidateExecutables = true
            }
            Logging = {
                LogLevel = "Information"
                FileEnabled = true
                FilePath = "logs/zapeditor.log"
                MaxFileSize = 1048576L
                MaxFiles = 5
            }
            CodeExecution = {
                TimeoutSeconds = 30
                MaxConcurrentExecutions = 1
                EnableDebug = true
            }
            UI = {
                WindowWidth = 1200
                WindowHeight = 800
                RememberWindowSize = true
                RememberLanguage = true
            }
        }

    do loadConfig()

    interface IConfigurationService with
        member this.GetConfig() =
            match config with
            | Some cfg -> cfg
            | None ->
                loadConfig()
                config.Value

        member this.Reload() = loadConfig()

    // 静的ヘルパー
    static member Load(?configPath: string) =
        ConfigurationService(?configPath = configPath) :> IConfigurationService