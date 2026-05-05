using System.Windows;
using System.Windows.Controls;

namespace PdfManager.App.Views.Dialogs;

public partial class NewDocumentDialog : Window
{
    public double PageWidthPt  { get; private set; } = 595;
    public double PageHeightPt { get; private set; } = 842;

    public NewDocumentDialog() => InitializeComponent();

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (Tabs.SelectedIndex == 0)
        {
            if (PaperList.SelectedItem is ListBoxItem item)
            {
                var parts = ((string)item.Tag).Split(',');
                PageWidthPt  = double.Parse(parts[0]);
                PageHeightPt = double.Parse(parts[1]);
            }
        }
        else
        {
            var dpiValues = new[] { 72.0, 96.0, 150.0, 300.0 };
            double dpi = dpiValues[DpiBox.SelectedIndex];
            if (!double.TryParse(PixW.Text, out double w) || !double.TryParse(PixH.Text, out double h) || w <= 0 || h <= 0)
            {
                MessageBox.Show("Dimensioni non valide.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PageWidthPt  = w * 72.0 / dpi;
            PageHeightPt = h * 72.0 / dpi;
        }
        DialogResult = true;
    }
}
