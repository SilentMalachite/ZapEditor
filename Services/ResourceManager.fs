namespace ZapEditor.Services

open System
open System.Globalization
open System.Resources
open System.Reflection

type ResourceManager() =
    static let resourceManager =
        System.Resources.ResourceManager("ZapEditor.Resources.Strings", Assembly.GetExecutingAssembly())

    static let mutable currentCulture = CultureInfo.CurrentUICulture

    static member CurrentCulture
        with get() = currentCulture
        and set(value) =
            currentCulture <- value
            CultureInfo.CurrentUICulture <- value

    static member GetString(key: string) =
        resourceManager.GetString(key, currentCulture)

    static member GetString(key: string, culture: CultureInfo) =
        resourceManager.GetString(key, culture)

    static member FormatString(key: string, args: obj array) =
        let format = ResourceManager.GetString(key)
        match format with
        | null -> key
        | _ -> String.Format(format, args)

    static member SetLanguage(languageCode: string) =
        let culture =
            match languageCode.ToLower() with
            | "ja" -> CultureInfo("ja-JP")
            | "en" -> CultureInfo("en-US")
            | "zh" -> CultureInfo("zh-CN")
            | _ -> CultureInfo.CurrentUICulture

        ResourceManager.CurrentCulture <- culture
