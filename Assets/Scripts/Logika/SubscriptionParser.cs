using System;
using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Parsowanie odpowiedzi REST z Firebase do <see cref="SubscriptionState"/>.
    /// Czysta klasa C# (bez MonoBehaviour), testowana w EditMode.
    /// </summary>
    public static class SubscriptionParser
    {
        /// <summary>
        /// Próbuje sparsować JSON. Zwraca false, gdy odpowiedź jest pusta, równa "null"
        /// (węzeł nie istnieje w bazie) albo nie jest poprawnym obiektem JSON.
        /// Brakujące pole jest traktowane jako false (funkcja zablokowana).
        /// </summary>
        public static bool TryParse(string json, out SubscriptionState stan, out string blad)
        {
            stan = null;
            blad = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                blad = "Pusta odpowiedź serwera";
                return false;
            }

            string tekst = json.Trim();
            if (tekst == "null")
            {
                blad = "Brak danych w bazie (odpowiedź null) - sprawdź ścieżkę i import JSON";
                return false;
            }

            if (!tekst.StartsWith("{", StringComparison.Ordinal) || !tekst.EndsWith("}", StringComparison.Ordinal))
            {
                blad = "Odpowiedź nie jest obiektem JSON";
                return false;
            }

            try
            {
                SubscriptionState wynik = JsonUtility.FromJson<SubscriptionState>(tekst);
                if (wynik == null)
                {
                    blad = "Nie udało się odczytać obiektu JSON";
                    return false;
                }
                stan = wynik;
                return true;
            }
            catch (ArgumentException e)
            {
                blad = "Błędny JSON: " + e.Message;
                return false;
            }
        }
    }
}
