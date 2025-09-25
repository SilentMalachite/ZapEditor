namespace ZapEditor.ViewModels

open System
open System.ComponentModel
open System.IO
open System.Runtime.CompilerServices
open System.Threading.Tasks
open System.Windows.Input
open Avalonia.Controls
open Avalonia.Platform.Storage
open Avalonia.Threading
open ZapEditor.Controls
open ZapEditor.Services
open System.Threading

type INotifyPropertyChanged with
    member this.NotifyPropertyChanged([<CallerMemberName>] ?propertyName: string) =
        match propertyName with
        | Some prop -> this.PropertyChanged.Invoke(this, PropertyChangedEventArgs(prop))
        | None -> ()

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

type MainWindowViewModel() as this =
    inherit INotifyPropertyChanged()

    let mutable currentFileContent = ""
    let mutable currentFileName = ResourceManager.GetString("App_Untitled")
    let mutable currentLanguage = ResourceManager.GetString("Language_AutoDetect")
    let mutable statusBarText = ResourceManager.GetString("Status_Ready")
    let mutable currentFilePath: string option = None
    let mutable currentLanguageCode = "ja"
    let mutable editor: SyntaxHighlightEditor option = None

    let mutable propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.NotifyPropertyChanged(propertyName) =
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

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

    member this.NewFileCommand = RelayCommand(fun _ -> this.NewFile())
    member this.OpenFileCommand = RelayCommand(fun _ -> this.OpenFile())
    member this.SaveFileCommand = RelayCommand(fun _ -> this.SaveFile())
    member this.SaveAsFileCommand = RelayCommand(fun _ -> this.SaveAsFile())
    member this.ExitCommand = RelayCommand(fun _ -> this.Exit())
    member this.RunCodeCommand = RelayCommand(fun _ -> this.RunCode())
    member this.DebugCodeCommand = RelayCommand(fun _ -> this.DebugCode())
    member this.SetLanguageCommand = RelayCommand(fun param -> this.SetLanguage(param :?> string))

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
        match editor with
        | Some editor -> editor.Text <- ""
        | None -> ()

    member private this.OpenFile() =
        async {
            let! result = this.ShowOpenFileDialog()
            match result with
            | Some path ->
                try
                    let content = File.ReadAllText(path)
                    this.CurrentFileContent <- content
                    this.CurrentFileName <- Path.GetFileName(path)
                    this.CurrentFilePath <- Some path
                    this.DetectLanguage(path)
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileOpened", [| this.CurrentFileName :> obj |])
                    match editor with
                    | Some editor ->
                        editor.SetLanguage(this.CurrentLanguage)
                        editor.Text <- content
                    | None -> ()
                with ex ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileOpenError", [| ex.Message :> obj |])
            | None -> ()
        } |> Async.Start

    member private this.SaveFile() =
        match currentFilePath with
        | Some path ->
            try
                let content =
                    match editor with
                    | Some editor -> editor.Text
                    | None -> this.CurrentFileContent
                this.CurrentFileContent <- content
                File.WriteAllText(path, content)
                this.StatusBarText <- ResourceManager.FormatString("Status_FileSaved", [| this.CurrentFileName :> obj |])
            with ex ->
                this.StatusBarText <- ResourceManager.FormatString("Status_FileSaveError", [| ex.Message :> obj |])
        | None -> this.SaveAsFile()

    member private this.SaveAsFile() =
        async {
            let! result = this.ShowSaveFileDialog()
            match result with
            | Some path ->
                try
                    let content =
                        match editor with
                        | Some editor -> editor.Text
                        | None -> this.CurrentFileContent
                    this.CurrentFileContent <- content
                    File.WriteAllText(path, content)
                    this.CurrentFileName <- Path.GetFileName(path)
                    this.CurrentFilePath <- Some path
                    this.DetectLanguage(path)
                    match editor with
                    | Some editor -> editor.SetLanguage(this.CurrentLanguage)
                    | None -> ()
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileSaved", [| this.CurrentFileName :> obj |])
                with ex ->
                    this.StatusBarText <- ResourceManager.FormatString("Status_FileSaveError", [| ex.Message :> obj |])
            | None -> ()
        } |> Async.Start

    member private this.Exit() =
        this.StatusBarText <- "アプリケーションを終了します"
        Environment.Exit(0)

    member private this.RunCode() =
        let content =
            match editor with
            | Some editor -> editor.Text
            | None -> this.CurrentFileContent

        if String.IsNullOrWhiteSpace(content) then
            this.StatusBarText <- ResourceManager.GetString("Status_NoCode")
        else
            this.CurrentFileContent <- content
            match this.CurrentLanguage with
            | "F#" -> this.RunFSharpCode(content)
            | "C#" -> this.RunCSharpCode(content)
            | "Python" -> this.RunPythonCode(content)
            | "JavaScript" -> this.RunJavaScriptCode(content)
            | _ -> this.StatusBarText <- ResourceManager.GetString("Status_UnsupportedLanguage")

    member private this.DebugCode() =
        this.StatusBarText <- ResourceManager.GetString("Status_DebugInDevelopment")

    member private this.SetLanguage(langCode: string) =
        this.CurrentLanguageCode <- langCode
        ResourceManager.SetLanguage(langCode)
        this.StatusBarText <- ResourceManager.FormatString("Status_LanguageChanged", [| langCode :> obj |])

        // UIの言語を切り替えた後で、現在の状態を更新
        this.CurrentFileName <- this.CurrentFileName
        this.CurrentLanguage <- this.CurrentLanguage

    member private this.DetectLanguage(filePath: string) =
        let ext = Path.GetExtension(filePath).ToLower()
        match ext with
        | ".fs" | ".fsx" ->
            this.CurrentLanguage <- "F#"
            match editor with
            | Some editor -> editor.SetLanguage("F#")
            | None -> ()
        | ".cs" ->
            this.CurrentLanguage <- "C#"
            match editor with
            | Some editor -> editor.SetLanguage("C#")
            | None -> ()
        | ".py" ->
            this.CurrentLanguage <- "Python"
            match editor with
            | Some editor -> editor.SetLanguage("Python")
            | None -> ()
        | ".js" ->
            this.CurrentLanguage <- "JavaScript"
            match editor with
            | Some editor -> editor.SetLanguage("JavaScript")
            | None -> ()
        | ".ts" ->
            this.CurrentLanguage <- "TypeScript"
            match editor with
            | Some editor -> editor.SetLanguage("TypeScript")
            | None -> ()
        | ".html" | ".htm" ->
            this.CurrentLanguage <- "HTML"
            match editor with
            | Some editor -> editor.SetLanguage("HTML")
            | None -> ()
        | ".css" ->
            this.CurrentLanguage <- "CSS"
            match editor with
            | Some editor -> editor.SetLanguage("CSS")
            | None -> ()
        | ".json" ->
            this.CurrentLanguage <- "JSON"
            match editor with
            | Some editor -> editor.SetLanguage("JSON")
            | None -> ()
        | ".xml" ->
            this.CurrentLanguage <- "XML"
            match editor with
            | Some editor -> editor.SetLanguage("XML")
            | None -> ()
        | _ ->
            this.CurrentLanguage <- "テキスト"
            match editor with
            | Some editor -> editor.SetLanguage("テキスト")
            | None -> ()

    member private this.RunFSharpCode(code: string) =
        this.StatusBarText <- "F#コードを実行中..."
        Task.Run(fun () ->
            task {
                let! result = CodeExecutionService.ExecuteFSharpCode(code)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- "F#コードが正常に実行されました"
                        // 実行結果を表示（将来的には別のウィンドウやパネルで）
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            this.StatusBarText <- sprintf "実行結果: %s" result.Output
                    else
                        this.StatusBarText <- sprintf "実行エラー: %s" result.Error
                )
            }
        ) |> ignore

    member private this.RunCSharpCode(code: string) =
        this.StatusBarText <- "C#コードを実行中..."
        Task.Run(fun () ->
            task {
                let! result = CodeExecutionService.ExecuteCSharpCode(code)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- "C#コードが正常に実行されました"
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            this.StatusBarText <- sprintf "実行結果: %s" result.Output
                    else
                        this.StatusBarText <- sprintf "実行エラー: %s" result.Error
                )
            }
        ) |> ignore

    member private this.RunPythonCode(code: string) =
        this.StatusBarText <- "Pythonコードを実行中..."
        Task.Run(fun () ->
            task {
                let! result = CodeExecutionService.ExecutePythonCode(code)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- "Pythonコードが正常に実行されました"
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            this.StatusBarText <- sprintf "実行結果: %s" result.Output
                    else
                        this.StatusBarText <- sprintf "実行エラー: %s" result.Error
                )
            }
        ) |> ignore

    member private this.RunJavaScriptCode(code: string) =
        this.StatusBarText <- "JavaScriptコードを実行中..."
        Task.Run(fun () ->
            task {
                let! result = CodeExecutionService.ExecuteJavaScriptCode(code)
                Dispatcher.UIThread.Post(fun () ->
                    if result.Success then
                        this.StatusBarText <- "JavaScriptコードが正常に実行されました"
                        if not (String.IsNullOrWhiteSpace(result.Output)) then
                            this.StatusBarText <- sprintf "実行結果: %s" result.Output
                    else
                        this.StatusBarText <- sprintf "実行エラー: %s" result.Error
                )
            }
        ) |> ignore

    member private this.Undo() =
        this.StatusBarText <- "元に戻す"

    member private this.Redo() =
        this.StatusBarText <- "やり直し"

    member private this.Cut() =
        this.StatusBarText <- "切り取り"

    member private this.Copy() =
        this.StatusBarText <- "コピー"

    member private this.Paste() =
        this.StatusBarText <- "貼り付け"

    member private this.Delete() =
        this.StatusBarText <- "削除"

    member private this.SelectAll() =
        this.StatusBarText <- "すべて選択"

    member val CurrentFilePath: string option = None with get, set

    let mutable openFileDialogFunc: unit -> Task<string option> = fun () -> Task.FromResult None
    let mutable saveFileDialogFunc: unit -> Task<string option> = fun () -> Task.FromResult None

    member this.SetFileOperations(openFunc, saveFunc) =
        openFileDialogFunc <- openFunc
        saveFileDialogFunc <- saveFunc

    member this.SetEditor(editorControl: SyntaxHighlightEditor) =
        editor <- Some editorControl
        editorControl.SetLanguage(this.CurrentLanguage)
        editorControl.Text <- this.CurrentFileContent

    member private this.ShowOpenFileDialog() =
        async {
            let! result = openFileDialogFunc()
            return result
        }

    member private this.ShowSaveFileDialog() =
        async {
            let! result = saveFileDialogFunc()
            return result
        }