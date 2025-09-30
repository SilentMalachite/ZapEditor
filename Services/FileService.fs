namespace ZapEditor.Services

open System
open System.IO
open System.Threading.Tasks
open System.Security

type FileService(?maxFileSize: int64, ?configService: IConfigurationService) =
    let maxFileSize =
        match maxFileSize, configService with
        | Some size, _ -> size
        | None, Some config -> config.GetConfig().Application.MaxFileSize
        | None, None -> 10L * 1024L * 1024L // 10MB default

    new() = FileService(?maxFileSize = None, ?configService = None)
    new(maxFileSize: int64) = FileService(?maxFileSize = Some maxFileSize, ?configService = None)

    interface IFileService with
        member this.OpenFileDialog() =
            Task.FromResult<string option>(None)

        member this.SaveFileDialog() =
            Task.FromResult<string option>(None)

        member this.ReadFile(path) =
            task {
                try
                    if not (Path.IsPathRooted(path)) then
                        return Error (RelativePathNotSupported path)

                    if not (File.Exists(path)) then
                        return Error (FileNotFound path)

                    let fileInfo = new FileInfo(path)
                    if fileInfo.Length > maxFileSize then
                        return Error (FileTooLarge (path, fileInfo.Length, maxFileSize))

                    use fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize = 4096, useAsync = true)
                    use streamReader = new StreamReader(fileStream)
                    let! content = streamReader.ReadToEndAsync()
                    return Ok content
                with
                | :? UnauthorizedAccessException -> return Error (UnauthorizedAccess path)
                | :? PathTooLongException -> return Error (PathTooLong path)
                | :? DirectoryNotFoundException -> return Error (DirectoryNotFound path)
                | :? SecurityException -> return Error (SecurityError path)
                | :? IOException as ex when ex.Message.Contains("使用中") -> return Error (FileInUse path)
                | ex -> return Error (UnknownError ex.Message)
            }

        member this.WriteFile(path) (content) =
            task {
                try
                    if String.IsNullOrEmpty(content) then
                        return Error EmptyContent

                    let directory = Path.GetDirectoryName(path)
                    if not (String.IsNullOrEmpty(directory)) && not (Directory.Exists(directory)) then
                        Directory.CreateDirectory(directory) |> ignore

                    use fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize = 4096, useAsync = true)
                    use streamWriter = new StreamWriter(fileStream)
                    streamWriter.Write(content)
                    streamWriter.Flush()
                    return Ok ()
                with
                | :? UnauthorizedAccessException -> return Error (UnauthorizedAccess path)
                | :? PathTooLongException -> return Error (PathTooLong path)
                | :? DirectoryNotFoundException -> return Error (DirectoryNotFound path)
                | :? SecurityException -> return Error (SecurityError path)
                | :? IOException as ex when ex.Message.Contains("使用中") -> return Error (FileInUse path)
                | ex -> return Error (UnknownError ex.Message)
            }

        member this.FileExists(path) =
            File.Exists(path)

        member this.GetFileName(path) =
            Path.GetFileName(path)

        member this.GetExtension(path) =
            Path.GetExtension(path).ToLower()