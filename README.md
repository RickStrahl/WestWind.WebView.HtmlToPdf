# Html to PDF using WebView on Windows


[![NuGet Pre Release](https://img.shields.io/nuget/vpre/westwind.webview.htmltopdf.svg)](https://www.nuget.org/packages?q=Westwind.webview.htmltopdf)

This library provides a quick way to print HTML to PDF on Windows using the WebView control. You can generate PDF from HTML using a few different mechanisms:

* To file
* To Stream
* Using Async Call
* Using Event Callbacks

## Prerequisites
The components supports:

* Running on Windows
* Running with an Interactive User Context 

The component does not support:

* Non Windows platforms
* Running inside of a server, non-user context (ie. SYSTEM)

## Using the library
You can install the library from NuGet:

```ps
PS> install-package westwind.webview.htmltopdf
```

or:

```ps
dotnet add package westwind.webview.htmltopdf
```

### Async Call Syntax for File Output

```csharp
// Full path to File or URL
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test2.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();

// all overrides are optional!
var pdfPrintSettings = new WebViewPrintSettings()
{
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 0.8F,
    ShouldPrintHeaderandFooter = false,
    ColorMode = "Color",
};
var result = await host.PrintToPdfAsync(htmlFile, outputFile, pdfPrintSettings);

Assert.IsTrue(result.IsSuccess, result.Message);
ShellUtils.OpenUrl(outputFile);
```

### Async Call Syntax for Stream Result

```csharp
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test3.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();
var pdfPrintSettings = new WebViewPrintSettings()
{
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 1,
    HeaderTitle = "Markdown Monster",
    ShouldPrintHeaderandFooter = true,
    ColorMode = "Grayscale",  // doesn't work
    FooterUri = "https://west-wind.com"
};

// We're interested in result.ResultStream
var result = await host.PrintToPdfStreamAsync(htmlFile, pdfPrintSettings);

Assert.IsTrue(result.IsSuccess, result.Message);
Assert.IsNotNull(result.ResultStream); // MemoryStream instance

Debug.WriteLine($"Stream Length: {result.ResultStream.Length}");

// Copy resultstream to output file and display
File.Delete(outputFile);
using var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write);
result.ResultStream.CopyTo(fstream);
result.ResultStream.Close(); // Close returned stream!

ShellUtils.OpenUrl(outputFile);
```

### Event Syntax to PDF File

```csharp
var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();            

// Callback when complete
host.OnPrintCompleteAction = (result) =>
{
    if (result.IsSuccess)
    {
        ShellUtils.OpenUrl(outputFile);
        Assert.IsTrue(true);
    }
    else
    {
        Assert.Fail(result.Message);
    }
};
var pdfPrintSettings = new WebViewPrintSettings()
{
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 0.8f
};
host.PrintToPdf(htmlFile, outputFile, pdfPrintSettings);
```

### Event Syntax to Stream

```csharp
// File or URL
var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");                       
var host = new HtmlToPdfHost();

// Callback on completion
host.OnPrintCompleteAction = (result) =>
{
    if (result.IsSuccess)
    {
        // create file so we can display
        var outputFile = Path.GetFullPath(@".\test1.pdf");
        File.Delete(outputFile);

        using var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write);
        result.ResultStream.CopyTo(fstream);

        result.ResultStream.Close(); // Close returned stream!

        ShellUtils.OpenUrl(outputFile);
        Assert.IsTrue(true);
    }
    else
    {
        Assert.Fail(result.Message);
    }
};
var pdfPrintSettings = new WebViewPrintSettings()
{
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 0.8f,
};
host.PrintToPdfStream(htmlFile, pdfPrintSettings);
```
