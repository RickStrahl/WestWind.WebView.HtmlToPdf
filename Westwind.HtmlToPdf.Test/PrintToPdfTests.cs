using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Utilities;
using Westwind.WebView.HtmlToPdf;

namespace Westwind.PdfToHtml.Test
{

    [TestClass]
    public class PrintToPdfTests
    {


        /// <summary>
        /// Async Result operation - to file
        /// </summary>
        [TestMethod]
        public async Task PrintToPdfFileAsyncTest()
        {            
            // File or URL to render
            var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");
            var outputFile = Path.GetFullPath(@".\test2.pdf");
            File.Delete(outputFile);

            var host = new HtmlToPdfHost();
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                // margins are 0.4F default
                MarginTop = 0.3f,
                MarginBottom = 0.3F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                
                ScaleFactor = 0.9F,
                //ShouldPrintBackgrounds = false
                //PageRanges = "1-3,5-8"
                //ColorMode = WebViewColorMode.Monochrome // this is broken in WebView - always color                
            };
            //host.CssAndScriptOptions.CssToInject = "h3 { color: green }";

            // output file is created
            var result = await host.PrintToPdfAsync(htmlFile, outputFile, pdfPrintSettings);

            Assert.IsTrue(result.IsSuccess, result.Message);
            ShellUtils.OpenUrl(outputFile);  // display it
        }



        /// <summary>
        /// Async Result Operation - to stream      
        /// </summary>        
        [TestMethod]
        public async Task PrintToPdfStreamAsyncTest()
        {
            var outputFile = Path.GetFullPath(@".\test3.pdf");
            var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");

            var host = new HtmlToPdfHost();
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                ShouldPrintHeaderAndFooter = true,
                HeaderTitle = "Blog Post Title",

                ScaleFactor = 0.9F,
                //ShouldPrintBackgrounds = false
                //PageRanges = "1-3,5-8"
                //ColorMode = WebViewColorMode.Monochrome // this is broken in WebView - always color
            };           
            host.CssAndScriptOptions.KeepTextTogether = true;            

            // We're interested in result.ResultStream
            var result = await host.PrintToPdfStreamAsync(htmlFile, pdfPrintSettings);

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.IsNotNull(result.ResultStream); // THIS

            // Copy resultstream to output file
            File.Delete(outputFile);
            using (var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                result.ResultStream.CopyTo(fstream);
                result.ResultStream.Close(); // Close returned stream!
            }
            ShellUtils.OpenUrl(outputFile);
        }

        /// <summary>
        /// Event callback on completion - to stream (in-memory)
        /// </summary>
        /// <remarks>
        /// Using async here only to facilitate waiting for completion.
        /// actual call does not require async calling method
        /// </remarks>
        [TestMethod]
        public async Task PrintToPdfStreamTest()
        {
            // File or URL
            var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");

            var tcs = new TaskCompletionSource<bool>();

            var host = new HtmlToPdfHost();
            Action<PdfPrintResult> onPrintComplete = (result) =>
            {
                if (result.IsSuccess)
                {
                    // create file so we can display
                    var outputFile = Path.GetFullPath(@".\test1.pdf");
                    File.Delete(outputFile);

                    using (var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        result.ResultStream.CopyTo(fstream);

                        result.ResultStream.Close(); // Close returned stream!                        
                        Assert.IsTrue(true);
                        ShellUtils.OpenUrl(outputFile);
                    }
                }
                else
                {
                    Assert.Fail(result.Message);
                }

                tcs.SetResult(true); 
            };
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                // default margins are 0.4F
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f,
                ScaleFactor = 0.8f,
                PageRanges = "1,2,5-8"
            };
            // doesn't wait for completion
            host.PrintToPdfStream(htmlFile, onPrintComplete, pdfPrintSettings) ;


            // wait for completion
            await tcs.Task;
        }

        /// <summary>
        /// Event callback on completion - to file
        /// </summary>
        /// <remarks>
        /// Using async here only to facilitate waiting for completion.
        /// actual call does not require async calling method
        /// </remarks>
        [TestMethod]
        public async Task PrintToPdfFileTest()
        {        
            // File or URL
            var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");
            // Full Path to output file
            var outputFile = Path.GetFullPath(@".\test.pdf");
            File.Delete(outputFile);

            var tcs = new TaskCompletionSource<bool>();

            var host = new HtmlToPdfHost();           
            
            Action<PdfPrintResult> onPrintComplete = (result) =>
            {
                if (result.IsSuccess)
                {                   
                    Assert.IsTrue(true);
                    ShellUtils.OpenUrl(outputFile);
                }
                else
                {
                    Assert.Fail(result.Message);
                }

                tcs.SetResult(true);
            };

            // doesn't wait for completion
            host.PrintToPdf(htmlFile,  outputFile, onPrintComplete);

            // wait for completion
            await tcs.Task;
        }


        [TestMethod]
        public async Task InjectedCssTest()
        {
            var outputFile = Path.GetFullPath(@".\test3.pdf");
            var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");

            var host = new HtmlToPdfHost();
            //host.CssAndScriptOptions.KeepTextTogether = true;
            host.CssAndScriptOptions.OptimizePdfFonts = true; // force built-in OS fonts (Segoe UI, apple-system, Helvetica) 
            host.CssAndScriptOptions.CssToInject = "h1 { color: red } h2 { color: green } h3 { color: goldenrod }";

            // We're interested in result.ResultStream
            var result = await host.PrintToPdfStreamAsync(htmlFile);

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.IsNotNull(result.ResultStream); // THIS

            // Copy resultstream to output file
            File.Delete(outputFile);
            using (var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                result.ResultStream.CopyTo(fstream);
                result.ResultStream.Close(); // Close returned stream!
            }
            ShellUtils.OpenUrl(outputFile);
        }
    }

}