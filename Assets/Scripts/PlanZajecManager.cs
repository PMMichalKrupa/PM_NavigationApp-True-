// =============================================================================
// PlanZajecManager.cs
// Politechnika Morska w Szczecinie — Wirtualny Dziekanat
// Integracja planu zajęć z silnikiem Unity
// =============================================================================
//
// OPIS OGÓLNY
// -----------
// Komponent MonoBehaviour pobierający dane z systemu Wirtualny Dziekanat
// (plany.am.szczecin.pl). Umożliwia:
//   1. Pobranie listy wszystkich dostępnych toków studiów (kierunek + tok).
//   2. Pobranie planu zajęć dla wybranego toku w zadanym przedziale dat.
//   3. Znalezienie sali, w której odbywają się zajęcia o podanym dniu i godzinie.
//
// ARCHITEKTURA
// ------------
// Klasa główna:   PlanZajecManager     — MonoBehaviour, centrum logiki
// Modele danych:  TokStudiow           — reprezentacja toku (kierunek, nazwa, ID)
//                 ZajeciaCsvRow        — wiersz planu z pliku CSV
//
// UŻYTE ENDPOINTY (brak oficjalnego API — dostęp przez HTML scraping i CSV)
// -------------------------------------------------------------------------
//   Lista toków (HTML):
//     GET https://plany.am.szczecin.pl/Plany/ZnajdzTok
//
//   Eksport planu toku (CSV):
//     GET https://plany.am.szczecin.pl/Plany/WydrukTokuCsv/{tokId}
//         ?dO={dataOd}&dD={dataDo}
//     Format daty: "MM/dd/yyyy HH:mm:ss" (URL-encoded)
//
// WYMAGANIA
// ---------
//   • Unity 2020.3 LTS lub nowsze
//   • Dostęp do internetu z poziomu buildu (ustawienie Project Settings → Player)
//   • Brak dodatkowych paczek — kod używa wyłącznie UnityEngine.Networking
//
// PRZYKŁAD UŻYCIA
// ---------------
//   Podłącz PlanZajecManager do GameObject w scenie.
//   Wywołaj ZnajdzSale() po wcześniejszym załadowaniu toków przez PobierzListeTokow().
//   Patrz: klasa PrzykladUzycia na końcu pliku.
//
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

// =============================================================================
// MODELE DANYCH
// =============================================================================

/// <summary>
/// Reprezentuje pojedynczy tok studiów pobrany ze strony Wirtualnego Dziekanatu.
/// Tok identyfikuje konkretną grupę studencką (kierunek + specjalność + nabór + tryb).
/// </summary>
[Serializable]
public class TokStudiow
{
    /// <summary>
    /// Unikalny identyfikator toku w systemie Wirtualnego Dziekanatu.
    /// Używany jako parametr ścieżki w URL-ach eksportu CSV.
    /// Przykład: 455
    /// </summary>
    public int Id;

    /// <summary>
    /// Nazwa kierunku studiów wyodrębniona z tabeli na stronie.
    /// Przykład: "Informatyka", "Logistyka", "Nawigacja"
    /// </summary>
    public string Kierunek;

    /// <summary>
    /// Pełna etykieta toku, która pojawia się jako tekst linku na stronie.
    /// Zawiera: specjalność, tryb, czas trwania, rok akademicki i porę roku.
    /// Przykład: "Sztuczna Inteligencja S mgr 1.50 2025/2026 zima"
    /// </summary>
    public string NazwaToku;

    /// <summary>
    /// Rok akademicki wyodrębniony z nazwy toku.
    /// Przykład: "2025/2026"
    /// </summary>
    public string RokAkademicki;

    /// <summary>
    /// Pora roku semestru ("zima" lub "lato") wyodrębniona z nazwy toku.
    /// </summary>
    public string Semestr;

    /// <summary>
    /// Zwraca czytelną reprezentację toku w formacie:
    /// [ID] Kierunek — NazwaToku
    /// </summary>
    public override string ToString() =>
        $"[{Id}] {Kierunek} — {NazwaToku}";
}

/// <summary>
/// Reprezentuje jeden wiersz danych z pliku CSV eksportowanego dla toku.
/// Każdy wiersz odpowiada jednemu blokowi zajęć (wykład, ćwiczenia, laboratorium itp.).
/// </summary>
[Serializable]
public class ZajeciaCsvRow
{
    /// <summary>Data zajęć w postaci tekstowej (np. "2026-03-06").</summary>
    public string DataRaw;

    /// <summary>
    /// Godzina rozpoczęcia zajęć w postaci tekstowej (np. "08:00").
    /// Używana przy wyszukiwaniu sali po godzinie.
    /// </summary>
    public string GodzinaOdRaw;

    /// <summary>Godzina zakończenia zajęć w postaci tekstowej (np. "09:30").</summary>
    public string GodzinaDoRaw;

    /// <summary>
    /// Godzina rozpoczęcia zajęć sparsowana do TimeSpan.
    /// Pole null, jeśli parsowanie nie powiodło się.
    /// </summary>
    public TimeSpan GodzinaOd;

    /// <summary>
    /// Godzina zakończenia zajęć sparsowana do TimeSpan.
    /// Pole null, jeśli parsowanie nie powiodło się.
    /// </summary>
    public TimeSpan GodzinaDo;

    /// <summary>
    /// Liczba godzin dydaktycznych bloku zajęć (np. "2").
    /// </summary>
    public string LiczbaGodzin;

    /// <summary>Nazwa przedmiotu (np. "Algorytmy i struktury danych").</summary>
    public string Przedmiot;

