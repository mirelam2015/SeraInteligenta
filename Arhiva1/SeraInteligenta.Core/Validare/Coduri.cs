namespace SeraInteligenta.Core.Validare;

/// <summary>Codurile stabile ale problemelor de validare (folosite în teste și în documentație).</summary>
public static class Coduri
{
    // Seră și alei
    public const string SeraDimensiuniInvalide = "SERA_DIMENSIUNI_INVALIDE";
    public const string AleePreaIngusta = "ALEE_PREA_INGUSTA";
    public const string AleeIngusta = "ALEE_INGUSTA";
    public const string ProcentCuloarNeobisnuit = "PROCENT_CULOAR_NEOBISNUIT";

    // Parcele
    public const string ZonaDimensiuniInvalide = "ZONA_DIMENSIUNI_INVALIDE";
    public const string ZonaMaiMareDecatSera = "ZONA_MAI_MARE_DECAT_SERA";
    public const string ZonaPreaMica = "ZONA_PREA_MICA";

    // Suprafață și amplasare
    public const string SuprafataDepasita = "SUPRAFATA_DEPASITA";
    public const string CuloarSubMinim = "CULOAR_SUB_MINIM";
    public const string SuprafataOk = "SUPRAFATA_OK";
    public const string AmplasareDepasita = "AMPLASARE_DEPASITA";

    // Plante
    public const string ZonaFaraPlanta = "ZONA_FARA_PLANTA";
    public const string PlantaNecunoscuta = "PLANTA_NECUNOSCUTA";
    public const string PlantaDublata = "PLANTA_DUBLATA";
    public const string PlanteIncompatibile = "PLANTE_INCOMPATIBILE";
    public const string ConditiiIncompatibile = "CONDITII_INCOMPATIBILE";
    public const string ConditiiLaLimita = "CONDITII_LA_LIMITA";
    public const string LuminaDiferita = "LUMINA_DIFERITA";
    public const string AceeasiFamilie = "ACEEASI_FAMILIE";
    public const string AsociereRecomandata = "ASOCIERE_RECOMANDATA";
    public const string InaltimeInsuficienta = "INALTIME_INSUFICIENTA";

    // Climatul comun al serei
    public const string ClimatFaraIntervalComun = "CLIMAT_FARA_INTERVAL_COMUN";
    public const string ClimatComunOk = "CLIMAT_COMUN_OK";
}
