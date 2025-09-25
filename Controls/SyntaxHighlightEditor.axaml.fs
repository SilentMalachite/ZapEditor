namespace ZapEditor.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open AvaloniaEdit
open AvaloniaEdit.TextMate
open System
open System.IO
open System.Threading.Tasks

type SyntaxHighlightEditor () as this =
    inherit UserControl ()

    let mutable editor: TextEditor option = None
    let mutable textMate: TextMate.Installation option = None

    do this.InitializeComponent()

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)
        this.Loaded.Add(this.OnLoaded)

    member private this.OnLoaded(sender: obj, e: RoutedEventArgs) =
        let editorControl = this.FindControl<TextEditor>("Editor")
        match editorControl with
        | null -> ()
        | editor ->
            this.SetupEditor(editor)

    member private this.SetupEditor(editor: TextEditor) =
        // TextMateによるシンタックスハイライトを設定
        let registryOptions = TextMate.RegistryOptions()
        let textMateInstallation = editor.InstallTextMate(registryOptions)

        // 基本的な設定
        editor.Options <- EditorOptions(
            ConvertTabsToSpaces = true,
            IndentationSize = 4,
            EnableTextWrapping = false,
            ShowLineNumbers = true,
            HighlightCurrentLine = true
        )

        textMateInstallation <- Some textMateInstallation
        this.Editor <- Some editor

    member this.SetLanguage(language: string) =
        match textMateInstallation with
        | Some textMate ->
            let langName =
                match language.ToLower() with
                | "f#" -> "fsharp"
                | "c#" -> "csharp"
                | "python" -> "python"
                | "javascript" -> "javascript"
                | "typescript" -> "typescript"
                | "html" -> "html"
                | "css" -> "css"
                | "json" -> "json"
                | "xml" -> "xml"
                | "markdown" -> "markdown"
                | "sql" -> "sql"
                | "java" -> "java"
                | "cpp" | "c++" -> "cpp"
                | "go" -> "go"
                | "rust" -> "rust"
                | _ -> "text"

            textMate.SetGrammarByScopeName(sprintf "source.%s" langName) |> ignore
        | None -> ()

    member this.Text
        with get() =
            match editor with
            | Some editor -> editor.Text
            | None -> ""
        and set(value) =
            match editor with
            | Some editor -> editor.Text <- value
            | None -> ()

    member val Editor: TextEditor option = None with get, set
    member val TextMateInstallation: TextMate.Installation option = None with get, set