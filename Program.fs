namespace ZapEditor

open System
open Avalonia
open ZapEditor.Services

module Program =

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        try
            // Initialize dependency injection container
            let container = ApplicationComposition.InitializeApplication()
            LogHelper.LogInfo "Dependency injection container initialized"

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace(areas = Array.empty)
        with
        | ex ->
            printfn $"Failed to initialize application: {ex.Message}"
            raise

    [<EntryPoint; STAThread>]
    let main argv =
        try
            buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
        with
        | ex ->
            printfn $"Application crashed: {ex.Message}"
            1
