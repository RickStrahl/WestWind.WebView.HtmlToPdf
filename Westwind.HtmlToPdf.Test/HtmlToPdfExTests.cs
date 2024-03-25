//using System.Diagnostics;
//using System.IO;
//using Westwind.Utilities;
//using Westwind.WebView.HtmlToPdf;

//namespace Westwind.HtmlToPdf.Test
//{

//    [TestClass]
//    public class HtmlToPdfTests
//    {

//        /// <summary>
//        /// Event callback on completion - to file
//        /// </summary>
//        /// <remarks>
//        /// Using async here only to facilitate waiting for completion.
//        /// actual call does not require async calling method
//        /// </remarks>
//        [TestMethod]
//        public async Task PrintToPdfFileTest()
//        {
//            var outputFile = Path.GetFullPath(@".\test.pdf");

//            // File or URL
//            var htmlFile = Path.GetFullPath("HtmlSampleFile-SelfContained.html");

//            File.Delete(outputFile);

//            var host = new HtmlToPdfHostEx();
//            bool isComplete = false;
            
//            host.OnPrintCompleteAction = (result) =>
//            {
//                if (result.IsSuccess)
//                {
//                    ShellUtils.OpenUrl(outputFile);
//                    Assert.IsTrue(true);
//                    isComplete = true;
//                }
//                else
//                {
//                    Assert.Fail(result.Message);

//                    isComplete = true;
//                }

//            };
//            var pdfPrintSettings = new WebViewPdfPrintSettings()
//            {
//                MarginBottom = 0.2F,
//                MarginLeft = 0.2f,
//                MarginRight = 0.2f,
//                MarginTop = 0.4f,
//                ScaleFactor = 0.8f,
//                ColorMode = "Grayscale",    // this doesn't work: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4445
//                ShouldPrintBackgrounds = false,
//                ShouldPrintHeaderandFooter = false,
//            };
//            host.PrintToPdf(htmlFile, outputFile,pdfPrintSettings);

//            while(!isComplete)
//            {
//                await Task.Delay(100);
//            }

//        }

//   }

//}