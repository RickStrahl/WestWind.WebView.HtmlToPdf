namespace Westwind.WebView.HtmlToPdf
{

    /// <summary>
    /// Proxy object of Core WebView settings options to avoid requiring
    /// a direct reference to the WebView control in the calling
    /// application/project.
    /// </summary>
    public class WebViewPrintSettings
    {
        private float _scaleFactor = 1F;

        /// <summary>
        /// Scale Factor up to 2
        /// </summary>
        public float ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                _scaleFactor = value;
                if (_scaleFactor > 2F)
                    ScaleFactor = 2F;
            }
        }

        /// <summary>
        /// Portrait, Landscape
        /// </summary>
        public WebViewPrintOrientations Orientation { get; set; } = WebViewPrintOrientations.Portrait;

        /// <summary>
        /// Width in inches
        /// </summary>
        public float PageWidth { get; set; } = 8.5F;

        /// <summary>
        /// Height in inches
        /// </summary>
        public float PageHeight { get; set; } = 11F;


        /// <summary>
        /// Top Margin in inches
        /// </summary>
        public float MarginTop { get; set; } = 0.25F;

        /// <summary>
        /// Bottom Margin in inches
        /// </summary>
        public float MarginBottom { get; set; } = 0.15F;

        /// <summary>
        /// Left Margin in inches
        /// </summary>
        public float MarginLeft { get; set; } = 0.20F;

        /// <summary>
        /// Right Margin in inches
        /// </summary>
        public float MarginRight { get; set; } = 0.20F;



        /// <summary>
        /// Page ranges as specified 1,2,3,5-7
        /// </summary>
        public string PageRanges { get; set; }


        /// <summary>
        /// Determines whether background colors are printed. Use to
        /// save ink on printing or for more legible in print/pdf scenarios
        /// </summary>
        public bool ShouldPrintBackgrounds { get; set; } = true;


        /// <summary>
        /// Color, Grayscale, Monochrome
        /// 
        /// CURRENTLY DOESN'T WORK FOR PDF GENERATION
        /// </summary>
        public WebViewPrintColorModes ColorMode { get; set; } = WebViewPrintColorModes.Color;


        /// <summary>
        /// When true prints only the section of the document selected 
        /// </summary>
        public bool ShouldPrintSelectionOnly { get; set; } = false;

        /// <summary>
        /// Determines whether headers and footers are printed
        /// </summary>
        public bool ShouldPrintHeaderAndFooter { get; set; } = false;

        /// <summary>
        /// Title displayed on every page as a thin header - only displayed if ShouldPrintHeaderAndFooter is true
        /// </summary>
        public string HeaderTitle { get; set; }

        /// <summary>
        /// Url displayed on footer - only displayed if ShouldPrintHeaderAndFooter is set
        /// </summary>
        public string FooterUri { get; set; }




        #region Print Settings - ignored for PDF

        /// <summary>
        /// Printer name when printing to a printer (not applicable for PDF)
        /// 
        /// NO EFFECT ON PDF PRINTING
        /// </summary>
        public string PrinterName { get; set; }

        /// <summary>
        /// Number of Copies to print
        /// 
        /// NO EFFECT ON PDF PRINTING
        /// </summary>
        public int Copies { get; set; } = 1;

        /// <summary>
        /// Default, OneSided, TwoSidedLongEdge, TwoSidedShortEdge
        /// 
        /// NO EFFECT ON PDF PRINTING
        /// </summary>
        public WebViewPrintDuplexes Duplex { get; set; } = WebViewPrintDuplexes.Default;

        /// <summary>
        /// Default, Collated, Uncollated
        /// 
        /// NO EFFECT OF PDF PRINTING
        /// </summary>
        public WebViewPrintCollations Collation { get; set; } = WebViewPrintCollations.Default;

        /// <summary>
        /// Allows multiple pages to be packed into a single page.
        /// 
        /// NO EFFECT ON PDF PRINTING
        /// </summary>
        public int PagesPerSide { get; set; } = 1;

        #endregion
    }
}