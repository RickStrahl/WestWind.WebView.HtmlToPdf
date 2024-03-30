using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig;
using Westwind.Utilities;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Westwind.WebView.HtmlToPdf
{
    public class HtmlToPdfHostExtended : HtmlToPdfHost
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
            foreach (var node in nodes)
            {                
                var text = node.InnerText.Trim();
                var textIndent = node.Name.Replace("h", "");                
                if (!int.TryParse(textIndent, out int level) || level > maxOutlineLevel)
                    continue;

                var headerItem = new HeaderItem { Level = level, Text = text };
                headers.Add(headerItem);         
            }

            headers = BuildTree(headers);

            return headers;
        }

        public List<HeaderItem> BuildTree(List<HeaderItem> headers)
        {
            HeaderItem rootItem = new HeaderItem();

            int lastIndex = 0;

            for(int i = 0; i < headers.Count; i++)
            {
                var item = headers[i];
                var lastItem = headers[lastIndex];

                if (item.Level == 1)
                {
                    rootItem.Children.Add(item);
                    item.Parent = rootItem;
                }
                else if (item.Level == lastItem.Level)
                {
                    if (lastItem.Parent == null)
                    {
                        rootItem.Children.Add(item);
                        item.Parent = rootItem;
                    }
                    else
                    {
                        lastItem.Parent.Children.Add(item);
                        item.Parent = headers[lastIndex].Parent;
                    }
                }
                else if (item.Level > lastItem.Level)
                {
                        lastItem.Children.Add(item);
                        item.Parent = headers[lastIndex];                
                }
                else if (item.Level < lastItem.Level)
                {
                    if (lastItem.Parent == null)
                    {
                        rootItem.Children.Add(item);
                        item.Parent = rootItem;
                    }
                    else
                    {

                        var parent = lastItem.Parent;
                        while (parent != null && item.Level < parent.Level)
                        {
                            parent = parent.Parent;
                        }

                        parent.Parent.Children.Add(item);
                        item.Parent = parent.Parent;
                    }
                }


                lastIndex = i;
            }

            return rootItem.Children;
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

                    var childList = AddChildren(headerItem, pageLinkList);

                    var node = new DocumentBookmarkNode(headerItem.Text,  
                        headerItem.Level,
                        new ExplicitDestination(pageLinkItem.PageIndex, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
                        childList);
                    

                    bookmarkList.Add(node);
                }   
                


                builder.Bookmarks = new Bookmarks(bookmarkList);

                return builder.Build();                
            }            

        }

        List<BookmarkNode> AddChildren(HeaderItem topLevelHeaderItem, List<PageLinkItem> pageLinkList)
        {            
            var list = new List<BookmarkNode>();

            foreach(var headerItem in topLevelHeaderItem.Children)
            {
                List<BookmarkNode> childList = new List<BookmarkNode>();
                if (headerItem.Children.Count > 0)
                {                    
                    childList = AddChildren(headerItem, pageLinkList);
                }

                var pageLinkItem = pageLinkList.FirstOrDefault(pll => pll.Text.Contains(headerItem.Text));
                if (pageLinkItem == null) continue;

                var node = new DocumentBookmarkNode(headerItem.Text,
                    headerItem.Level,
                    new ExplicitDestination(pageLinkItem.PageIndex, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
                    childList);
                list.Add(node);
            }

            return list;
        }



    

    }
    
    public class HeaderItem
    {
        public string Text { get; set; }

        public int Level { get; set; }

        public int Page { get; set; }

        public HeaderItem Parent { get; set; }

        public List<HeaderItem> Children { get; set; } = new List<HeaderItem>();

        public override string ToString()
        {
            return $"{Level} - {Text} - {Children.Count}";
        }
    }


    
    public class PageLinkItem
    {
        public int PageIndex { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return $"{PageIndex} - {Text}";
        }
    }
}
