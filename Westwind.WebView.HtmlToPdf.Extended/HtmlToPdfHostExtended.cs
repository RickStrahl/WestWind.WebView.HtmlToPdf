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
        /// <summary>
        /// Determines whether the PDF output includes a generated 
        /// TOC. The TOC is generated off H1-H6 
        /// (depending MaxTocOutlineLevel) elements in the base HTML
        /// </summary>
        public bool GenerateToc { get; set; } = true;

        /// <summary>
        /// The maximum TOC nesting level. Corresponds to H1-H6 
        /// values that are picked up for TOC composition.
        /// </summary>
        public int MaxTocOutlineLevel { get; set; } = 5;

        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Result is returned as a Memory Stream in
        /// result.ResultStream on success. 
        /// 
        /// Check result.IsSuccess to check for successful completion.
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>        
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param> 
        public override async Task<PdfPrintResult> PrintToPdfStreamAsync(string url, WebViewPrintSettings webViewPrintSettings = null)
        {        
            // Create the pdf
            var printResult = await base.PrintToPdfStreamAsync(url, webViewPrintSettings);
            if (!printResult.IsSuccess)
            {
                return printResult;
            }

            IList<HeaderItem> headerList = new List<HeaderItem>();
            if (GenerateToc)
                headerList = await CreateTocItems(url, MaxTocOutlineLevel);

            if (headerList.Count > 0)
            {                
                var bytes = AddTocToPdf(printResult.ResultStream, headerList);
                var ms = new MemoryStream(bytes);
                ms.Position = 0;
                printResult.ResultStream = ms;
            }

            return printResult;
        }



        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Check result.IsSuccess to check for 
        /// successful completion of the file output generation or use File.Exists()
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>
        /// <param name="outputFile">output file for generated PDF</param>
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param>
        public override async Task<PdfPrintResult> PrintToPdfAsync(string url, string outputFile, WebViewPrintSettings webViewPrintSettings = null)
        {
            var printResult = await PrintToPdfStreamAsync(url, webViewPrintSettings);
            if (!printResult.IsSuccess)
            {
                return printResult;
            }

            try
            {
                using (var fstream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    printResult.ResultStream.CopyTo(fstream);
                }

                printResult.ResultStream?.Close();
                printResult.ResultStream = null;
            }
            catch (Exception ex)
            {
                printResult.Message = $"Failed to write out PDF file: {ex.Message}";
                printResult.LastException = ex;
                printResult.IsSuccess = false;
            }            
            return printResult;
        }


        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF 
        /// using a new thread and a hosted form returning the result
        /// as an in-memory stream in result.ResultStream.
        /// 
        /// You get notified via onPrintComplete 'event' (Action) if
        /// you pass it in.
        /// </summary>
        /// <param name="url">The filename or URL to print to PDF</param>
        /// <param name="onPrintComplete">Optional action to fire when printing (or failure) is complete</param>
        /// <param name="webViewPrintSettings">PDF output options</param>
        public override void PrintToPdfStream(string url, Action<PdfPrintResult> onPrintComplete = null, WebViewPrintSettings webViewPrintSettings = null)
        {
        
            Action<PdfPrintResult> overriddenPrintComplete = async (printResult) => {

                try
                {
                    IList<HeaderItem> headerList = new List<HeaderItem>();
                    if (GenerateToc)
                        headerList = await CreateTocItems(url, MaxTocOutlineLevel);

                    if (headerList.Count > 0)
                    {
                        var bytes = AddTocToPdf(printResult.ResultStream, headerList);
                        var ms = new MemoryStream(bytes);
                        ms.Position = 0;
                        printResult.ResultStream = ms;
                    }
                }
                catch (Exception ex) { 
                    printResult.LastException=ex;
                    printResult.IsSuccess = false;  
                    printResult.Message=ex.Message;
                }

                onPrintComplete?.Invoke(printResult);
                
            };

            base.PrintToPdfStream(url,  overriddenPrintComplete, webViewPrintSettings);
        }


        public override void PrintToPdf(string url, string outputFile, Action<PdfPrintResult> onPrintComplete = null, WebViewPrintSettings webViewPrintSettings = null)
        {
            Action<PdfPrintResult> overriddenPrintComplete = async (printResult) =>
            {
                try
                {
                    IList<HeaderItem> headerList = new List<HeaderItem>();
                    if (GenerateToc)
                        headerList = await CreateTocItems(url, MaxTocOutlineLevel);

                    if (headerList.Count > 0)
                    {
                        var bytes = AddTocToPdf(printResult.ResultStream, headerList);
                        var ms = new MemoryStream(bytes);
                        ms.Position = 0;
                        printResult.ResultStream = ms;
                    }
                }
                catch (Exception ex)
                {
                    printResult.LastException = ex;
                    printResult.IsSuccess = false;
                    printResult.Message = ex.Message;
                }

                try
                {
                    File.Delete(outputFile);
                    using (var fstream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    {
                        printResult.ResultStream.CopyTo(fstream);
                    }
                    printResult.ResultStream?.Close();
                    printResult.ResultStream = null;
                }
                catch (Exception ex)
                {
                    printResult.LastException = ex;
                    printResult.IsSuccess = false;
                    printResult.Message = $"Failed to write out PDF file: {ex.Message}";
                }

                onPrintComplete?.Invoke(printResult);
            };

            base.PrintToPdfStream(url, overriddenPrintComplete, webViewPrintSettings);
        }


        #region Html Header Items Retrieval

        /// <summary>
        /// Parse HTML to retrieve H1-H6 elements and creates a list of nested 
        /// header items.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="maxOutlineLevel"></param>
        /// <returns></returns>
        private async Task<IList<HeaderItem>> CreateTocItems(string url, int maxOutlineLevel=6)
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

            headers = BuildHeaderListTree(headers);

            return headers;
        }

        /// <summary>
        /// Builds a nested list of HeaderItems from a previously created linear 
        /// list. Populates Children and creates a hierarchy that can be used
        /// to populate the PDF outliner.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private List<HeaderItem> BuildHeaderListTree(List<HeaderItem> headers)
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

#endregion

        #region PDF Toc Genaration (via PDFPig)


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pdfStream">Input stream that contains a PDF.</param>
        /// <param name="headerList">A previously generated header list from a URL or file</param>
        /// <returns></returns>
        private byte[] AddTocToPdf(Stream pdfStream, IList<HeaderItem> headerList)
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

        /// <summary>
        /// Adds new TOC nodes including the nesting hierarcy
        /// </summary>
        /// <param name="topLevelHeaderItem"></param>
        /// <param name="pageLinkList"></param>
        /// <returns></returns>
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

        #endregion
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
