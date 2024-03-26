using System.Diagnostics;
using System.IO;
using Westwind.Utilities;
using Westwind.WebView.HtmlToPdf;

namespace Westwind.PdfToHtml.Test
{

    [TestClass]
    public class PrintToPdfTests
    {
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
            var outputFile = Path.GetFullPath(@".\test.pdf");

            // File or URL
            var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");

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
                    Assert.Fail(result.Message);
                }
            };
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                // All margins default 0.4F
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f, 

                ScaleFactor = 0.8f,
                ColorMode = WebViewPrintColorModes.Grayscale,    // this doesn't work: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4445
                ShouldPrintBackgrounds = true,
                ShouldPrintHeaderAndFooter = true,                
                HeaderTitle = "Blog Post",
                FooterUri = "https://west-wind.com"
                //PageRanges = "3-5"
            };
            host.PrintToPdf(htmlFile, outputFile, pdfPrintSettings);

            // have to wait for completion of event callback
            for (int i = 0; i < 50; i++)
            {
                if (host.IsComplete)
                    return;

                await Task.Delay(100);
            }

            Assert.Fail("Document did not complete in time.");
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

            var host = new HtmlToPdfHost();
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
                ColorMode = WebViewPrintColorModes.Grayscale,    // this doesn't work: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4445
                ShouldPrintBackgrounds = false,
                ShouldPrintHeaderAndFooter = false,
            };
            host.PrintToPdfStream(htmlFile, pdfPrintSettings);

            for (int i = 0; i < 50; i++)
            {
                if (host.IsComplete)
                    return;

                await Task.Delay(100);
            }

            Assert.Fail("Document did not complete in time.");
        }


        /// <summary>
        /// Async Result operation - to file
        /// </summary>
        [TestMethod]
        public async Task PrintToPdfFileAsyncTest()
        {
            var outputFile = Path.GetFullPath(@".\test2.pdf");
            var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");

            File.Delete(outputFile);

            var host = new HtmlToPdfHost();
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                HeaderTitle = "Markdown Monster",
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f,
                ScaleFactor = 0.8F,
                ShouldPrintHeaderAndFooter = false,
                ColorMode = WebViewPrintColorModes.Grayscale,  // this doesn't work   
                FooterUri = "https://west-wind.com"
            };
            var result = await host.PrintToPdfAsync(htmlFile, outputFile, pdfPrintSettings);

            Assert.IsTrue(result.IsSuccess, result.Message);
            ShellUtils.OpenUrl(outputFile);
        }



        /// <summary>
        /// Async Result Operation - to stream      
        /// </summary>        
        [TestMethod]
        public async Task PrintToPdfStreamAsyncTest()
        {
            var outputFile = Path.GetFullPath(@".\test3.pdf");
            var htmlFile = Path.GetFullPath("HtmlSampleFileLonger-SelfContained.html");

            File.Delete(outputFile);

            var host = new HtmlToPdfHost();
            var pdfPrintSettings = new WebViewPrintSettings()
            {
                HeaderTitle = "Markdown Monster",
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f,
                ScaleFactor = 1,
                ShouldPrintHeaderAndFooter = false,
                ColorMode = WebViewPrintColorModes.Grayscale,
                FooterUri = "https://west-wind.com"
            };

            // We're interested in result.ResultStream
            var result = await host.PrintToPdfStreamAsync(htmlFile, pdfPrintSettings);

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.IsNotNull(result.ResultStream); // THIS

            Debug.WriteLine($"Stream Length: {result.ResultStream.Length}");

            // Copy resultstream to output file
            File.Delete(outputFile);

            using var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write);
            result.ResultStream.CopyTo(fstream);

            result.ResultStream.Close(); // Close returned stream!

            ShellUtils.OpenUrl(outputFile);
        }
    }

}