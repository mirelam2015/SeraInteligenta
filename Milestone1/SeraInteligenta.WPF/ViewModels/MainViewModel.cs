using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Services;
using SeraInteligenta.WPF.Helpers;

namespace SeraInteligenta.WPF.ViewModels;

/// <summary>
/// ViewModel-ul ferestrei principale. Toate dependențele vin prin constructor, din containerul IoC;
/// clasa nu creează niciodată singură un senzor, un validator sau un fișier.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    /// <summary>Câți pixeli are un metru pe hartă (harta se scalează apoi automat la fereastră).</summary>
    public const double Scala = 30;

    private static readonly TimeSpan IntervalCitire = TimeSpan.FromSeconds(2);
    private const int MaxIntrariJurnal = 200;

    private readonly IDepozitConfiguratie _depozit;
    private readonly ICatalogPlante _catalog;
    private readonly IValidatorSera _validator;
    private readonly IServiciuAmplasare _amplasare;
    private readonly IServiciuMonitorizare _monitor;
    private readonly IServiciuAmplasareSenzori _amplasareSenzori;
    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<string, SenzorHartaViewModel> _senzoriDupaId = new();

    private ConfiguratieSera _config = new();
    private RezultatValidare _rezultat = new(Array.Empty<ProblemaValidare>());
    private PlanAmplasare _plan = new(Array.Empty<AmplasareZona>(), 0, 0);

    private string _seraLungimeText = string.Empty;
    private string _seraLatimeText = string.Empty;
    private string _seraInaltimeText = string.Empty;
    private string _latimeAleeText = string.Empty;
    private string _procentCuloarText = string.Empty;
    private ZonaViewModel? _zonaSelectata;
    private ProblemaValidare? _problemaSelectata;
    private bool _citireAutomata;
    private string _ultimaCitire = "–";
    private int _ultimulNumarErori = -1;
    private AnalizaClimat _tintaTemperatura = ClimatComun.Analizeaza(Array.Empty<ProfilPlanta>(), TipSenzor.Temperatura);
    private AnalizaClimat _tintaUmiditate = ClimatComun.Analizeaza(Array.Empty<ProfilPlanta>(), TipSenzor.UmiditateAer);
    private (double Medie, double Min, double Max)? _climatTemperatura;
    private (double Medie, double Min, double Max)? _climatUmiditate;

    public MainViewModel(IDepozitConfiguratie depozit, ICatalogPlante catalog, IValidatorSera validator,
                         IServiciuAmplasare amplasare, IServiciuMonitorizare monitor,
                         IServiciuAmplasareSenzori amplasareSenzori)
    {
        _depozit = depozit;
        _catalog = catalog;
        _validator = validator;
        _amplasare = amplasare;
        _monitor = monitor;
        _amplasareSenzori = amplasareSenzori;
        _dispatcher = Dispatcher.CurrentDispatcher;

        Plante = catalog.Toate;
        PlanteSecundare = new[] { new ProfilPlanta { IdPlanta = string.Empty, Nume = "(fără plantă secundară)" } }
            .Concat(catalog.Toate).ToList();

        SalveazaCommand = new RelayCommand(_ => Salveaza(), _ => PoateSalva);
        ReincarcaCommand = new RelayCommand(_ => Reincarca());
        ImpliciteCommand = new RelayCommand(_ => Incarca(ConfiguratieSera.Implicita(), "S-a revenit la configurația implicită (nesalvată)."));
        CitesteAcumCommand = new RelayCommand(_ => AplicaCitiri(_monitor.CitesteToti()));

        _monitor.CitiriNoi += LaCitiriNoi;

        Incarca(_depozit.Incarca(), $"Aplicația a pornit. Catalog: {catalog.Toate.Count} plante.");
        CitireAutomata = true;
    }

    // ================= Colecții pentru interfață =================

    public IReadOnlyList<ProfilPlanta> Plante { get; }
    public IReadOnlyList<ProfilPlanta> PlanteSecundare { get; }
    public ObservableCollection<ZonaViewModel> Zone { get; } = new();
    public ObservableCollection<ProblemaValidare> Probleme { get; } = new();
    public ObservableCollection<string> Jurnal { get; } = new();

    /// <summary>Senzorii desenați pe hartă; se recalculează la fiecare modificare a dimensiunilor.</summary>
    public ObservableCollection<SenzorHartaViewModel> SenzoriHarta { get; } = new();

    // ================= Dimensiunile serei =================

    public string SeraLungimeText
    {
        get => _seraLungimeText;
        set => SeteazaNumar(ref _seraLungimeText, value, v => _config.Sera.Lungime = v, 1, 500, "o lungime între 1 și 500 m");
    }

    public string SeraLatimeText
    {
        get => _seraLatimeText;
        set => SeteazaNumar(ref _seraLatimeText, value, v => _config.Sera.Latime = v, 1, 200, "o lățime între 1 și 200 m");
    }

    public string SeraInaltimeText
    {
        get => _seraInaltimeText;
        set => SeteazaNumar(ref _seraInaltimeText, value, v => _config.Sera.Inaltime = v, 1, 20, "o înălțime între 1 și 20 m");
    }

    public string LatimeAleeText
    {
        get => _latimeAleeText;
        set => SeteazaNumar(ref _latimeAleeText, value, v => _config.Sera.LatimeAlee = v, 0, 10, "o lățime între 0 și 10 m");
    }

    public string ProcentCuloarText
    {
        get => _procentCuloarText;
        set => SeteazaNumar(ref _procentCuloarText, value, v => _config.Sera.ProcentMinimCuloare = v, 0, 90, "un procent între 0 și 90");
    }

    private void SeteazaNumar(ref string camp, string valoare, Action<double> aplica, double min, double max,
                              string descriere, [CallerMemberName] string? proprietate = null)
    {
        if (!SetProperty(ref camp, valoare, proprietate)) return;

        if (ParserNumere.TryParse(valoare, out double v) && v >= min && v <= max)
        {
            SeteazaEroare(proprietate!, null);
            aplica(v);
            Revalideaza();
        }
        else
        {
            SeteazaEroare(proprietate!, $"Introdu {descriere}.");
            OnPropertyChanged(nameof(MotivSalvare));
        }
    }

    // ================= Indicatori =================

    public string SuprafataTotalaText => $"{_config.Sera.SuprafataTotala:0.#} m²";
    public string VolumText => $"{_config.Sera.VolumTotal:0.#} m³";
    public string SuprafataOcupataText => $"{SuprafataOcupata:0.#} m² ({ProcentDin(SuprafataOcupata):0.#}%)";
    public string CuloareText => $"{_config.Sera.SuprafataTotala - SuprafataOcupata:0.#} m² ({ProcentDin(_config.Sera.SuprafataTotala - SuprafataOcupata):0.#}%)";
    public string CuloareMinimText => $"minim {_config.Sera.ProcentMinimCuloare:0.#}% ({_config.Sera.SuprafataMinimaCuloare:0.#} m²)";

    private double SuprafataOcupata => _config.Zone.Sum(z => Math.Max(0, z.SuprafataMp));
    private double ProcentDin(double valoare) =>
        _config.Sera.SuprafataTotala > 0 ? valoare / _config.Sera.SuprafataTotala * 100 : 0;

    public int NumarErori => _rezultat.NumarErori;
    public int NumarAvertismente => _rezultat.NumarAvertismente;

    public NivelSeveritate StareGenerala =>
        NumarErori > 0 ? NivelSeveritate.Eroare
        : NumarAvertismente > 0 ? NivelSeveritate.Avertisment
        : NivelSeveritate.Info;

    public string StareGeneralaText =>
        NumarErori > 0 ? $"{Plural(NumarErori, "eroare", "erori")}, {Plural(NumarAvertismente, "avertisment", "avertismente")}"
        : NumarAvertismente > 0 ? $"Validă, cu {Plural(NumarAvertismente, "avertisment", "avertismente")}"
        : "Configurație validă";

    private static string Plural(int n, string unu, string multe) => n == 1 ? $"1 {unu}" : $"{n} {multe}";

    // ================= Harta =================

    /// <summary>Suprafața desenată: sera, dar extinsă dacă parcelele ies din ea (ca să se vadă depășirea).</summary>
    public double HartaLatime => Math.Max(1, Math.Max(_config.Sera.Lungime, _plan.LungimeNecesara)) * Scala;
    public double HartaInaltime => Math.Max(1, Math.Max(_config.Sera.Latime, _plan.LatimeNecesara)) * Scala;
    public double SeraLatimePx => Math.Max(1, _config.Sera.Lungime) * Scala;
    public double SeraInaltimePx => Math.Max(1, _config.Sera.Latime) * Scala;

    public ZonaViewModel? ZonaSelectata
    {
        get => _zonaSelectata;
        set
        {
            if (_zonaSelectata == value) return;
            if (_zonaSelectata is not null) _zonaSelectata.EsteSelectata = false;
            _zonaSelectata = value;
            if (_zonaSelectata is not null) _zonaSelectata.EsteSelectata = true;
            OnPropertyChanged();
        }
    }

    /// <summary>Un clic pe o problemă din listă selectează zona ei pe hartă.</summary>
    public ProblemaValidare? ProblemaSelectata
    {
        get => _problemaSelectata;
        set
        {
            if (!SetProperty(ref _problemaSelectata, value)) return;
            if (value?.ZonaId is int id) ZonaSelectata = Zone.FirstOrDefault(z => z.Id == id);
        }
    }

    // ================= Senzori =================

    public bool CitireAutomata
    {
        get => _citireAutomata;
        set
        {
            if (!SetProperty(ref _citireAutomata, value)) return;
            if (value) _monitor.Porneste(IntervalCitire);
            else _monitor.Opreste();
            AdaugaInJurnal(value ? $"Citire automată pornită (la {IntervalCitire.TotalSeconds:0} s)." : "Citire automată oprită.");
        }
    }

    public string UltimaCitire { get => _ultimaCitire; private set => SetProperty(ref _ultimaCitire, value); }

    // ---------- Climatul serei (aer comun, din stațiile de climat) ----------

    public string ClimatTemperaturaText => _climatTemperatura is { } t ? $"{t.Medie:0.0} °C" : "–";
    public string ClimatTemperaturaDetaliu => Detaliu(_climatTemperatura, _tintaTemperatura, " °C");
    public StareValoare ClimatTemperaturaStare => Stare(_climatTemperatura, _tintaTemperatura);

    public string ClimatUmiditateText => _climatUmiditate is { } u ? $"{u.Medie:0} %" : "–";
    public string ClimatUmiditateDetaliu => Detaliu(_climatUmiditate, _tintaUmiditate, "%");
    public StareValoare ClimatUmiditateStare => Stare(_climatUmiditate, _tintaUmiditate);

    public string RezumatSenzoriText
    {
        get
        {
            var d = _monitor.Dispozitive;
            int climat = d.Count(x => x.EsteStatieClimat);
            int sol = d.Count(x => x.Tipuri.Contains(TipSenzor.UmiditateSol));
            int lumina = d.Count(x => x.Tipuri.Contains(TipSenzor.Lumina));
            return $"{climat} stații de climat · {sol} senzori de sol · {lumina} senzori de lumină";
        }
    }

    private static string Detaliu((double Medie, double Min, double Max)? v, AnalizaClimat tinta, string unitate) =>
        v is { } x ? $"între stații {x.Min:0.#}–{x.Max:0.#}{unitate} · țintă {tinta.Tinta}{unitate}" : $"țintă {tinta.Tinta}{unitate}";

    private static StareValoare Stare((double Medie, double Min, double Max)? v, AnalizaClimat tinta) =>
        v is { } x ? tinta.Tinta.Compara(x.Medie) : StareValoare.Necunoscuta;

    // ================= Comenzi =================

    public ICommand SalveazaCommand { get; }
    public ICommand ReincarcaCommand { get; }
    public ICommand ImpliciteCommand { get; }
    public ICommand CitesteAcumCommand { get; }

    private bool AreEroriIntroducere => HasErrors || Zone.Any(z => z.HasErrors);
    private bool PoateSalva => !AreEroriIntroducere && NumarErori == 0;

    public string MotivSalvare =>
        AreEroriIntroducere ? "Corectează câmpurile marcate cu roșu."
        : NumarErori > 0 ? "Configurația are erori; rezolvă-le înainte de salvare."
        : "Salvează configurația în Date/configuratie_sera.json.";

    // ================= Logica =================

    private void Incarca(ConfiguratieSera config, string mesaj)
    {
        _config = config;

        _seraLungimeText = ParserNumere.Formateaza(config.Sera.Lungime);
        _seraLatimeText = ParserNumere.Formateaza(config.Sera.Latime);
        _seraInaltimeText = ParserNumere.Formateaza(config.Sera.Inaltime);
        _latimeAleeText = ParserNumere.Formateaza(config.Sera.LatimeAlee);
        _procentCuloarText = ParserNumere.Formateaza(config.Sera.ProcentMinimCuloare);
        foreach (var p in new[] { nameof(SeraLungimeText), nameof(SeraLatimeText), nameof(SeraInaltimeText), nameof(LatimeAleeText), nameof(ProcentCuloarText) })
        {
            SeteazaEroare(p, null);
            OnPropertyChanged(p);
        }

        ZonaSelectata = null;
        Zone.Clear();
        foreach (var zona in config.Zone.OrderBy(z => z.Id))
        {
            var vm = new ZonaViewModel(zona, _catalog, LaModificareZona, z => ZonaSelectata = z);
            vm.ErrorsChanged += (_, _) => OnPropertyChanged(nameof(MotivSalvare));
            Zone.Add(vm);
        }

        _ultimulNumarErori = -1;
        Revalideaza();
        AdaugaInJurnal(mesaj);
        AdaugaInJurnal($"Senzori amplasați: {RezumatSenzoriText}.");
    }

    private void LaModificareZona(ZonaViewModel zona, bool plantaPrincipalaSchimbata)
    {
        if (plantaPrincipalaSchimbata)
            AdaugaInJurnal($"{zona.Nume}: planta principală este acum {zona.PlantaPrincipala?.Nume ?? "nesetată"}.");
        Revalideaza();
    }

    /// <summary>Recalculează amplasarea și rulează toate regulile de validare.</summary>
    private void Revalideaza()
    {
        _plan = _amplasare.Calculeaza(_config);
        _rezultat = _validator.Valideaza(_config);

        foreach (var zona in Zone)
        {
            zona.AplicaAmplasare(_plan.PentruZona(zona.Id), Scala);
            zona.AplicaProbleme(_rezultat.Probleme.Where(p => p.ZonaId == zona.Id), _rezultat.NivelMaximPentruZona(zona.Id));
        }

        Probleme.Clear();
        foreach (var p in _rezultat.Probleme) Probleme.Add(p);

        // Climatul țintă al serei (comun tuturor plantelor)
        var plante = ClimatComun.PlanteDinSera(_config, _catalog);
        _tintaTemperatura = ClimatComun.Analizeaza(plante, TipSenzor.Temperatura);
        _tintaUmiditate = ClimatComun.Analizeaza(plante, TipSenzor.UmiditateAer);

        // Senzorii se re-amplasează după noile dimensiuni și primesc noile ținte
        int numarAnterior = _monitor.Dispozitive.Count;
        var dispozitive = _amplasareSenzori.Calculeaza(_config, _plan);
        _monitor.Configureaza(_config, dispozitive);
        ReconstruiesteSenzoriHarta(dispozitive);
        if (numarAnterior > 0 && numarAnterior != dispozitive.Count)
            AdaugaInJurnal($"Senzorii au fost re-amplasați: {RezumatSenzoriText}.");
        AplicaCitiri(_monitor.CitesteToti());

        foreach (var p in new[]
                 {
                     nameof(SuprafataTotalaText), nameof(VolumText), nameof(SuprafataOcupataText), nameof(CuloareText),
                     nameof(CuloareMinimText), nameof(NumarErori), nameof(NumarAvertismente), nameof(StareGenerala),
                     nameof(StareGeneralaText), nameof(HartaLatime), nameof(HartaInaltime), nameof(SeraLatimePx),
                     nameof(SeraInaltimePx), nameof(MotivSalvare), nameof(RezumatSenzoriText)
                 })
        {
            OnPropertyChanged(p);
        }

        if (_rezultat.NumarErori != _ultimulNumarErori)
        {
            if (_ultimulNumarErori >= 0) AdaugaInJurnal($"Validare: {StareGeneralaText}.");
            _ultimulNumarErori = _rezultat.NumarErori;
        }
        CommandManager.InvalidateRequerySuggested();
    }

    private void Reincarca()
    {
        try
        {
            Incarca(_depozit.Incarca(), "Configurația a fost reîncărcată din fișier.");
        }
        catch (Exception ex)
        {
            AdaugaInJurnal($"Fișierul de configurație nu a putut fi citit: {ex.Message}");
        }
    }

    private void Salveaza()
    {
        try
        {
            _depozit.Salveaza(_config);
            AdaugaInJurnal("Configurația a fost salvată.");
        }
        catch (Exception ex)
        {
            AdaugaInJurnal($"Salvarea a eșuat: {ex.Message}");
        }
    }

    private void LaCitiriNoi(object? sender, IReadOnlyList<CitireSenzor> citiri)
    {
        // Evenimentul vine de pe firul timerului; interfața se modifică doar de pe firul UI
        if (_dispatcher.HasShutdownStarted) return;
        _dispatcher.InvokeAsync(() => AplicaCitiri(citiri));
    }

    private void ReconstruiesteSenzoriHarta(IReadOnlyList<DispozitivSenzor> dispozitive)
    {
        SenzoriHarta.Clear();
        _senzoriDupaId.Clear();
        foreach (var d in dispozitive)
        {
            var vm = new SenzorHartaViewModel(d, Scala);
            SenzoriHarta.Add(vm);
            _senzoriDupaId[d.Id] = vm;
        }
    }

    private void AplicaCitiri(IReadOnlyList<CitireSenzor> citiri)
    {
        // Stațiile de climat: media serei și, pentru fiecare zonă, o estimare din cele mai apropiate stații
        var climat = citiri.Where(c => c.ZonaId is null).ToList();
        _climatTemperatura = Statistica(climat, TipSenzor.Temperatura);
        _climatUmiditate = Statistica(climat, TipSenzor.UmiditateAer);
        int statiiFolosite = Math.Min(EstimatorClimat.Vecini, climat.Count(c => c.Tip == TipSenzor.Temperatura));

        foreach (var zona in Zone)
        {
            zona.AplicaCitiri(
                citiri.Where(c => c.ZonaId == zona.Id),
                EstimatorClimat.Estimeaza(climat, TipSenzor.Temperatura, zona.CentruX, zona.CentruY),
                EstimatorClimat.Estimeaza(climat, TipSenzor.UmiditateAer, zona.CentruX, zona.CentruY),
                statiiFolosite);
        }

        foreach (var grup in citiri.GroupBy(c => c.DispozitivId))
        {
            if (_senzoriDupaId.TryGetValue(grup.Key, out var vm)) vm.Actualizeaza(grup);
        }

        // Pentru tabul „Senzori”: la ce zone e folosită fiecare stație de climat și cu ce pondere
        // (aceleași stații și ponderi pentru temperatură și umiditate, fiind în aceleași puncte)
        var folosire = new Dictionary<string, List<string>>();
        foreach (var zona in Zone)
        {
            foreach (var (citire, _, pondere) in EstimatorClimat.Ponderi(climat, TipSenzor.Temperatura, zona.CentruX, zona.CentruY))
            {
                if (!folosire.TryGetValue(citire.DispozitivId, out var lista)) folosire[citire.DispozitivId] = lista = new List<string>();
                lista.Add($"Zona {zona.Id}: {pondere:P0}");
            }
        }
        foreach (var senzor in SenzoriHarta.Where(s => s.Dispozitiv.EsteStatieClimat))
        {
            senzor.Folosire = folosire.TryGetValue(senzor.Id, out var lista)
                ? string.Join(" · ", lista)
                : "doar în media serei";
        }

        foreach (var p in new[]
                 {
                     nameof(ClimatTemperaturaText), nameof(ClimatTemperaturaDetaliu), nameof(ClimatTemperaturaStare),
                     nameof(ClimatUmiditateText), nameof(ClimatUmiditateDetaliu), nameof(ClimatUmiditateStare)
                 })
        {
            OnPropertyChanged(p);
        }
        UltimaCitire = DateTime.Now.ToString("HH:mm:ss");
    }

    private static (double Medie, double Min, double Max)? Statistica(IEnumerable<CitireSenzor> citiri, TipSenzor tip)
    {
        var valori = citiri.Where(c => c.Tip == tip).Select(c => c.Valoare).ToList();
        return valori.Count == 0 ? null : (valori.Average(), valori.Min(), valori.Max());
    }

    private void AdaugaInJurnal(string mesaj)
    {
        Jurnal.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {mesaj}");
        while (Jurnal.Count > MaxIntrariJurnal) Jurnal.RemoveAt(Jurnal.Count - 1);
    }
}
