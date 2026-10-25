namespace SeraInteligenta.WPF;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Sensors;

public partial class MainWindow : Window
{
    private DimensiuniSera _dimensiuniSera = new(24.0, 12.0, 3.8, procentCuloare: 15.0);
    private readonly List<ZonaCultura> _zone = new();
    private readonly List<ProfilPlanta> _catalogPlante = new();

    // Senzori pentru fiecare din cele 4 zone
    private readonly Dictionary<int, (ISenzor Umiditate, ISenzor Lumina)> _senzoriZone = new();

    public MainWindow()
    {
        InitializeComponent();
        
        // 1. Inițializăm senzorii pentru fiecare zonă
        for (int i = 1; i <= 4; i++)
        {
            _senzoriZone[i] = (new SenzorUmiditateSimulat(55.0 + i * 2), new SenzorLuminaSimulata(450.0 + i * 50));
        }

        // 2. Încărcăm catalogul de plante din JSON
        IncarcaCatalogPlante();

        // 3. Inițializăm cele 4 zone
        _zone.Add(new ZonaCultura(1, "Zona 1 - Parcela Nord", 10.0, 5.0, "rosii", 65.0, 600.0));
        _zone.Add(new ZonaCultura(2, "Zona 2 - Parcela Est", 10.0, 5.0, "castraveti", 80.0, 750.0));
        _zone.Add(new ZonaCultura(3, "Zona 3 - Parcela Sud", 10.0, 5.0, "salata", 55.0, 350.0));
        _zone.Add(new ZonaCultura(4, "Zona 4 - Parcela Vest", 10.0, 5.0, "ierburi", 60.0, 650.0));

        // 4. Populăm ComboBox-urile cu plante
        PopuleazaComboBoxuriPlante();

        // 5. Actualizăm interfața grafică completă
        ActualizeazaDateComuneSera();
        ActualizeazaToateZonele();

        AdaugaLog("Aplicația a pornit. 4 zone încărcate. Catalogul de plante din Resources este activ.");
    }

    private void IncarcaCatalogPlante()
    {
        string[] caiPosibile = {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "plante_catalog.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "plante_catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Resources", "plante_catalog.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "plante_catalog.json")
        };

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (string cale in caiPosibile)
        {
            if (File.Exists(cale))
            {
                try
                {
                    string json = File.ReadAllText(cale);
                    var plante = JsonSerializer.Deserialize<List<ProfilPlanta>>(json, options);
                    if (plante != null && plante.Count > 0)
                    {
                        _catalogPlante.Clear();
                        _catalogPlante.AddRange(plante);
                        AdaugaLog($"Catalogul complet cu {plante.Count} specii de plante a fost încărcat din: {Path.GetFileName(cale)}.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AdaugaLog($"Atenție la citirea fișierului {cale}: {ex.Message}");
                }
            }
        }

        // Fallback în memorie garantat cu toate cele 10 specii de plante
        _catalogPlante.Clear();
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "rosii", Nume = "Roșii (Tomate)", Categorie = "Solanacee", UmiditateOptima = 65, LuminaOptima = 600, TemperaturaOptima = 23, Descriere = "Lumină intensă și umiditate medie moderată." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "castraveti", Nume = "Castraveți", Categorie = "Cucurbitacee", UmiditateOptima = 80, LuminaOptima = 750, TemperaturaOptima = 25, Descriere = "Sensibili la secetă, cer umiditate ridicată și iluminare puternică." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "ardei", Nume = "Ardei Gras", Categorie = "Solanacee", UmiditateOptima = 68, LuminaOptima = 700, TemperaturaOptima = 24, Descriere = "Termofil, necesită stabilitate fără excese de apă." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "vinete", Nume = "Vinete", Categorie = "Solanacee", UmiditateOptima = 72, LuminaOptima = 800, TemperaturaOptima = 26, Descriere = "Iubitoare de căldură și soare direct." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "salata", Nume = "Salată Verde", Categorie = "Legume Frunzoase", UmiditateOptima = 55, LuminaOptima = 350, TemperaturaOptima = 18, Descriere = "Creștere rapidă, nu tolerează căldura excesivă." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "spanac", Nume = "Spanac", Categorie = "Legume Frunzoase", UmiditateOptima = 58, LuminaOptima = 400, TemperaturaOptima = 17, Descriere = "Tolerează temperaturi mai scăzute și lumină moderată." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "capsuni", Nume = "Căpșuni", Categorie = "Fructe de Pădure", UmiditateOptima = 62, LuminaOptima = 550, TemperaturaOptima = 20, Descriere = "Polenizare dependentă de fluxul de aer și iluminare adecvată." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "ierburi", Nume = "Ierburi Aromatice (Busuioc)", Categorie = "Plante Aromatice", UmiditateOptima = 60, LuminaOptima = 650, TemperaturaOptima = 22, Descriere = "Lumină generoasă pentru uleiurile aromatice." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "varza", Nume = "Varză Timpurie", Categorie = "Brasicacee", UmiditateOptima = 70, LuminaOptima = 500, TemperaturaOptima = 16, Descriere = "Tolerează temperaturi mai scăzute, cerințe constante de umiditate." });
        _catalogPlante.Add(new ProfilPlanta { IdPlanta = "dovlecei", Nume = "Dovlecei (Zucchini)", Categorie = "Cucurbitacee", UmiditateOptima = 75, LuminaOptima = 700, TemperaturaOptima = 22, Descriere = "Creștere viguroasă, lumină abundentă și aport hidric regulat." });

