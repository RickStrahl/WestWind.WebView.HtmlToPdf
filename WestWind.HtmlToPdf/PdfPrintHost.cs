using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//using Microsoft.Web.WebView2.Wpf;

namespace WestWind.HtmlToPdf
{
    public class PdfPrintHost
    {

        public WebViewPrintSettings WebViewPrintSettings = new WebViewPrintSettings();

        /// <summary>
        /// Event Action that is fired when the print operation is complete.
        /// Check the IsSuccess property to see if the print operation was successful
        /// and you the message and Last Exception for error information.
        /// </summary>
        public Action<PdfPrintResult> OnPrintCompleteAction { get; set; }

        public bool IsComplete { get; set; }

        public string WebViewEnvironmentPath { get; set; } = Path.Combine(Path.GetTempPath(), "WebView2_Environment");

        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF 
        /// using a new thread and a hosted form. You get notified via a completion
        /// event (Action) when the print operation is complete.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="outputFile"></param>
        /// <param name="settings"></param>
        public void PrintToPdf(string url, string outputFile, WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            Thread thread = new Thread(() =>
            {
                IsComplete = false;

                var form = new WebViewFormHost(this);                                
                form.Left = 100000;
                form.Top = 100000;
                form.PrintFromUrl(url, outputFile);
                form.ShowDialog();

                var result = new PdfPrintResult()
                {
                    IsSuccess = form.IsSuccess,
                    Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException.Message,
                    LastException = form.LastException
                };

                OnPrintCompleteAction?.Invoke(result);
                IsComplete = true;
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();          
        }

        public void PrintToPdfStream(string url, WebViewPrintSettings webViewPrintSettings = null)
        {
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            Thread thread = new Thread(() =>
            {
                IsComplete = false;

                var form = new WebViewFormHost(this);
                form.Left = 100000;
                form.Top = 100000;
                form.PrintFromUrlStream(url);
                form.ShowDialog();

                var result = new PdfPrintResult()
                {
                    IsSuccess = form.IsSuccess,
                    ResultStream = form.ResultStream as MemoryStream,
                    Message = form.IsSuccess ? "PDF was generated." : "PDF generation failed: " + form.LastException.Message,
                    LastException = form.LastException
                };

                OnPrintCompleteAction?.Invoke(result);
                IsComplete = true;
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }


        /// <summary>
        /// This method prints a PDF from an HTML URl or File to PDF 
        /// using an **existing STA Application Context**. If you're using a WPF or 
        /// WinForms application you can call this method directly as an async method
        /// from your existing application context. 
        /// event (Action) when the print operation is complete.
        /// </summary>
        /// <remarks>
        /// Requires that it's called from an STA based application context.
        /// </remarks>
        /// <param name="url"></param>
        /// <param name="outputFile"></param>
        /// <param name="webViewPrintSettings"></param>
        public async Task<PdfPrintResult> PrintToPdfAsync(string url, 
            string outputFile, 
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new() { 
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
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
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();
            
            // I FEEL DIRTY AWARD! Loop and Task.Delay for 10 seconds
            for (int i = 0; i < 100; i++)
            {
                if (IsComplete)
                    break;

                await Task.Delay(100);
            }

            return result;
        }

        public async Task<PdfPrintResult> PrintToPdfStreamAsync(string url,            
            WebViewPrintSettings webViewPrintSettings = null)
        {
            IsComplete = false;
            WebViewPrintSettings = webViewPrintSettings ?? WebViewPrintSettings;

            PdfPrintResult result = new()
            {
                IsSuccess = false,
                Message = "PDF generation didn't complete.",
            };

            Thread thread = new Thread(() =>
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
            });

            thread.SetApartmentState(ApartmentState.STA); // MUST BE STA!
            thread.Start();

            // I FEEL DIRTY AWARD! Loop and Task.Delay for 10 seconds
            for (int i = 0; i < 100; i++)
            {
                if (IsComplete)
                    break;

                await Task.Delay(100);
            }

            return result;
        }

    }


    public class PdfPrintResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } 

        public Exception LastException { get; set;  }
        public Stream ResultStream { get; set; }
    }

    public class WebViewPrintSettings
    {
        public float ScaleFactor { get; set; } = 1F;

        public float MarginTop { get; set; } = 0.25F;

        public float MarginBottom { get; set; } = 0.15F;

        public float MarginLeft { get; set; } = 0.20F;
        public float MarginRight { get; set; } = 0.20F;

        public float PageWidth { get; set; } = 8.5F;
        public float PageHeight { get; set; } = 11F;

        public int Copies { get; set; } = 1;

        /// <summary>
        /// Default, OneSided, TwoSidedLongEdge, TwoSidedShortEdge
        /// </summary>
        public string Duplex { get; set; } = "Default";

        /// <summary>
        /// Default, Collated, Uncollated
        /// </summary>
        public string Collation { get; set; } = "Default";

        /// <summary>
        /// Color, Grayscale, Monochrome
        /// </summary>
        public string ColorMode { get; set; } = "Color";

        /// <summary>
        /// Portrait, Landscape
        /// </summary>
        public string Orientation { get; set;  } = "Portrait";

        public string HeaderTitle { get; set; } 

        public bool ShouldPrintHeaderandFooter { get; set; } = false;

        public bool ShouldPrintSelectionOnly { get; set; } = false; 

        public bool ShouldPrintBackgrounds { get; set; } = true;
        public string FooterUri { get; set; }
        public int PagesPerSide { get; set; } = 1;
        public string PageRanges { get; set; }
        public string PrinterName { get; set; }
    }
}
