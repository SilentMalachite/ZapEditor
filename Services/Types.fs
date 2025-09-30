namespace ZapEditor.Services

open System

type FileOperationError =
    | RelativePathNotSupported of path: string
    | FileNotFound of path: string
    | FileTooLarge of path: string * size: int64 * limit: int64
    | EmptyContent
    | UnauthorizedAccess of path: string
    | PathTooLong of path: string
    | DirectoryNotFound of path: string
    | SecurityError of path: string
    | FileInUse of path: string
    | UnknownError of message: string

type CodeExecutionError =
    | ExecutableNotFound of executable: string
    | UnsafeExecutionPath of path: string
    | AccessDenied of executable: string
    | ProcessStartFailed
    | ValidationFailed of message: string
    | UnknownExecutionError of message: string

type ValidationResult<'T> = Result<'T, FileOperationError>
type ExecutionResult<'T> = Result<'T, CodeExecutionError>