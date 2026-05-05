using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfManager.App.Services;
using PdfManager.Core.Services;

namespace PdfManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IPdfRenderer _renderer;
    private readonly IPdfDocumentService _docService;

    [ObservableProperty] private PdfTabViewModel? _activeTab;

    public ObservableCollection<PdfTabViewModel> OpenTabs { get; } = new();
    public PenToolViewModel PenTool { get; } = new();

    public bool IsPenMode
    {
        get => PenTool.EditingMode == InkCanvasEditingMode.Ink;
        set
        {
            if (value) { _isTextMode = false; _isPanMode = false; PenTool.ActivatePen(); }
            else if (IsPenMode) PenTool.Deactivate();
            NotifyAllTools();
        }
    }

    public bool IsTextMode
    {
        get => _isTextMode;
        set
        {
            if (value) { PenTool.Deactivate(); _isPanMode = false; }
            SetProperty(ref _isTextMode, value);
            NotifyAllTools();
        }
    }
    private bool _isTextMode;

    public bool IsEraserMode
    {
        get => PenTool.IsEraserActive;
        set
        {
            if (value) { _isTextMode = false; _isPanMode = false; PenTool.ActivateEraser(); }
            else if (IsEraserMode) PenTool.Deactivate();
            NotifyAllTools();
        }
    }

    private void NotifyAllTools()
    {
        OnPropertyChanged(nameof(IsPenMode));
        OnPropertyChanged(nameof(IsTextMode));
        OnPropertyChanged(nameof(IsEraserMode));
        OnPropertyChanged(nameof(IsPanMode));
    }

    [ObservableProperty]
    private bool _isPanMode;

    partial void OnIsPanModeChanged(bool value)
    {
        if (value) { PenTool.Deactivate(); _isTextMode = false; }
        NotifyAllTools();
    }

    public MainViewModel(IPdfRenderer renderer, IPdfDocumentService docService)
    {
        _renderer = renderer;
        _docService = docService;
    }

    partial void OnActiveTabChanged(PdfTabViewModel? oldValue, PdfTabViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnActiveTabPropertyChanged;
        if (newValue != null) newValue.PropertyChanged += OnActiveTabPropertyChanged;
        OnPropertyChanged(nameof(ZoomLabel));
    }

    private void OnActiveTabPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfTabViewModel.Zoom))
            OnPropertyChanged(nameof(ZoomLabel));
    }

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".gif", ".webp" };

    [RelayCommand]
    private async Task OpenAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Tutti i file supportati (*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.gif)|*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif|" +
                     "File PDF (*.pdf)|*.pdf|" +
                     "Immagini (*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var f in dlg.FileNames) await OpenFileAsync(f);
    }

    public async Task OpenFileAsync(string path, string? password = null)
    {
        App.Log($"OpenFile: {path}");
        try
        {
            // Convert image to temp PDF
            if (ImageExtensions.Contains(Path.GetExtension(path)))
            {
                App.Log("Conversione immagine → PDF...");
                path = await Task.Run(() => _docService.ImageToTempPdf(path));
                App.Log($"PDF temp: {path}");
                password = null;
            }

            App.Log("Controllo password...");
            if (password == null && _renderer.IsPasswordProtected(path))
            {
                password = AskPassword(path);
                if (password == null) return;
            }

            App.Log("Creo tab...");
            var tab = new PdfTabViewModel(path, _renderer, _docService, password);
            OpenTabs.Add(tab);
            ActiveTab = tab;
            App.Log("LoadAsync...");
            await tab.LoadAsync();
            App.Log("LoadAsync completato.");
        }
        catch (PdfPasswordRequiredException)
        {
            var pwd = AskPassword(path);
            if (pwd != null) await OpenFileAsync(path, pwd);
        }
        catch (Exception ex)
        {
            App.Log($"ERRORE OpenFile: {ex}");
            MessageBox.Show($"Errore apertura PDF:\n{ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? AskPassword(string path)
    {
        var dlg = new Views.Dialogs.PasswordDialog(System.IO.Path.GetFileName(path));
        return dlg.ShowDialog() == true ? dlg.EnteredPassword : null;
    }

    [RelayCommand]
    private async Task SaveAsync() { if (ActiveTab != null) await ActiveTab.SaveCommand.ExecuteAsync(null); }

    [RelayCommand]
    private async Task SaveAsAsync() { if (ActiveTab != null) await ActiveTab.SaveAsCommand.ExecuteAsync(null); }

    [RelayCommand]
    private void CloseTab(PdfTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;

        if (tab.IsDirty)
        {
            var r = MessageBox.Show(
                $"Vuoi salvare le modifiche a '{tab.Title.TrimEnd(' ', '*')}'?",
                "Modifiche non salvate",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            if (r == MessageBoxResult.Cancel) return;
            if (r == MessageBoxResult.Yes) tab.SaveCommand.Execute(null);
        }
        OpenTabs.Remove(tab);
        ActiveTab = OpenTabs.LastOrDefault();
    }

    [RelayCommand]
    private void OpenCombine()
    {
        var dlg = new Views.Dialogs.CombineDialog();
        dlg.ShowDialog();
    }

    [RelayCommand]
    private void OpenSplit()
    {
        if (ActiveTab == null) return;
        var dlg = new Views.Dialogs.SplitDialog(ActiveTab.FilePath, ActiveTab.Pages.Count);
        dlg.ShowDialog();
    }

    [RelayCommand]
    private void RotateRight() => ActiveTab?.RotatePageRightCommand.Execute(null);

    [RelayCommand]
    private void RotateLeft() => ActiveTab?.RotatePageLeftCommand.Execute(null);

    [RelayCommand]
    private void ZoomIn()    { if (ActiveTab != null) ActiveTab.Zoom = Math.Min(4.0, Math.Round(ActiveTab.Zoom + 0.15, 2)); }

    [RelayCommand]
    private void ZoomOut()   { if (ActiveTab != null) ActiveTab.Zoom = Math.Max(0.2, Math.Round(ActiveTab.Zoom - 0.15, 2)); }

    [RelayCommand]
    private void ZoomReset() { if (ActiveTab != null) ActiveTab.Zoom = 1.0; }

    public string ZoomLabel => ActiveTab != null ? $"{(int)Math.Round(ActiveTab.Zoom * 100)}%" : "100%";

    [RelayCommand]
    private void DeletePage() => ActiveTab?.DeletePageCommand.Execute(null);

    [RelayCommand]
    private void Undo() => ActiveTab?.Undo();

    [RelayCommand]
    private void Redo() => ActiveTab?.Redo();

    [RelayCommand]
    private void TogglePen() { if (PenTool.IsPenActive) PenTool.Deactivate(); else PenTool.ActivatePen(); OnPropertyChanged(nameof(IsPenMode)); OnPropertyChanged(nameof(IsEraserMode)); }

    [RelayCommand]
    private void ToggleEraser() { if (PenTool.IsEraserActive) PenTool.Deactivate(); else PenTool.ActivateEraser(); OnPropertyChanged(nameof(IsPenMode)); OnPropertyChanged(nameof(IsEraserMode)); }

    [RelayCommand]
    private void DeactivateTool() { PenTool.Deactivate(); IsTextMode = false; IsPanMode = false; OnPropertyChanged(nameof(IsPenMode)); OnPropertyChanged(nameof(IsEraserMode)); }

    [RelayCommand]
    private void TogglePan() { IsPanMode = !IsPanMode; }

    public bool IsDarkMode => ThemeService.Instance.IsDark;

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeService.Instance.Toggle();
        OnPropertyChanged(nameof(IsDarkMode));
    }

    public void HandleFileDrop(string[] paths)
    {
        foreach (var p in paths)
        {
            var ext = Path.GetExtension(p);
            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) || ImageExtensions.Contains(ext))
                _ = OpenFileAsync(p);
        }
    }
}