    /// <summary>
    /// Typ zajęć: W = Wykład, C = Ćwiczenia, L = Laboratorium, P = Projekt itp.
    /// </summary>
    public string Typ;

    /// <summary>Imię i nazwisko prowadzącego zajęcia.</summary>
    public string Prowadzacy;

    /// <summary>
    /// Numer lub nazwa sali — główna wartość zwracana przy zapytaniu o salę.
    /// Przykłady: "A101", "LAB-3", "Aula Główna"
    /// Wartość null lub pusty string oznacza brak sali (zajęcia zdalne / nieprzypisane).
    /// </summary>
    public string Sala;

    /// <summary>Numer lub kod grupy dziekańskiej (np. "IFI-21-A").</summary>
    public string Grupa;

    /// <summary>
    /// Zwraca czytelną reprezentację zajęć w formacie:
    /// DataRaw GodzinaOdRaw-GodzinaDoRaw | Przedmiot (Typ) | Sala | Prowadzący
    /// </summary>
    public override string ToString() =>
        $"{DataRaw} {GodzinaOdRaw}-{GodzinaDoRaw} | {Przedmiot} ({Typ}) | Sala: {Sala} | {Prowadzacy}";
}

// =============================================================================
// GŁÓWNY KOMPONENT
// =============================================================================

