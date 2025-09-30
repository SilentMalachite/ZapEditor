namespace ZapEditor.ViewModels

open System
open System.ComponentModel
open System.IO
open System.Runtime.CompilerServices
open Microsoft.FSharp.Control
open System.Threading.Tasks
open System.Security
open System.Windows.Input
open Avalonia.Controls
open Avalonia.Platform.Storage
open Avalonia.Threading
open ZapEditor.Controls
open ZapEditor.Services
open System.Threading

type RelayCommand(action: Action<obj>, canExecute: Func<obj, bool>) =
    let canExecuteChanged = Event<EventHandler, EventArgs>()
    interface ICommand with
        member this.CanExecute(parameter) =
            match canExecute with
            | null -> true
            | _ -> canExecute.Invoke(parameter)
        member this.Execute(parameter) = action.Invoke(parameter)
        member this.add_CanExecuteChanged(handler) = canExecuteChanged.Publish.AddHandler(handler)
        member this.remove_CanExecuteChanged(handler) = canExecuteChanged.Publish.RemoveHandler(handler)

    new(action) = RelayCommand(action, null)

type MainWindowViewModel(?fileService: IFileService, ?editorService: IEditorService, ?toolValidator: IExternalToolValidator, ?configService: IConfigurationService, ?loggerService: ILoggerService, ?cancellationToken: CancellationToken) =
    do ()

    let fileService = defaultArg fileService (FileService(?configService = configService) :> IFileService)
    let toolValidator = defaultArg toolValidator (ExternalToolValidator() :> IExternalToolValidator)
    let configService = defaultArg configService (ConfigurationService.Load() :> IConfigurationService)
    let loggerService = defaultArg loggerService (LoggingService.Create(configService) :> ILoggerService)
    let config = configService.GetConfig()
    let cts = new CancellationTokenSource()
    let cancellationToken = defaultArg cancellationToken cts.Token

    // Replace magic numbers with configuration values
    let maxOutputLength = config.Application.MaxOutputLength

    // ログを初期化
    do LogHelper.Initialize(loggerService)

    // 起動時にツール依存性を検証
    do this.ValidateToolDependencies()

    // ログ記録
    do LogHelper.LogInfo "MainWindowViewModel initialized"
    let mutable editorService = editorService
    let mutable currentFileContent = ""
    let mutable currentFileName = ResourceManager.GetString("App_Untitled")
    let mutable currentLanguage = ResourceManager.GetString("Language_AutoDetect")
    let mutable statusBarText = ResourceManager.GetString("Status_Ready")
    let mutable currentFilePath: string option = None
    let mutable currentLanguageCode = "ja"
    let mutable openFileDialogFunc: unit -> Task<string option> = fun () -> Task.FromResult<string option>(None)
    let mutable saveFileDialogFunc: unit -> Task<string option> = fun () -> Task.FromResult<string option>(None)
    let availableLanguages = [| "自動検出"; "F#"; "C#"; "Python"; "JavaScript"; "TypeScript"; "HTML"; "CSS"; "JSON"; "XML"; "テキスト" |]

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.NotifyPropertyChanged([<CallerMemberName>] ?propertyName: string) =
        match propertyName with
        | Some name -> propertyChanged.Trigger(this, PropertyChangedEventArgs(name))
        | None -> ()

    member this.CurrentFileContent
        with get() = currentFileContent
        and set(value) =
            if currentFileContent <> value then
                currentFileContent <- value
                this.NotifyPropertyChanged(nameof this.CurrentFileContent)

    member this.CurrentFileName
        with get() = currentFileName
        and set(value) =
            if currentFileName <> value then
                currentFileName <- value
                this.NotifyPropertyChanged(nameof this.CurrentFileName)

    member this.CurrentLanguage
        with get() = currentLanguage
        and set(value) =
            if currentLanguage <> value then
                currentLanguage <- value
                this.NotifyPropertyChanged(nameof this.CurrentLanguage)

    member this.StatusBarText
        with get() = statusBarText
        and set(value) =
            if statusBarText <> value then
                statusBarText <- value
                this.NotifyPropertyChanged(nameof this.StatusBarText)

    member this.CurrentLanguageCode
        with get() = currentLanguageCode
        and set(value) =
            if currentLanguageCode <> value then
                currentLanguageCode <- value
                this.NotifyPropertyChanged("CurrentLanguageCode")

    member this.CurrentFilePath
        with get() = currentFilePath
        and set(value) =
            if currentFilePath <> value then
                currentFilePath <- value
                this.NotifyPropertyChanged(nameof this.CurrentFilePath)

    member this.AvailableLanguages = availableLanguages

    member this.SelectedLanguageIndex
        with get() =
            let index = availableLanguages |> Array.findIndex (fun lang -> lang = currentLanguage)
            if index >= 0 then index else 0
        and set(value) =
            if value >= 0 && value < availableLanguages.Length then
                let selectedLang = availableLanguages.[value]
                if selectedLang <> currentLanguage then
                    this.OnLanguageChanged(selectedLang)

    member this.NewFileCommand = RelayCommand(fun _ -> this.NewFile())
    member this.OpenFileCommand = RelayCommand(fun _ -> this.OpenFile())
    member this.SaveFileCommand = RelayCommand(fun _ -> this.SaveFile())
    member this.SaveAsFileCommand = RelayCommand(fun _ -> this.SaveAsFile())
    member this.ExitCommand = RelayCommand(fun _ -> this.Exit())
    member this.RunCodeCommand = RelayCommand(fun _ -> this.RunCode())
    member this.DebugCodeCommand = RelayCommand(fun _ -> this.DebugCode())
    member this.SetLanguageCommand = RelayCommand(fun param -> this.SetLanguage(param :?> string))
    member this.LanguageChangedCommand = RelayCommand(fun param -> this.OnLanguageChanged(param :?> string))

    member this.UndoCommand = RelayCommand(fun _ -> this.Undo())
    member this.RedoCommand = RelayCommand(fun _ -> this.Redo())
    member this.CutCommand = RelayCommand(fun _ -> this.Cut())
    member this.CopyCommand = RelayCommand(fun _ -> this.Copy())
    member this.PasteCommand = RelayCommand(fun _ -> this.Paste())
    member this.DeleteCommand = RelayCommand(fun _ -> this.Delete())
    member this.SelectAllCommand = RelayCommand(fun _ -> this.SelectAll())

    member private this.NewFile() =
        this.CurrentFileContent <- ""
        this.CurrentFileName <- ResourceManager.GetString("App_Untitled")
        this.CurrentFilePath <- None
        this.StatusBarText <- ResourceManager.GetString("Status_NewFile")
        match editorService with
        | Some service -> service.Text <- ""
        | None -> ()

    member private this.OpenFile() =
        let startTime = DateTime.UtcNow
        task {
            let! result = openFileDialogFunc() |> Async.AwaitTask
            match result with
            | Some path ->
                LogHelper.LogDebug $"Opening file: {path}"
                let! readResult = fileService.ReadFile path
                match readResult with
                | Ok content ->
                    do! AsyncHelper.UiThreadInvokeAsync(fun () ->
                        this.CurrentFileContent <- content
                        this.CurrentFileName <- fileService.GetFileName(path)
                        this.CurrentFilePath <- Some path
                        this.DetectLanguage(path)

                        match editorService with
                        | Some service ->
                            service.Text <- content
                            service.SetLanguage(this.CurrentLanguage)
                        | None -> ()

                        this.StatusBarText <- ResourceManager.FormatString("Status_FileOpened", [| this.CurrentFileName :> obj |]))

                    LogHelper.LogOperation "OpenFile" startTime true None
                | Error error ->
                    let errorMessage = this.GetFileErrorMessage(error)
                    AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg) errorMessage
                    LogHelper.LogOperation "OpenFile" startTime false (Some (Exception(errorMessage)))
            | None -> ()
        } |> Async.AwaitTask |> Async.Start

    member private this.GetFileErrorMessage(error: FileOperationError) =
        match error with
        | FileNotFound path -> ResourceManager.FormatString("Status_FileNotFound", [| path :> obj |])
        | FileAccessDenied _ -> ResourceManager.GetString("Status_FileAccessDenied")
        | FileTooLarge _ -> ResourceManager.GetString("Status_FileTooLarge")
        | EmptyContent -> ResourceManager.GetString("Status_NoContentToSave")
        | PathTooLong _ -> ResourceManager.GetString("Status_PathTooLong")
        | DirectoryNotFound _ -> ResourceManager.GetString("Status_DirectoryNotFound")
        | SecurityError _ -> ResourceManager.GetString("Status_SecurityError")
        | FileInUse _ -> ResourceManager.GetString("Status_FileInUse")
        | UnknownError msg -> msg

    member private this.SaveFile() =
        match currentFilePath with
        | Some path ->
            async {
                try
                    let content =
                        match editorService with
                        | Some service -> service.Text
                        | None -> this.CurrentFileContent
                    
                    if String.IsNullOrEmpty(content) then
                        this.StatusBarText <- ResourceManager.GetString("Status_NoContentToSave")
                    else
                        this.CurrentFileContent <- content
                        do! fileService.WriteFile path content |> Async.AwaitTask
                        this.StatusBarText <- ResourceManager.FormatString("Status_FileSaved", [| this.CurrentFileName :> obj |])
                with
                | :? UnauthorizedAccessException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_FileAccessDenied")
                | :? PathTooLongException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_PathTooLong")
                | :? DirectoryNotFoundException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_DirectoryNotFound")
                | :? SecurityException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_SecurityError")
                | :? IOException as ex when ex.Message.Contains("使用中") ->
                    this.StatusBarText <- ResourceManager.GetString("Status_FileInUse")
                | ex ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileSaveError", [| ex.Message :> obj |])
            } |> Async.Start
        | None -> this.SaveAsFile()

    member private this.SaveAsFile() =
        async {
            let! result = this.ShowSaveFileDialog()
            match result with
            | Some path ->
                try
                    let content =
                        match editorService with
                        | Some service -> service.Text
                        | None -> this.CurrentFileContent
                    
                    if String.IsNullOrEmpty(content) then
                        this.StatusBarText <- ResourceManager.GetString("Status_NoContentToSave")
                    else
                        this.CurrentFileContent <- content
                        do! fileService.WriteFile path content |> Async.AwaitTask
                        
                        this.CurrentFileName <- fileService.GetFileName(path)
                        this.CurrentFilePath <- Some path
                        this.DetectLanguage(path)
                        match editorService with
                        | Some service -> service.SetLanguage(this.CurrentLanguage)
                        | None -> ()
                        this.StatusBarText <- ResourceManager.FormatString("Status_FileSaved", [| this.CurrentFileName :> obj |])
                with
                | :? UnauthorizedAccessException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_FileAccessDenied")
                | :? PathTooLongException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_PathTooLong")
                | :? DirectoryNotFoundException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_DirectoryNotFound")
                | :? SecurityException ->
                    this.StatusBarText <- ResourceManager.GetString("Status_SecurityError")
                | :? IOException as ex when ex.Message.Contains("使用中") ->
                    this.StatusBarText <- ResourceManager.GetString("Status_FileInUse")
                | ex ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileSaveError", [| ex.Message :> obj |])
            | None -> ()
        } |> Async.Start

    member private this.Exit() =
        this.StatusBarText <- ResourceManager.GetString("App_Exiting")
        Environment.Exit(0)

    member private this.RunCode() =
        let content =
            match editorService with
            | Some service -> service.Text
            | None -> this.CurrentFileContent

        if String.IsNullOrWhiteSpace(content) then
            this.StatusBarText <- ResourceManager.GetString("Status_NoCode")
        else
            if content.Length > 50000 then
                this.StatusBarText <- ResourceManager.GetString("Status_CodeTooLarge")
            else
                this.CurrentFileContent <- content
                match this.CurrentLanguage with
                | "F#" -> this.RunFSharpCode(content)
                | "C#" -> this.RunCSharpCode(content)
                | "Python" -> this.RunPythonCode(content)
                | "JavaScript" -> this.RunJavaScriptCode(content)
                | _ -> this.StatusBarText <- ResourceManager.GetString("Status_UnsupportedLanguage")

    member private this.DebugCode() =
        let content =
            match editorService with
            | Some service -> service.Text
            | None -> this.CurrentFileContent

        if String.IsNullOrWhiteSpace(content) then
            this.StatusBarText <- ResourceManager.GetString("Status_NoCode")
        else
            match this.CurrentLanguage with
            | "F#" -> this.DebugFSharpCode(content)
            | "C#" -> this.DebugCSharpCode(content)
            | "Python" -> this.DebugPythonCode(content)
            | "JavaScript" -> this.DebugJavaScriptCode(content)
            | _ -> this.StatusBarText <- ResourceManager.GetString("Status_UnsupportedLanguage")

    member private this.DebugFSharpCode(code: string) =
        this.StatusBarText <- ResourceManager.GetString("Status_DebuggingFSharp")
        task {
            try
                let debugCode = sprintf "#debug\nprintfn \"デバッグ開始\"\n%s\nprintfn \"デバッグ終了\"" code
                
                let! result = CodeExecutionService.ExecuteFSharpCode(debugCode)
                do! Dispatcher.UIThread.InvokeAsync(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_DebugSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let truncatedOutput = 
                                if result.Output.Length > 200 then 
                                    result.Output.Substring(0, 200) + "..."
                                else 
                                    result.Output
                            this.StatusBarText <- ResourceManager.FormatString("Status_DebugOutput", [| truncatedOutput :> obj |])
                    else
                        let truncatedError = 
                            if result.Error.Length > 200 then 
                                result.Error.Substring(0, 200) + "..."
                            else 
                                result.Error
                        this.StatusBarText <- ResourceManager.FormatString("Status_DebugError", [| truncatedError :> obj |])
                )
            with ex ->
                do! Dispatcher.UIThread.InvokeAsync(fun () ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_DebugFailed", [| ex.Message :> obj |])
                )
        }
        |> ignore

    member private this.DebugCSharpCode(code: string) =
        this.StatusBarText <- ResourceManager.GetString("Status_DebuggingCSharp")
        task {
            try
                let debugCode = sprintf "using System;\n\npublic class Program\n{\n    public static void Main()\n    {\n        Console.WriteLine(\"デバッグ開始\");\n        %s\n        Console.WriteLine(\"デバッグ終了\");\n    }\n}" code
                
                let! result = CodeExecutionService.ExecuteCSharpCode(debugCode)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_DebugSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let truncatedOutput = 
                                if result.Output.Length > 200 then 
                                    result.Output.Substring(0, 200) + "..."
                                else 
                                    result.Output
                            this.StatusBarText <- ResourceManager.FormatString("Status_DebugOutput", [| truncatedOutput :> obj |])
                    else
                        let truncatedError = 
                            if result.Error.Length > 200 then 
                                result.Error.Substring(0, 200) + "..."
                            else 
                                result.Error
                        this.StatusBarText <- ResourceManager.FormatString("Status_DebugError", [| truncatedError :> obj |])
                )
            with ex ->
                Dispatcher.UIThread.Post(fun () ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_DebugFailed", [| ex.Message :> obj |])
                )
        }
        |> ignore

    member private this.DebugPythonCode(code: string) =
        this.StatusBarText <- ResourceManager.GetString("Status_DebuggingPython")
        task {
            try
                let debugCode = sprintf "print(\"デバッグ開始\")\n%s\nprint(\"デバッグ終了\")" code
                
                let! result = CodeExecutionService.ExecutePythonCode(debugCode)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_DebugSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let truncatedOutput = 
                                if result.Output.Length > 200 then 
                                    result.Output.Substring(0, 200) + "..."
                                else 
                                    result.Output
                            this.StatusBarText <- ResourceManager.FormatString("Status_DebugOutput", [| truncatedOutput :> obj |])
                    else
                        let truncatedError = 
                            if result.Error.Length > 200 then 
                                result.Error.Substring(0, 200) + "..."
                            else 
                                result.Error
                        this.StatusBarText <- ResourceManager.FormatString("Status_DebugError", [| truncatedError :> obj |])
                )
            with ex ->
                Dispatcher.UIThread.Post(fun () ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_DebugFailed", [| ex.Message :> obj |])
                )
        }
        |> ignore

    member private this.DebugJavaScriptCode(code: string) =
        this.StatusBarText <- ResourceManager.GetString("Status_DebuggingJavaScript")
        task {
            try
                let debugCode = sprintf "console.log(\"デバッグ開始\");\n%s\nconsole.log(\"デバッグ終了\");" code
                
                let! result = CodeExecutionService.ExecuteJavaScriptCode(debugCode)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_DebugSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let truncatedOutput = 
                                if result.Output.Length > 200 then 
                                    result.Output.Substring(0, 200) + "..."
                                else 
                                    result.Output
                            this.StatusBarText <- ResourceManager.FormatString("Status_DebugOutput", [| truncatedOutput :> obj |])
                    else
                        let truncatedError = 
                            if result.Error.Length > 200 then 
                                result.Error.Substring(0, 200) + "..."
                            else 
                                result.Error
                        this.StatusBarText <- ResourceManager.FormatString("Status_DebugError", [| truncatedError :> obj |])
                )
            with ex ->
                Dispatcher.UIThread.Post(fun () ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_DebugFailed", [| ex.Message :> obj |])
                )
        }
        |> ignore

    member private this.SetLanguage(langCode: string) =
        this.CurrentLanguageCode <- langCode
        ResourceManager.SetLanguage(langCode)
        this.StatusBarText <- ResourceManager.FormatString("Status_LanguageChanged", [| langCode :> obj |])

