using Microsoft.Extensions.DependencyInjection;
using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Services;
using SeraInteligenta.Core.Validare;
using SeraInteligenta.Core.Validare.Reguli;
using SeraInteligenta.Infrastructure.Date;
using SeraInteligenta.Infrastructure.Monitorizare;
using SeraInteligenta.Infrastructure.Senzori;
using SeraInteligenta.WPF.ViewModels;
using SeraInteligenta.WPF.Views;

namespace SeraInteligenta.WPF;

/// <summary>
/// Rădăcina de compoziție (composition root): SINGURUL loc din aplicație unde se aleg implementările.
/// Toate celelalte clase primesc dependențele prin constructor.
/// </summary>
public static class CompozitieServicii
{
    public static ServiceProvider Construieste(string folderDate)
    {
        var servicii = new ServiceCollection();

        // Date (JSON). În M4 se adaugă o variantă SQLite pentru istoric.
        servicii.AddSingleton(new OptiuniDate(folderDate));
        servicii.AddSingleton<ICatalogPlante, CatalogPlanteJson>();
        servicii.AddSingleton<IDepozitConfiguratie, DepozitConfiguratieJson>();

        // Logica serei
        servicii.AddSingleton<IServiciuAmplasare, ServiciuAmplasare>();

        // Regulile de validare: mai multe implementări ale aceleiași interfețe.
        // ValidatorSera le primește pe toate ca IEnumerable<IRegulaValidare>.
        servicii.AddSingleton<IRegulaValidare, RegulaDimensiuniSera>();
        servicii.AddSingleton<IRegulaValidare, RegulaDimensiuniZone>();
        servicii.AddSingleton<IRegulaValidare, RegulaSuprafata>();
        servicii.AddSingleton<IRegulaValidare, RegulaAmplasare>();
        servicii.AddSingleton<IRegulaValidare, RegulaCompatibilitatePlante>();
        servicii.AddSingleton<IRegulaValidare, RegulaClimatComun>();
        servicii.AddSingleton<IRegulaValidare, RegulaInaltimePlante>();
        servicii.AddSingleton<IValidatorSera, ValidatorSera>();

        // Senzori: unde se montează (calculat din dimensiuni) și ce senzori se folosesc.
        // Aici se schimbă simularea cu hardware real sau MQTT (M3), fără alte modificări.
        servicii.AddSingleton<IServiciuAmplasareSenzori, ServiciuAmplasareSenzori>();
        servicii.AddSingleton(_ => new MediuSeraSimulat(new Random()));
        servicii.AddSingleton<IFabricaSenzori>(sp => new FabricaSenzoriSimulati(
            sp.GetRequiredService<MediuSeraSimulat>(), sp.GetRequiredService<ICatalogPlante>(), samanta: 2026));
        servicii.AddSingleton<IServiciuMonitorizare, ServiciuMonitorizare>();

        // Interfața (MVVM)
        servicii.AddSingleton<MainViewModel>();
        servicii.AddSingleton<MainWindow>();

        // ValidateOnBuild: dacă lipsește o înregistrare, aplicația anunță imediat la pornire
        return servicii.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
