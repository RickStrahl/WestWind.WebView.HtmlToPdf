# .NET Html to PDF Conversion using WebView on Windows
| Library        | Nuget Package          |
|----------------|----------------|
| Westwind.WebView.HtmlToPdf | <a href="https://www.nuget.org/packages/Westwind.WebView.HtmlToPdf/">![](https://img.shields.io/nuget/v/Westwind.WebView.HtmlToPdf.svg)</a>  <a href="https://www.nuget.org/packages/Westwind.WebView.HtmlToPdf/">![](https://img.shields.io/nuget/dt/Westwind.WebView.HtmlToPdf.svg)</a> |
| Westwind.WebView.HtmlToPdf.Extended | <a href="https://www.nuget.org/packages/Westwind.WebView.HtmlToPdf.Extended/">![](https://img.shields.io/nuget/v/Westwind.WebView.HtmlToPdf.Extended.svg)</a>  <a href="https://www.nuget.org/packages/Westwind.WebView.HtmlToPdf.Extended/">![](https://img.shields.io/nuget/dt/Westwind.WebView.HtmlToPdf.Extended.svg)</a> |

 > Please note this is a very new project and there's significant churn at the moment. While below v1.0 semantic versioning is not used and there may be significant breaking changes between minor versions.

This library provides a quick way to print HTML to PDF on Windows using the WebView control. You can generate PDF from HTML using a few different mechanisms:

* To file
* To Stream
* Using Async Call
* Using Event Callbacks

This library uses the built-in **WebView2 Runtime in Windows so it has no external dependencies for your applications** assuming you are running on a recent version of Windows that has the WebView2 Runtime installed.

## Prerequisites
The components works with:

### Support for
* Windows 11/10 Server 2019/2022
* Desktop Applications
* Console Applications
* Service Application

The component does not support:

* Non Windows platforms

### Targets

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
There are two versions of the library:

* **Westwind.WebView.HtmlToPdf**  
The base Html to PDF conversion library. This library only has a dependency on the WebView control and provides base conversion. This library is lean and fast and does just base PDF conversion.

* **Westwind.WebView.HtmlToPdf.Extended**  
This library provides all the base features and adds TOC generation and CSS injection (in progress) and document information configuration (in progress). This library has additional dependencies, a larger footprint, and renders considerably slower as it has to parse the incoming URL/file multiple times. 

You can install either one of these NuGet packages (no need for both!):

```ps
dotnet add package westwind.webview.htmltopdf

dotnet add package westwind.webview.htmltopdf.Extended
```

Note the `.Extended` package has a dependency on the base package so no need to include both. Both libraries have identical interfaces via these two top level classes respectively:

* **HtmlToPdfHost**
* **HtmlToPdfHostExtended**

There are 4 separate output methods:

* PrintToPdf()  - Prints to file with a Callback
* PrintToPdfStream() - Prints and returns a `result.ResultStream` in a Callback
* PrintToPdfAsync() - Runs async to create a PDF file and waits for completion 
* PrintToPdfStreamAsync() - Runs async and returns a `result.ResultStream`

All of the methods take a file or Url as input. File names have to be fully qualified with a path. Output to file requires that you provide a filename.

All requests return a `PdfPrintResult` structure which has a `IsSuccess` flag you can check. For stream results, the `ResultStream` property will be set with a `MemoryStream` instance on success. Errors can use the `Message` or `LastException` to retrieve error information.

### Async Call Syntax for Stream Result

```cs
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test3.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHostExtended();  // or new HtmlPdfHost()

// optional Pdf/Print settings
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

// Copy resultstream to output file so we can display it
File.Delete(outputFile);
using var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write);
result.ResultStream.CopyTo(fstream);
result.ResultStream.Close(); // Close returned stream!

ShellUtils.OpenUrl(outputFile);
```

### Async Stream Example in a Web Application

```csharp
[HttpGet("rawpdf")]
public async Task<IActionResult> RawPdf()
{
    // source file or URL to render to PDF
    var file = Path.GetFullPath("./HtmlSampleFile-SelfContained.html");

    var pdf = new HtmlToPdfHostExtended();
    var pdfResult = await pdf.PrintToPdfStreamAsync(file, new WebViewPrintSettings {  PageRanges = "1-10"});

    if (pdfResult == null || !pdfResult.IsSuccess)
    {
        Response.StatusCode = 500;                
        return new JsonResult(new
        {
            isError = true,
            message = pdfResult.Message
        });
    }

    return new FileStreamResult(pdfResult.ResultStream, "application/pdf");             
}
```


### Async Call Syntax for File Output

```csharp
// Url or full qualified file path
var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test2.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost(); // or new HtmlToPdfHostExtended()
var result = await host.PrintToPdfAsync(htmlFile, outputFile);

Assert.IsTrue(result.IsSuccess, result.Message);
ShellUtils.OpenUrl(outputFile);  // display the PDF file you specified
```


### Callback Syntax to PDF File

```csharp
var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");
var outputFile = Path.GetFullPath(@".\test.pdf");
File.Delete(outputFile);

var host = new HtmlToPdfHost();            

// Callback when complete
var onPrintComplete = (PdfPrintResult result) =>
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
host.PrintToPdf(htmlFile, outputFile, onPrintComplete, pdfPrintSettings);

// make sure app keeps running
```

### Event Syntax to Stream

```csharp
// File or URL
var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");                       
var host = new HtmlToPdfHost();

// Callback on completion
var onPrintComplete = (PdfPrintResult result) =>
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
host.PrintToPdfStream(htmlFile, onPrintComplete, pdfPrintSettings);

// make sure app keeps running
```

The `Task` based methods are easiest to use so that's the recommended syntax. The callback based methods are there so you can more easily use this if you are running in a non-async and can't easily transition to async. 

Both approaches run on a separate STA thread to ensure that the WebView can run regardless of whether you are running inside of an application that has a main UI/STA thread and it works inside of Windows Service contexts.

## Support us
If you use this project and it provides value to you, please consider supporting by contributing or supporting via the sponsor link or one time donation at the top of this page. Value for value.