// UIの言語を切り替えた後で、現在の状態を更新
        this.CurrentFileName <- this.CurrentFileName
        this.CurrentLanguage <- this.CurrentLanguage

    member private this.OnLanguageChanged(language: string) =
        if language <> "自動検出" then
            this.CurrentLanguage <- language
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBoxの選択を更新
            match editorService with
            | Some service -> service.SetLanguage(language)
            | None -> ()
            this.StatusBarText <- ResourceManager.FormatString("Status_LanguageChanged", [| language :> obj |])

    member private this.DetectLanguage(filePath: string) =
        let ext = fileService.GetExtension(filePath)
        match ext with
        | ".fs" | ".fsx" ->
            this.CurrentLanguage <- "F#"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("F#")
            | None -> ()
        | ".cs" ->
            this.CurrentLanguage <- "C#"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("C#")
            | None -> ()
        | ".py" ->
            this.CurrentLanguage <- "Python"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("Python")
            | None -> ()
        | ".js" ->
            this.CurrentLanguage <- "JavaScript"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("JavaScript")
            | None -> ()
        | ".ts" ->
            this.CurrentLanguage <- "TypeScript"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("TypeScript")
            | None -> ()
        | ".html" | ".htm" ->
            this.CurrentLanguage <- "HTML"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("HTML")
            | None -> ()
        | ".css" ->
            this.CurrentLanguage <- "CSS"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("CSS")
            | None -> ()
        | ".json" ->
            this.CurrentLanguage <- "JSON"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("JSON")
            | None -> ()
        | ".xml" ->
            this.CurrentLanguage <- "XML"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("XML")
            | None -> ()
        | _ ->
            this.CurrentLanguage <- "テキスト"
            this.NotifyPropertyChanged(nameof this.SelectedLanguageIndex) // ComboBox選択を更新
            match editorService with
            | Some service -> service.SetLanguage("テキスト")
            | None -> ()

    member private this.RunFSharpCode(code: string) =
        AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg) (ResourceManager.GetString("Status_ExecutingFSharp"))

        task {
            try
                let! result = CodeExecutionService.ExecuteFSharpCode(code)
                do! AsyncHelper.UiThreadInvokeAsync(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_ExecutionSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let output = this.TruncateOutput(result.Output)
                            this.StatusBarText <- ResourceManager.FormatString("Status_ExecutionResult", [| output :> obj |])
                    else
                        let error = this.TruncateOutput(result.Error)
                        this.StatusBarText <- ResourceManager.FormatString("Status_ExecutionError", [| error :> obj |]))
            with ex ->
                AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg)
                    (ResourceManager.FormatString("Status_ExecutionFailed", [| ex.Message :> obj |]))
        } |> Async.AwaitTask |> Async.Start

    member private this.TruncateOutput(text: string) =
        if text.Length > maxOutputLength then
            text.Substring(0, maxOutputLength) + "..."
        else
            text

    member private this.RunCSharpCode(code: string) =
        this.ExecuteCodeWithStatus(code, "Status_ExecutingCSharp", CodeExecutionService.ExecuteCSharpCode)

    member private this.RunPythonCode(code: string) =
        this.ExecuteCodeWithStatus(code, "Status_ExecutingPython", CodeExecutionService.ExecutePythonCode)

    member private this.RunJavaScriptCode(code: string) =
        this.ExecuteCodeWithStatus(code, "Status_ExecutingJavaScript", CodeExecutionService.ExecuteJavaScriptCode)

    member private this.ExecuteCodeWithStatus(code: string, statusKey: string, executor: string -> Task<CodeExecutionResult>) =
        AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg) (ResourceManager.GetString(statusKey))

        task {
            try
                let! result = executor(code)
                do! AsyncHelper.UiThreadInvokeAsync(fun () ->
                    if result.Success then
                        this.StatusBarText <- ResourceManager.GetString("Status_ExecutionSuccess")
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            let output = this.TruncateOutput(result.Output)
                            this.StatusBarText <- ResourceManager.FormatString("Status_ExecutionResult", [| output :> obj |])
                    else
                        let error = this.TruncateOutput(result.Error)
                        this.StatusBarText <- ResourceManager.FormatString("Status_ExecutionError", [| error :> obj |]))
            with ex ->
                AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg)
                    (ResourceManager.FormatString("Status_ExecutionFailed", [| ex.Message :> obj |]))
        } |> Async.AwaitTask |> Async.Start

    
    member private this.Undo() =
        match editorService with
        | Some service ->
            if service.CanUndo then
                service.Undo()
                this.StatusBarText <- ResourceManager.GetString("Edit_Undo")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToUndo")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.Redo() =
        match editorService with
        | Some service ->
            if service.CanRedo then
                service.Redo()
                this.StatusBarText <- ResourceManager.GetString("Edit_Redo")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToRedo")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.Cut() =
        match editorService with
        | Some service ->
            if not (String.IsNullOrEmpty(service.SelectedText)) then
                service.Cut()
                this.StatusBarText <- ResourceManager.GetString("Edit_Cut")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToCut")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.Copy() =
        match editorService with
        | Some service ->
            if not (String.IsNullOrEmpty(service.SelectedText)) then
                service.Copy()
                this.StatusBarText <- ResourceManager.GetString("Edit_Copy")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToCopy")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.Paste() =
        match editorService with
        | Some service ->
            try
                service.Paste()
                this.StatusBarText <- ResourceManager.GetString("Edit_Paste")
            with
            | :? System.Runtime.InteropServices.COMException ->
                this.StatusBarText <- ResourceManager.GetString("Status_ClipboardEmpty")
            | ex ->
                this.StatusBarText <- ResourceManager.FormatString("Status_PasteError", [| ex.Message :> obj |])
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.Delete() =
        match editorService with
        | Some service ->
            if not (String.IsNullOrEmpty(service.SelectedText)) then
                service.SelectedText <- ""
                this.StatusBarText <- ResourceManager.GetString("Edit_Delete")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToDelete")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member private this.SelectAll() =
        match editorService with
        | Some service ->
            if not (String.IsNullOrEmpty(service.Text)) then
                service.SelectAll()
                this.StatusBarText <- ResourceManager.GetString("Edit_SelectAll")
            else
                this.StatusBarText <- ResourceManager.GetString("Status_NothingToSelect")
        | None ->
            this.StatusBarText <- ResourceManager.GetString("Status_NoEditor")

    member this.SetFileOperations(openFunc, saveFunc) =
        openFileDialogFunc <- openFunc
        saveFileDialogFunc <- saveFunc

    member this.SetEditor(editorControl: SyntaxHighlightEditor) =
        editorService <- Some(editorControl :> IEditorService)
        let service = editorControl :> IEditorService
        service.SetLanguage(this.CurrentLanguage)
        service.Text <- this.CurrentFileContent

    member private this.ShowOpenFileDialog() =
        async {
            let! result = openFileDialogFunc() |> Async.AwaitTask
            return result
        }

    member private this.ShowSaveFileDialog() =
        async {
            let! result = saveFileDialogFunc() |> Async.AwaitTask
            return result
        }

    member private this.ValidateToolDependencies() =
        task {
            try
                let! results = toolValidator.ValidateAllTools(ExternalToolValidator.DefaultToolDependencies)

                let availableTools = results |> List.filter (fun r -> r.IsAvailable)
                let missingTools = results |> List.filter (fun r -> not r.IsAvailable)
                let requiredMissing = missingTools |> List.filter (fun r -> r.Tool.Required)

                do! AsyncHelper.UiThreadInvokeAsync(fun () ->
                    if requiredMissing.IsEmpty then
                        if missingTools.IsEmpty then
                            this.StatusBarText <- ResourceManager.GetString("Status_Ready")
                        else
                            let missingNames = missingTools |> List.map (fun r -> r.Tool.Name) |> String.concat ", "
                            this.StatusBarText <- $"Optional tools not available: {missingNames}"
                    else
                        let missingRequired = requiredTools |> List.map (fun r -> r.Tool.Name) |> String.concat ", "
                        this.StatusBarText <- $"Required tools missing: {missingRequired}")

                // 結果をデバッグ出力（オプション）
                results |> List.iter (fun r ->
                    let message = ToolValidationHelper.GetToolStatusMessage(r)
                    printfn $"{message}")
            with
            | ex ->
                AsyncHelper.UpdateStatusSafely (fun msg -> this.StatusBarText <- msg)
                    $"Tool validation failed: {ex.Message}"
        } |> Async.AwaitTask |> Async.Start
