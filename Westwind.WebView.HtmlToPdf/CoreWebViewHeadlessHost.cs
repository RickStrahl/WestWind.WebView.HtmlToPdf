using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using Westwind.WebView.HtmlToPdf.Utilities;


namespace Westwind.WebView.HtmlToPdf
{

    /// <summary>
    /// This class provides the invisible WebView instance used to
    /// print the PDF. 
    /// </summary>
    internal class CoreWebViewHeadlessHost
    {
        /// <summary>
        /// The internal print settings picked up from the passed in host
        /// </summary>
        internal WebViewPrintSettings WebViewPrintSettings { get; set; } = new WebViewPrintSettings();

        private string _outputFile { get; set; }

        /// <summary>
        /// Passed in high level host
        /// </summary>
        internal HtmlToPdfHost HtmlToPdfHost { get; set; }

        internal bool IsSuccess { get; set; } = false;

        internal Exception LastException { get; set; }

        internal Stream ResultStream { get; set; }

        /// <summary>
        /// Determines when PDF output generation is complete
        /// </summary>
        internal bool IsComplete { get; set; }

        /// <summary>
        /// The internal WebView instance we load and print from
        /// </summary>
        CoreWebView2 WebView { get; set; }

        private PdfPrintOutputModes PdfPrintOutputMode { get; set; } = PdfPrintOutputModes.File;

        protected TaskCompletionSource<bool> IsInitializedTaskCompletionSource = new TaskCompletionSource<bool>(); 

        internal CoreWebViewHeadlessHost(HtmlToPdfHost htmlToPdfHost)
        {
            HtmlToPdfHost = htmlToPdfHost;
            WebViewPrintSettings = htmlToPdfHost.WebViewPrintSettings;
            InitializeAsync();
        }

        private IntPtr HWND_MESSAGE = new IntPtr(-3);

        protected async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: HtmlToPdfHost.WebViewEnvironmentPath);