/// <summary>
/// Główny komponent Unity odpowiedzialny za komunikację z Wirtualnym Dziekanatem
/// Politechniki Morskiej w Szczecinie (plany.am.szczecin.pl).
///
/// <para>
/// Podłącz ten komponent do dowolnego GameObject w scenie Unity.
/// Wszystkie operacje sieciowe są asynchroniczne (Coroutine).
/// Wyniki przekazywane są przez callbacki (Action&lt;T&gt;).
/// </para>
///
/// <para>
/// Typowy przepływ użycia:
/// <code>
///   1. PobierzListeTokow(onDone)   — jednorazowo przy starcie
///   2. ZnajdzSale(kierunek, tok, data, godzina, onResult)
/// </code>
/// </para>
/// </summary>
public class PlanZajecManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // STAŁE I KONFIGURACJA
    // -------------------------------------------------------------------------

    /// <summary>
    /// Bazowy URL systemu Wirtualny Dziekanat.
    /// Zmień tylko jeśli serwer zmieni domenę lub protokół.
    /// </summary>
    private const string BaseUrl = "https://plany.am.szczecin.pl";

    /// <summary>
    /// Ścieżka do strony z listą toków.
    /// Strona renderuje tabelę HTML z linkami do planów poszczególnych toków.
    /// </summary>
    private const string SciezkaListaTokow = "/Plany/ZnajdzTok";

    /// <summary>
    /// Wzorzec URL eksportu CSV dla toku.
    /// Parametry: {0} = tokId, {1} = dataOd (URL-encoded), {2} = dataDo (URL-encoded).
    /// </summary>
    private const string SzablonCsvUrl =
        "/Plany/WydrukTokuCsv/{0}?dO={1}&dD={2}";

    /// <summary>
    /// Tolerancja w minutach przy porównywaniu godzin zajęć z podaną godziną.
    /// Zapobiega problemom wynikającym z zaokrągleń (np. 08:00 vs 08:01).
    /// </summary>
    private const int TolMinut = 5;

    /// <summary>
    /// Timeout żądań HTTP w sekundach.
    /// Zwiększ jeśli połączenie z serwerem jest wolne.
    /// </summary>
    private const int TimeoutSekund = 15;

    // -------------------------------------------------------------------------
    // STAN WEWNĘTRZNY
    // -------------------------------------------------------------------------

    /// <summary>
    /// Załadowana lista wszystkich toków studiów.
    /// Wypełniana przez <see cref="PobierzListeTokow"/>.
    /// Dostępna publicznie do odczytu (np. w celu wyświetlenia w UI).
    /// </summary>
    public List<TokStudiow> ZaladowaneToki { get; private set; } = new List<TokStudiow>();

    /// <summary>
    /// Flaga informująca, czy lista toków została już pobrana z serwera.
    /// Użyj tej flagi przed wywołaniem ZnajdzSale(), aby upewnić się,
    /// że dane są gotowe.
    /// </summary>
    public bool TokiZaladowane { get; private set; } = false;

    // =========================================================================
    // PUBLICZNE API
    // =========================================================================

    /// <summary>
    /// Pobiera z serwera pełną listę toków studiów i wypełnia
    /// <see cref="ZaladowaneToki"/>.
    ///
    /// <para>
    /// Metoda wykonuje żądanie GET na stronę /Plany/ZnajdzTok,
    /// parsuje zwrócony HTML i wyodrębnia: ID toku, kierunek oraz nazwę toku.
    /// </para>
    ///
    /// <para>
    /// Zaleca się wywołanie tej metody raz, przy inicjalizacji aplikacji (np. w Start()).
    /// Po zakończeniu flaga <see cref="TokiZaladowane"/> jest ustawiana na true.
    /// </para>
    /// </summary>
    /// <param name="onDone">
    /// Callback wywoływany po zakończeniu operacji.
    /// Parametr: lista załadowanych toków lub pusta lista przy błędzie.
    /// </param>
    /// <example>
    /// <code>
    /// planManager.PobierzListeTokow(toki =>
    /// {
    ///     Debug.Log($"Załadowano {toki.Count} toków.");
    ///     foreach (var tok in toki)
    ///         Debug.Log(tok.ToString());
    /// });
    /// </code>
    /// </example>
    public void PobierzListeTokow(Action<List<TokStudiow>> onDone = null)
    {
        StartCoroutine(PobierzListeTokow_Coroutine(onDone));
    }

    /// <summary>
    /// Znajduje salę, w której dany tok ma zajęcia w podanym dniu i o podanej godzinie.
    ///
    /// <para>
    /// Metoda wymaga wcześniejszego załadowania listy toków przez
    /// <see cref="PobierzListeTokow"/>. Jeśli lista nie jest gotowa,
    /// callback otrzyma wartość null z komunikatem błędu w logach.
    /// </para>
    ///
    /// <para>
    /// Algorytm:
    /// <list type="number">
    ///   <item>Wyszukuje tok pasujący do podanego kierunku i nazwy.</item>
    ///   <item>Pobiera CSV dla tego toku na dany dzień.</item>
    ///   <item>Przeszukuje wiersze CSV w poszukiwaniu godziny z tolerancją ±<see cref="TolMinut"/> min.</item>
    ///   <item>Zwraca salę lub null, gdy zajęcia nie są zaplanowane.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="kierunek">
    /// Nazwa kierunku studiów (musi dokładnie pasować do wartości
    /// w <see cref="TokStudiow.Kierunek"/>).
    /// Przykład: "Informatyka"
    /// </param>
    /// <param name="nazwaLubFragmentToku">
    /// Fragment lub pełna nazwa toku (dopasowanie częściowe, bez uwzględnienia wielkości liter).
    /// Przykład: "Sztuczna Inteligencja S mgr" albo "2025/2026 zima"
    /// </param>
    /// <param name="data">Data zajęć.</param>
    /// <param name="godzinaOd">
    /// Godzina rozpoczęcia szukanych zajęć (np. <c>new TimeSpan(8, 0, 0)</c> dla 8:00).
    /// </param>
    /// <param name="onResult">
    /// Callback z wynikiem:
    /// <list type="bullet">
    ///   <item>Numer/nazwa sali (string niepusty) — gdy znaleziono zajęcia.</item>
    ///   <item>null — gdy o podanej godzinie nie ma zajęć.</item>
    /// </list>
    /// </param>
    /// <example>
    /// <code>
    /// planManager.ZnajdzSale(
    ///     kierunek:              "Informatyka",
    ///     nazwaLubFragmentToku:  "Sztuczna Inteligencja S mgr",
    ///     data:                  new DateTime(2026, 3, 10),
    ///     godzinaOd:             new TimeSpan(10, 0, 0),
    ///     onResult: sala =>
    ///     {
    ///         if (sala != null)
    ///             Debug.Log($"Zajęcia w sali: {sala}");
    ///         else
    ///             Debug.Log("Brak zajęć o podanej godzinie.");
    ///     }
    /// );
    /// </code>
    /// </example>
    public void ZnajdzSale(string kierunek, string nazwaLubFragmentToku,
                           DateTime data, TimeSpan godzinaOd,
                           Action<string> onResult)
    {
        if (!TokiZaladowane)
        {
            Debug.LogError("[PlanZajec] Lista toków nie została jeszcze załadowana. " +
                           "Wywołaj PobierzListeTokow() przed ZnajdzSale().");
            onResult?.Invoke(null);
            return;
        }

        // Wyszukaj pasujący tok
        TokStudiow dopasowany = WyszukajTok(kierunek, nazwaLubFragmentToku);
        if (dopasowany == null)
        {
            Debug.LogWarning($"[PlanZajec] Nie znaleziono toku dla kierunku='{kierunek}' " +
                             $"fragment='{nazwaLubFragmentToku}'.");
            onResult?.Invoke(null);
            return;
        }

        StartCoroutine(ZnajdzSale_Coroutine(dopasowany.Id, data, godzinaOd, onResult));
    }

    /// <summary>
    /// Pobiera wszystkie zajęcia dla danego toku w podanym przedziale dat.
    ///
    /// <para>
    /// Przydatne do wyświetlania tygodniowego lub miesięcznego widoku planu.
    /// Wymaga wcześniejszego wywołania <see cref="PobierzListeTokow"/>.
    /// </para>
    /// </summary>
    /// <param name="kierunek">Nazwa kierunku studiów.</param>
    /// <param name="nazwaLubFragmentToku">Fragment lub pełna nazwa toku.</param>
    /// <param name="dataOd">Początek przedziału dat.</param>
    /// <param name="dataDo">Koniec przedziału dat.</param>
    /// <param name="onResult">
    /// Callback z listą zajęć lub null przy błędzie / braku toku.
    /// </param>
    /// <example>
    /// <code>
    /// planManager.PobierzPlanToku(
    ///     "Nawigacja", "Transport Morski",
    ///     new DateTime(2026, 3, 2), new DateTime(2026, 3, 8),
    ///     zajecia =>
    ///     {
    ///         if (zajecia == null) return;
    ///         foreach (var z in zajecia)
    ///             Debug.Log(z.ToString());
    ///     }
    /// );
    /// </code>
    /// </example>
    public void PobierzPlanToku(string kierunek, string nazwaLubFragmentToku,
                                DateTime dataOd, DateTime dataDo,
                                Action<List<ZajeciaCsvRow>> onResult)
    {
        if (!TokiZaladowane)
        {
            Debug.LogError("[PlanZajec] Lista toków nie jest załadowana.");
            onResult?.Invoke(null);
            return;
        }

        TokStudiow dopasowany = WyszukajTok(kierunek, nazwaLubFragmentToku);
        if (dopasowany == null)
        {
            Debug.LogWarning($"[PlanZajec] Nie znaleziono toku dla kierunku='{kierunek}' " +
                             $"fragment='{nazwaLubFragmentToku}'.");
            onResult?.Invoke(null);
            return;
        }

        StartCoroutine(PobierzCSV_Coroutine(dopasowany.Id, dataOd, dataDo, onResult));
    }

    /// <summary>
    /// Zwraca listę unikalnych nazw kierunków ze wszystkich załadowanych toków.
    ///
    /// <para>
    /// Przydatne do wypełniania list rozwijanych (dropdown) w UI.
    /// Wymaga wcześniejszego wywołania <see cref="PobierzListeTokow"/>.
    /// </para>
    /// </summary>
    /// <returns>
    /// Posortowana alfabetycznie lista unikalnych nazw kierunków.
    /// Pusta lista, jeśli toki nie zostały jeszcze załadowane.
    /// </returns>
    public List<string> GetKierunki()
    {
        var kierunki = new HashSet<string>();
        foreach (var tok in ZaladowaneToki)
            if (!string.IsNullOrWhiteSpace(tok.Kierunek))
                kierunki.Add(tok.Kierunek);

        var lista = new List<string>(kierunki);
        lista.Sort(StringComparer.OrdinalIgnoreCase);
        return lista;
    }

    /// <summary>
    /// Zwraca listę toków dla konkretnego kierunku.
    ///
    /// <para>
    /// Przydatne do kaskadowego wypełniania UI: najpierw wybierz kierunek,
    /// potem na podstawie tego wywołania wypełnij listę toków.
    /// </para>
    /// </summary>
    /// <param name="kierunek">
    /// Dokładna nazwa kierunku (tak jak zwracana przez <see cref="GetKierunki"/>).
    /// </param>
    /// <returns>
    /// Lista toków należących do podanego kierunku.
    /// Pusta lista, jeśli kierunek nie istnieje lub toki nie są załadowane.
    /// </returns>
    public List<TokStudiow> GetTokiDlaKierunku(string kierunek)
    {
        var wynik = new List<TokStudiow>();
        foreach (var tok in ZaladowaneToki)
            if (string.Equals(tok.Kierunek, kierunek, StringComparison.OrdinalIgnoreCase))
                wynik.Add(tok);
        return wynik;
    }

    // =========================================================================
    // LOGIKA WEWNĘTRZNA — POBIERANIE I PARSOWANIE
    // =========================================================================

    /// <summary>
    /// Wyszukuje tok pasujący do podanego kierunku i fragmentu nazwy.
    /// Porównanie jest niewrażliwe na wielkość liter.
    /// </summary>
    /// <param name="kierunek">Dokładna nazwa kierunku.</param>
    /// <param name="fragmentNazwy">Fragment nazwy toku (dopasowanie Contains).</param>
    /// <returns>Pasujący TokStudiow lub null, jeśli nic nie znaleziono.</returns>
    private TokStudiow WyszukajTok(string kierunek, string fragmentNazwy)
    {
        foreach (var tok in ZaladowaneToki)
        {
            bool kierunekPasuje = string.Equals(tok.Kierunek, kierunek,
                StringComparison.OrdinalIgnoreCase);
            bool nazwaLubFragmentPasuje = tok.NazwaToku.IndexOf(
                fragmentNazwy, StringComparison.OrdinalIgnoreCase) >= 0;

            if (kierunekPasuje && nazwaLubFragmentPasuje)
                return tok;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // COROUTINE: pobieranie listy toków (HTML scraping)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Coroutine pobierająca stronę /Plany/ZnajdzTok i parsująca tabelę toków z HTML.
    ///
    /// <para>
    /// Strona renderuje tabelę z kolumnami: Nabór, Kierunek, Specjalność, Nazwa Toku.
    /// Linki do planów mają format: /Plany/PlanyTokow/{id}
    /// Wyodrębniane są: id, kierunek oraz pełna etykieta toku z tekstu linku.
    /// </para>
    /// </summary>
    private IEnumerator PobierzListeTokow_Coroutine(Action<List<TokStudiow>> onDone)
    {
        string url = BaseUrl + SciezkaListaTokow;
        Debug.Log($"[PlanZajec] Pobieranie listy toków: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = TimeoutSekund;
            req.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (compatible; UnityPlanZajec/2.0)");
            req.SetRequestHeader("Accept",
                "text/html,application/xhtml+xml,*/*");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PlanZajec] Błąd HTTP przy pobieraniu listy toków: {req.error}");
                onDone?.Invoke(new List<TokStudiow>());
                yield break;
            }

            string html = req.downloadHandler.text;
            List<TokStudiow> toki = ParseujListeTokow(html);

            ZaladowaneToki = toki;
            TokiZaladowane = true;

            Debug.Log($"[PlanZajec] Załadowano {toki.Count} toków studiów.");
            onDone?.Invoke(toki);
        }
    }

    /// <summary>
    /// Parsuje kod HTML strony /Plany/ZnajdzTok i wyodrębnia listę toków.
    ///
    /// <para>
    /// Algorytm skanuje HTML w poszukiwaniu wzorca:
    /// <c>&lt;a href="/Plany/PlanyTokow/{id}"&gt;{etykieta}&lt;/a&gt;</c>
    /// a następnie dla każdego linku szuka kolumny "Kierunek" w otaczającym wierszu.
    /// </para>
    ///
    /// <para>
    /// Uwaga: parsowanie opiera się na stabilnej strukturze HTML strony.
    /// Zmiana szablonu strony może wymagać aktualizacji wyrażeń regularnych.
    /// </para>
    /// </summary>
    /// <param name="html">Surowy HTML pobrany z serwera.</param>
    /// <returns>Lista sparsowanych toków. Może być pusta jeśli HTML ma nieoczekiwaną strukturę.</returns>
    private List<TokStudiow> ParseujListeTokow(string html)
    {
        var wynik = new List<TokStudiow>();

        // Wyrażenie regularne wyodrębnia ID toku i etykietę linku z tabeli wyników.
        // Wzorzec: <a href="/Plany/PlanyTokow/NNN">Etykieta toku</a>
        // Kolumna Kierunek leży w poprzednim <td> w tym samym wierszu tabeli.
        var wzorzecLinku = new Regex(
            @"href=""/Plany/PlanyTokow/(\d+)"">([^<]+)</a>",
            RegexOptions.IgnoreCase);

        // Wzorzec do wyodrębnienia wiersza tabeli zawierającego link do toku.
        // Zakładamy, że wiersz wygląda tak:
        //   <td>...</td>              — Nabór
        //   <td>KIERUNEK</td>         — Kierunek
        //   <td>...</td>              — Specjalność (opcjonalna)
        //   <td><a href="...">...</a></td>   — Nazwa Toku (link)
        var wzorzecWiersza = new Regex(
            @"<td[^>]*>\s*(.*?)\s*</td>\s*" +   // Nabór
            @"<td[^>]*>\s*(.*?)\s*</td>\s*" +   // Kierunek
            @"<td[^>]*>\s*(.*?)\s*</td>\s*" +   // Specjalność
            @"<td[^>]*>.*?href=""/Plany/PlanyTokow/(\d+)"">([^<]+)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Próbujemy najpierw dopasowania z pełnym kontekstem wiersza
        MatchCollection dopasowaniaWierszy = wzorzecWiersza.Matches(html);

        if (dopasowaniaWierszy.Count > 0)
        {
            // Ścieżka preferowana: mamy pełny kontekst wiersza z kierunkiem
            foreach (Match m in dopasowaniaWierszy)
            {
                string nakor     = OczyyscHtml(m.Groups[1].Value);
                string kierunek  = OczyyscHtml(m.Groups[2].Value);
                string tokIdStr  = m.Groups[4].Value.Trim();
                string etykieta  = OczyyscHtml(m.Groups[5].Value);

                if (!int.TryParse(tokIdStr, out int tokId)) continue;
                if (string.IsNullOrWhiteSpace(etykieta)) continue;

                wynik.Add(BudujTok(tokId, kierunek, etykieta));
            }
        }
        else
        {
            // Fallback: parsujemy samodzielnie linki i kierunek z sąsiednich <td>
            // (np. gdy struktura tabeli jest inna niż oczekiwana)
            var wzorzecFallback = new Regex(
                @"<td[^>]*>\s*([^<]*)\s*</td>\s*(?:<td[^>]*>.*?</td>\s*)*?" +
                @"href=""/Plany/PlanyTokow/(\d+)"">([^<]+)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match m in wzorzecFallback.Matches(html))
            {
                string kierunek = OczyyscHtml(m.Groups[1].Value);
                if (!int.TryParse(m.Groups[2].Value.Trim(), out int tokId)) continue;
                string etykieta = OczyyscHtml(m.Groups[3].Value);
                if (string.IsNullOrWhiteSpace(etykieta)) continue;
                wynik.Add(BudujTok(tokId, kierunek, etykieta));
            }

            // Jeśli nadal nic — przynajmniej zbieramy same linki (bez kierunku)
            if (wynik.Count == 0)
            {
                foreach (Match m in wzorzecLinku.Matches(html))
                {
                    if (!int.TryParse(m.Groups[1].Value, out int tokId)) continue;
                    string etykieta = OczyyscHtml(m.Groups[2].Value);
                    if (string.IsNullOrWhiteSpace(etykieta)) continue;
                    wynik.Add(BudujTok(tokId, kierunek: "", etykieta));
                }
            }
        }

        // Usuwamy duplikaty po ID (tabela może zawierać wiersze wiele razy)
        var unikalne = new Dictionary<int, TokStudiow>();
        foreach (var tok in wynik)
            if (!unikalne.ContainsKey(tok.Id))
                unikalne[tok.Id] = tok;

        return new List<TokStudiow>(unikalne.Values);
    }

    /// <summary>
    /// Tworzy obiekt TokStudiow i uzupełnia pola RokAkademicki i Semestr
    /// na podstawie etykiety tekstowej.
    /// </summary>
    /// <param name="id">ID toku z URL.</param>
    /// <param name="kierunek">Nazwa kierunku studiów.</param>
    /// <param name="etykieta">Pełna etykieta toku (tekst linku ze strony).</param>
    /// <returns>Wypełniony obiekt TokStudiow.</returns>
    private TokStudiow BudujTok(int id, string kierunek, string etykieta)
    {
        var tok = new TokStudiow
        {
            Id       = id,
            Kierunek = kierunek.Trim(),
            NazwaToku = etykieta.Trim()
        };

        // Wyodrębnij rok akademicki (np. "2025/2026")
        var mRok = Regex.Match(etykieta, @"\d{4}/\d{4}");
        if (mRok.Success) tok.RokAkademicki = mRok.Value;

        // Wyodrębnij semestr (zima / lato)
        var mSem = Regex.Match(etykieta, @"\b(zima|lato)\b", RegexOptions.IgnoreCase);
        if (mSem.Success) tok.Semestr = mSem.Value.ToLower();

        return tok;
    }

    // -------------------------------------------------------------------------
    // COROUTINE: znajdowanie sali
    // -------------------------------------------------------------------------

    /// <summary>
    /// Coroutine wykonująca faktyczną logikę wyszukiwania sali:
    /// pobiera CSV dla danego toku i dnia, następnie przeszukuje wiersze.
    /// </summary>
    /// <param name="tokId">ID toku w systemie.</param>
    /// <param name="data">Data dla której szukamy zajęć.</param>
    /// <param name="godzinaOd">Godzina początku szukanych zajęć.</param>
    /// <param name="onResult">Callback: sala (string) lub null.</param>
    private IEnumerator ZnajdzSale_Coroutine(int tokId, DateTime data,
                                              TimeSpan godzinaOd,
                                              Action<string> onResult)
    {
        List<ZajeciaCsvRow> zajecia = null;

        // Pobieramy dane tylko dla jednego dnia (dataOd == dataDo)
        yield return StartCoroutine(PobierzCSV_Coroutine(
            tokId, data.Date, data.Date, rows => zajecia = rows));

        if (zajecia == null)
        {
            Debug.LogWarning($"[PlanZajec] Nie udało się pobrać CSV dla toku {tokId}.");
            onResult?.Invoke(null);
            yield break;
        }

        if (zajecia.Count == 0)
        {
            // Brak zajęć w tym dniu — to sytuacja normalna (np. weekend, przerwa)
            Debug.Log($"[PlanZajec] Brak zajęć dla toku {tokId} w dniu {data:dd.MM.yyyy}.");
            onResult?.Invoke(null);
            yield break;
        }

        // Szukaj wiersza o godzinie z tolerancją TolMinut
        string znalezionaSala = null;
        foreach (var wiersz in zajecia)
        {
            double roznica = Math.Abs((wiersz.GodzinaOd - godzinaOd).TotalMinutes);
            if (roznica <= TolMinut)
            {
                // Zwracamy salę nawet jeśli jest pusta — null sygnalizuje brak przypisania
                znalezionaSala = string.IsNullOrWhiteSpace(wiersz.Sala)
                    ? null
                    : wiersz.Sala.Trim();

                Debug.Log($"[PlanZajec] Znaleziono: {wiersz}");
                break;
            }
        }

        if (znalezionaSala == null)
            Debug.Log($"[PlanZajec] Brak zajęć o godzinie {godzinaOd:hh\\:mm} " +
                      $"(±{TolMinut} min) dla toku {tokId} w dniu {data:dd.MM.yyyy}.");

        onResult?.Invoke(znalezionaSala);
    }

    // -------------------------------------------------------------------------
    // COROUTINE: pobieranie i parsowanie CSV
    // -------------------------------------------------------------------------

    /// <summary>
    /// Coroutine pobierająca plik CSV planu toku z serwera.
    ///
    /// <para>
    /// Wywołuje endpoint: /Plany/WydrukTokuCsv/{tokId}?dO={dataOd}&amp;dD={dataDo}
    /// i parsuje zwrócony CSV za pomocą <see cref="ParseujCSV"/>.
    /// </para>
    ///
    /// <para>
    /// Uwaga: serwer zwraca plik zakodowany w Windows-1250. UnityWebRequest
    /// domyślnie interpretuje odpowiedź jako UTF-8, co może powodować błędy
    /// w polskich znakach. W razie problemów z kodowaniem użyj DownloadHandlerBuffer
    /// i ręcznie zdekoduj z Encoding.GetEncoding(1250).
    /// </para>
    /// </summary>
    /// <param name="tokId">ID toku.</param>
    /// <param name="dataOd">Pierwsza data przedziału.</param>
    /// <param name="dataDo">Ostatnia data przedziału.</param>
    /// <param name="onResult">Callback z listą wierszy lub null przy błędzie.</param>
    private IEnumerator PobierzCSV_Coroutine(int tokId,
                                              DateTime dataOd, DateTime dataDo,
                                              Action<List<ZajeciaCsvRow>> onResult)
    {
        // Budowanie URL — format daty wymagany przez serwer: "MM/dd/yyyy HH:mm:ss"
        string dO = Uri.EscapeDataString(dataOd.ToString("MM/dd/yyyy 00:00:00"));
        string dD = Uri.EscapeDataString(dataDo.ToString("MM/dd/yyyy 23:59:59"));
        string url = BaseUrl + string.Format(SzablonCsvUrl, tokId, dO, dD);

        Debug.Log($"[PlanZajec] Pobieranie CSV toku {tokId}: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = TimeoutSekund;
            req.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (compatible; UnityPlanZajec/2.0)");
            req.SetRequestHeader("Accept", "text/csv,text/plain,*/*");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PlanZajec] Błąd HTTP przy pobieraniu CSV " +
                               $"(tok={tokId}): {req.error} | URL: {url}");
                onResult?.Invoke(null);
                yield break;
            }

            string csv = req.downloadHandler.text;
            Debug.Log($"[PlanZajec] Pobrano CSV dla toku {tokId} " +
                      $"({csv.Length} znaków, " +
                      $"{dataOd:dd.MM.yyyy}–{dataDo:dd.MM.yyyy}).");

            onResult?.Invoke(ParseujCSV(csv));
        }
    }

    // -------------------------------------------------------------------------
    // PARSOWANIE CSV
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parsuje zawartość pliku CSV zwróconego przez serwer.
    ///
    /// <para>
    /// Format serwera: plik rozdzielany średnikami (;), pierwsza linia to nagłówek.
    /// Przewidywane kolumny (kolejność może się różnić w zależności od wersji serwera):
    /// <list type="table">
    ///   <item><term>Data Zajęć</term><description>Data w formacie DD.MM.YYYY lub YYYY-MM-DD</description></item>
    ///   <item><term>Godz. od</term><description>Godzina start, np. "08:00"</description></item>
    ///   <item><term>Godz. do</term><description>Godzina koniec, np. "09:30"</description></item>
    ///   <item><term>Liczba godzin</term><description>Liczba godzin dydaktycznych</description></item>
    ///   <item><term>Przedmiot</term><description>Nazwa przedmiotu</description></item>
    ///   <item><term>Typ</term><description>Typ zajęć (W/C/L/P)</description></item>
    ///   <item><term>Prowadzący</term><description>Imię i nazwisko prowadzącego</description></item>
    ///   <item><term>Sala</term><description>Numer sali</description></item>
    ///   <item><term>Grupa</term><description>Kod grupy dziekańskiej</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Metoda jest odporna na: brakujące kolumny, różną kolejność kolumn,
    /// puste linie, cudzysłowy wokół wartości.
    /// </para>
    /// </summary>
    /// <param name="csv">Surowa zawartość pliku CSV jako string.</param>
    /// <returns>
    /// Lista sparsowanych wierszy. Pusta lista jeśli CSV jest pusty lub zawiera
    /// tylko nagłówek (brak zajęć w danym przedziale).
    /// </returns>
    private List<ZajeciaCsvRow> ParseujCSV(string csv)
    {
        var wynik = new List<ZajeciaCsvRow>();

        if (string.IsNullOrWhiteSpace(csv))
        {
            Debug.Log("[PlanZajec] Plik CSV jest pusty — brak zajęć w podanym przedziale.");
            return wynik;
        }

        // Normalizacja znaków nowej linii (Windows \r\n, Unix \n, stary Mac \r)
        string[] linie = csv.Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.RemoveEmptyEntries);

        if (linie.Length < 2)
        {
            Debug.Log("[PlanZajec] CSV zawiera tylko nagłówek lub jest pusty.");
            return wynik;
        }

        // Parsowanie nagłówka — budujemy słownik: nazwaKolumny → indeks
        string[] naglowek = SplitCSVLinia(linie[0]);
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < naglowek.Length; i++)
            idx[naglowek[i].Trim().Trim('"')] = i;

        // Parsowanie wierszy danych
        for (int li = 1; li < linie.Length; li++)
        {
            string[] pola = SplitCSVLinia(linie[li]);
            if (pola.Length < 2) continue;

            try
            {
                var row = new ZajeciaCsvRow
                {
                    DataRaw      = GetKolumna(pola, idx, "Data Zajęć", "Date", "Data"),
                    GodzinaOdRaw = GetKolumna(pola, idx, "Godz. od", "Time from", "Godz od"),
                    GodzinaDoRaw = GetKolumna(pola, idx, "Godz. do", "Time to", "Godz do"),
                    LiczbaGodzin = GetKolumna(pola, idx, "Liczba godzin", "Hours"),
                    Przedmiot    = GetKolumna(pola, idx, "Przedmiot", "Subject", "Zajęcia"),
                    Typ          = GetKolumna(pola, idx, "Typ", "Type"),
                    Prowadzacy   = GetKolumna(pola, idx, "Prowadzący", "Lecturer", "Prowadzacy"),
                    Sala         = GetKolumna(pola, idx, "Sala", "Room", "Sala zajęć"),
                    Grupa        = GetKolumna(pola, idx, "Grupa", "Group")
                };

                // Parsowanie godziny rozpoczęcia do TimeSpan
                if (!string.IsNullOrEmpty(row.GodzinaOdRaw) &&
                    TimeSpan.TryParse(row.GodzinaOdRaw, out TimeSpan godz))
                    row.GodzinaOd = godz;

                // Parsowanie godziny zakończenia do TimeSpan
                if (!string.IsNullOrEmpty(row.GodzinaDoRaw) &&
                    TimeSpan.TryParse(row.GodzinaDoRaw, out TimeSpan godzDo))
                    row.GodzinaDo = godzDo;

                wynik.Add(row);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlanZajec] Błąd parsowania wiersza CSV nr {li + 1}: {ex.Message}");
            }
        }

        Debug.Log($"[PlanZajec] Sparsowano {wynik.Count} wierszy z CSV.");
        return wynik;
    }

    // =========================================================================
    // METODY POMOCNICZE
    // =========================================================================

    /// <summary>
    /// Dzieli linię CSV po średniku, zachowując wartości w cudzysłowach.
    /// Obsługuje pola zawierające średniki wewnątrz cudzysłowów.
    /// </summary>
    /// <param name="linia">Pojedyncza linia pliku CSV.</param>
    /// <returns>Tablica wartości pól bez cudzysłowów.</returns>
    private string[] SplitCSVLinia(string linia)
    {
        // Prosta implementacja dla plików z separatorem ";"
        // bez zagnieżdżonych cudzysłowów zawierających ";"
        var pola = linia.Split(';');
        for (int i = 0; i < pola.Length; i++)
            pola[i] = pola[i].Trim().Trim('"').Trim();
        return pola;
    }

    /// <summary>
    /// Pobiera wartość kolumny z wiersza CSV na podstawie listy możliwych nazw kolumn.
    /// Iteruje przez podane nazwy i zwraca pierwszą znalezioną wartość.
    /// </summary>
    /// <param name="pola">Pola wiersza CSV po podziale.</param>
    /// <param name="idx">Słownik mapujący nazwy kolumn na indeksy.</param>
    /// <param name="nazwyKolumn">
    /// Lista alternatywnych nazw kolumny (np. polska i angielska wersja językowa).
    /// </param>
    /// <returns>
    /// Wartość kolumny jako string lub pusty string jeśli kolumna nie istnieje.
    /// </returns>
    private string GetKolumna(string[] pola, Dictionary<string, int> idx,
                               params string[] nazwyKolumn)
    {
        foreach (string nazwa in nazwyKolumn)
            if (idx.TryGetValue(nazwa, out int i) && i < pola.Length)
                return pola[i];
        return string.Empty;
    }

    /// <summary>
    /// Usuwa podstawowe tagi HTML i encje HTML z tekstu.
    /// Używane przy parsowaniu zawartości komórek tabeli.
    /// </summary>
    /// <param name="html">Fragment HTML do oczyszczenia.</param>
    /// <returns>Tekst pozbawiony tagów HTML i zdekodowanymi encjami.</returns>
    private string OczyyscHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Usuń tagi HTML
        string tekst = Regex.Replace(html, @"<[^>]+>", string.Empty);

        // Zdekoduj podstawowe encje HTML
        tekst = tekst
            .Replace("&amp;",  "&")
            .Replace("&lt;",   "<")
            .Replace("&gt;",   ">")
            .Replace("&nbsp;", " ")
            .Replace("&quot;", "\"")
            .Replace("&#39;",  "'");

        return tekst.Trim();
    }
}

