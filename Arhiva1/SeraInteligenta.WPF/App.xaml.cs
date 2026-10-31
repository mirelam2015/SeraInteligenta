using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FereastraPrincipala = SeraInteligenta.WPF.Views.MainWindow;

namespace SeraInteligenta.WPF;

public partial class App : Application
{
    private ServiceProvider? _servicii;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            string folderDate = Path.Combine(AppContext.BaseDirectory, "Date");
            _servicii = CompozitieServicii.Construieste(folderDate);

            var fereastra = _servicii.GetRequiredService<FereastraPrincipala>();
            this.MainWindow = fereastra;
            fereastra.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Aplicația nu a putut porni.\n\n{ex.Message}",
                "Sera Inteligentă", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Oprește și timerul de monitorizare (ServiciuMonitorizare e IDisposable)
        _servicii?.Dispose();
        base.OnExit(e);
    }
}
