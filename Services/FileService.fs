namespace ZapEditor.Services

open System
open System.IO
open System.Threading.Tasks
open System.Security

type FileService() =
    interface IFileService with
        member this.OpenFileDialog() =
            Task.FromResult<string option>(None)

        member this.SaveFileDialog() =
            Task.FromResult<string option>(None)

        member this.ReadFile(path) =
            task {
                if not (Path.IsPathRooted(path)) then
                    failwith "相対パスはサポートされていません"
                
                if not (File.Exists(path)) then
                    failwithf "ファイルが見つかりません: %s" path
                
                let fileInfo = new FileInfo(path)
                if fileInfo.Length > 10L * 1024L * 1024L then
                    failwith "ファイルサイズが大きすぎます (10MB制限)"
                
                use fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize = 4096, useAsync = true)
                use streamReader = new StreamReader(fileStream)
                return! streamReader.ReadToEndAsync()
            }

        member this.WriteFile(path) (content) =
            task {
                if String.IsNullOrEmpty(content) then
                    failwith "保存する内容がありません"
                
                let directory = Path.GetDirectoryName(path: string)
                if not (Directory.Exists(directory)) then
                    Directory.CreateDirectory(directory) |> ignore
                
                use fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize = 4096, useAsync = true)
                use streamWriter = new StreamWriter(fileStream)
                streamWriter.Write(content)
                streamWriter.Flush()
            }

        member this.FileExists(path) =
            File.Exists(path)

        member this.GetFileName(path) =
            Path.GetFileName(path)

        member this.GetExtension(path) =
            Path.GetExtension(path).ToLower()