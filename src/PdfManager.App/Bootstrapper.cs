using Microsoft.Extensions.DependencyInjection;
using PdfManager.App.ViewModels;
using PdfManager.Core.Services;

namespace PdfManager.App;

public static class Bootstrapper
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IAnnotationWriter, PdfAnnotationWriter>();
        services.AddSingleton<IAnnotationReader, PdfAnnotationReader>();
        services.AddSingleton<IPdfRenderer, PdfiumRenderer>();
        services.AddSingleton<IPdfDocumentService, PdfSharpDocumentService>();

        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }
}
