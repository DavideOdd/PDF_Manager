using System.Globalization;
using PdfManager.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace PdfManager.Core.Services;

public sealed class PdfAnnotationReader : IAnnotationReader
{
    public IReadOnlyList<InkStroke> ReadInk(PdfPage page, int pageIndex)
    {
        var result = new List<InkStroke>();
        var annots = page.Elements.GetArray("/Annots");
        if (annots == null) return result;

        foreach (var item in annots.Elements)
        {
            var d = PdfAnnotationWriter.ResolveDictionary(item);
            if (d == null) continue;
            if (d.Elements.GetName("/Subtype") != "/Ink") continue;

            var (r, g, b) = ReadColor(d.Elements.GetArray("/C"));
            double width = ReadBorderWidth(d);
            string id = d.Elements.GetString("/NM") ?? string.Empty;
            bool foreign = !id.StartsWith(PdfAnnotationWriter.OwnNamePrefix, StringComparison.Ordinal);

            var polylines = new List<List<PointPt>>();
            var inkList = d.Elements.GetArray("/InkList");
            if (inkList != null)
            {
                foreach (var sub in inkList.Elements)
                {
                    var pts = sub as PdfArray ?? (sub is PdfReference rr ? rr.Value as PdfArray : null);
                    if (pts == null) continue;
                    var poly = new List<PointPt>();
                    for (int i = 0; i + 1 < pts.Elements.Count; i += 2)
                    {
                        double x = ReadReal(pts.Elements[i]);
                        double y = ReadReal(pts.Elements[i + 1]);
                        poly.Add(new PointPt(x, y));
                    }
                    if (poly.Count > 0) polylines.Add(poly);
                }
            }

            if (polylines.Count == 0) continue;

            result.Add(new InkStroke
            {
                Id = string.IsNullOrEmpty(id) ? $"{PdfAnnotationWriter.OwnNamePrefix}{Guid.NewGuid():N}" : id,
                PageIndex = pageIndex,
                ColorR = (byte)Math.Round(r * 255),
                ColorG = (byte)Math.Round(g * 255),
                ColorB = (byte)Math.Round(b * 255),
                WidthPt = width,
                Polylines = polylines,
                IsForeign = foreign
            });
        }

        return result;
    }

    public IReadOnlyList<FreeTextNote> ReadFreeText(PdfPage page, int pageIndex)
    {
        var result = new List<FreeTextNote>();
        var annots = page.Elements.GetArray("/Annots");
        if (annots == null) return result;

        foreach (var item in annots.Elements)
        {
            var d = PdfAnnotationWriter.ResolveDictionary(item);
            if (d == null) continue;
            if (d.Elements.GetName("/Subtype") != "/FreeText") continue;

            string id = d.Elements.GetString("/NM") ?? string.Empty;
            bool foreign = !id.StartsWith(PdfAnnotationWriter.OwnNamePrefix, StringComparison.Ordinal);
            string text = d.Elements.GetString("/Contents") ?? string.Empty;

            var rect = ReadRect(d.Elements.GetArray("/Rect"));
            var (cr, cg, cb, fontSize) = ParseDA(d.Elements.GetString("/DA") ?? string.Empty);

            result.Add(new FreeTextNote
            {
                Id = string.IsNullOrEmpty(id) ? $"{PdfAnnotationWriter.OwnNamePrefix}{Guid.NewGuid():N}" : id,
                PageIndex = pageIndex,
                Text = text,
                Rect = rect,
                ColorR = (byte)Math.Round(cr * 255),
                ColorG = (byte)Math.Round(cg * 255),
                ColorB = (byte)Math.Round(cb * 255),
                FontSizePt = fontSize,
                IsForeign = foreign
            });
        }

        return result;
    }

    private static (double R, double G, double B) ReadColor(PdfArray? arr)
    {
        if (arr == null || arr.Elements.Count < 3) return (0, 0, 0);
        return (ReadReal(arr.Elements[0]), ReadReal(arr.Elements[1]), ReadReal(arr.Elements[2]));
    }

    private static double ReadBorderWidth(PdfDictionary annot)
    {
        var bs = annot.Elements.GetDictionary("/BS");
        if (bs != null && bs.Elements.ContainsKey("/W"))
            return bs.Elements.GetReal("/W");
        var border = annot.Elements.GetArray("/Border");
        if (border != null && border.Elements.Count >= 3)
            return ReadReal(border.Elements[2]);
        return 1.0;
    }

    private static RectPt ReadRect(PdfArray? arr)
    {
        if (arr == null || arr.Elements.Count < 4) return new RectPt(0, 0, 0, 0);
        return new RectPt(
            ReadReal(arr.Elements[0]), ReadReal(arr.Elements[1]),
            ReadReal(arr.Elements[2]), ReadReal(arr.Elements[3]));
    }

    private static double ReadReal(PdfItem item) => item switch
    {
        PdfReal r => r.Value,
        PdfInteger i => i.Value,
        PdfReference rr => ReadReal(rr.Value),
        _ => 0.0
    };

    private static (double R, double G, double B, double FontSize) ParseDA(string da)
    {
        double r = 0, g = 0, b = 0, size = 12;
        if (string.IsNullOrEmpty(da)) return (r, g, b, size);
        var parts = da.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "rg" && i >= 3)
            {
                double.TryParse(parts[i - 3], NumberStyles.Float, CultureInfo.InvariantCulture, out r);
                double.TryParse(parts[i - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
                double.TryParse(parts[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out b);
            }
            else if (parts[i] == "Tf" && i >= 2)
            {
                double.TryParse(parts[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out size);
            }
        }
        return (r, g, b, size);
    }
}