// =============================================================================
// PRZYKŁAD UŻYCIA
// =============================================================================

/// <summary>
/// Przykładowy komponent demonstrujący typowy przepływ pracy z PlanZajecManager.
///
/// <para>
/// Podłącz go do tego samego lub innego GameObject co PlanZajecManager
/// i przypisz referencję w Inspektorze.
/// </para>
///
/// <para>
/// Kolejność wywołań:
/// <list type="number">
///   <item>Start() → PobierzListeTokow() — ładuje katalog kierunków i toków</item>
///   <item>Po załadowaniu → GetKierunki() — lista kierunków do UI</item>
///   <item>Po wyborze kierunku → GetTokiDlaKierunku() — lista toków do UI</item>
///   <item>Po wyborze toku + daty + godziny → ZnajdzSale() — zwraca salę lub null</item>
/// </list>
/// </para>
/// </summary>
public class PrzykladUzycia : MonoBehaviour
{
    /// <summary>
    /// Referencja do managera planu zajęć. Przypisz w Inspektorze Unity.
    /// </summary>
    [SerializeField]
    [Tooltip("Referencja do komponentu PlanZajecManager w scenie.")]
    private PlanZajecManager planManager;

    // -------------------------------------------------------------------------

    private void Start()
    {
        if (planManager == null)
        {
            Debug.LogError("[PrzykladUzycia] Brak referencji do PlanZajecManager!");
            return;
        }

        // KROK 1: Załaduj katalog toków przy starcie aplikacji.
        // Operacja sieciowa — wynik dostępny w callbacku.
        planManager.PobierzListeTokow(OnTokiZaladowane);
    }

