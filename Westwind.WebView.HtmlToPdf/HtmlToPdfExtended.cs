using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig;
using Westwind.Utilities;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Xml.Linq;
using System.Reflection;

namespace Westwind.WebView.HtmlToPdf
{
    public class HtmlToPdfExtended : HtmlToPdfHost
    {
        public override async Task<PdfPrintResult> PrintToPdfStreamAsync(string url, WebViewPrintSettings webViewPrintSettings = null)
        {
            // Create header outline
            var headerList = await CreateTocItems(url);
            

            // Create the pdf
            var printResult = await base.PrintToPdfStreamAsync(url, webViewPrintSettings);

            if (headerList.Count > 0)
            {                
                var bytes = AddTocToPdf(printResult.ResultStream, headerList);
                var ms = new MemoryStream(bytes);
                ms.Position = 0;
                printResult.ResultStream = ms;
            }

            return printResult;
        }

        public async Task<IList<HeaderItem>> CreateTocItems(string url, int maxOutlineLevel=6)
        {
            var list = new List<HeaderItem>();
            string html = null;
            if (url.StartsWith("https:") || url.StartsWith("http:"))
            {
                html = await HttpUtils.HttpRequestStringAsync(url);
            }
            else
            {
                if (!File.Exists(url))
                {
                    return list;
                }
                html = File.ReadAllText(url);
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);


            var xpath = "//*[self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6]";
            var nodes = doc.DocumentNode.SelectNodes(xpath);

            // nothing to do
            if (nodes == null)
                return null;


            var headers = new List<HeaderItem>();
            
            var root  = new HeaderItem { Level = 0 };  // root
            HeaderItem lastHeaderItem = root;
            HeaderItem parentHeaderItem = lastHeaderItem;
            foreach (var node in nodes)
            {                
                var text = node.InnerText.Trim();
                var textIndent = node.Name.Replace("h", "");                
                if (!int.TryParse(textIndent, out int level) || level > maxOutlineLevel)
                    continue;

                var headerItem = new HeaderItem { Level = level, Text = text };
                headers.Add(headerItem);         
            }

            return headers;
        }

        




        public byte[] AddTocToPdf(Stream pdfStream, IList<HeaderItem> headerList)
        {
            var builder = new PdfDocumentBuilder();

            var pageLinkList = new List<PageLinkItem>();

            using (var pdf = PdfDocument.Open(pdfStream))
            {
                int count = 0;
                var existingPages = pdf.GetPages();
                foreach (var page in existingPages)
                {
                    count++;

                    //var page.Text;
                    // Either extract based on order in the underlying document with newlines and spaces.
                    var text = ContentOrderTextExtractor.GetText(page).Replace("\r\n", " ").Replace("\n"," ").Replace("\r"," ");
                    pageLinkList.Add( new PageLinkItem { Text = text, PageIndex = count });

                    // Create new document with existing pages
                    builder.AddPage(pdf, count);
                    
                }

                // now add bookmarks
                var bookmarkList = new List<DocumentBookmarkNode>();

                foreach(var headerItem in headerList)
                {
                    var pageLinkItem = pageLinkList.FirstOrDefault(pll => pll.Text.Contains(headerItem.Text ));
                    if (pageLinkItem == null) continue;
                    
                    var node = new DocumentBookmarkNode(headerItem.Text,  
                        headerItem.Level,
                        new ExplicitDestination(pageLinkItem.PageIndex, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
                        Array.Empty<BookmarkNode>());
                    bookmarkList.Add(node);
                }                
                builder.Bookmarks = new Bookmarks(bookmarkList);

                return builder.Build();                
            }            

        }

    }

    [DebuggerDisplay("{Level} - {Text}")]
    public class HeaderItem
    {
        public string Text { get; set; }

        public int Level { get; set; }

        public int Page { get; set; }

        public HeaderItem Parent { get; set; }

        public List<HeaderItem> Children { get; set; } = new List<HeaderItem>();
    }

    public class PageLinkItem
    {
        public int PageIndex { get; set; }
        public string Text { get; set; }
    }
}
