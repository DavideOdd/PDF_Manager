using PdfManager.Core.Models;

namespace PdfManager.Core.Services;

public interface IPdfRenderer
{
    int GetPageCount(string path, string? password = null);
    (double WidthPt, double HeightPt) GetPageSize(string path, int pageIndex, string? password = null);
    RenderedPage RenderPage(string path, int pageIndex, double dpi, string? password = null);
    bool IsPasswordProtected(string path);
}
