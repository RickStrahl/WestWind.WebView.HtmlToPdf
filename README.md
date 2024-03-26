# Html to PDF using WebView on Windows

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/westwind.webview.htmltopdf.svg)](https://www.nuget.org/packages?q=Westwind.webview.htmltopdf)

This library provides a quick way to print HTML to PDF on Windows using the WebView control. You can generate PDF from HTML using a few different mechanisms:

* To file
* To Stream
* Using Async Call
* Using Event Callbacks

This library uses the built-in **WebView2 Runtime in Windows so it has no external dependencies for your applications** assuming you are running on a recent version of Windows that has the WebView2 Runtime installed.

## Prerequisites
The components requires:

* Running on Windows 11/10 Server 2019/2022
* App that targets `net8.0-windows` or `net6.0-windows`
* Running with an Interactive User Context 
   * Desktop Applications
   * Console Applications

The component does not support:

* Non Windows platforms
* Running inside of a server, non-logged-on user context (ie. SYSTEM)

## Using the library

You can install the library from NuGet:

```ps
PS> install-package westwind.webview.htmltopdf
```

or:

```ps
dotnet add package westwind.webview.htmltopdf
```

The library has 4 separate output methods:

* PrintToPdf()  - Prints to file with a Callback
* PrintToPdfStream() - Prints and returns a `result.ResultStream` in a Callback
* PrintToPdfAsync() - Runs async to create a PDF file and waits for completion 
* PrintToPdfStreamAsync() - Runs async and returns a `result.ResultStream`

All of the methods take a file or Url as input. File names have to be fully qualified with a path. Output to file requires that you provide a filename.

All requests return a `PdfPrintResult` structure which has a `IsSuccess` flag you can check. For stream results, the `ResultStream` property will be set with a `MemoryStream` instance on success. Errors can use the `Message` or `LastException` to retrieve error information.

### Async Call Syntax for File Output

```csharp
// File or URL
var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");

// Full Path to output file
var outputFile = Path.GetFullPath(@".\test.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();            
host.OnPrintCompleteAction = (result) =>
{
    if (result.IsSuccess)
    {
        ShellUtils.OpenUrl(outputFile);
        Assert.IsTrue(true);
    }
    else
    {
        Assert.Fail(result.Message);  // also result.LastException
    }
};
host.PrintToPdf(htmlFile, outputFile);
```

### Async Call Syntax for Stream Result

```csharp
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test3.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();

// We're interested in result.ResultStream
var result = await host.PrintToPdfStreamAsync(htmlFile);

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
    // default margins are 0.4F
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 0.8f,
    PageRanges = "3-6"
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
