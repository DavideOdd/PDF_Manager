using System.Windows.Ink;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfManager.Core.Models;

namespace PdfManager.App.ViewModels;

public sealed partial class PageViewModel : ObservableObject
{
    [ObservableProperty] private BitmapSource? _renderedImage;
    [ObservableProperty] private BitmapSource? _thumbnailImage;
    [ObservableProperty] private bool _isLoading = true;

    public int PageIndex { get; init; }
    public double PageWidthPt { get; init; }
    public double PageHeightPt { get; init; }
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    public double RenderDpi { get; init; }

    public double DisplayWidthDip => PageWidthPt * (96.0 / 72.0);
    public double DisplayHeightDip => PageHeightPt * (96.0 / 72.0);

    public StrokeCollection Strokes { get; } = new();

    public List<FreeTextNote> Notes { get; } = new();

    public string Label => $"Pagina {PageIndex + 1}";

    public double PxToPt(double inkCoord) => inkCoord * 72.0 / 96.0;
    public double PtToPx(double pt) => pt * 96.0 / 72.0;

    public double FlipY(double inkY) => PageHeightPt - PxToPt(inkY);
    public double FlipYFromPdf(double pdfY) => PtToPx(PageHeightPt - pdfY);
}
