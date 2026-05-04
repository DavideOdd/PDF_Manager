using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfManager.Core.Models;

namespace PdfManager.App.ViewModels;

public sealed partial class CombineDialogViewModel : ObservableObject
{
    [ObservableProperty] private CombineItem? _selectedItem;

    public ObservableCollection<CombineItem> Items { get; } = new();

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedItem == null) return;
        var idx = Items.IndexOf(SelectedItem);
        if (idx > 0) Items.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedItem == null) return;
        var idx = Items.IndexOf(SelectedItem);
        if (idx < Items.Count - 1) Items.Move(idx, idx + 1);
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedItem == null) return;
        Items.Remove(SelectedItem);
        SelectedItem = null;
    }

    [RelayCommand]
    private void RotateItem()
    {
        if (SelectedItem == null) return;
        SelectedItem.RotationDeg = (SelectedItem.RotationDeg + 90) % 360;
        OnPropertyChanged(nameof(SelectedItem));
    }

    public bool CanGenerate => Items.Count > 0;

    public void AddFile(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var kind = ext == ".pdf" ? CombineItemKind.Pdf : CombineItemKind.Image;
        Items.Add(new CombineItem { Path = path, Kind = kind });
        OnPropertyChanged(nameof(CanGenerate));
    }

    partial void OnSelectedItemChanged(CombineItem? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        RotateItemCommand.NotifyCanExecuteChanged();
    }
}
