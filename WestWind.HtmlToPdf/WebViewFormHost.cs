﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace WestWind.HtmlToPdf
{
    public partial class WebViewFormHost : Form
    {
        public WebViewPrintSettings WebViewPrintSettings { get; set; } = new WebViewPrintSettings();
        
        private string _outputFile { get; set; }
        
        public HtmlToPdfHost HtmlToPdfHost { get; set; }

        public bool IsSuccess { get; set; } = false;

        public Exception LastException { get; set; }

        public Stream ResultStream { get; set; }


        private PdfPrintOutputModes PdfPrintOutputMode { get; set; } = PdfPrintOutputModes.File;

        public WebViewFormHost(HtmlToPdfHost printHost)
        {
            HtmlToPdfHost = printHost;

            InitializeComponent();

            this.WebViewPrintSettings = printHost.WebViewPrintSettings;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            // must create a data folder if running out of a secured folder that can't write like Program Files
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: "c:\\temp\\WebViewEnvironment",
                options:null);
            
            await WebView.EnsureCoreWebView2Async(environment);

            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }


        private async void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (PdfPrintOutputMode == PdfPrintOutputModes.File)
                await PrintToPdf();
            else
                await PrintToPdfStream();
        }


        public void PrintFromUrl(string url, string outputFile)
        {
            PdfPrintOutputMode = PdfPrintOutputModes.File;
            _outputFile = outputFile;
            WebView.Source = new Uri(url);                        
        }

        public void PrintFromUrlStream(string url)
        {
            PdfPrintOutputMode = PdfPrintOutputModes.Stream;
            WebView.Source = new Uri(url);
        }

        public async Task PrintToPdf()
        {
            var webViewPrintSettings = SetWebViewPrintSettings();
            
        
            if(File.Exists(_outputFile))
                File.Delete(_outputFile);

            try
            {
                if (File.Exists(_outputFile))
                    File.Delete(_outputFile);

                await WebView.CoreWebView2.PrintToPdfAsync(_outputFile, webViewPrintSettings);
                
                if(File.Exists(_outputFile))
                    IsSuccess = true;
                else 
                    IsSuccess= false;
            }
            catch(Exception ex)
            {
                IsSuccess = false;
                LastException = ex;
            }

            Close();
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
                await using var stream = await WebView.CoreWebView2.PrintToPdfStreamAsync(webViewPrintSettings);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                ResultStream = ms;
                
                Close(); // close the form

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
            var wvps = WebView.CoreWebView2.Environment.CreatePrintSettings();
            
            var ps = WebViewPrintSettings;

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

    internal enum PdfPrintOutputModes
    {
        File,
        Stream
    }
}