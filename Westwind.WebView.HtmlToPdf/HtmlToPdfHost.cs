using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    public class HtmlToPdfHost
    {        
        internal WebViewPrintSettings WebViewPrintSettings = new WebViewPrintSettings();
        

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
        /// Max Render Timeout for PDF output generation
        /// </summary>
        public int RenderTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Result is returned as a Memory Stream in
        /// result.ResultStream on success. 
        /// 
        /// Check result.IsSuccess to check for successful completion.
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>        
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param>       
        public virtual Task<PdfPrintResult> PrintToPdfStreamAsync(string url,
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new PdfPrintResult()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            var tcs = new TaskCompletionSource<PdfPrintResult>();

            Thread thread = new Thread( () =>
            {
                // Create a Windows Forms Synchronization Context we can execute
                // which works without a desktop!
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                if (SynchronizationContext.Current == null)
                { 
                    tcs.SetResult(new PdfPrintResult {  IsSuccess = false, Message = "Couldn't create STA Synchronization Context." });
                    return;
                }
                SynchronizationContext.Current.Post( async (state)=>                 
                {
                    try
                    {
                        IsComplete = false;
                        var host = new CoreWebViewHeadlessHost(this);
                        await host.PrintFromUrlStream(url);

                        await WaitForHostComplete(host);                        

                        if (!host.IsComplete)
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = false,
                                Message = "Pdf generation timed out or failed to render inside of a non-Desktop context."
                            };
                        }
                        else
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = host.IsSuccess,
                                Message = host.IsSuccess ? "PDF was generated." : "PDF generation failed: " + host.LastException?.Message,
                                ResultStream = host.ResultStream,
                                LastException = host.LastException
                            };
                        }                        
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.ToString();
                        result.LastException = ex;
                        tcs.SetResult(result);
                    }
                    finally
                    {
                        IsComplete = true;
                        Application.ExitThread();  // now kill the event loop and thread
                    }
                }, null);                
                Application.Run();  // Windows Event loop needed for WebView in system context!
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF and awaits
        /// the result to be returned. Check result.IsSuccess to check for 
        /// successful completion of the file output generation or use File.Exists()
        /// </summary>
        /// <param name="url">File or URL to print to PDF</param>
        /// <param name="outputFile">output file for generated PDF</param>
        /// <param name="webViewPrintSettings">WebView PDF generation settings</param>
        public virtual Task<PdfPrintResult> PrintToPdfAsync(string url, 
            string outputFile,            
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new PdfPrintResult { 
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            var tcs = new TaskCompletionSource<PdfPrintResult>();
            Thread thread = new Thread(() =>
            {
                // Create a Windows Forms Synchronization Context we can execute
                // which works without a desktop!
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                if (SynchronizationContext.Current == null)
                {
                    tcs.SetResult(new PdfPrintResult { IsSuccess = false, Message = "Couldn't create STA Synchronization Context." });
                    return;
                }
                SynchronizationContext.Current.Post(async (state) =>
                {
                    try
                    {
                        IsComplete = false;
                        var host = new CoreWebViewHeadlessHost(this);
                        await host.PrintFromUrl(url, outputFile);

                        await WaitForHostComplete(host);

                        if (!host.IsComplete)
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = false,
                                Message = "Pdf generation timed out or failed to render inside of a non-Desktop context."
                            };
                        }
                        else
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = host.IsSuccess,
                                Message = host.IsSuccess ? "PDF was generated." : "PDF generation failed: " + host.LastException?.Message,                                
                                LastException = host.LastException
                            };
                        }                        
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.ToString();
                        result.LastException = ex;
                        tcs.SetResult(result);
                    }
                    finally
                    {
                        IsComplete = true;
                        Application.ExitThread();  // now kill the event loop and thread
                    }
                }, null);
                Application.Run();  // Windows Event loop needed for WebView in system context!
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();

            return tcs.Task;            
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
        /// <param name="onPrintComplete">Optional action to fire when printing (or failure) is complete</param>
        /// <param name="webViewPrintSettings">PDF output options</param>
        public virtual void PrintToPdfStream(string url, Action<PdfPrintResult> onPrintComplete = null, WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new PdfPrintResult
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
            {
                // Create a Windows Forms Synchronization Context we can execute
                // which works without a desktop!
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                if (SynchronizationContext.Current == null)
                {
                    IsComplete = true;
                    onPrintComplete?.Invoke(new PdfPrintResult { IsSuccess = false, Message = "Couldn't create STA Synchronization Context." });
                    return;
                }
                SynchronizationContext.Current.Post(async (state) =>
                {
                    try
                    {
                        IsComplete = false;

                        var host = new CoreWebViewHeadlessHost(this);
                        await host.PrintFromUrlStream(url);

                        for (int i = 0; i < RenderTimeoutMs / 20; i++)
                        {
                            if (host.IsComplete)
                                break;
                            await Task.Delay(20);
                        }

                        if (!host.IsComplete)
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = false,
                                Message = "Pdf generation timed out or failed to render inside of a non-Desktop context."
                            };
                        }
                        else
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = host.IsSuccess,
                                Message = host.IsSuccess ? "PDF was generated." : "PDF generation failed: " + host.LastException?.Message,
                                ResultStream = host.ResultStream,
                                LastException = host.LastException
                            };
                        }

                        onPrintComplete?.Invoke(result);
                        
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.ToString();
                        result.LastException = ex;                        
                    }
                    finally
                    {
                        IsComplete = true;
                        Application.ExitThread();  // now kill the event loop and thread
                    }
                }, null);
                Application.Run();  // Windows Event loop needed for WebView in system context!
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// This method prints a PDF from an HTML Url or File to PDF 
        /// using a new thread and a hosted form. The method **returns immediately**
        /// and returns completion via the `onPrintComplete` Action parameter.
        /// 
        /// This method works in non-UI scenarios as it creates its own STA thread
        /// </summary>
        /// <param name="url">The filename or URL to print to PDF</param>
        /// <param name="outputFile">File to generate the output to</param>
        /// <param name="onPrintComplete">Action to fire when printing is complete</param>
        /// <param name="webViewPrintSettings">PDF output options</param>
        public virtual void PrintToPdf(string url, string outputFile,
            Action<PdfPrintResult> onPrintComplete = null,
            WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new PdfPrintResult
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
            {
                // Create a Windows Forms Synchronization Context we can execute
                // which works without a desktop!
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                if (SynchronizationContext.Current == null)
                {
                    IsComplete = true;
                    onPrintComplete?.Invoke(new PdfPrintResult { IsSuccess = false, Message = "Couldn't create STA Synchronization Context." });
                    return;
                }
                SynchronizationContext.Current.Post(async (state) =>
                {
                    try
                    {
                        IsComplete = false;
                        var host = new CoreWebViewHeadlessHost(this);
                        await host.PrintFromUrl(url, outputFile);

                        await WaitForHostComplete(host);

                        if (!host.IsComplete)
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = false,
                                Message = "Pdf generation timed out or failed to render inside of a non-Desktop context."
                            };
                        }
                        else
                        {
                            result = new PdfPrintResult()
                            {
                                IsSuccess = host.IsSuccess,
                                Message = host.IsSuccess ? "PDF was generated." : "PDF generation failed: " + host.LastException?.Message,
                                LastException = host.LastException
                            };
                        }

                        onPrintComplete?.Invoke(result);                        
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.ToString();
                        result.LastException = ex;                     
                    }
                    finally
                    {
                        IsComplete = true;
                        Application.ExitThread();  // now kill the event loop and thread
                    }
                }, null);
                Application.Run();  // Windows Event loop needed for WebView in system context!
            });
           
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();          
        }


        private async Task WaitForHostComplete(CoreWebViewHeadlessHost host)
        {
            for (int i = 0; i < RenderTimeoutMs / 20; i++)
            {
                if (host.IsComplete)
                    break;
                await Task.Delay(10);
            }
        }

    }
}
