namespace ZapEditor.Controls

open System
open System.Globalization
open Avalonia.Data.Converters
open ZapEditor.Services

type WritingModeConverter() =
    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            match value with
            | :? bool as isVertical ->
                if isVertical then
                    ResourceManager.GetString("WritingMode_Vertical") :> obj
                else
                    ResourceManager.GetString("WritingMode_Horizontal") :> obj
            | _ -> ResourceManager.GetString("WritingMode_Horizontal") :> obj
        
        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            raise (NotSupportedException())
