#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Utilities;
using Westwind.WebView.HtmlToPdf;

namespace Westwind.HtmlToPdf.Test
{
    [TestClass]
    public class PdfOutlineTests
    {
        public string SamplePdf { get; set; } = Path.GetFullPath("PdfSampleFile.pdf");
        public string SamplePdf_Outline { get; set; } = Path.GetFullPath("PdfSampleFile_1.pdf");

        [TestMethod]
        public void AddTocToPdfTest()
        {
        }

    }

    

}
#endif