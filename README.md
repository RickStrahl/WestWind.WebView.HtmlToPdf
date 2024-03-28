# Html to PDF using WebView on Windows

<a href="https://www.nuget.org/packages/Westwind.WebView.HtmlToPdf/">![](https://img.shields.io/nuget/v/Westwind.WebView.HtmlToPdf.svg)</a> ![](https://img.shields.io/nuget/dt/Westwind.WebView.HtmlToPdf.svg) 

This library provides a quick way to print HTML to PDF on Windows using the WebView control. You can generate PDF from HTML using a few different mechanisms:

* To file
* To Stream
* Using Async Call
* Using Event Callbacks

This library uses the built-in **WebView2 Runtime in Windows so it has no external dependencies for your applications** assuming you are running on a recent version of Windows that has the WebView2 Runtime installed.

## Prerequisites
The components works with:

* Windows 11/10 Server 2019/2022
* Apps that target `net8.0-windows` or `net6.0-windows`
* Desktop Applications
* Console Applications
* Service Application

The component does not support:

* Non Windows platforms

Targets:

* net8.0-windows
* net6.0-windows
* net472

### Dependencies
Deployed applications have the following dependencies:  

* [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH&ch=1#download)  
On recent updates of Windows 11 and 10, the WebView is pre-installed as a system component. On Servers however, you may have to explicitly install the WebView Runtime.

* [Windows Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
The WebView2 component is dependent on Windows Desktop Runtime libraries and therefore requires the Desktop runtime to be installed **even for server applications**. 

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
// Url or full qualified file path
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test2.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();
var result = await host.PrintToPdfAsync(htmlFile, outputFile);

Assert.IsTrue(result.IsSuccess, result.Message);
ShellUtils.OpenUrl(outputFile);  // display the PDF file
```

### Async Call Syntax for Stream Result

```cs
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test3.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();
var pdfPrintSettings = new WebViewPrintSettings()
{                
    // default margins are 0.4F
    MarginBottom = 0.2F,
    MarginLeft = 0.2f,
    MarginRight = 0.2f,
    MarginTop = 0.4f,
    ScaleFactor = 0.8F 
    ShouldPrintHeaderAndFooter = true,
    HeaderTitle = "Blog Post Title"
};

// We're interested in result.ResultStream
var result = await host.PrintToPdfStreamAsync(htmlFile, pdfPrintSettings);

Assert.IsTrue(result.IsSuccess, result.Message);
Assert.IsNotNull(result.ResultStream); // This is what we're after

Debug.WriteLine($"Stream Length: {result.ResultStream.Length}");

// Copy resultstream to output file so we can display it
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
    PageRanges = "1,2,5-7"
};
host.PrintToPdf(htmlFile, outputFile, pdfPrintSettings);

// make sure app keeps running
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

// make sure app keeps running
```

The `Task` based methods are easiest to use so that's the recommended syntax. The event based methods are there so you can more easily use this if you are not running in some sort of async environment already. Both approaches run on a separate STA thread to ensure that the WebView can run regardless of whether you are running inside of an application that has a main UI/STA thread.

