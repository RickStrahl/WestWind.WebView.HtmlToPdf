//#if !NETFRAMEWORK

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection.Metadata;

//using MigraDoc.Rendering;
//using PdfSharp.Drawing;
//using PdfSharp.Pdf;
//using PdfSharp.Pdf.Content;
//using PdfSharp.Pdf.Content.Objects;
//using PdfSharp.Pdf.IO;


//namespace Westwind.WebView.HtmlToPdf
//{
//    public class PdfDocumentOutline
//    {

//        public bool AddOutlineToDocument(string filename)
//        {
//            var document = PdfReader.Open(filename, PdfDocumentOpenMode.Modify);

//            int count = 0;
//            foreach(var page  in document.Pages)
//            {
//                var content = page.Contents;
//                CObject contents = ContentReader.ReadContent(page);
//                var extractedText = ExtractText(contents);
//                Debug.WriteLine(contents);


//                // Create the outline (bookmarks)
//                PdfOutline outline = document.Outlines.Add("Page " + (count + 1), document.Pages[count], true, PdfOutlineStyle.Bold, XColor.FromKnownColor(XKnownColor.SteelBlue));

//                // Add sub-entries if needed
//                PdfOutline subOutline = outline.Outlines.Add($"Section {count + 1}.1", document.Pages[count], true);

//                count++;
//            }

            
//            // Save the document
//            document.Save(filename);

//            return true;
//        }


//        public static IEnumerable<string> ExtractText(CObject cObject)
//        {
//            if (cObject is COperator)
//            {
//                var cOperator = cObject as COperator;
//                if (cOperator.OpCode.Name == OpCodeName.Tj.ToString() ||
//                    cOperator.OpCode.Name == OpCodeName.TJ.ToString())
//                {
//                    foreach (var cOperand in cOperator.Operands)
//                    foreach (var txt in ExtractText(cOperand))
//                        yield return txt;
//                }
//            }
//            else if (cObject is CSequence)
//            {
//                var cSequence = cObject as CSequence;
//                foreach (var element in cSequence)
//                foreach (var txt in ExtractText(element))
//                    yield return txt;
//            }
//            else if (cObject is CString)
//            {
//                var cString = cObject as CString;
//                yield return cString.Value;
//            }
//        }
//    }
//}
//#endif