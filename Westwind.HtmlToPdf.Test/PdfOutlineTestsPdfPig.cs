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
    public class PdfPigTests
    {
        public string SampleHtml { get; set; } = Path.GetFullPath("HtmlSampleFile-SelfContained.html");
        public string SamplePdf { get; set; } = Path.GetFullPath("PdfSampleFile.pdf");
        public string SamplePdf_Outline { get; set; } = Path.GetFullPath("PdfSampleFile_1.pdf");

        [TestMethod]
        public async Task PrintPdfStreamAsyncExtendedTest()
        {
            var pdf = new HtmlToPdfExtended();
            var result = await pdf.PrintToPdfStreamAsync(SampleHtml);

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
        public void PdfPigTest()
        {





            File.Delete(SamplePdf_Outline);
            var pages = new List<Page>();
            var builder = new PdfDocumentBuilder();

            using (var pdf = PdfDocument.Open(SamplePdf))
            {
                int count = 0;
                var existingPages = pdf.GetPages();
                foreach (var page in existingPages)
                {
                    
                    // Either extract based on order in the underlying document with newlines and spaces.
                    var text = ContentOrderTextExtractor.GetText(page);

                    var marked = page.GetMarkedContents();

                    // Or based on grouping letters into words.
                    var otherText = string.Join(" ", page.GetWords());

                    // Or the raw text of the page's content stream.
                    var rawText = page.Text;

                    count++;
                    Console.WriteLine("\nPage " + count);
                    Console.WriteLine("--------------------------");
                    Console.WriteLine(text);

                    builder.AddPage(pdf, count);
                }

                // Create Table of Contents
                var node = new DocumentBookmarkNode("Chapter 1", 1,
                    new ExplicitDestination(1, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty), 
                    Array.Empty<BookmarkNode>());

                builder.Bookmarks = new Bookmarks(new List<DocumentBookmarkNode>() { node });
                byte[] documentBytes = builder.Build();

                File.WriteAllBytes(SamplePdf_Outline, documentBytes);
            }

            ShellUtils.OpenUrl(SamplePdf_Outline);
        }
    }

}
#endif