namespace SeraInteligenta.Core.Interfaces;

/// <summary>
/// Interfața de bază pentru toți senzorii din seră.
/// </summary>
public interface ISenzor
{
    double Valoare { get; }
    double CitesteValoare();
}