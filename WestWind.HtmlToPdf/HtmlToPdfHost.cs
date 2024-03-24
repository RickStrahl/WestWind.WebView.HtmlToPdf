using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WestWind.HtmlToPdf
{


    /// <summary>
    /// Converts an HTML document to PDF using the Windows WebView control.    
    /// </summary>
    /// <remarks>
    /// * Recommend you use a new instance for each PDF generation
    /// * Works only on Windows
    /// * Requires net8.0-windows target to work
    /// </remarks>
    public class HtmlToPdfHost
    {        
        internal WebViewPrintSettings WebViewPrintSettings = new WebViewPrintSettings();

        /// <summary>
        /// Event Action that is fired when the print operation is complete.
        /// Check the IsSuccess property to see if the print operation was successful
        /// and you the message and Last Exception for error information.
        /// </summary>
        public Action<PdfPrintResult> OnPrintCompleteAction { get; set; }


        /// <summary>
        /// A flag you can check to see if the conversion process has completed.
        /// </summary>
        public bool IsComplete { get; set; }

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
        /// This method prints a PDF from an HTML URl or File to PDF 
        /// using a new thread and a hosted form. 
        /// 
        /// You get notified via OnPrintCompleteAction 'event' (Action) when the 
        /// output operation is complete.
        /// </summary>
        /// <param name="url">The filename or URL to print to PDF</param>
        /// <param name="outputFile">File to generate the output to</param>
        /// <param name="webViewPrintSettings">PDF output options</param>
        public void PrintToPdf(string url, string outputFile, WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
            {
                try
                {
                    IsComplete = false;

                    var form = new WebViewFormHost(this);
                    form.Left = 100000;
                    form.Top = 100000;
                    form.PrintFromUrl(url, outputFile);
                    form.ShowDialog();

                    result = new PdfPrintResult()
                    {
                        IsSuccess = form.IsSuccess,
                        Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException.Message,
                        LastException = form.LastException
                    };

                    OnPrintCompleteAction?.Invoke(result);
                    IsComplete = true;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.LastException = ex;
                    result.Message = ex.Message;
                    OnPrintCompleteAction?.Invoke(result);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();          
        }

        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF 
        /// using a new thread and a hosted form returning the result
        /// as an in-memory stream in result.ResultStream.
        /// 
        /// You get notified via OnPrintCompleteAction 'event' (Action) when the 
        /// output operation is complete.
        /// </summary>
        /// <param name="url">The filename or URL to print to PDF</param>        
        /// <param name="webViewPrintSettings">PDF output options</param>
        public void PrintToPdfStream(string url, WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
            {
                try
                {
                    IsComplete = false;

                    var form = new WebViewFormHost(this);
                    form.Left = 100000;
                    form.Top = 100000;
                    form.PrintFromUrlStream(url);
                    form.ShowDialog();

                    result = new PdfPrintResult()
                    {
                        IsSuccess = form.IsSuccess,
                        ResultStream = form.ResultStream as MemoryStream,
                        Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException.Message,
                        LastException = form.LastException
                    };

                    OnPrintCompleteAction?.Invoke(result);
                    IsComplete = true;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.LastException = ex;
                    result.Message = ex.Message;
                    OnPrintCompleteAction?.Invoke(result);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }


        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Check result.IsSuccess to check for 
        /// successful completion of the file output generation or use File.Exists()
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>
        /// <param name="outputFile">output file for generated PDF</param>
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param>
        public Task<PdfPrintResult> PrintToPdfAsync(string url, 
            string outputFile, 
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new() { 
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            var tcs = new TaskCompletionSource<PdfPrintResult>();

            Thread thread = new Thread(() =>
            {
                try
                {
                    IsComplete = false;

                    var form = new WebViewFormHost(this);
                    form.Left = 10_000;
                    form.Top = 10_000;
                    form.Height = 1;
                    form.Width = 1;
                    form.PrintFromUrl(url, outputFile);
                    form.ShowDialog();

                    result = new PdfPrintResult()
                    {
                        IsSuccess = form.IsSuccess,
                        Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException?.Message,
                        LastException = form.LastException
                    };

                    IsComplete = true;
                    OnPrintCompleteAction?.Invoke(result);
                    tcs.SetResult(result);                    
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Message = ex.ToString();
                    result.LastException = ex;

                    tcs.SetResult(result);
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();

            return tcs.Task;            
        }


        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Result is returned as a Memory Stream in
        /// result.ResultStream on success. 
        /// 
        /// Check result.IsSuccess to check for successful completion.
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>        
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param>
        public Task<PdfPrintResult> PrintToPdfStreamAsync(string url,            
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            var tcs = new TaskCompletionSource<PdfPrintResult>();

            Thread thread = new Thread(() =>
            {
                try
                {
                    IsComplete = false;

                    var form = new WebViewFormHost(this);
                    form.Left = 100_000;
                    form.Top = 100_000;
                    form.Width = 1;
                    form.Height = 1;
                    form.PrintFromUrlStream(url);
                    form.ShowDialog();

                    result = new PdfPrintResult()
                    {
                        IsSuccess = form.IsSuccess,
                        ResultStream = form.ResultStream,
                        Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException?.Message,
                        LastException = form.LastException
                    };

                    IsComplete = true;
                    OnPrintCompleteAction?.Invoke(result);
                    tcs.SetResult(result);
                }
                catch(Exception ex)
                {
                    result.IsSuccess = false;
                    result.LastException = ex;
                    result.Message = ex.Message;

                    tcs.SetResult(result);
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();

            return tcs.Task;
        }

    }
}
