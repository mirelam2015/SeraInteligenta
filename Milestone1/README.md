# Sera Inteligentă – Milestone 1

**Capilna Mirela**, grupa 10LD541 · Proiect comun IoT și IoC

Aplicație WPF (.NET 8) pentru configurarea unei sere pe zone, validarea parcelelor și monitorizarea senzorilor simulați.

Documentația este în folderul `Documentatie`: planul proiectului pe 5 etape și documentația acestei etape (Word și PDF).

## Cum rulezi

1. Ai nevoie de **Visual Studio 2022 sau mai nou** cu componenta **„.NET desktop development”** (include .NET 8).
2. Dublu-clic pe `SeraInteligenta.sln` (sau pe `Deschide_Proiectul.bat`).
3. În Solution Explorer: clic dreapta pe **SeraInteligenta.WPF** → **Set as Startup Project**.
4. **F5**. La primul build, Visual Studio descarcă singur pachetele NuGet.

Testele: meniul **Test → Run All Tests** (sau `dotnet test` în folderul soluției).

## Structura soluției

| Proiect | Rol | Depinde de |
|---|---|---|
| `SeraInteligenta.Core` | Modele, interfețe, amplasarea parcelelor și a senzorilor, climatul comun, regulile de validare | nimic |
| `SeraInteligenta.Infrastructure` | Citire/scriere JSON, mediul și senzorii simulați, serviciul de monitorizare | Core |
| `SeraInteligenta.WPF` | Interfața (MVVM) și containerul de IoC (`CompozitieServicii.cs`) | Core, Infrastructure |
| `SeraInteligenta.Tests` | 61 de teste xUnit pentru reguli, amplasare, climat, JSON și senzori | Core, Infrastructure |
| `Date/` | `plante_catalog.json` (10 plante) și `configuratie_sera.json` (4 zone) | – |

## Ce face

- **Configurarea serei**: dimensiuni, lățimea aleilor, procentul minim pentru culoare de acces.
- **Parcele**: fiecare zonă are dimensiuni, o plantă principală și, opțional, una secundară.
- **Validări**: 7 reguli (clase separate) cu peste 20 de verificări, pe 3 niveluri: eroare, avertisment, info.
  - spațiu: dimensiuni valide, parcela nu e mai mare decât sera, suprafața totală, culoarul minim,
    încadrarea **fizică** cu alei între parcele, lățimea aleilor, înălțimea serei față de plante;
  - plante în aceeași parcelă: incompatibilități declarate, umiditatea solului, lumină, aceeași familie botanică, asocieri;
  - climatul comun: aerul serei e unul singur, deci temperatura și umiditatea aerului trebuie să convină tuturor plantelor din seră.
- **Harta 2D la scară**: parcelele sunt „plantate” cu iconița plantei, colorate după probleme; clic pe o parcelă o selectează.
- **Senzori amplasați după dimensiuni** și recalculați la orice modificare:
  - **stații de climat** (temperatură + umiditate aer), comune serei: în colțuri, la 1 m de pereți, și în interior,
    la cel mult 8 m una de alta (12 stații pentru 24 × 12 m);
  - pe fiecare parcelă: **umiditatea solului** (un senzor la 20 m²) și **lumina** (un senzor la 40 m²).
- **Simulare realistă**: un singur aer pentru toată sera (lângă pereți puțin mai rece și mai umed), sol și lumină pe parcele,
  plus eroarea de calibrare a fiecărui senzor. Temperatura la o parcelă e estimată din cele mai apropiate 3 stații.
- **Salvare / reîncărcare** a configurației în JSON.

## Unde se vede IoC

- `CompozitieServicii.cs` e singurul loc unde se aleg implementările; restul claselor primesc interfețe prin constructor.
- Regulile de validare sunt mai multe implementări ale `IRegulaValidare`; `ValidatorSera` le primește ca listă.
  O regulă nouă = o clasă + un rând de înregistrare.
- `IFabricaSenzori` decide ce senzori se creează: în M3 se înlocuiește cu senzori MQTT fără alte modificări.
- În teste, regulile primesc un catalog fals (`CatalogInMemorie`), fără fișiere.

## Notă

Datele agronomice din catalog (intervale, incompatibilități, asocieri) sunt simplificate, cu scop demonstrativ.

## Instrumente folosite

La proiectarea arhitecturii, scrierea codului, a testelor și a documentației am folosit asistentul AI Claude (Anthropic).
Codul este compilat, rulat și testat în Visual Studio.
