using Microsoft.Web.WebView2.Core;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Westwind.WebView.HtmlToPdf
{ 
    /// <summary>
    /// Converts an HTML document to PDF using the Windows WebView control.    
    /// </summary>
    /// <remarks>
    /// * Recommend you use a new instance for each PDF generation
    /// * Works only on Windows
    /// * Requires net8.0-windows target to work
    /// </remarks>
    public class HtmlToPdfHostEx
    {
      
        public bool IsSuccess { get; set; } = false;

        public bool IsComplete { get; set; }

        public Stream ResultStream { get; set; }

        public string ErrorMessage { get; set; }
        
        public Exception LastException { get; set; }


        internal Westwind.WebView.HtmlToPdf.WebViewPdfPrintSettings WebViewPdfPrintSettings { get; set; } = new();

        internal CoreWebView2Controller WebView { get; set;  }

        private PdfPrintOutputModes PdfPrintOutputMode { get; set; } = PdfPrintOutputModes.File;

        private string OutputFile { get; set; }

        private string CurrentUrl { get; set; }

        public PdfPrintResult Result { get; set; } = new() { IsSuccess = false, Message = "Pdf has not been generated."};

        /// <summary>
        /// The location of the WebView environment folder that is required
        /// for WebView operation. Uses a default in the temp folder but you
        /// can customize to use an application specific folder.
        /// 
        /// (If you already use a WebView keep all WebViews pointing at the same environment: 
        /// https://weblog.west-wind.com/posts/2023/Oct/31/Caching-your-WebView-Environment-to-manage-multiple-WebView2-Controls
        /// </summary>
        public string WebViewEnvironmentPath { get; set; } = Path.Combine(Path.GetTempPath(), "WebView2_Environment");

        /// <summary>
        /// Event Action that is fired when the print operation is complete.
        /// Check the IsSuccess property to see if the print operation was successful
        /// and you the message and Last Exception for error information.
        /// </summary>
        public Action<PdfPrintResult> OnPrintCompleteAction { get; set; }


        /// <summary>
        /// Wait for document to be loaded - then print
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (PdfPrintOutputMode == PdfPrintOutputModes.File)
                await PrintToPdfInternalAsync();
            //else
            //    await PrintToPdfStream();
        }


        public void PrintToPdf(string url, string outputFile, WebViewPdfPrintSettings webViewPdfPrintSettings = null)
        {
            WebViewPdfPrintSettings = webViewPdfPrintSettings ?? WebViewPdfPrintSettings;
            OutputFile = outputFile;
            CurrentUrl = url;
            PdfPrintOutputMode = PdfPrintOutputModes.File;
        
            PdfPrintResult result = new()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };
            var tcs = new TaskCompletionSource();

            Thread thread = new Thread(async () =>
            {                
                try
                {
                    IsComplete = false;

                    // navigate and wait for completion
                    var result = await PrintToPdfInternalAsync();

                    //while (!IsComplete)
                    //{
                    //    await Task.Delay(100);
                    Result = new()
                    {
                        IsSuccess = IsSuccess,
                        Message = ErrorMessage,
                        LastException = LastException,
                        ResultStream = ResultStream,
                    };

                    OnPrintCompleteAction?.Invoke(Result);
                    IsComplete = true;
                }
                catch (Exception ex)
                {
                    Result = new()
                    {
                        IsSuccess = false,
                        LastException = ex,
                        Message = ex.Message,
                        ResultStream = ResultStream,
                    };
                    OnPrintCompleteAction?.Invoke(Result);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();


        }


        public void PrintFromUrlStream(string url)
        {
            PdfPrintOutputMode = PdfPrintOutputModes.Stream;
            WebView.CoreWebView2.Navigate(url);
        }

        public async Task<bool> PrintToPdfInternalAsync()
        {           
            IsSuccess = false;
           
            try
            {
                if (File.Exists(OutputFile))
                    File.Delete(OutputFile);
                               
                // must create a data folder if running out of a secured folder that can't write like Program Files
                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: WebViewEnvironmentPath,
                    options: null);
                WebView = await environment.CreateCoreWebView2ControllerAsync(new IntPtr(-3));
                WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                
                var wvSettings = SetWebViewPrintSettings();

                // Navigate and initiate the Page load
                bool result = await WebView.CoreWebView2.PrintToPdfAsync(OutputFile, wvSettings);

                
                if (File.Exists(OutputFile))
                    IsSuccess = true;
                else
                {
                    IsSuccess = false;
                    ErrorMessage = "PDF generation failed.";                    
                }
            }
            catch (Exception ex)
            {                
                IsSuccess = false;
                ErrorMessage = ex.Message;
                LastException = ex;
            }
            finally
            {
                IsComplete = true;
            }

            return IsSuccess;
        }

        

        ///// <summary>
        ///// Prints the current document in the WebView to a MemoryStream
        ///// </summary>
        ///// <returns></returns>
        //public async Task<Stream> PrintToPdfStream()
        //{
        //    var webViewPrintSettings = SetWebViewPrintSettings();

        //    try
        //    {
        //        // we have to turn the stream into something physical because the form won't stay alive
        //        await using var stream = await WebView.CoreWebView2.PrintToPdfStreamAsync(webViewPrintSettings);
        //        var ms = new MemoryStream();
        //        await stream.CopyToAsync(ms);
        //        ms.Position = 0;
        //        ResultStream = ms;

        //        Close(); // close the form

        //        IsSuccess = true;
        //        return ResultStream;
        //    }
        //    catch (Exception ex)
        //    {
        //        IsSuccess = false;
        //        LastException = ex;
        //        return null;
        //    }
        //}


        private CoreWebView2PrintSettings SetWebViewPrintSettings()
        {
            var wvps = WebView.CoreWebView2.Environment.CreatePrintSettings();

            var ps = WebViewPdfPrintSettings;

            wvps.ScaleFactor = ps.ScaleFactor;
            wvps.MarginTop = ps.MarginTop;
            wvps.MarginBottom = ps.MarginBottom;
            wvps.MarginLeft = ps.MarginLeft;
            wvps.MarginRight = ps.MarginRight;

            wvps.PageWidth = ps.PageWidth;
            wvps.PageHeight = ps.PageHeight;

            wvps.Copies = ps.Copies;

            wvps.HeaderTitle = ps.HeaderTitle;
            wvps.ShouldPrintHeaderAndFooter = ps.ShouldPrintHeaderandFooter;
            wvps.ShouldPrintBackgrounds = ps.ShouldPrintBackgrounds;
            wvps.FooterUri = ps.FooterUri;

            wvps.PagesPerSide = ps.PagesPerSide;
            wvps.ShouldPrintSelectionOnly = ps.ShouldPrintSelectionOnly;
            wvps.Orientation = ps.Orientation == "Portrait" ? CoreWebView2PrintOrientation.Portrait : CoreWebView2PrintOrientation.Landscape;
            wvps.Duplex = ps.Duplex == "Default" ? CoreWebView2PrintDuplex.Default :
                ps.Duplex == "OneSided" ? CoreWebView2PrintDuplex.OneSided :
                ps.Duplex == "TwoSidedLongEdge" ? CoreWebView2PrintDuplex.TwoSidedLongEdge :
                CoreWebView2PrintDuplex.TwoSidedShortEdge;
            wvps.ColorMode = ps.ColorMode == "Color" ? CoreWebView2PrintColorMode.Color : CoreWebView2PrintColorMode.Grayscale;
            wvps.PageRanges = ps.PageRanges;
            wvps.PrinterName = ps.PrinterName;

            return wvps;
        }

    }
}
