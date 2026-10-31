using System.Globalization;

namespace SeraInteligenta.WPF.Helpers;

/// <summary>
/// Acceptă numere scrise atât cu virgulă („4,5”), cât și cu punct („4.5”),
/// indiferent de setările regionale ale Windows-ului.
/// </summary>
public static class ParserNumere
{
    public static bool TryParse(string? text, out double valoare)
    {
        valoare = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string normalizat = text.Trim().Replace(" ", string.Empty).Replace(',', '.');
        return double.TryParse(normalizat, NumberStyles.Float, CultureInfo.InvariantCulture, out valoare)
               && !double.IsNaN(valoare) && !double.IsInfinity(valoare);
    }

    public static string Formateaza(double valoare) => valoare.ToString("0.##", CultureInfo.CurrentCulture);
}
