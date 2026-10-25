namespace SeraInteligenta.Core.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Modelează geometria serei și validează alocarea spațiului pentru zone și culoare de acces.
/// </summary>
public class DimensiuniSera
{
    public double Lungime { get; set; } = 24.0;
    public double Latime { get; set; } = 12.0;
    public double Inaltime { get; set; } = 3.8;

    /// <summary>
    /// Procentul minim din suprafața serei rezervat exclusiv culoarelor de acces și aleilor tehnice (ex: 15%).
    /// </summary>
    public double ProcentMinimCuloare { get; set; } = 15.0;

    public double SuprafataTotala => Math.Round(Lungime * Latime, 2);
    public double VolumTotal => Math.Round(SuprafataTotala * Inaltime, 2);

    /// <summary>
    /// Suprafața minimă necesară pentru culoare de acces conform normelor agronomice.
    /// </summary>
    public double SuprafataMinimaCuloare => Math.Round(SuprafataTotala * (ProcentMinimCuloare / 100.0), 2);

    /// <summary>
    /// Suprafața maximă ce poate fi alocată parcelelor de cultură.
    /// </summary>
    public double SuprafataMaximaCultivabila => Math.Round(SuprafataTotala - SuprafataMinimaCuloare, 2);

    public DimensiuniSera() { }

    public DimensiuniSera(double lungime, double latime, double inaltime, double procentCuloare = 15.0)
    {
        if (lungime <= 0) throw new ArgumentOutOfRangeException(nameof(lungime));
        if (latime <= 0) throw new ArgumentOutOfRangeException(nameof(latime));
        if (inaltime <= 0) throw new ArgumentOutOfRangeException(nameof(inaltime));

        Lungime = lungime;
        Latime = latime;
        Inaltime = inaltime;
        ProcentMinimCuloare = Math.Clamp(procentCuloare, 5.0, 50.0);
    }

    /// <summary>
    /// Validează dacă lista de zone respectă constrângerile de suprafață și culoarele de acces.
    /// </summary>
    public bool Valideaza(IEnumerable<ZonaCultura> zone, out string mesajEroare, out double suprafataOcupata, out double suprafataCuloarReal)
    {
        suprafataOcupata = Math.Round(zone.Sum(z => z.SuprafataMp), 2);
        suprafataCuloarReal = Math.Round(SuprafataTotala - suprafataOcupata, 2);

        if (suprafataOcupata > SuprafataTotala)
        {
            mesajEroare = $"EROARE CRITICĂ: Suprafața celor 4 zone ({suprafataOcupata} m²) depășește suprafața totală a serei ({SuprafataTotala} m²) cu {suprafataOcupata - SuprafataTotala:F1} m²!";
            return false;
        }

        if (suprafataCuloarReal < SuprafataMinimaCuloare)
        {
            mesajEroare = $"AVERTISMENT CULOAR: Spațiul rămas pentru alei ({suprafataCuloarReal} m²) este sub pragul minim recomandat de {ProcentMinimCuloare}% ({SuprafataMinimaCuloare} m²).";
            return false;
        }

        mesajEroare = $"VALIDARE REUȘITĂ: Zonele ocupă {suprafataOcupata} m² ({((suprafataOcupata/SuprafataTotala)*100):F1}%). Culoare acces: {suprafataCuloarReal} m².";
        return true;
    }
}