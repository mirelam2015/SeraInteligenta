using System.Collections.ObjectModel;
using System.Windows;
using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.WPF.Helpers;

namespace SeraInteligenta.WPF.ViewModels;

/// <summary>
/// O parcelă, așa cum o vede interfața: același ViewModel e folosit și pe hartă, și în lista din dreapta.
/// Înlocuiește cele 4 seturi de controale copiate din versiunea anterioară (TxtZ1…TxtZ4).
/// </summary>
public sealed class ZonaViewModel : ViewModelBase
{
    private readonly ZonaCultura _model;
    private readonly ICatalogPlante _catalog;
    private readonly Action<ZonaViewModel, bool> _laModificare;

    private string _lungimeText;
    private string _latimeText;
    private double _hartaX, _hartaY, _hartaLatime, _hartaInaltime;
    private bool _incape = true;
    private NivelSeveritate? _nivelMaxim;
    private bool _esteSelectata;

    /// <param name="laModificare">Anunță ViewModel-ul principal; al doilea parametru = s-a schimbat planta principală.</param>
    public ZonaViewModel(ZonaCultura model, ICatalogPlante catalog,
                         Action<ZonaViewModel, bool> laModificare, Action<ZonaViewModel> laSelectare)
    {
        _model = model;
        _catalog = catalog;
        _laModificare = laModificare;
        _lungimeText = ParserNumere.Formateaza(model.Lungime);
        _latimeText = ParserNumere.Formateaza(model.Latime);

        Senzori = new ObservableCollection<ValoareSenzorViewModel>(
            InfoSenzori.Toate.Select(tip => new ValoareSenzorViewModel(tip)));
        SelecteazaCommand = new RelayCommand(_ => laSelectare(this));
        ActualizeazaIntervaleSenzori();
    }

    public ZonaCultura Model => _model;
    public int Id => _model.Id;
    public string Nume => _model.Nume;

    // ---------- Dimensiuni ----------

    public string LungimeText
    {
        get => _lungimeText;
        set => SeteazaDimensiune(ref _lungimeText, value, v => _model.Lungime = v);
    }

    public string LatimeText
    {
        get => _latimeText;
        set => SeteazaDimensiune(ref _latimeText, value, v => _model.Latime = v);
    }

    public string SuprafataText => $"{_model.SuprafataMp:0.#} m²";
    public string DimensiuniText => $"{ParserNumere.Formateaza(_model.Lungime)} × {ParserNumere.Formateaza(_model.Latime)} m";

    private void SeteazaDimensiune(ref string camp, string valoare, Action<double> aplica,
                                   [System.Runtime.CompilerServices.CallerMemberName] string? proprietate = null)
    {
        if (!SetProperty(ref camp, valoare, proprietate)) return;

        if (ParserNumere.TryParse(valoare, out double v) && v > 0 && v <= 200)
        {
            SeteazaEroare(proprietate!, null);
            aplica(v);
            OnPropertyChanged(nameof(SuprafataText));
            OnPropertyChanged(nameof(DimensiuniText));
            _laModificare(this, false);
        }
        else
        {
            SeteazaEroare(proprietate!, "Introdu un număr între 0 și 200, de exemplu 10 sau 4,5.");
        }
    }

    // ---------- Plante ----------

    public string PlantaPrincipalaId
    {
        get => _model.PlantaPrincipalaId;
        set
        {
            value ??= string.Empty;
            if (value == _model.PlantaPrincipalaId) return;
            _model.PlantaPrincipalaId = value;
            OnPropertyChanged();
            NotificaPlante();
            ActualizeazaIntervaleSenzori();
            _laModificare(this, true);
        }
    }

    public string PlantaSecundaraId
    {
        get => _model.PlantaSecundaraId;
        set
        {
            value ??= string.Empty;
            if (value == _model.PlantaSecundaraId) return;
            _model.PlantaSecundaraId = value;
            OnPropertyChanged();
            NotificaPlante();
            _laModificare(this, false);
        }
    }

