namespace ZapEditor.Services

open System
open System.Threading.Tasks

type IFileService =
    abstract member OpenFileDialog: unit -> Task<string option>
    abstract member SaveFileDialog: unit -> Task<string option>
    abstract member ReadFile: string -> Task<ValidationResult<string>>
    abstract member WriteFile: string -> string -> Task<ValidationResult<unit>>
    abstract member FileExists: string -> bool
    abstract member GetFileName: string -> string
    abstract member GetExtension: string -> string