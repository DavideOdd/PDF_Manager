using PdfManager.Core.Models;
using PdfSharp.Pdf;

namespace PdfManager.Core.Services;

public interface IAnnotationWriter
{
    void WriteInk(PdfPage page, InkStroke stroke);
    void WriteFreeText(PdfPage page, FreeTextNote note);
    void RemoveOwnAnnotations(PdfPage page);
}
