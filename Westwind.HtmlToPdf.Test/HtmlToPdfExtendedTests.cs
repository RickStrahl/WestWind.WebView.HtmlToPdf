#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;
using Westwind.Utilities;
using Westwind.WebView.HtmlToPdf;

namespace Westwind.HtmlToPdf.Test
{
    [TestClass]
    public class HtmlToPdfExtendedTests
    {
        public string SampleHtml { get; set; } = Path.GetFullPath("HtmlSampleFile-SelfContained.html");
        public string SamplePdf { get; set; } = Path.GetFullPath("PdfSampleFile.pdf");
        public string SamplePdf_Outline { get; set; } = Path.GetFullPath("PdfSampleFile_1.pdf");

        [TestMethod]
        public async Task PrintPdfStreamAsyncExtendedTest()
        {
            var pdf = new HtmlToPdfHostExtended();
            var result = await pdf.PrintToPdfStreamAsync(SampleHtml, new WebViewPrintSettings
            {
                ScaleFactor = 1F
            }) ;

            File.Delete(SamplePdf_Outline);
            using (var fstream = new FileStream(SamplePdf_Outline, FileMode.OpenOrCreate, FileAccess.Write))
            {
                result.ResultStream.CopyTo(fstream);
                result.ResultStream.Close(); // Close returned stream!

                ShellUtils.OpenUrl(SamplePdf_Outline);
            }
            Assert.IsNotNull(result,result.Message);
            ShellUtils.OpenUrl(SamplePdf_Outline);
        }



        [TestMethod]
        public async Task PrintPdfAsyncFromUrlExtendedTest()
        {
            var outputFile = SamplePdf_Outline.Replace("_1", "_2");

            var pdf = new HtmlToPdfHostExtended() { MaxTocOutlineLevel = 3 };
            var result = await pdf.PrintToPdfAsync(SampleHtml, outputFile);

            Assert.IsTrue(result.IsSuccess,result.Message);

            ShellUtils.OpenUrl(outputFile);
        }


        [TestMethod]
        public async Task PrintPdfStreamExtendedTest()
        {
            var outputFile = SamplePdf_Outline.Replace("_1", "_3");

            var pdf = new HtmlToPdfHostExtended();            
            var tcs = new TaskCompletionSource();

            var onPrintComplete = (PdfPrintResult result) =>
            {
                Assert.IsTrue(result.IsSuccess, result.Message);

                File.Delete(outputFile);
                using (var fstream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    result.ResultStream.CopyTo(fstream);
                }
                result.ResultStream.Close(); // Close returned stream!    
                ShellUtils.OpenUrl(outputFile);

                tcs.SetResult();
            };        

            pdf.PrintToPdfStream(SampleHtml, onPrintComplete, new WebViewPrintSettings
            {
                ScaleFactor = 1F
            });

            await tcs.Task;
        }


        [TestMethod]
        public async Task PrintPdfExtendedTest()
        {
            var outputFile = SamplePdf_Outline.Replace("_1", "_4");

            var pdf = new HtmlToPdfHostExtended();
            var tcs = new TaskCompletionSource();
            File.Delete(outputFile);

            var onPrintComplete = (PdfPrintResult result) =>
            {
                Assert.IsTrue(result.IsSuccess, result.Message);
                ShellUtils.OpenUrl(outputFile);

                tcs.SetResult();
            };

            pdf.PrintToPdf(SampleHtml, outputFile, onPrintComplete, new WebViewPrintSettings
            {
                ScaleFactor = 1F
            });

            await tcs.Task;
        }


    }

}
#endif