using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PdfManager.App.ViewModels;
using PdfManager.Core.Services;

namespace PdfManager.App.Views.Dialogs;

public partial class CombineDialog : Window
{
    public CombineDialog()
    {
        InitializeComponent();
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "File supportati (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;
        var vm = (CombineDialogViewModel)DataContext;
        foreach (var f in dlg.FileNames) vm.AddFile(f);
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "File PDF (*.pdf)|*.pdf",
            FileName = "combinato.pdf",
            DefaultExt = ".pdf"
        };
        if (dlg.ShowDialog() != true) return;

        var vm = (CombineDialogViewModel)DataContext;
        var service = App.Services.GetRequiredService<IPdfDocumentService>();
        try
        {
            service.Combine(vm.Items.ToList(), dlg.FileName);
            MessageBox.Show($"PDF salvato in:\n{dlg.FileName}", "Combinazione completata",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante la combinazione:\n{ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        var vm = (CombineDialogViewModel)DataContext;
        foreach (var f in files) vm.AddFile(f);
    }
}