        AdaugaLog($"Catalog inițializat cu {_catalogPlante.Count} specii disponibile în baza de date.");
    }

    private void PopuleazaComboBoxuriPlante()
    {
        ComboBox[] combos = { CmbZ1Planta, CmbZ2Planta, CmbZ3Planta, CmbZ4Planta };

        for (int i = 0; i < 4; i++)
        {
            var cmb = combos[i];
            cmb.ItemsSource = null;
            cmb.ItemsSource = _catalogPlante;
            cmb.SelectedValuePath = "IdPlanta";

            string selectatId = _zone[i].PlantaSelectataId;
            cmb.SelectedValue = selectatId;
        }
    }

    private void CmbPlanta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cmb && cmb.Tag != null && int.TryParse(cmb.Tag.ToString(), out int zonaId))
        {
            if (cmb.SelectedItem is ProfilPlanta planta)
            {
                var zona = _zone.FirstOrDefault(z => z.Id == zonaId);
                if (zona != null)
                {
                    zona.AplicaProfilPlanta(planta);
                    ActualizeazaZonaUI(zonaId);
                    AdaugaLog($"Zona {zonaId}: planta setată pe '{planta.Nume}'. Ținte: {planta.UmiditateOptima}% umid., {planta.LuminaOptima} Lux.");
                }
            }
        }
    }

    private void ActualizeazaZonaUI(int zonaId)
    {
        var zona = _zone.FirstOrDefault(z => z.Id == zonaId);
        if (zona == null) return;

        double procent = (_dimensiuniSera.SuprafataTotala > 0) ? (zona.SuprafataMp / _dimensiuniSera.SuprafataTotala) * 100 : 0;

        switch (zonaId)
        {
            case 1:
                TxtZ1Suprafata.Text = $"{zona.SuprafataMp:F1} m² ({procent:F1}%)";
                TxtZ1TintaUmid.Text = $"Umiditate Țintă: {zona.UmiditateTinta:F0}%";
                TxtZ1TintaLux.Text = $"Lumină Țintă: {zona.LuminaTinta:F0} Lux";
                break;
            case 2:
                TxtZ2Suprafata.Text = $"{zona.SuprafataMp:F1} m² ({procent:F1}%)";
                TxtZ2TintaUmid.Text = $"Umiditate Țintă: {zona.UmiditateTinta:F0}%";
                TxtZ2TintaLux.Text = $"Lumină Țintă: {zona.LuminaTinta:F0} Lux";
                break;
            case 3:
                TxtZ3Suprafata.Text = $"{zona.SuprafataMp:F1} m² ({procent:F1}%)";
                TxtZ3TintaUmid.Text = $"Umiditate Țintă: {zona.UmiditateTinta:F0}%";
                TxtZ3TintaLux.Text = $"Lumină Țintă: {zona.LuminaTinta:F0} Lux";
                break;
            case 4:
                TxtZ4Suprafata.Text = $"{zona.SuprafataMp:F1} m² ({procent:F1}%)";
                TxtZ4TintaUmid.Text = $"Umiditate Țintă: {zona.UmiditateTinta:F0}%";
                TxtZ4TintaLux.Text = $"Lumină Țintă: {zona.LuminaTinta:F0} Lux";
                break;
        }
    }

    private void ActualizeazaToateZonele()
    {
        for (int i = 1; i <= 4; i++) ActualizeazaZonaUI(i);
    }

    private void ActualizeazaDateComuneSera()
    {
        TxtInfoSeraComun.Text = $"Suprafață Totală: {_dimensiuniSera.SuprafataTotala:F1} m² | Volum Aer: {_dimensiuniSera.VolumTotal:F1} m³";

        bool esteValid = _dimensiuniSera.Valideaza(_zone, out string mesaj, out double ocupat, out double culoar);
        double procentOcupat = _dimensiuniSera.SuprafataTotala > 0 ? (ocupat / _dimensiuniSera.SuprafataTotala) * 100 : 0;
        double procentCuloar = _dimensiuniSera.SuprafataTotala > 0 ? (culoar / _dimensiuniSera.SuprafataTotala) * 100 : 0;

        TxtInfoOcupare.Text = $"Ocupat de cele 4 zone: {ocupat:F1} m² ({procentOcupat:F1}%)";
        TxtInfoCuloar.Text = $"Spațiu Culoare Acces: {culoar:F1} m² ({procentCuloar:F1}%) — Prag minim: {_dimensiuniSera.ProcentMinimCuloare}% ({_dimensiuniSera.SuprafataMinimaCuloare:F1} m²)";
        TxtValidareStatus.Text = mesaj;

        if (ocupat > _dimensiuniSera.SuprafataTotala)
        {
            // Eroare critică de depășire
            BorderValidare.Background = new SolidColorBrush(Color.FromArgb(235, 69, 10, 10));
            BorderValidare.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            BadgeStatusContainer.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            TxtValidareBadge.Text = "EROARE CRITICĂ";
            TxtValidareStatus.Foreground = new SolidColorBrush(Color.FromRgb(254, 226, 226));
            TxtValidareRecomandare.Text = $"Capacitate fizică depășită cu {ocupat - _dimensiuniSera.SuprafataTotala:F1} m²! Micșorați dimensiunile parcelelor sau măriți dimensiunile serei.";
            TxtValidareRecomandare.Foreground = new SolidColorBrush(Color.FromRgb(252, 165, 165));
        }
        else if (culoar < _dimensiuniSera.SuprafataMinimaCuloare)
        {
            // Avertisment culoare acces
            BorderValidare.Background = new SolidColorBrush(Color.FromArgb(235, 69, 35, 5));
            BorderValidare.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            BadgeStatusContainer.Background = new SolidColorBrush(Color.FromRgb(217, 119, 6));
            TxtValidareBadge.Text = "AVERTISMENT";
            TxtValidareStatus.Foreground = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            TxtValidareRecomandare.Text = $"Culoarul tehnic ({culoar:F1} m²) este sub pragul minim normativ ({_dimensiuniSera.SuprafataMinimaCuloare:F1} m²). Se recomandă eliberarea a cel puțin {(_dimensiuniSera.SuprafataMinimaCuloare - culoar):F1} m².";
            TxtValidareRecomandare.Foreground = new SolidColorBrush(Color.FromRgb(253, 230, 138));
        }
        else
        {
            // Validare reușită
            BorderValidare.Background = new SolidColorBrush(Color.FromArgb(235, 6, 78, 59));
            BorderValidare.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            BadgeStatusContainer.Background = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            TxtValidareBadge.Text = "VALIDAT";
            TxtValidareStatus.Foreground = new SolidColorBrush(Color.FromRgb(236, 253, 245));
            TxtValidareRecomandare.Text = "Toate condițiile sunt îndeplinite: cele 4 zone încap perfect în seră și culoarul de acces este garantat.";
            TxtValidareRecomandare.Foreground = new SolidColorBrush(Color.FromRgb(167, 243, 208));
        }
    }

    private void BtnAplicaDimensiuniZone_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(TxtZ1L.Text, out double z1L) && double.TryParse(TxtZ1l.Text, out double z1l)) { _zone[0].Lungime = z1L; _zone[0].Latime = z1l; _zone[0].SuprafataMp = z1L * z1l; }
        if (double.TryParse(TxtZ2L.Text, out double z2L) && double.TryParse(TxtZ2l.Text, out double z2l)) { _zone[1].Lungime = z2L; _zone[1].Latime = z2l; _zone[1].SuprafataMp = z2L * z2l; }
        if (double.TryParse(TxtZ3L.Text, out double z3L) && double.TryParse(TxtZ3l.Text, out double z3l)) { _zone[2].Lungime = z3L; _zone[2].Latime = z3l; _zone[2].SuprafataMp = z3L * z3l; }
        if (double.TryParse(TxtZ4L.Text, out double z4L) && double.TryParse(TxtZ4l.Text, out double z4l)) { _zone[3].Lungime = z4L; _zone[3].Latime = z4l; _zone[3].SuprafataMp = z4L * z4l; }

        ActualizeazaToateZonele();
        ActualizeazaDateComuneSera();
        AdaugaLog("Dimensiunile celor 4 zone au fost actualizate și recalculate.");
    }

    private void BtnAplicaSera_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(TxtSeraL.Text, out double L) &&
            double.TryParse(TxtSeral.Text, out double l) &&
            double.TryParse(TxtSerah.Text, out double h))
        {
            _dimensiuniSera = new DimensiuniSera(L, l, h, procentCuloare: 15.0);
            ActualizeazaDateComuneSera();
            ActualizeazaToateZonele();
            AdaugaLog($"Dimensiuni seră modificate: {L}m x {l}m x {h}m.");
        }
    }

    private void BtnCitesteZona_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int zonaId))
        {
            CitesteSenzoriZona(zonaId);
        }
    }

    private void CitesteSenzoriZona(int zonaId)
    {
        if (!_senzoriZone.TryGetValue(zonaId, out var senzori)) return;

        double umid = senzori.Umiditate.CitesteValoare();
        double lux = senzori.Lumina.CitesteValoare();

        switch (zonaId)
        {
            case 1: TxtZ1ValUmid.Text = $"{umid:F1} %"; TxtZ1ValLux.Text = $"{lux:F0} Lux"; break;
            case 2: TxtZ2ValUmid.Text = $"{umid:F1} %"; TxtZ2ValLux.Text = $"{lux:F0} Lux"; break;
            case 3: TxtZ3ValUmid.Text = $"{umid:F1} %"; TxtZ3ValLux.Text = $"{lux:F0} Lux"; break;
            case 4: TxtZ4ValUmid.Text = $"{umid:F1} %"; TxtZ4ValLux.Text = $"{lux:F0} Lux"; break;
        }

        AdaugaLog($"[Zona {zonaId}] Umiditate: {umid:F1}% | Lumină: {lux:F0} Lux.");
    }

    private void BtnCitesteTotiSenzorii_Click(object sender, RoutedEventArgs e)
    {
        for (int i = 1; i <= 4; i++) CitesteSenzoriZona(i);
        AdaugaLog("S-au citit toți senzorii celor 4 zone simultan.");
    }

    private void AdaugaLog(string mesaj)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        TxtJurnal.AppendText($"[{timestamp}] {mesaj}\n");
        TxtJurnal.ScrollToEnd();
    }
}