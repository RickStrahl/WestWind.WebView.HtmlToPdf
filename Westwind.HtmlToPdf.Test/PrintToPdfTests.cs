using System.Diagnostics;
using System.IO;
using Westwind.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using WestWind.HtmlToPdf;

namespace Westwind.PdfToHtml.Test
{
    
    [TestClass]
    public class PrintToPdfTests
    {
        [TestMethod]
        public async Task PrintToPdfFileTest()
        {
            var outputFile = @"c:\temp\test2.pdf";
            var htmlFile = "C:/Temp/Temp_Local/_MarkdownMonster_Preview.html";

            File.Delete(outputFile);

            var host = new PdfPrintHost();
            host.WebViewPrintSettings = new WebViewPrintSettings()
            {         
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f,
                ScaleFactor = 0.75f,                
                ShouldPrintHeaderandFooter = false,                
            };
            host.OnPrintCompleteAction = (result) =>
            {
                if (result.IsSuccess)
                {
                    ShellUtils.GoUrl(outputFile);
                    Assert.IsTrue(true);
                }
                else
                {
                    Assert.Fail(result.Message);
                }
            };
            host.PrintToPdf(htmlFile, outputFile);
            
            for (int i = 0; i < 50; i++)
            {
                if (host.IsComplete)
                    return;

                await Task.Delay(100);
            }

            Assert.Fail ("Document did not complete in time.");
        }

        
        [TestMethod]
        public async Task PrintToPdfAsyncTest()
        {
            // We have to force the thread to be STA for the
            // async, non-event version to work so this setup is
            // a bit ugly
            var thread = new Thread(PrintToPdfAsyncTest_Run);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var testComplete = false;

            async void PrintToPdfAsyncTest_Run()
            {
                var outputFile = @"c:\temp\test2.pdf";
                var htmlFile = "file:///C:/Temp/Temp_Local/_MarkdownMonster_Preview.html";

                File.Delete(outputFile);

                var host = new PdfPrintHost();
                host.WebViewPrintSettings = new WebViewPrintSettings()
                {
                    HeaderTitle = "Markdown Monster",
                    MarginBottom = 0.2F,
                    MarginLeft = 0.2f,
                    MarginRight = 0.2f,
                    MarginTop = 0.4f,
                    ScaleFactor = 1,
                    ShouldPrintHeaderandFooter = true,
                    ColorMode = "Grayscale",
                    FooterUri = "https://west-wind.com"
                };
                var result = await host.PrintToPdfAsync(htmlFile, outputFile);

                Assert.IsTrue(result.IsSuccess, result.Message);
                ShellUtils.GoUrl(outputFile);
                
                testComplete = true;
            }

            for (int i = 0; i < 100; i++)
            {
                if (testComplete)
                    return;

                await Task.Delay(100);
            }            
        }

        


        [TestMethod]
        public async Task PrintToPdfStreamAsyncTest()
        {
            // We have to force the thread to be STA for the
            // async, non-event version to work so this setup is
            // a bit ugly
            var thread = new Thread(PrintToPdfStreamAsyncTest_Run);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var testComplete = false;
            async void PrintToPdfStreamAsyncTest_Run()
            {
                var outputFile = @"c:\temp\test2.pdf";
                var htmlFile = "file:///C:/Temp/Temp_Local/_MarkdownMonster_Preview.html";

                File.Delete(outputFile);

                var host = new PdfPrintHost();
                host.WebViewPrintSettings = new WebViewPrintSettings()
                {
                    HeaderTitle = "Markdown Monster",
                    MarginBottom = 0.2F,
                    MarginLeft = 0.2f,
                    MarginRight = 0.2f,
                    MarginTop = 0.4f,
                    ScaleFactor = 1,
                    ShouldPrintHeaderandFooter = true,
                    ColorMode = "Grayscale",
                    FooterUri = "https://west-wind.com"
                };
                var result = await host.PrintToPdfStreamAsync("https://markdownmonster.west-wind.com"); // htmlFile);

                Assert.IsTrue(result.IsSuccess, result.Message);
                Assert.IsNotNull(result.ResultStream);

                Debug.WriteLine($"Stream Length: {result.ResultStream.Length}");

                // Copy resultstream to output file
                using var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write);
                result.ResultStream.CopyTo(fstream);
                result.ResultStream.Close();

                ShellUtils.GoUrl(outputFile);
                testComplete = true;
            }

            for (int i = 0; i < 100; i++)
            {
                if (testComplete)
                    return;

                await Task.Delay(100);
            }
        }

        private async void PrintToPdfStreamAsyncTest_Run()
        {
            var outputFile = @"c:\temp\test2.pdf";
            var htmlFile = "file:///C:/Temp/Temp_Local/_MarkdownMonster_Preview.html";

            File.Delete(outputFile);

            var host = new PdfPrintHost();
            host.WebViewPrintSettings = new WebViewPrintSettings()
            {
                HeaderTitle = "Markdown Monster",
                MarginBottom = 0.2F,
                MarginLeft = 0.2f,
                MarginRight = 0.2f,
                MarginTop = 0.4f,
                ScaleFactor = 1,
                ShouldPrintHeaderandFooter = true,
                ColorMode = "Grayscale",
                FooterUri = "https://west-wind.com"
            };
            var result = await  host.PrintToPdfStreamAsync(htmlFile);

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.IsNotNull(result.ResultStream);

            Debug.WriteLine($"Stream Length: {result.ResultStream.Length}");
           
        }
    }
}