using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Services;

namespace SeraInteligenta.Infrastructure.Senzori;

/// <summary>
/// „Lumea fizică” simulată a serei. Senzorii nu inventează valori fiecare pe cont propriu, ci citesc
/// din aceeași stare, ca într-o seră reală:
///  - un singur aer pentru toată sera (temperatură și umiditate), cu mici diferențe după poziție:
///    lângă pereți e puțin mai rece și mai umed;
///  - umiditatea solului și lumina, separate pe fiecare parcelă.
/// Valorile tind spre intervalul optim al plantelor, cu variații aleatoare și salturi ocazionale.
/// În M2, aici vor acționa actuatoarele (pompă, ventilator, încălzire, lămpi).
/// </summary>
public sealed class MediuSeraSimulat
{
    private const double RevenireSpreTinta = 0.1;
    private const double ProbabilitatePerturbare = 0.02;
    private const int MaxPasiOdata = 30;

    /// <summary>Cât de puternic e efectul pereților și pe ce distanță (m) se stinge.</summary>
    public const double RacireLaPerete = 1.0;
    public const double UmezireLaPerete = 3.0;
    public const double DistantaEfectPerete = 1.5;

    private readonly object _lock = new();
    private readonly Random _random;
    private readonly Stare _temperatura = new(ClimatComun.ImplicitPentru(TipSenzor.Temperatura));
    private readonly Stare _umiditateAer = new(ClimatComun.ImplicitPentru(TipSenzor.UmiditateAer));
    private readonly Dictionary<int, Stare> _sol = new();
    private readonly Dictionary<int, Stare> _lumina = new();
    private double _lungime = 24, _latime = 12;
    private DateTime _ultimulPas = DateTime.MinValue;

    public MediuSeraSimulat(Random random) => _random = random;

    /// <summary>Țintele climatului comun și ale fiecărei parcele, după plantele alese.</summary>
    public void Configureaza(ConfiguratieSera configuratie, ICatalogPlante catalog)
    {
        lock (_lock)
        {
            _lungime = Math.Max(0.1, configuratie.Sera.Lungime);
            _latime = Math.Max(0.1, configuratie.Sera.Latime);

            var plante = ClimatComun.PlanteDinSera(configuratie, catalog);
            _temperatura.Tinta = ClimatComun.Analizeaza(plante, TipSenzor.Temperatura).Tinta;
            _umiditateAer.Tinta = ClimatComun.Analizeaza(plante, TipSenzor.UmiditateAer).Tinta;

            var iduri = configuratie.Zone.Select(z => z.Id).ToHashSet();
            foreach (var id in _sol.Keys.Where(k => !iduri.Contains(k)).ToList()) _sol.Remove(id);
            foreach (var id in _lumina.Keys.Where(k => !iduri.Contains(k)).ToList()) _lumina.Remove(id);

            foreach (var zona in configuratie.Zone)
            {
                var planta = catalog.Gaseste(zona.PlantaPrincipalaId);
                SeteazaTinta(_sol, zona.Id, planta?.UmiditateSol ?? new IntervalValori(60, 75));
                SeteazaTinta(_lumina, zona.Id, planta?.Lumina ?? new IntervalValori(15, 30));
            }
        }
    }

    /// <summary>Valoarea reală (fără eroarea senzorului) a unei mărimi într-un punct al serei.</summary>
    public double Valoare(TipSenzor tip, int? zonaId, double x, double y)
    {
        lock (_lock)
        {
            AvanseazaDupaTimp();
            return tip switch
            {
                TipSenzor.Temperatura => _temperatura.Valoare - RacireLaPerete * EfectPerete(x, y),
                TipSenzor.UmiditateAer => _umiditateAer.Valoare + UmezireLaPerete * EfectPerete(x, y),
                TipSenzor.UmiditateSol => zonaId is int zs && _sol.TryGetValue(zs, out var s) ? s.Valoare : 50,
                TipSenzor.Lumina => zonaId is int zl && _lumina.TryGetValue(zl, out var l) ? l.Valoare : 20,
                _ => 0
            };
        }
    }

    /// <summary>Un pas de simulare (o secundă). Public pentru teste.</summary>
    public void Pas()
    {
        lock (_lock)
        {
            Avanseaza(_temperatura);
            Avanseaza(_umiditateAer);
            foreach (var s in _sol.Values) Avanseaza(s);
            foreach (var l in _lumina.Values) Avanseaza(l);
        }
    }

    private void AvanseazaDupaTimp()
    {
        var acum = DateTime.UtcNow;
        if (_ultimulPas == DateTime.MinValue) { _ultimulPas = acum; return; }

        int pasi = (int)(acum - _ultimulPas).TotalSeconds;
        if (pasi <= 0) return;
        _ultimulPas = _ultimulPas.AddSeconds(pasi);
        for (int i = 0; i < Math.Min(pasi, MaxPasiOdata); i++) Pas();
    }

    private void Avanseaza(Stare stare)
    {
        var t = stare.Tinta;
        double latime = Math.Max(0.5, t.Latime);
        stare.Valoare += (t.Mijloc - stare.Valoare) * RevenireSpreTinta + Aleator.Gauss(_random) * latime / 15;
        if (_random.NextDouble() < ProbabilitatePerturbare)
            stare.Valoare += (_random.NextDouble() < 0.5 ? -1 : 1) * latime * 0.8;
    }

