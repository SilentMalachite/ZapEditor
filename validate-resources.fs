#!/usr/bin/env dotnet fsi

module ResourceValidator

open System
open System.IO
open System.Xml

// リソースファイルの整合性を検証するスクリプト
let extractResourceKeys (filePath: string) =
    try
        let doc = XmlDocument()
        doc.Load(filePath)

        let keys =
            doc.GetElementsByTagName("data")
            |> Seq.cast<XmlNode>
            |> Seq.choose (fun node ->
                let nameAttr = node.Attributes.["name"]
                if nameAttr <> null then
                    let value = nameAttr.Value
                    if value.StartsWith("App_") ||
                       value.StartsWith("Status_") ||
                       value.StartsWith("Edit_") ||
                       value.StartsWith("Menu_") ||
                       value.StartsWith("Language_") ||
                       value.StartsWith("Dialog_") ||
                       value.StartsWith("Toolbar_") ||
                       value.StartsWith("Sidebar_") then
                        Some value
                    else
                        None
                else
                    None)
            |> Seq.sort
            |> Seq.toList

        Some keys
    with
    | ex ->
        printfn $"Error reading {filePath}: {ex.Message}"
        None

let main () =
    let resourceDir = "/Users/hiro/Projetct/GitHub/Zap/ZapEditor/Resources"
    let files = ["Strings.ja.resx"; "Strings.en.resx"; "Strings.zh.resx"]

    let resourceMaps =
        files
        |> List.map (fun file ->
            let path = Path.Combine(resourceDir, file)
            let keys = extractResourceKeys path
            (file, keys))
        |> List.filter (fun (_, keys) -> keys.IsSome)
        |> List.map (fun (file, keys) -> (file, keys.Value))

    if resourceMaps.Length <> files.Length then
        printfn "Error: Could not read all resource files"
        exit 1

    let jaKeys = resourceMaps |> List.find (fun (file, _) -> file.Contains("ja")) |> snd
    let enKeys = resourceMaps |> List.find (fun (file, _) -> file.Contains("en")) |> snd
    let zhKeys = resourceMaps |> List.find (fun (file, _) -> file.Contains("zh")) |> snd

    printfn "Resource key validation:"
    printfn $"Japanese keys: {jaKeys.Length}"
    printfn $"English keys: {enKeys.Length}"
    printfn $"Chinese keys: {zhKeys.Length}"
    printfn ""

    let jaSet = Set.ofList jaKeys
    let enSet = Set.ofList enKeys
    let zhSet = Set.ofList zhKeys

    // Keys only existing in Japanese
    let jaOnly = Set.difference jaSet enSet
    if not (Set.isEmpty jaOnly) then
        printfn "Keys missing from English:"
        jaOnly |> Set.iter (printfn "  - %s")
        printfn ""

    let jaOnlyZh = Set.difference jaSet zhSet
    if not (Set.isEmpty jaOnlyZh) then
        printfn "Keys missing from Chinese:"
        jaOnlyZh |> Set.iter (printfn "  - %s")
        printfn ""

    // Keys only existing in English
    let enOnly = Set.difference enSet jaSet
    if not (Set.isEmpty enOnly) then
        printfn "Keys missing from Japanese:"
        enOnly |> Set.iter (printfn "  - %s")
        printfn ""

    // Keys only existing in Chinese
    let zhOnly = Set.difference zhSet jaSet
    if not (Set.isEmpty zhOnly) then
        printfn "Keys missing from Japanese:"
        zhOnly |> Set.iter (printfn "  - %s")
        printfn ""

    if Set.isEmpty jaOnly && Set.isEmpty jaOnlyZh && Set.isEmpty enOnly && Set.isEmpty zhOnly then
        printfn "✅ All resource files are synchronized!"
    else
        printfn "❌ Resource files have inconsistencies"

main ()