    public ProfilPlanta? PlantaPrincipala => _catalog.Gaseste(_model.PlantaPrincipalaId);
    public ProfilPlanta? PlantaSecundara => _catalog.Gaseste(_model.PlantaSecundaraId);

    public bool AreDouaPlante =>
        PlantaPrincipala is not null && PlantaSecundara is not null &&
        PlantaPrincipala.IdPlanta != PlantaSecundara.IdPlanta;

    /// <summary>Pe hartă, parcela se împarte în două când are două culturi.</summary>
    public GridLength LatimeColoanaSecundara =>
        AreDouaPlante ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    public string EtichetaPlante =>
        AreDouaPlante ? $"{PlantaPrincipala!.Nume} + {PlantaSecundara!.Nume}"
        : PlantaPrincipala?.Nume ?? "Fără plantă";

    private void NotificaPlante()
    {
        OnPropertyChanged(nameof(PlantaPrincipala));
        OnPropertyChanged(nameof(PlantaSecundara));
        OnPropertyChanged(nameof(AreDouaPlante));
        OnPropertyChanged(nameof(LatimeColoanaSecundara));
        OnPropertyChanged(nameof(EtichetaPlante));
    }

    // ---------- Harta ----------

    public double HartaX { get => _hartaX; private set => SetProperty(ref _hartaX, value); }
    public double HartaY { get => _hartaY; private set => SetProperty(ref _hartaY, value); }
    public double HartaLatime { get => _hartaLatime; private set => SetProperty(ref _hartaLatime, value); }
    public double HartaInaltime { get => _hartaInaltime; private set => SetProperty(ref _hartaInaltime, value); }

    /// <summary>False dacă parcela iese din seră; pe hartă apare cu roșu.</summary>
    public bool Incape { get => _incape; private set => SetProperty(ref _incape, value); }

    /// <summary>Cea mai gravă problemă a zonei (fără mesajele Info), sau null dacă zona e în regulă.</summary>
    public NivelSeveritate? NivelMaxim { get => _nivelMaxim; private set => SetProperty(ref _nivelMaxim, value); }

    public bool EsteSelectata { get => _esteSelectata; set => SetProperty(ref _esteSelectata, value); }

    public RelayCommand SelecteazaCommand { get; }

    /// <summary>Centrul parcelei pe planul serei, în metri (pentru estimarea climatului la parcelă).</summary>
    public double CentruX { get; private set; }
    public double CentruY { get; private set; }

    public void AplicaAmplasare(AmplasareZona? amplasare, double scala)
    {
        if (amplasare is null) return;
        CentruX = amplasare.X + amplasare.Lungime / 2;
        CentruY = amplasare.Y + amplasare.Latime / 2;
        HartaX = amplasare.X * scala;
        HartaY = amplasare.Y * scala;
        HartaLatime = Math.Max(amplasare.Lungime * scala, 2);
        HartaInaltime = Math.Max(amplasare.Latime * scala, 2);
        Incape = amplasare.Incape;
    }

    // ---------- Validare ----------

    public ObservableCollection<ProblemaValidare> Probleme { get; } = new();

    public void AplicaProbleme(IEnumerable<ProblemaValidare> probleme, NivelSeveritate? nivelMaxim)
    {
        Probleme.Clear();
        foreach (var p in probleme) Probleme.Add(p);
        NivelMaxim = nivelMaxim;
    }

    // ---------- Senzori ----------

    public ObservableCollection<ValoareSenzorViewModel> Senzori { get; }

    /// <summary>Text scurt afișat pe hartă: mărimile măsurate pe parcelă (sol și lumină).</summary>
    public string RezumatSenzori
    {
        get
        {
            var sol = Senzori.FirstOrDefault(s => s.Tip == TipSenzor.UmiditateSol);
            var lumina = Senzori.FirstOrDefault(s => s.Tip == TipSenzor.Lumina);
            return sol?.Valoare is null ? "fără citiri" : $"sol {sol.ValoareText} · {lumina?.ValoareText}";
        }
    }

