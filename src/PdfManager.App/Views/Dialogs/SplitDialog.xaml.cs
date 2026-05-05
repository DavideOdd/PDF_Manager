using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PdfManager.Core.Services;

namespace PdfManager.App.Views.Dialogs;

public partial class SplitDialog : Window
{
    private readonly string _sourcePath;
    private readonly int _pageCount;

    public SplitDialog(string sourcePath, int pageCount)
    {
        InitializeComponent();
        _sourcePath = sourcePath;
        _pageCount  = pageCount;
        InfoText.Text = $"Documento: {System.IO.Path.GetFileName(sourcePath)} — {pageCount} pagine";
        UpdateAutoPreview();
        AddManualBlock(); // start with one block
    }

    // ── Auto ────────────────────────────────────────────────────────────────

    private void BlockSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BlockCountLabel == null) return;
        BlockCountLabel.Text = ((int)BlockSlider.Value).ToString();
        UpdateAutoPreview();
    }

    private void UpdateAutoPreview()
    {
        if (AutoPreview == null) return;
        int n = (int)BlockSlider.Value;
        var items = new List<string>();
        var ranges = ComputeAutoRanges(n);
        for (int i = 0; i < ranges.Count; i++)
            items.Add($"Blocco {i + 1}: pagine {ranges[i].start + 1}–{ranges[i].end + 1}");
        AutoPreview.ItemsSource = items;
    }

    private List<(int start, int end)> ComputeAutoRanges(int n)
    {
        var list = new List<(int, int)>();
        int perBlock = _pageCount / n;
        int remainder = _pageCount % n;
        int cursor = 0;
        for (int i = 0; i < n; i++)
        {
            int size = perBlock + (i < remainder ? 1 : 0);
            if (size == 0) break;
            list.Add((cursor, cursor + size - 1));
            cursor += size;
        }
        return list;
    }

    // ── Manual ──────────────────────────────────────────────────────────────

    private int _blockNumber = 1;

    private void AddBlock_Click(object sender, RoutedEventArgs e) => AddManualBlock();

    private void AddManualBlock()
    {
        int num = _blockNumber++;
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        panel.Children.Add(new TextBlock
        {
            Text = $"Blocco {num}:",
            Width = 80,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13
        });
        var tb = new TextBox
        {
            Width = 200,
            Height = 28,
            FontSize = 13,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 0, 6, 0),
            Tag = num,
            ToolTip = "Es: 1-5  oppure  1,3,5,7"
        };
        panel.Children.Add(tb);
        var del = new Button
        {
            Content = "✕",
            Width = 26, Height = 26,
            Margin = new Thickness(6, 0, 0, 0),
            FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        del.Click += (_, _) => ManualBlocksPanel.Children.Remove(panel);
        panel.Children.Add(del);
        ManualBlocksPanel.Children.Add(panel);
    }

    private List<(int block, List<int> pages)> ParseManualBlocks()
    {
        var result = new List<(int, List<int>)>();
        foreach (StackPanel row in ManualBlocksPanel.Children)
        {
            var tb = row.Children.OfType<TextBox>().FirstOrDefault();
            int blockNum = tb?.Tag is int n ? n : 0;
            var pages = ParsePageRange(tb?.Text ?? string.Empty);
            if (pages.Count > 0)
                result.Add((blockNum, pages));
        }
        return result;
    }

    private List<int> ParsePageRange(string text)
    {
        var pages = new HashSet<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Contains('-'))
            {
                var seg = t.Split('-');
                if (int.TryParse(seg[0].Trim(), out int a) && int.TryParse(seg[1].Trim(), out int b))
                    for (int i = a; i <= b; i++) pages.Add(i - 1); // 0-indexed
            }
            else if (int.TryParse(t, out int p))
                pages.Add(p - 1);
        }
        return pages.Where(p => p >= 0 && p < _pageCount).OrderBy(p => p).ToList();
    }

    // ── Save ────────────────────────────────────────────────────────────────

    private void DoSplit_Click(object sender, RoutedEventArgs e)
    {
        var service = App.Services.GetRequiredService<IPdfDocumentService>();
        bool isAuto = Tabs.SelectedIndex == 0;

        List<List<int>> blocks;
        if (isAuto)
        {
            blocks = ComputeAutoRanges((int)BlockSlider.Value)
                .Select(r => Enumerable.Range(r.start, r.end - r.start + 1).ToList())
                .ToList();
        }
        else
        {
            var manual = ParseManualBlocks();
            if (manual.Count == 0)
            {
                MessageBox.Show("Nessun blocco definito.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            blocks = manual.Select(b => b.pages).ToList();
        }

        var picker = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Scegli cartella di destinazione (naviga nella cartella e premi Salva)",
            FileName = "Seleziona questa cartella",
            Filter = "Cartella|*.none",
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true
        };
        if (picker.ShowDialog() != true) return;
        string folder = System.IO.Path.GetDirectoryName(picker.FileName)!;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(_sourcePath);
        int saved = 0;
        try
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Count == 0) continue;
                string outPath = System.IO.Path.Combine(folder, $"{baseName}_parte{i + 1}.pdf");
                service.Split(_sourcePath, blocks[i], outPath);
                saved++;
            }
            MessageBox.Show($"Divisione completata: {saved} file salvati in\n{folder}",
                "Fatto", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante la divisione:\n{ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