            var controller = await environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE); 
            
            WebView = controller.CoreWebView2;                        
            WebView.DOMContentLoaded += CoreWebView2_DOMContentLoaded;

            // Ensure that control is initialized before we can navigate!
            IsInitializedTaskCompletionSource.SetResult(true);            
        }


        
        /// <summary>
        /// Internally navigates the the browser to the document to render
        /// </summary>
        /// <param name="url"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        internal async Task PrintFromUrl(string url, string outputFile)
        {
            await IsInitializedTaskCompletionSource.Task;

            PdfPrintOutputMode = PdfPrintOutputModes.File;
            _outputFile = outputFile;
            WebView.Navigate(url);
        }

        /// <summary>
        /// Internally navigates t
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task PrintFromUrlStream(string url)
        {
            // Can't navigate until initialized
            await IsInitializedTaskCompletionSource.Task;

            PdfPrintOutputMode = PdfPrintOutputModes.Stream;
            WebView.Navigate(url);
        }


        private async void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            try
            {
                await InjectCssAndScript();
                
                if (PdfPrintOutputMode == PdfPrintOutputModes.File)
                    await PrintToPdf();
                else
                    await PrintToPdfStream();
            }
            finally
            {
                IsComplete = true;
                HtmlToPdfHost.IsCompleteTaskCompletionSource.SetResult(true);
            }
        }

        private async Task InjectCssAndScript()
        {
            string css = null;
            if (HtmlToPdfHost.CssAndScriptOptions.KeepTextTogether)
            {
                css = PageBreakCss;
            }
            if (!string.IsNullOrEmpty(HtmlToPdfHost.CssAndScriptOptions.CssToInject))
                css += "\n" + HtmlToPdfHost.CssAndScriptOptions.CssToInject;
            if (!string.IsNullOrEmpty(css))
            {
                var script = "document.head.appendChild(document.createElement('style')).innerHTML = " + StringUtils.ToJsonString(css) + ";";
                await WebView.ExecuteScriptAsync(script);
            }
        }

        internal async Task PrintToPdf()
        {
            var webViewPrintSettings = SetWebViewPrintSettings();

            if (File.Exists(_outputFile))
                File.Delete(_outputFile);

            try
            {
                if (File.Exists(_outputFile))
                    File.Delete(_outputFile);

                await WebView.PrintToPdfAsync(_outputFile, webViewPrintSettings);

                if (File.Exists(_outputFile))
                    IsSuccess = true;
                else
                    IsSuccess = false;
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                LastException = ex;
            }
        }

        /// <summary>
        /// Prints the current document in the WebView to a MemoryStream
        /// </summary>
        /// <returns></returns>
        internal async Task<Stream> PrintToPdfStream()
        {
            var webViewPrintSettings = SetWebViewPrintSettings();

            try
            {
                // we have to turn the stream into something physical because the form won't stay alive
                using (var stream = await WebView.PrintToPdfStreamAsync(webViewPrintSettings))
                {
                    var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;
                    ResultStream = ms;
                    IsSuccess = true;
                    return ResultStream;
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                LastException = ex;
                return null;
            }
        }

        /// <summary>
        /// Map our private type to the CoreWebView type.                
        /// </summary>
        /// <returns></returns>

        private CoreWebView2PrintSettings SetWebViewPrintSettings()
        {
            var wvps = WebView.Environment.CreatePrintSettings();

            var ps = WebViewPrintSettings;

            wvps.ScaleFactor = ps.ScaleFactor;
            wvps.MarginTop = ps.MarginTop;
            wvps.MarginBottom = ps.MarginBottom;
            wvps.MarginLeft = ps.MarginLeft;
            wvps.MarginRight = ps.MarginRight;

            wvps.PageWidth = ps.PageWidth;
            wvps.PageHeight = ps.PageHeight;

            wvps.Copies = ps.Copies;
            wvps.PageRanges = ps.PageRanges;

            wvps.ShouldPrintBackgrounds = ps.ShouldPrintBackgrounds;

            wvps.ShouldPrintHeaderAndFooter = ps.ShouldPrintHeaderAndFooter;
            wvps.HeaderTitle = ps.HeaderTitle;
            wvps.FooterUri = ps.FooterUri;

            wvps.ShouldPrintSelectionOnly = ps.ShouldPrintSelectionOnly;
            wvps.Orientation = ps.Orientation == WebViewPrintOrientations.Portrait ? CoreWebView2PrintOrientation.Portrait : CoreWebView2PrintOrientation.Landscape;
            wvps.Duplex = ps.Duplex == WebViewPrintDuplexes.Default ? CoreWebView2PrintDuplex.Default :
                ps.Duplex == WebViewPrintDuplexes.OneSided ? CoreWebView2PrintDuplex.OneSided :
                ps.Duplex == WebViewPrintDuplexes.TwoSidedLongEdge ? CoreWebView2PrintDuplex.TwoSidedLongEdge :
                CoreWebView2PrintDuplex.TwoSidedShortEdge;
            wvps.Collation = ps.Collation == WebViewPrintCollations.Default ? CoreWebView2PrintCollation.Default :
                    ps.Collation == WebViewPrintCollations.Collated ? CoreWebView2PrintCollation.Collated :
                    CoreWebView2PrintCollation.Uncollated;
            wvps.ColorMode = ps.ColorMode == WebViewPrintColorModes.Color ? CoreWebView2PrintColorMode.Color : CoreWebView2PrintColorMode.Grayscale;


            wvps.PrinterName = ps.PrinterName;
            wvps.PagesPerSide = ps.PagesPerSide;

            return wvps;
        }


        string PageBreakCss { get; } = @"
@media print {

    html, body {
       text-rendering: optimizeLegibility;
       height: auto;
    }

    pre {
       white-space: pre-wrap;
       word-break: normal;
       word-wrap: normal;
    }
    pre > code {
        white-space: pre-wrap;
        padding: 1em !important;
    }

    /* keep paragraphs together */
    p, li, ul, code, pre {
       page-break-inside: avoid;
       break-inside: avoid;
    }

    /* keep headers and content together */
    h1, h2, h3, h4, h5, h6 {
       page-break-after: avoid;
       break-after: avoid;
    }                              
}
";
        string OptimizedFontCss { get; } = 
            @"font-family: ""Segoe UI Emoji"", ""Apple Color Emoji"", -apple-system, BlinkMacSystemFont,""Segoe UI"", Helvetica, Helvetica, Arial, sans-serif;";
    

        }
    }
