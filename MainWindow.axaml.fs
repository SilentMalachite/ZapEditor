namespace ZapEditor

open Avalonia
open Avalonia.Controls
open Avalonia.Diagnostics
open Avalonia.Markup.Xaml
open Avalonia.Platform.Storage
open ZapEditor.ViewModels
open ZapEditor.Controls
open System.IO
open System.Threading.Tasks

type MainWindow () as this =
    inherit Window ()

    let viewModel = ServiceLocator.GetService<MainWindowViewModel>()

    do
        this.InitializeComponent()
        this.DataContext <- viewModel

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)

    override this.OnOpened(e) =
        base.OnOpened(e)
        // ViewModelにファイルダイアログ機能を提供
        viewModel.SetFileOperations(
            (fun () -> this.ShowOpenFileDialogAsync()),
            (fun () -> this.ShowSaveFileDialogAsync())
        )

        // Get syntax highlight editor
        this.FindControl<SyntaxHighlightEditor>("Editor")
        |> Option.ofObj
        |> Option.iter (fun editorControl ->
            // Connect ViewModel with editor
            viewModel.SetEditor(editorControl)
        )

    member private this.ShowOpenFileDialogAsync() =
        task {
            let openOptions = FilePickerOpenOptions(
                    Title = "ファイルを開く",
                    FileTypeFilter = [| FilePickerFileTypes.All |]
                )
            let! files = this.StorageProvider.OpenFilePickerAsync(openOptions)
            return
                if files.Count > 0 then
                    Some (files.[0].TryGetLocalPath())
                else
                    None
        }

    member private this.ShowSaveFileDialogAsync() =
        task {
            let saveOptions = FilePickerSaveOptions(
                    Title = "名前を付けて保存",
                    FileTypeChoices = [| FilePickerFileTypes.All |]
                )
            let! file = this.StorageProvider.SaveFilePickerAsync(saveOptions)
            return
                if file <> null then
                    Some (file.TryGetLocalPath())
                else
                    None
        }
