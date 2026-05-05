using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfManager.App.Services;

public sealed partial class ThemeService : ObservableObject
{
    public static ThemeService Instance { get; } = new();

    [ObservableProperty] private bool _isDark;

    private const string LightUri = "Resources/Themes/Light.xaml";
    private const string DarkUri  = "Resources/Themes/Dark.xaml";

    private ThemeService() { }

    public void Toggle()
    {
        IsDark = !IsDark;
        Apply();
    }

    public void Apply()
    {
        var uri  = new Uri(IsDark ? DarkUri : LightUri, UriKind.Relative);
        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Light.xaml") == true ||
            d.Source?.OriginalString.Contains("Dark.xaml")  == true);
        if (existing != null) dicts.Remove(existing);
        dicts.Add(new ResourceDictionary { Source = uri });
    }
}
