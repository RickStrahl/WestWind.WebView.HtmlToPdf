using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Westwind.WebView.HtmlToPdf
{

    /// <summary>
    /// IMPORTANT: DOES NOT WORK but should according to WebView docs
    /// https://github.com/MicrosoftEdge/WebView2Feedback/issues/202
    /// 
    /// 
    /// This is Headless Host 
    /// </summary>
    public class CoreWebViewHeadlessHost
    {
        public WebViewPrintSettings WebViewPrintSettings { get; set; } = new WebViewPrintSettings();

        private string _outputFile { get; set; }

        public HtmlToPdfHost HtmlToPdfHost { get; set; }

        public bool IsSuccess { get; set; } = false;

        public Exception LastException { get; set; }

        public Stream ResultStream { get; set; }

        public bool IsComplete { get; set; }

        CoreWebView2 WebView { get; set; }

        private PdfPrintOutputModes PdfPrintOutputMode { get; set; } = PdfPrintOutputModes.File;

        private bool _IsInitialized = false;

        public string UrlOrFile { get; set;  }
        public CoreWebViewHeadlessHost(HtmlToPdfHost printHost, string urlOrFile)
        {
            HtmlToPdfHost = printHost;
            UrlOrFile = urlOrFile;

            WebViewPrintSettings = printHost.WebViewPrintSettings;

            InitializeAsync();
        }


        protected async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: HtmlToPdfHost.WebViewEnvironmentPath);

            var controller = await environment.CreateCoreWebView2ControllerAsync( new IntPtr(-3) ); // HWMD_MSG

            WebView = controller.CoreWebView2;
            
            WebView.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            
            //PrintFromUrlStream(UrlOrFile);
        }


        private async void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            try
            {
                if (PdfPrintOutputMode == PdfPrintOutputModes.File)
                    await PrintToPdf();
                else
                    await PrintToPdfStream();
            }
            finally
            {
                IsComplete = true;
            }
        }


        public void PrintFromUrl(string url, string outputFile)
        {
            PdfPrintOutputMode = PdfPrintOutputModes.File;
            _outputFile = outputFile;
            WebView.Navigate(url);
        }

        public void PrintFromUrlStream(string url)
        {
            PdfPrintOutputMode = PdfPrintOutputModes.Stream;
            WebView.Navigate(url);
        }

        public async Task PrintToPdf()
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
        public async Task<Stream> PrintToPdfStream()
        {
            var webViewPrintSettings = SetWebViewPrintSettings();

            try
            {
                // we have to turn the stream into something physical because the form won't stay alive
                await using var stream = await WebView.PrintToPdfStreamAsync(webViewPrintSettings);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                ResultStream = ms;

              

                IsSuccess = true;
                return ResultStream;
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                LastException = ex;
                return null;
            }
        }


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

    }
}
