using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Interfaces;

// Toate abstracțiile aplicației. Restul codului depinde DOAR de aceste interfețe,
// iar containerul de IoC (în SeraInteligenta.WPF/CompozitieServicii.cs) alege implementările.

/// <summary>Un senzor fizic sau simulat: o singură mărime măsurată de un dispozitiv.</summary>
public interface ISenzor
{
    string Id { get; }
    DispozitivSenzor Dispozitiv { get; }
    TipSenzor Tip { get; }
    string Unitate { get; }

    /// <summary>Ultima valoare citită.</summary>
    double Valoare { get; }

    CitireSenzor Citeste();
}

/// <summary>Creează senzorii pentru dispozitivele amplasate în seră (pattern-ul Factory).</summary>
public interface IFabricaSenzori
{
    IReadOnlyList<ISenzor> Creeaza(ConfiguratieSera configuratie, IReadOnlyList<DispozitivSenzor> dispozitive);
}

/// <summary>Calculează unde se montează senzorii, în funcție de dimensiunile serei și ale parcelelor.</summary>
public interface IServiciuAmplasareSenzori
{
    IReadOnlyList<DispozitivSenzor> Calculeaza(ConfiguratieSera configuratie, PlanAmplasare plan);
}

/// <summary>Citește periodic toți senzorii din seră.</summary>
public interface IServiciuMonitorizare : IDisposable
{
    bool Ruleaza { get; }

    /// <summary>Dispozitivele monitorizate în acest moment.</summary>
    IReadOnlyList<DispozitivSenzor> Dispozitive { get; }

    /// <summary>Se declanșează după fiecare citire automată. Poate veni de pe un fir de fundal.</summary>
    event EventHandler<IReadOnlyList<CitireSenzor>>? CitiriNoi;

    /// <summary>Înlocuiește toți senzorii, de exemplu după ce s-au schimbat dimensiunile serei sau plantele.</summary>
    void Configureaza(ConfiguratieSera configuratie, IReadOnlyList<DispozitivSenzor> dispozitive);

    IReadOnlyList<CitireSenzor> CitesteToti();

    void Porneste(TimeSpan interval);
    void Opreste();
}

/// <summary>Catalogul de plante disponibile.</summary>
public interface ICatalogPlante
{
    IReadOnlyList<ProfilPlanta> Toate { get; }
    ProfilPlanta? Gaseste(string? idPlanta);
}

/// <summary>Încarcă și salvează configurația serei.</summary>
public interface IDepozitConfiguratie
{
    ConfiguratieSera Incarca();
    void Salveaza(ConfiguratieSera configuratie);
}

/// <summary>Așază parcelele pe planul serei, cu alei între ele și pe margini.</summary>
public interface IServiciuAmplasare
{
    PlanAmplasare Calculeaza(ConfiguratieSera configuratie);
}

/// <summary>
/// O singură regulă de validare. Fiecare regulă e o clasă separată, înregistrată în containerul IoC;
/// pentru o regulă nouă scrii o clasă și adaugi un rând la înregistrare.
/// </summary>
public interface IRegulaValidare
{
    string Nume { get; }
    IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie);
}

/// <summary>Rulează toate regulile și adună rezultatele.</summary>
public interface IValidatorSera
{
    RezultatValidare Valideaza(ConfiguratieSera configuratie);
}