    /// <summary>1 lângă perete, scade spre 0 în interiorul serei.</summary>
    private double EfectPerete(double x, double y)
    {
        double d = Math.Max(0, Math.Min(Math.Min(x, _lungime - x), Math.Min(y, _latime - y)));
        return Math.Exp(-d / DistantaEfectPerete);
    }

    private static void SeteazaTinta(Dictionary<int, Stare> stari, int zonaId, IntervalValori tinta)
    {
        if (stari.TryGetValue(zonaId, out var stare)) stare.Tinta = tinta;
        else stari[zonaId] = new Stare(tinta);
    }

    private sealed class Stare
    {
        public Stare(IntervalValori tinta)
        {
            Tinta = tinta;
            Valoare = tinta.Mijloc;
        }

        public IntervalValori Tinta { get; set; }
        public double Valoare { get; set; }
    }
}

/// <summary>
/// Un senzor simulat: citește valoarea reală din mediu, în poziția dispozitivului,
/// și adaugă o eroare de calibrare (fixă, proprie fiecărui senzor) și zgomot de măsurare.
/// </summary>
public sealed class SenzorSimulat : ISenzor
{
    private readonly MediuSeraSimulat _mediu;
    private readonly Random _random;
    private readonly double _eroareCalibrare;
    private readonly object _lock = new();

    public SenzorSimulat(DispozitivSenzor dispozitiv, TipSenzor tip, MediuSeraSimulat mediu, Random random)
    {
        Dispozitiv = dispozitiv;
        Tip = tip;
        Id = $"{dispozitiv.Id}-{tip}";
        Unitate = InfoSenzori.Unitate(tip);
        _mediu = mediu;
        _random = random;
        _eroareCalibrare = Aleator.Gauss(random) * EroareCalibrare(tip);
    }

    public string Id { get; }
    public DispozitivSenzor Dispozitiv { get; }
    public TipSenzor Tip { get; }
    public string Unitate { get; }
    public double Valoare { get; private set; }

    public CitireSenzor Citeste()
    {
        lock (_lock)
        {
            double v = _mediu.Valoare(Tip, Dispozitiv.ZonaId, Dispozitiv.X, Dispozitiv.Y)
                       + _eroareCalibrare + Aleator.Gauss(_random) * Zgomot(Tip);
            var domeniu = InfoSenzori.DomeniuFizic(Tip);
            Valoare = Math.Round(Math.Clamp(v, domeniu.Min, domeniu.Max), 1);
            return new CitireSenzor(Id, Dispozitiv.Id, Dispozitiv.ZonaId, Tip, Valoare, Unitate,
                                    Dispozitiv.X, Dispozitiv.Y, DateTime.Now);
        }
    }

    // Valori tipice pentru senzori ieftini (de exemplu SHT31 pentru aer, senzori capacitivi pentru sol)
    private static double EroareCalibrare(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => 0.2,
        TipSenzor.UmiditateAer => 1.0,
        TipSenzor.UmiditateSol => 2.0,
        _ => 1.0
    };

    private static double Zgomot(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => 0.05,
        TipSenzor.UmiditateAer => 0.3,
        TipSenzor.UmiditateSol => 0.4,
        _ => 0.5
    };
}

/// <summary>Creează câte un senzor simulat pentru fiecare mărime măsurată de fiecare dispozitiv.</summary>
public sealed class FabricaSenzoriSimulati : IFabricaSenzori
{
    private readonly MediuSeraSimulat _mediu;
    private readonly ICatalogPlante _catalog;
    private readonly int _samanta;

    /// <param name="samanta">Aceeași sămânță = aceleași erori de calibrare; util în teste.</param>
    public FabricaSenzoriSimulati(MediuSeraSimulat mediu, ICatalogPlante catalog, int samanta)
    {
        _mediu = mediu;
        _catalog = catalog;
        _samanta = samanta;
    }

    public IReadOnlyList<ISenzor> Creeaza(ConfiguratieSera configuratie, IReadOnlyList<DispozitivSenzor> dispozitive)
    {
        _mediu.Configureaza(configuratie, _catalog);

        // Sămânța derivă din ID-ul senzorului: un senzor recreat (de exemplu după redimensionarea
        // unei parcele) își păstrează aceeași eroare de calibrare, deci valorile nu „sar”.
        return dispozitive
            .SelectMany(d => d.Tipuri.Select(tip =>
                (ISenzor)new SenzorSimulat(d, tip, _mediu, new Random(_samanta ^ Aleator.HashStabil($"{d.Id}-{tip}")))))
            .ToList();
    }
}

internal static class Aleator
{
    /// <summary>Distribuție normală standard (Box–Muller).</summary>
    public static double Gauss(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    /// <summary>Hash FNV-1a: același text dă același număr la fiecare rulare (spre deosebire de GetHashCode).</summary>
    public static int HashStabil(string text)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char c in text) h = (h ^ c) * 16777619;
            return (int)h;
        }
    }
}