    /// <summary>
    /// Callback wywoływany po załadowaniu listy toków.
    /// Tu można wypełnić UI (dropdown z kierunkami).
    /// </summary>
    private void OnTokiZaladowane(List<TokStudiow> toki)
    {
        Debug.Log($"[PrzykladUzycia] Dostępne toki ({toki.Count}):");
        foreach (var tok in toki)
            Debug.Log($"  {tok}");

        // KROK 2: Pobierz listę kierunków do np. dropdownu
        List<string> kierunki = planManager.GetKierunki();
        Debug.Log($"[PrzykladUzycia] Kierunki ({kierunki.Count}): " +
                  string.Join(", ", kierunki));

        // KROK 3: Sprawdź salę dla konkretnego zapytania.
        // Zastąp parametry wartościami z UI.
        SprawdzSaleDemo();
    }

    /// <summary>
    /// Demonstracja zapytania o salę — zastąp wartości hard-coded danymi z UI.
    /// </summary>
    private void SprawdzSaleDemo()
    {
        // --- PARAMETRY ZAPYTANIA --- (w prawdziwej aplikacji: z dropdownów/pól UI)
        string kierunek        = "Informatyka";
        string fragmentToku    = "Sztuczna Inteligencja S mgr";
        DateTime data          = new DateTime(2026, 3, 10);   // wtorek
        TimeSpan godzinaRozp   = new TimeSpan(10, 0, 0);      // 10:00
        // ---

        planManager.ZnajdzSale(
            kierunek:              kierunek,
            nazwaLubFragmentToku:  fragmentToku,
            data:                  data,
            godzinaOd:             godzinaRozp,
            onResult: sala =>
            {
                if (sala != null)
                    Debug.Log($"[PrzykladUzycia] ✓ Sala: {sala}");
                else
                    Debug.Log("[PrzykladUzycia] ✗ Brak zajęć o podanej godzinie (null).");
            }
        );

        // Przykład pobierania pełnego planu tygodniowego
        planManager.PobierzPlanToku(
            kierunek:              "Nawigacja",
            nazwaLubFragmentToku:  "Transport Morski",
            dataOd:                new DateTime(2026, 3, 2),
            dataDo:                new DateTime(2026, 3, 8),
            onResult: zajecia =>
            {
                if (zajecia == null) { Debug.LogWarning("Nie udało się pobrać planu."); return; }
                Debug.Log($"[PrzykladUzycia] Plan tygodniowy ({zajecia.Count} zajęć):");
                foreach (var z in zajecia)
                    Debug.Log($"  {z}");
            }
        );
    }
}
