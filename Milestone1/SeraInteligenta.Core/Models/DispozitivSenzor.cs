namespace SeraInteligenta.Core.Models;

/// <summary>
/// Un dispozitiv montat în seră, cu poziția lui pe plan (în metri, din colțul stânga-sus).
/// Un dispozitiv poate măsura mai multe mărimi: o stație de climat măsoară temperatura și umiditatea aerului.
/// </summary>
/// <param name="ZonaId">Zona în care e montat; null pentru stațiile de climat, care aparțin întregii sere.</param>
public sealed record DispozitivSenzor(
    string Id,
    int? ZonaId,
    double X,
    double Y,
    IReadOnlyList<TipSenzor> Tipuri)
{
    public bool EsteStatieClimat => ZonaId is null;

    public string Descriere =>
        EsteStatieClimat ? $"Stație de climat {Id}"
        : Tipuri.Contains(TipSenzor.Lumina) ? $"Senzor de lumină {Id}"
        : $"Senzor de umiditate a solului {Id}";
}
