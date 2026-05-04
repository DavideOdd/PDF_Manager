# Generates assets\icons\app.ico using inline C# with GDI+
# Design: indigo document, folded corner, red PDF band

$root   = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $root "assets\icons"
$outPath = Join-Path $outDir "app.ico"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

public static class IconMaker
{
    public static void Make(string outPath)
    {
        int[] sizes = { 256, 64, 48, 32, 16 };
        var pngs = new List<byte[]>();
        foreach (var sz in sizes) pngs.Add(MakeFrame(sz));
        WriteIco(outPath, sizes, pngs);
    }

    static byte[] MakeFrame(int s)
    {
        using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode    = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        int pad  = Math.Max(1, (int)(s * 0.10));
        int fold = Math.Max(2, (int)(s * 0.22));
        int docW = s - pad * 2;
        int docH = (int)(s * 0.80);
        int docX = pad;
        int docY = (s - docH) / 2;

        // Document body (indigo)
        var bodyPts = new Point[] {
            new Point(docX,              docY + fold),
            new Point(docX,              docY + docH),
            new Point(docX + docW,       docY + docH),
            new Point(docX + docW,       docY + fold),
            new Point(docX + docW - fold, docY),
            new Point(docX,              docY)
        };
        using var bodyBrush = new SolidBrush(Color.FromArgb(255, 63, 81, 181));
        g.FillPolygon(bodyBrush, bodyPts);

        // Fold triangle (lighter indigo)
        var foldPts = new Point[] {
            new Point(docX + docW - fold, docY),
            new Point(docX + docW,        docY + fold),
            new Point(docX + docW - fold, docY + fold)
        };
        using var foldBrush = new SolidBrush(Color.FromArgb(255, 121, 134, 203));
        g.FillPolygon(foldBrush, foldPts);

        // Red "PDF" band
        if (s >= 24)
        {
            int bandH = Math.Max(4, (int)(s * 0.22));
            int bandY = docY + (int)(docH * 0.50);
            using var redBrush = new SolidBrush(Color.FromArgb(255, 211, 47, 47));
            g.FillRectangle(redBrush, docX, bandY, docW, bandH);

            if (s >= 32)
            {
                float fs = Math.Max(6, bandH * 0.7f);
                using var font  = new Font("Arial", fs, FontStyle.Bold, GraphicsUnit.Pixel);
                using var white = new SolidBrush(Color.White);
                var sf = new StringFormat {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("PDF", font, white,
                    new RectangleF(docX, bandY, docW, bandH), sf);
            }
        }

        // Subtle outline
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

        bw.Write((ushort)0);             // reserved
        bw.Write((ushort)1);             // type ICO
        bw.Write((ushort)sizes.Length);  // count

        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)0);   // color count
            bw.Write((byte)0);   // reserved
            bw.Write((ushort)1); // planes
            bw.Write((ushort)32); // bpp
            bw.Write((uint)pngs[i].Length);
            bw.Write((uint)offset);
            offset += pngs[i].Length;
        }
        foreach (var png in pngs) bw.Write(png);

        bw.Flush();
        File.WriteAllBytes(path, ms.ToArray());
    }
}
"@ -ReferencedAssemblies "System.Drawing"

[IconMaker]::Make($outPath)
Write-Host "Icona generata: $outPath" -ForegroundColor Green
