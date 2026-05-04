using PdfManager.Core.Models;
using PdfSharp.Pdf;

namespace PdfManager.Core.Services;

public interface IAnnotationReader
{
    IReadOnlyList<InkStroke> ReadInk(PdfPage page, int pageIndex);
    IReadOnlyList<FreeTextNote> ReadFreeText(PdfPage page, int pageIndex);
}
