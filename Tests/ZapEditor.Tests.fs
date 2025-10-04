namespace ZapEditor.Tests
open NUnit.Framework
open System.Threading.Tasks
open ZapEditor.Services
open ZapEditor.ViewModels
open ZapEditor.Controls

[<TestFixture>]
type CodeExecutionServiceTests() =
    
    [<Test>]
    member _.``ExecutePythonCode_Success`` () : Task = 
        task {
            let code = "print('hello')"
            let! res = CodeExecutionService.ExecutePythonCode(code)
            Assert.IsTrue(res.Success, "Expected success")
            Assert.AreEqual("hello", res.Output.TrimEnd())
        }

[<TestFixture>]
type WritingModeTests() =
    
    [<Test>]
    member _.``MainWindowViewModel_InitialWritingMode_ShouldBeHorizontal`` () =
        let viewModel = MainWindowViewModel()
        Assert.IsFalse(viewModel.IsVerticalWritingMode, "Initial writing mode should be horizontal (false)")
    
    [<Test>]
    member _.``MainWindowViewModel_ToggleWritingMode_ShouldChangeState`` () =
        let viewModel = MainWindowViewModel()
        let initialMode = viewModel.IsVerticalWritingMode
        
        // Toggle once
        (viewModel.ToggleWritingModeCommand :> System.Windows.Input.ICommand).Execute(null)
        Assert.AreNotEqual(initialMode, viewModel.IsVerticalWritingMode, "Writing mode should toggle")
        
        // Toggle again
        (viewModel.ToggleWritingModeCommand :> System.Windows.Input.ICommand).Execute(null)
        Assert.AreEqual(initialMode, viewModel.IsVerticalWritingMode, "Writing mode should toggle back")
    
    [<Test>]
    member _.``MainWindowViewModel_SetVerticalWritingMode_ShouldUpdateProperty`` () =
        let viewModel = MainWindowViewModel()
        
        viewModel.IsVerticalWritingMode <- true
        Assert.IsTrue(viewModel.IsVerticalWritingMode, "Vertical writing mode should be true")
        
        viewModel.IsVerticalWritingMode <- false
        Assert.IsFalse(viewModel.IsVerticalWritingMode, "Vertical writing mode should be false")

[<TestFixture>]
type WritingModeConverterTests() =
    
    [<Test>]
    member _.``WritingModeConverter_VerticalMode_ShouldReturnVerticalString`` () =
        let converter = WritingModeConverter() :> Avalonia.Data.Converters.IValueConverter
        let result = converter.Convert(true, typeof<string>, null, System.Globalization.CultureInfo.InvariantCulture)
        
        // Should return "WritingMode_Vertical" resource string
        Assert.IsNotNull(result, "Result should not be null")
        Assert.IsInstanceOf<string>(result, "Result should be a string")
    
    [<Test>]
    member _.``WritingModeConverter_HorizontalMode_ShouldReturnHorizontalString`` () =
        let converter = WritingModeConverter() :> Avalonia.Data.Converters.IValueConverter
        let result = converter.Convert(false, typeof<string>, null, System.Globalization.CultureInfo.InvariantCulture)
        
        // Should return "WritingMode_Horizontal" resource string
        Assert.IsNotNull(result, "Result should not be null")
        Assert.IsInstanceOf<string>(result, "Result should be a string")
    
    [<Test>]
    member _.``WritingModeConverter_InvalidInput_ShouldReturnDefaultString`` () =
        let converter = WritingModeConverter() :> Avalonia.Data.Converters.IValueConverter
        let result = converter.Convert("invalid", typeof<string>, null, System.Globalization.CultureInfo.InvariantCulture)
        
        Assert.IsNotNull(result, "Result should not be null")
        Assert.IsInstanceOf<string>(result, "Result should be a string")