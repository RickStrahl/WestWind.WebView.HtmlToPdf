using System.Diagnostics;
using System.IO;
using Westwind.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using WestWind.HtmlToPdf;
using Westwind.HtmlToPdfPlaywright;

namespace Westwind.PdfToHtml.Test
{
    
    [TestClass]
    public class PrintToPdfPlaywrightTests
    {
        [TestMethod]
        public async Task PrintToPdfFileTest()
        {
            var outputFile = @"c:\temp\test2.pdf";
            var htmlFile = "C:/Temp/Temp_Local/_MarkdownMonster_Preview.html";
            File.Delete(outputFile);

            var pdfGen = new HtmlToPdfHostPlaywright();

            await pdfGen.Convert(htmlFile, outputFile);

            Assert.IsTrue(File.Exists(outputFile));
            ShellUtils.GoUrl(outputFile);

            

        }            
    }
}