using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.WPF.Converters;

/// <summary>ProfilPlanta (sau ID-ul ei) → iconița vectorială din Resources/IconitePlante.xaml.</summary>
public sealed class PlantaIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? id = value switch
        {
            ProfilPlanta p => p.IdPlanta,
            string s => s,
            _ => null
        };
        return Iconite.Pentru(id);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>ProfilPlanta → pensulă care repetă iconița plantei pe toată parcela (efectul de „plantat”).</summary>
public sealed class PlantaModelConverter : IValueConverter
{
    private static readonly Dictionary<string, Brush> Cache = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ProfilPlanta planta || string.IsNullOrEmpty(planta.IdPlanta))
            return Brushes.Transparent;

        if (Cache.TryGetValue(planta.IdPlanta, out var existenta)) return existenta;

        Brush pensula = Brushes.Transparent;
        if (Iconite.Pentru(planta.IdPlanta) is DrawingImage imagine)
        {
            var dalaj = new DrawingBrush(imagine.Drawing)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 26, 26),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Uniform
            };
            dalaj.Freeze();
            pensula = dalaj;
        }

        Cache[planta.IdPlanta] = pensula;
        return pensula;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// NivelSeveritate → culoare. Parametrul „fundal” dă varianta deschisă.
/// null (zonă fără probleme) → culoarea „ok”.
/// </summary>
public sealed class SeveritateBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool fundal = string.Equals(parameter as string, "fundal", StringComparison.OrdinalIgnoreCase);
        string cheie = value switch
        {
            NivelSeveritate.Eroare => "Eroare",
            NivelSeveritate.Avertisment => "Avertisment",
            NivelSeveritate.Info => "Info",
            _ => "Ok"
        };
        return Application.Current.TryFindResource($"Brush.{cheie}{(fundal ? ".Fundal" : string.Empty)}");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>NivelSeveritate → simbol (din fontul de iconițe Windows), ca severitatea să nu fie arătată doar prin culoare.</summary>
public sealed class SeveritateSimbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        NivelSeveritate.Eroare => "",       // ErrorBadge
        NivelSeveritate.Avertisment => "",  // Warning
        NivelSeveritate.Info => "",         // Info
        _ => ""                             // CheckMark
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>StareValoare → culoare pentru valoarea unui senzor.</summary>
public sealed class StareValoareBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string cheie = value switch
        {
            StareValoare.InInterval => "Brush.Ok",
            StareValoare.SubInterval or StareValoare.PesteInterval => "Brush.Avertisment",
            _ => "Brush.TextSecundar"
        };
        return Application.Current.TryFindResource(cheie);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool → grosimea chenarului (parcela selectată are chenar mai gros).</summary>
public sealed class SelectieGrosimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new Thickness(4) : new Thickness(2);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal static class Iconite
{
    public static ImageSource? Pentru(string? idPlanta)
    {
        var app = Application.Current;
        if (app is null) return null;
        return (string.IsNullOrEmpty(idPlanta) ? null : app.TryFindResource($"Icon.{idPlanta}") as ImageSource)
               ?? app.TryFindResource("Icon.generic") as ImageSource;
    }
}
