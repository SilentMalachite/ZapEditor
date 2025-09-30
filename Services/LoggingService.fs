namespace ZapEditor.Services

open System
open System.IO
open Microsoft.Extensions.Logging
open Serilog
open Serilog.Events

type ILoggerService =
    abstract member LogDebug: message: string -> unit
    abstract member LogInfo: message: string -> unit
    abstract member LogWarning: message: string -> unit
    abstract member LogError: message: string -> exn option -> unit
    abstract member LogCritical: message: string -> exn option -> unit

type LoggingService(?configService: IConfigurationService) =
    let config = defaultArg configService (ConfigurationService.Load() :> IConfigurationService).GetConfig()

    let logger =
        let logConfig = LoggerConfiguration()
            .MinimumLevel.Parse(config.Logging.LogLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", config.Application.Name)
            .Enrich.WithProperty("Version", config.Application.Version)

        let logConfig =
            if config.Logging.FileEnabled then
                let logDir = Path.GetDirectoryName(config.Logging.FilePath)
                if not (String.IsNullOrEmpty(logDir)) then
                    Directory.CreateDirectory(logDir) |> ignore

                logConfig.WriteTo.File(
                    path = config.Logging.FilePath,
                    rollingInterval = RollingInterval.Day,
                    retainedFileCountLimit = Nullable(config.Logging.MaxFiles),
                    fileSizeLimitBytes = Nullable(config.Logging.MaxFileSize),
                    outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Properties} {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel = LogEventLevel.Debug
                )
            else
                logConfig

        let logConfig =
            #if DEBUG
            logConfig.WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel = LogEventLevel.Debug
            )
            #else
            logConfig
            #endif

        logConfig.CreateLogger()

    interface ILoggerService with
        member this.LogDebug(message) =
            logger.Debug(message)

        member this.LogInfo(message) =
            logger.Information(message)

        member this.LogWarning(message) =
            logger.Warning(message)

        member this.LogError(message, exn) =
            match exn with
            | Some e -> logger.Error(e, message)
            | None -> logger.Error(message)

        member this.LogCritical(message, exn) =
            match exn with
            | Some e -> logger.Fatal(e, message)
            | None -> logger.Fatal(message)

    // 静的ヘルパー
    static member Create(?configService: IConfigurationService) =
        LoggingService(?configService = configService) :> ILoggerService

    // ログ設定の初期化
    static member InitializeGlobalLogger(?configService: IConfigurationService) =
        let service = LoggingService.Create(?configService = configService)
        Log.Logger <- (service :?> LoggingService).logger

module LogHelper =
    let mutable private loggerService: ILoggerService option = None

    let Initialize(service: ILoggerService) =
        loggerService <- Some service

    let LogDebug(message) =
        match loggerService with
        | Some logger -> logger.LogDebug(message)
        | None -> printfn $"[DEBUG] {message}"

    let LogInfo(message) =
        match loggerService with
        | Some logger -> logger.LogInfo(message)
        | None -> printfn $"[INFO] {message}"

    let LogWarning(message) =
        match loggerService with
        | Some logger -> logger.LogWarning(message)
        | None -> printfn $"[WARN] {message}"

    let LogError(message, ?exn: Exception) =
        match loggerService with
        | Some logger -> logger.LogError(message, exn)
        | None ->
            match exn with
            | Some e -> printfn $"[ERROR] {message}: {e.Message}"
            | None -> printfn $"[ERROR] {message}"

    let LogCritical(message, ?exn: Exception) =
        match loggerService with
        | Some logger -> logger.LogCritical(message, exn)
        | None ->
            match exn with
            | Some e -> printfn $"[CRITICAL] {message}: {e.Message}"
            | None -> printfn $"[CRITICAL] {message}"

    // 構造化ログのヘルパー
    let LogOperation(operation: string) (startTime: DateTime) (result: bool) (error: Exception option) =
        let duration = DateTime.UtcNow - startTime
        let message = $"Operation {operation} completed in {duration.TotalMilliseconds:F0}ms"

        if result then
            LogInfo($"{message} - Success")
        else
            match error with
            | Some e -> LogError($"{message} - Failed", e)
            | None -> LogError($"{message} - Failed")