    /// <param name="citiriParcela">Citirile senzorilor montați pe parcelă (sol și lumină).</param>
    /// <param name="temperatura">Temperatura aerului estimată la centrul parcelei, din stațiile de climat.</param>
    /// <param name="umiditateAer">Umiditatea aerului estimată la centrul parcelei.</param>
    /// <param name="statiiFolosite">Din câte stații s-a făcut estimarea.</param>
    public void AplicaCitiri(IEnumerable<CitireSenzor> citiriParcela, double? temperatura, double? umiditateAer, int statiiFolosite)
    {
        string estimare = $"estimată din {statiiFolosite} stații de climat";
        if (temperatura is double t) Senzor(TipSenzor.Temperatura)?.SeteazaValoare(t, estimare);
        if (umiditateAer is double u) Senzor(TipSenzor.UmiditateAer)?.SeteazaValoare(u, estimare);

        foreach (var grup in citiriParcela.GroupBy(c => c.Tip))
        {
            var valori = grup.Select(c => c.Valoare).ToList();
            string detaliu = valori.Count == 1
                ? "1 senzor pe parcelă"
                : $"medie din {valori.Count} senzori ({valori.Min():0.#}–{valori.Max():0.#})";
            Senzor(grup.Key)?.SeteazaValoare(valori.Average(), detaliu);
        }
        OnPropertyChanged(nameof(RezumatSenzori));
    }

    private ValoareSenzorViewModel? Senzor(TipSenzor tip) => Senzori.FirstOrDefault(s => s.Tip == tip);

    private void ActualizeazaIntervaleSenzori()
    {
        var planta = PlantaPrincipala;
        foreach (var s in Senzori)
            s.SeteazaInterval(planta?.IntervalPentru(s.Tip));
    }
}

/// <summary>Valoarea unui senzor, comparată cu intervalul optim al plantei principale.</summary>
public sealed class ValoareSenzorViewModel : ViewModelBase
{
    private double? _valoare;
    private string _detaliu = string.Empty;
    private IntervalValori? _interval;
    private StareValoare _stare = StareValoare.Necunoscuta;

    public ValoareSenzorViewModel(TipSenzor tip)
    {
        Tip = tip;
        Eticheta = InfoSenzori.EsteDeClimat(tip) ? $"{InfoSenzori.Eticheta(tip)} (aer comun)" : InfoSenzori.Eticheta(tip);
        Unitate = InfoSenzori.Unitate(tip);
    }

    /// <summary>De unde vine valoarea: estimare din stațiile de climat sau media senzorilor de pe parcelă.</summary>
    public string Detaliu { get => _detaliu; private set => SetProperty(ref _detaliu, value); }

    public TipSenzor Tip { get; }
    public string Eticheta { get; }
    public string Unitate { get; }

    public double? Valoare
    {
        get => _valoare;
        private set
        {
            if (SetProperty(ref _valoare, value)) OnPropertyChanged(nameof(ValoareText));
        }
    }

    public string ValoareText => _valoare is null ? "–" : $"{_valoare:0.0} {Unitate}";

    public StareValoare Stare
    {
        get => _stare;
        private set
        {
            if (SetProperty(ref _stare, value)) OnPropertyChanged(nameof(StareText));
        }
    }

    public string StareText => Stare switch
    {
        StareValoare.SubInterval => "sub optim",
        StareValoare.PesteInterval => "peste optim",
        StareValoare.InInterval => "optim",
        _ => string.Empty
    };

    public string IntervalText => _interval is null ? "fără țintă" : $"optim {_interval} {Unitate}";

    public void SeteazaInterval(IntervalValori? interval)
    {
        _interval = interval;
        OnPropertyChanged(nameof(IntervalText));
        RecalculeazaStare();
    }

    public void SeteazaValoare(double valoare, string detaliu)
    {
        Valoare = Math.Round(valoare, 1);
        Detaliu = detaliu;
        RecalculeazaStare();
    }

    private void RecalculeazaStare() =>
        Stare = _valoare is double v && _interval is not null ? _interval.Compara(v) : StareValoare.Necunoscuta;
}
