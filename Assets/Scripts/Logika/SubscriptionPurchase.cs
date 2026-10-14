namespace IoT.Subskrypcja
{
    /// <summary>
    /// Czysta logika zakupu / anulowania subskrypcji z poziomu symulacji:
    /// treść zapytania PATCH do Firebase i czytelny opis błędu zapisu.
    /// </summary>
    public static class SubscriptionPurchase
    {
        /// <summary>
        /// Treść PATCH zmieniająca tylko jedną flagę, np. {"rear_camera":true}.
        /// PATCH (a nie PUT) nie nadpisuje drugiej flagi.
        /// </summary>
        public static string ZbudujTresc(FeatureKind funkcja, bool aktywna)
        {
            return "{\"" + funkcja.KluczJson() + "\":" + SubscriptionState.Bool(aktywna) + "}";
        }

        /// <summary>Opis błędu zapisu dla HUD i konsoli.</summary>
        public static string OpisBledu(long kodHttp, string bladSieci)
        {
            if (kodHttp == 401 || kodHttp == 403)
                return "Brak uprawnień do zapisu (HTTP " + kodHttp + ") - opublikuj reguły z Firebase/database.rules.json";
            if (kodHttp >= 400)
                return "Chmura odrzuciła zapis (HTTP " + kodHttp + ")";
            return "Brak połączenia z chmurą" + (string.IsNullOrEmpty(bladSieci) ? "" : ": " + bladSieci);
        }
    }
}
