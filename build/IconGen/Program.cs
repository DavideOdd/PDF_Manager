using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

var root    = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
var outDir  = Path.Combine(root, "assets", "icons");
var outPath = Path.Combine(outDir, "app.ico");
Directory.CreateDirectory(outDir);

int[] sizes = [256, 64, 48, 32, 16];
var pngs = sizes.Select(MakeFrame).ToList();
WriteIco(outPath, sizes, pngs);
Console.WriteLine($"OK: {outPath}");

static byte[] MakeFrame(int s)
{
    using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using var g   = Graphics.FromImage(bmp);
    g.SmoothingMode      = SmoothingMode.AntiAlias;
    g.TextRenderingHint  = TextRenderingHint.AntiAliasGridFit;
    g.Clear(Color.Transparent);

    int pad  = Math.Max(1, (int)(s * 0.10));
    int fold = Math.Max(2, (int)(s * 0.22));
    int docW = s - pad * 2;
    int docH = (int)(s * 0.80);
    int docX = pad;
    int docY = (s - docH) / 2;

    Point[] bodyPts =
    [
        new(docX,               docY + fold),
        new(docX,               docY + docH),
        new(docX + docW,        docY + docH),
        new(docX + docW,        docY + fold),
        new(docX + docW - fold, docY),
        new(docX,               docY)
    ];

    using var bodyBrush = new SolidBrush(Color.FromArgb(255, 63, 81, 181));
    g.FillPolygon(bodyBrush, bodyPts);

    Point[] foldPts =
    [
        new(docX + docW - fold, docY),
        new(docX + docW,        docY + fold),
        new(docX + docW - fold, docY + fold)
    ];
    using var foldBrush = new SolidBrush(Color.FromArgb(255, 121, 134, 203));
    g.FillPolygon(foldBrush, foldPts);

    if (s >= 24)
    {
        int bandH = Math.Max(4, (int)(s * 0.22));
        int bandY = docY + (int)(docH * 0.50);
        using var redBrush = new SolidBrush(Color.FromArgb(255, 211, 47, 47));
        g.FillRectangle(redBrush, docX, bandY, docW, bandH);

        if (s >= 32)
        {
            float fs = Math.Max(6, bandH * 0.70f);
            using var font  = new Font("Arial", fs, FontStyle.Bold, GraphicsUnit.Pixel);
            using var white = new SolidBrush(Color.White);
            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("PDF", font, white,
                new RectangleF(docX, bandY, docW, bandH), sf);
        }
    }

    float pw = Math.Max(0.5f, s / 64f);
    using var pen = new Pen(Color.FromArgb(60, 255, 255, 255), pw);
    g.DrawPolygon(pen, bodyPts);

    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}

static void WriteIco(string path, int[] sizes, List<byte[]> pngs)
{
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    bw.Write((ushort)0);
    bw.Write((ushort)1);
    bw.Write((ushort)sizes.Length);

    int offset = 6 + 16 * sizes.Length;
    for (int i = 0; i < sizes.Length; i++)
    {
        int sz = sizes[i];
        bw.Write((byte)(sz >= 256 ? 0 : sz));
        bw.Write((byte)(sz >= 256 ? 0 : sz));
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((ushort)1);
        bw.Write((ushort)32);
        bw.Write((uint)pngs[i].Length);
        bw.Write((uint)offset);
        offset += pngs[i].Length;
    }
    foreach (var png in pngs) bw.Write(png);
    bw.Flush();
    File.WriteAllBytes(path, ms.ToArray());
}
