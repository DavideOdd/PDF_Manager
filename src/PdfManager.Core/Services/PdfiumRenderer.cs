using System.Runtime.InteropServices;
using PdfManager.Core.Models;
using PDFiumCore;

namespace PdfManager.Core.Services;

public sealed class PdfiumRenderer : IPdfRenderer, IDisposable
{
    private const int FPDFBitmap_BGRA = 4;
    private const int FPDF_ERR_PASSWORD = 4;

    // pdfium is NOT thread-safe for concurrent operations on its global state.
    // All pdfium calls are serialized through this lock.
    private static readonly SemaphoreSlim _pdfiumLock = new(1, 1);
    private static readonly object _initLock = new();
    private static bool _initialized;

    public PdfiumRenderer()
    {
        EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;
            fpdfview.FPDF_InitLibrary();
            _initialized = true;
        }
    }

    public bool IsPasswordProtected(string path)
    {
        _pdfiumLock.Wait();
        try
        {
            EnsureInitialized();
            var doc = fpdfview.FPDF_LoadDocument(path, null);
            if (doc != null) { fpdfview.FPDF_CloseDocument(doc); return false; }
            return fpdfview.FPDF_GetLastError() == FPDF_ERR_PASSWORD;
        }
        finally { _pdfiumLock.Release(); }
    }

    public int GetPageCount(string path, string? password = null)
    {
        _pdfiumLock.Wait();
        try
        {
            EnsureInitialized();
            var doc = fpdfview.FPDF_LoadDocument(path, password);
            if (doc == null) ThrowOnLoadError(path);
            try { return fpdfview.FPDF_GetPageCount(doc); }
            finally { fpdfview.FPDF_CloseDocument(doc); }
        }
        finally { _pdfiumLock.Release(); }
    }

    public (double WidthPt, double HeightPt) GetPageSize(string path, int pageIndex, string? password = null)
    {
        _pdfiumLock.Wait();
        try
        {
            EnsureInitialized();
            var doc = fpdfview.FPDF_LoadDocument(path, password);
            if (doc == null) ThrowOnLoadError(path);
            try
            {
                var size = new FS_SIZEF_();
                fpdfview.FPDF_GetPageSizeByIndexF(doc, pageIndex, size);
                return (size.Width, size.Height);
            }
            finally { fpdfview.FPDF_CloseDocument(doc); }
        }
        finally { _pdfiumLock.Release(); }
    }

    public RenderedPage RenderPage(string path, int pageIndex, double dpi, string? password = null)
    {
        _pdfiumLock.Wait();
        try
        {
            EnsureInitialized();
            var doc = fpdfview.FPDF_LoadDocument(path, password);
            if (doc == null) ThrowOnLoadError(path);

            try
            {
                var page = fpdfview.FPDF_LoadPage(doc, pageIndex);
                if (page == null) throw new InvalidOperationException($"Impossibile caricare pagina {pageIndex}.");

                try
                {
                    double widthPt  = fpdfview.FPDF_GetPageWidthF(page);
                    double heightPt = fpdfview.FPDF_GetPageHeightF(page);
                    int pxW = Math.Max(1, (int)Math.Round(widthPt  * dpi / 72.0));
                    int pxH = Math.Max(1, (int)Math.Round(heightPt * dpi / 72.0));

                    var bmp = fpdfview.FPDFBitmapCreateEx(pxW, pxH, FPDFBitmap_BGRA, IntPtr.Zero, 0);
                    if (bmp == null) throw new OutOfMemoryException("Allocazione bitmap PDFium fallita.");

                    try
                    {
                        fpdfview.FPDFBitmapFillRect(bmp, 0, 0, pxW, pxH, 0xFFFFFFFFul);
                        fpdfview.FPDF_RenderPageBitmap(bmp, page, 0, 0, pxW, pxH, 0, 0);

                        int stride    = fpdfview.FPDFBitmapGetStride(bmp);
                        IntPtr buffer = fpdfview.FPDFBitmapGetBuffer(bmp);
                        int byteCount = stride * pxH;
                        var managed   = new byte[byteCount];
                        Marshal.Copy(buffer, managed, 0, byteCount);

                        return new RenderedPage
                        {
                            PageIndex     = pageIndex,
                            PixelWidth    = pxW,
                            PixelHeight   = pxH,
                            PageWidthPt   = widthPt,
                            PageHeightPt  = heightPt,
                            Dpi           = dpi,
                            BgraBuffer    = managed,
                            Stride        = stride
                        };
                    }
                    finally { fpdfview.FPDFBitmapDestroy(bmp); }
                }
                finally { fpdfview.FPDF_ClosePage(page); }
            }
            finally { fpdfview.FPDF_CloseDocument(doc); }
        }
        finally { _pdfiumLock.Release(); }
    }

    private static void ThrowOnLoadError(string path)
    {
        var err = fpdfview.FPDF_GetLastError();
        if (err == FPDF_ERR_PASSWORD)
            throw new PdfPasswordRequiredException(path);
        throw new InvalidOperationException($"Apertura PDF fallita ({path}), codice errore PDFium {err}.");
    }

    public void Dispose() { }
}

public sealed class PdfPasswordRequiredException : Exception
{
    public string Path { get; }
    public PdfPasswordRequiredException(string path)
        : base($"Il PDF '{path}' richiede una password.") { Path = path; }
}
