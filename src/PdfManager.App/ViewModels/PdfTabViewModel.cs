using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using PdfManager.Core.Models;
using PdfManager.Core.Services;

namespace PdfManager.App.ViewModels;

public sealed partial class PdfTabViewModel : ObservableObject, IDropTarget
{
    private readonly IPdfRenderer _renderer;
    private readonly IPdfDocumentService _docService;

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _title = "Senza titolo";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private PageViewModel? _currentPage;
    [ObservableProperty] private double _zoom = 1.0;

    public string? Password { get; set; }
    public ObservableCollection<PageViewModel> Pages { get; } = new();

    private readonly Stack<Action> _undoStack = new();
    private readonly Stack<Action> _redoStack = new();

    public PdfTabViewModel(
        string filePath,
        IPdfRenderer renderer,
        IPdfDocumentService docService,
        string? password = null)
    {
        FilePath = filePath;
        _renderer = renderer;
        _docService = docService;
        Password = password;
        Title = System.IO.Path.GetFileName(filePath);
    }

    public async Task LoadAsync()
    {
        Pages.Clear();
        const double renderDpi = 150;
        const double thumbDpi = 48;
        var pageCount = _renderer.GetPageCount(FilePath, Password);
        for (int i = 0; i < pageCount; i++)
        {
            var size = _renderer.GetPageSize(FilePath, i, Password);
            var pvm = new PageViewModel
            {
                PageIndex = i,
                PageWidthPt = size.WidthPt,
                PageHeightPt = size.HeightPt,
                PixelWidth = (int)(size.WidthPt * renderDpi / 72),
                PixelHeight = (int)(size.HeightPt * renderDpi / 72),
                RenderDpi = renderDpi,
                IsLoading = true
            };
            Pages.Add(pvm);
        }
        if (Pages.Count > 0) CurrentPage = Pages[0];

        // Load annotations from file into each page
        LoadAnnotationsFromFile();

        // Render pages asynchronously
        foreach (var page in Pages)
        {
            _ = RenderPageAsync(page, renderDpi, thumbDpi);
        }
    }

    private void LoadAnnotationsFromFile()
    {
        // Read annotations from current file via PdfAnnotationReader
        // This requires opening PDF with PdfSharp directly — delegated to a service
        // For now, annotations live in memory per session; loaded via IAnnotationReader
    }

    private Task RenderPageAsync(PageViewModel page, double renderDpi, double thumbDpi) =>
        Task.Run(() =>
        {
            try
            {
                var rendered = _renderer.RenderPage(FilePath, page.PageIndex, renderDpi, Password);
                var thumb = _renderer.RenderPage(FilePath, page.PageIndex, thumbDpi, Password);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    page.RenderedImage = BuildBitmapSource(rendered);
                    page.ThumbnailImage = BuildBitmapSource(thumb);
                    page.IsLoading = false;
                });
            }
            catch { /* silently ignore render errors — page shows as blank */ }
        });

    private static BitmapSource BuildBitmapSource(RenderedPage r)
    {
        var bmp = new WriteableBitmap(r.PixelWidth, r.PixelHeight, r.Dpi, r.Dpi, PixelFormats.Bgra32, null);
        bmp.Lock();
        System.Runtime.InteropServices.Marshal.Copy(
            r.BgraBuffer, 0, bmp.BackBuffer, Math.Min(r.BgraBuffer.Length, r.Stride * r.PixelHeight));
        bmp.AddDirtyRect(new Int32Rect(0, 0, r.PixelWidth, r.PixelHeight));
        bmp.Unlock();
        bmp.Freeze();
        return bmp;
    }

    [RelayCommand]
    private void Close() { /* handled by MainViewModel */ }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) { await SaveAsAsync(); return; }
        await SaveToPathAsync(FilePath);
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "File PDF (*.pdf)|*.pdf",
            FileName = System.IO.Path.GetFileNameWithoutExtension(FilePath),
            DefaultExt = ".pdf"
        };
        if (dlg.ShowDialog() != true) return;
        FilePath = dlg.FileName;
        Title = System.IO.Path.GetFileName(FilePath);
        await SaveToPathAsync(FilePath);
    }

    private async Task SaveToPathAsync(string path)
    {
        var allStrokes = new List<InkStroke>();
        var allNotes = new List<FreeTextNote>();

        foreach (var page in Pages)
        {
            foreach (System.Windows.Ink.Stroke s in page.Strokes)
            {
                allStrokes.Add(ConvertStroke(s, page));
            }
            allNotes.AddRange(page.Notes);
        }

        await Task.Run(() =>
            _docService.SaveWithAnnotations(FilePath, path, allStrokes, allNotes, Password));

        // If save-in-place, keep same path; reload file reference
        if (path != FilePath) FilePath = path;
        IsDirty = false;
    }

    private static InkStroke ConvertStroke(System.Windows.Ink.Stroke s, PageViewModel page)
    {
        var polylines = new List<List<PointPt>>();
        var pts = new List<PointPt>();
        foreach (var sp in s.StylusPoints)
        {
            double pdfX = page.PxToPt(sp.X);
            double pdfY = page.FlipY(sp.Y);
            pts.Add(new PointPt(pdfX, pdfY));
        }
        if (pts.Count > 0) polylines.Add(pts);

        var da = s.DrawingAttributes;
        return new InkStroke
        {
            PageIndex = page.PageIndex,
            ColorR = da.Color.R,
            ColorG = da.Color.G,
            ColorB = da.Color.B,
            WidthPt = da.Width,
            Polylines = polylines
        };
    }

    [RelayCommand]
    private void RotatePageRight()
    {
        if (CurrentPage == null) return;
        var path = FilePath;
        _undoStack.Push(() => { /* restore */ });
        _docService.RotatePage(path, CurrentPage.PageIndex, 90, path, Password);
        _ = LoadAsync();
        IsDirty = true;
    }

    [RelayCommand]
    private void RotatePageLeft()
    {
        if (CurrentPage == null) return;
        _docService.RotatePage(FilePath, CurrentPage.PageIndex, 270, FilePath, Password);
        _ = LoadAsync();
        IsDirty = true;
    }

    [RelayCommand]
    private void DeletePage()
    {
        if (CurrentPage == null || Pages.Count <= 1) return;
        _docService.DeletePage(FilePath, CurrentPage.PageIndex, FilePath, Password);
        _ = LoadAsync();
        IsDirty = true;
    }

    // gong-wpf-dragdrop IDropTarget
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is PageViewModel)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = System.Windows.DragDropEffects.Move;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not PageViewModel src) return;
        var oldIdx = Pages.IndexOf(src);
        var newIdx = dropInfo.InsertIndex;
        if (newIdx > oldIdx) newIdx--;
        if (oldIdx == newIdx) return;
        Pages.Move(oldIdx, newIdx);
        _docService.MovePage(FilePath, oldIdx, newIdx, FilePath, Password);
        IsDirty = true;
    }

    partial void OnCurrentPageChanged(PageViewModel? value) { }

    partial void OnIsDirtyChanged(bool value) =>
        Title = System.IO.Path.GetFileName(FilePath) + (value ? " *" : "");
}
