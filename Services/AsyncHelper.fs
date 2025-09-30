namespace ZapEditor.Services

open System
open System.Threading
open System.Threading.Tasks
open Avalonia.Threading

type AsyncHelper() =

    /// UIスレッドで安全に実行するためのヘルパー関数
    static member UiThreadDispatch (action: unit -> unit) =
        Dispatcher.UIThread.Post(Action(action))

    /// UIスレッドで安全に実行し、結果を待機するヘルパー関数
    static member UiThreadInvokeAsync (action: unit -> 'T) : Task<'T> =
        Dispatcher.UIThread.InvokeAsync(Func<'T>(action)).Task

    /// キャンセレーションをサポートした非同期実行ヘルパー
    static member WithCancellation (cancellationToken: CancellationToken) (asyncOp: unit -> Task<'T>) : Task<'T> =
        task {
            use! _ = cancellationToken.Register(fun () -> ()) |> Async.AwaitTask
            return! asyncOp()
        }

    /// Safe file operation wrapper
    static member SafeFileOperation (operation: string -> Task<'T>) (path: string) (errorHandler: exn -> 'T) : Task<'T> =
        task {
            try
                return! operation path
            with
            | ex -> return errorHandler ex
        }

    /// Safe status message update on UI thread
    static member UpdateStatusSafely (statusSetter: string -> unit) (message: string) =
        AsyncHelper.UiThreadDispatch(fun () -> statusSetter message)