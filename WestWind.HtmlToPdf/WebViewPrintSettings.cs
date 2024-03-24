namespace WestWind.HtmlToPdf;

/// <summary>
/// Proxy object of Core WebView settings options to avoid requiring
/// a direct reference to the WebView control in the calling
/// application/project.
/// </summary>
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