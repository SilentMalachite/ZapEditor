namespace ZapEditor.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open AvaloniaEdit
open AvaloniaEdit.Editing
open AvaloniaEdit.TextMate
open System
open System.IO
open System.Threading.Tasks
open TextMateSharp.Grammars
open TextMateSharp.Registry
open ZapEditor.Services

type SyntaxHighlightEditor () as this =
    inherit UserControl ()
    
    let mutable textEditor: TextEditor option = None
    let mutable textMateInstallation: TextMate.Installation option = None

    do this.InitializeComponent()
    
    interface IEditorService with
        member this.Text
            with get() =
                match textEditor with
                | Some editor -> editor.Text
                | None -> ""
            and set(value) =
                match textEditor with
                | Some editor -> editor.Text <- value
                | None -> ()

        member this.SelectedText
            with get() =
                match textEditor with
                | Some editor -> editor.SelectedText
                | None -> ""
            and set(value) =
                match textEditor with
                | Some editor -> editor.SelectedText <- value
                | None -> ()

        member this.CanUndo
            with get() =
                match textEditor with
                | Some editor -> editor.CanUndo
                | None -> false

        member this.CanRedo
            with get() =
                match textEditor with
                | Some editor -> editor.CanRedo
                | None -> false

        member this.Undo() =
            match textEditor with
            | Some editor -> editor.Undo() |> ignore
            | None -> ()

        member this.Redo() =
            match textEditor with
            | Some editor -> editor.Redo() |> ignore
            | None -> ()

        member this.Cut() =
            match textEditor with
            | Some editor -> editor.Cut()
            | None -> ()

        member this.Copy() =
            match textEditor with
            | Some editor -> editor.Copy()
            | None -> ()

        member this.Paste() =
            match textEditor with
            | Some editor -> editor.Paste()
            | None -> ()

        member this.SelectAll() =
            match textEditor with
            | Some editor -> editor.SelectAll()
            | None -> ()

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

                textMate.SetGrammar(sprintf "source.%s" langName) |> ignore
            | None -> ()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.Loaded.Add(fun _ -> this.OnLoaded())

    member private this.OnLoaded() =
        match this.FindControl<TextEditor>("Editor") with
        | null -> ()
        | editor -> this.SetupEditor(editor)

    member private this.SetupEditor(editor: TextEditor) =
        let registryOptions = RegistryOptions(ThemeName.DarkPlus)
        let installation = editor.InstallTextMate(registryOptions)

        let options = editor.Options
        options.ConvertTabsToSpaces <- true
        options.IndentationSize <- 4
        options.EnableHyperlinks <- false
        options.EnableTextDragDrop <- true
        editor.ShowLineNumbers <- true

        textEditor <- Some editor
        textMateInstallation <- Some installation
        this.Editor <- Some editor
        this.TextMateInstallation <- Some installation

    member val Editor: TextEditor option = None with get, set
    member val TextMateInstallation: TextMate.Installation option = None with get, set
