using System;

namespace IoT.Subskrypcja
{
    /// <summary>Budowanie adresu zapytania REST do Firebase Realtime Database.</summary>
    public static class SubscriptionUrlBuilder
    {
        /// <summary>Fragment adresu-wzorca; jego obecność oznacza, że URL nie został uzupełniony.</summary>
        public const string ZnacznikPlaceholdera = "TWOJ-PROJEKT";

        /// <summary>
        /// Zwraca true, gdy adres bazy wygląda na prawdziwy (https, bez placeholdera).
        /// </summary>
        public static bool CzySkonfigurowany(string databaseUrl)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl)) return false;
            string url = databaseUrl.Trim();
            if (url.IndexOf(ZnacznikPlaceholdera, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
            return url.Length > "https://".Length;
        }

        /// <summary>
        /// {databaseUrl}/vehicles/{vehicleId}/subscriptions.json
        /// Usuwa końcowe ukośniki i ewentualny ".json" z adresu bazy, koduje vehicleId.
        /// </summary>
        public static string ZbudujUrl(string databaseUrl, string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl))
                throw new ArgumentException("Adres bazy nie może być pusty", "databaseUrl");
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Identyfikator pojazdu nie może być pusty", "vehicleId");

            string baza = databaseUrl.Trim();
            if (baza.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                baza = baza.Substring(0, baza.Length - ".json".Length);
            baza = baza.TrimEnd('/');

            return baza + "/vehicles/" + Uri.EscapeDataString(vehicleId.Trim()) + "/subscriptions.json";
        }
    }
}
