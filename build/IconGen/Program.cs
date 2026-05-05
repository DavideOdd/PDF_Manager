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

    // Rounded rectangle background — indigo #3F51B5
    float r = s * 0.18f;
    var bgRect = new RectangleF(0, 0, s, s);
    using var bgBrush = new SolidBrush(Color.FromArgb(255, 63, 81, 181));
    FillRoundedRect(g, bgBrush, bgRect, r);

    // "GP" text — white, bold, Inter/Arial
    if (s >= 24)
    {
        string text = s >= 32 ? "GP" : "G";
        float fontSize = s * (s >= 32 ? 0.42f : 0.52f);
        using var font  = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.White);
        var sf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(text, font, brush, new RectangleF(0, 0, s, s), sf);
    }

    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}

static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
{
    using var path = new GraphicsPath();
    float d = radius * 2;
    path.AddArc(rect.X,               rect.Y,                d, d, 180, 90);
    path.AddArc(rect.Right - d,       rect.Y,                d, d, 270, 90);
    path.AddArc(rect.Right - d,       rect.Bottom - d,       d, d,   0, 90);
    path.AddArc(rect.X,               rect.Bottom - d,       d, d,  90, 90);
    path.CloseFigure();
    g.FillPath(brush, path);
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
        bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((ushort)1); bw.Write((ushort)32);
        bw.Write((uint)pngs[i].Length);
        bw.Write((uint)offset);
        offset += pngs[i].Length;
    }
    foreach (var png in pngs) bw.Write(png);
    bw.Flush();
    File.WriteAllBytes(path, ms.ToArray());
}
