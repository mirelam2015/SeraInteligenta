using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Infrastructure.Monitorizare;

/// <summary>
/// Ține senzorii serei și îi citește periodic, pe un fir de fundal.
/// Nu știe dacă senzorii sunt simulați sau reali: îi primește de la IFabricaSenzori.
/// </summary>
public sealed class ServiciuMonitorizare : IServiciuMonitorizare
{
    private readonly IFabricaSenzori _fabrica;
    private readonly object _lock = new();
    private IReadOnlyList<ISenzor> _senzori = Array.Empty<ISenzor>();
    private IReadOnlyList<DispozitivSenzor> _dispozitive = Array.Empty<DispozitivSenzor>();
    private Timer? _timer;
    private int _citireInCurs;

    public ServiciuMonitorizare(IFabricaSenzori fabrica) => _fabrica = fabrica;

    public bool Ruleaza => _timer is not null;

    public IReadOnlyList<DispozitivSenzor> Dispozitive
    {
        get { lock (_lock) return _dispozitive; }
    }

    public event EventHandler<IReadOnlyList<CitireSenzor>>? CitiriNoi;

    public void Configureaza(ConfiguratieSera configuratie, IReadOnlyList<DispozitivSenzor> dispozitive)
    {
        var senzori = _fabrica.Creeaza(configuratie, dispozitive);
        lock (_lock)
        {
            _senzori = senzori;
            _dispozitive = dispozitive;
        }
    }

    public IReadOnlyList<CitireSenzor> CitesteToti()
    {
        IReadOnlyList<ISenzor> senzori;
        lock (_lock) senzori = _senzori;
        return senzori.Select(s => s.Citeste()).ToList();
    }

    public void Porneste(TimeSpan interval)
    {
        Opreste();
        _timer = new Timer(_ => LaTimer(), null, TimeSpan.Zero, interval);
    }

    public void Opreste()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void LaTimer()
    {
        // Dacă o citire durează mai mult decât intervalul, nu pornim alta în paralel
        if (Interlocked.Exchange(ref _citireInCurs, 1) == 1) return;
        try
        {
            CitiriNoi?.Invoke(this, CitesteToti());
        }
        finally
        {
            Interlocked.Exchange(ref _citireInCurs, 0);
        }
    }

    public void Dispose() => Opreste();
}
