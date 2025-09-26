namespace ZapEditor.Services

open System

type IEditorService =
    abstract Text: string with get, set
    abstract SelectedText: string with get, set
    abstract CanUndo: bool with get
    abstract CanRedo: bool with get
    abstract Undo: unit -> unit
    abstract Redo: unit -> unit
    abstract Cut: unit -> unit
    abstract Copy: unit -> unit
    abstract Paste: unit -> unit
    abstract SelectAll: unit -> unit
    abstract SetLanguage: string -> unit