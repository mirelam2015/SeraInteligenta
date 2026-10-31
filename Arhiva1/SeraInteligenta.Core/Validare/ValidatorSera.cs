using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Validare;

/// <summary>
/// Rulează toate regulile primite din containerul IoC (IEnumerable&lt;IRegulaValidare&gt;).
/// Validatorul nu știe ce reguli există: le primește din exterior.
/// </summary>
public sealed class ValidatorSera : IValidatorSera
{
    private readonly IReadOnlyList<IRegulaValidare> _reguli;

    public ValidatorSera(IEnumerable<IRegulaValidare> reguli)
    {
        _reguli = reguli.ToList();
    }

    public RezultatValidare Valideaza(ConfiguratieSera configuratie)
    {
        var probleme = _reguli
            .SelectMany(r => r.Verifica(configuratie))
            .OrderByDescending(p => p.Nivel)
            .ThenBy(p => p.ZonaId ?? 0)
            .ToList();

        return new RezultatValidare(probleme);
    }
}
