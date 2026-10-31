namespace SeraInteligenta.Core.Models;

/// <summary>Tipurile de senzori montați în fiecare zonă.</summary>
public enum TipSenzor
{
    Temperatura,
    UmiditateAer,
    UmiditateSol,
    Lumina
}

/// <summary>Gravitatea unei probleme de validare. Ordinea contează: Eroare e cea mai gravă.</summary>
public enum NivelSeveritate
{
    Info = 0,
    Avertisment = 1,
    Eroare = 2
}

/// <summary>Cum se raportează o valoare citită la intervalul optim al plantei.</summary>
public enum StareValoare
{
    Necunoscuta,
    SubInterval,
    InInterval,
    PesteInterval
}
