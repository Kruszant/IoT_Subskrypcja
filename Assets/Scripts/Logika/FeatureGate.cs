using System;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Wspólna logika bramki funkcji: funkcja jest włączona tylko wtedy,
    /// gdy flaga subskrypcji ma wartość true. Domyślnie zablokowana.
    /// </summary>
    public class FeatureGate
    {
        public FeatureKind Funkcja { get; private set; }
        public bool Odblokowana { get; private set; }

        /// <summary>Wywoływane tylko przy zmianie (argument: czy odblokowana).</summary>
        public event Action<bool> Zmieniona;

        public FeatureGate(FeatureKind funkcja)
        {
            Funkcja = funkcja;
            Odblokowana = false;
        }

        /// <summary>Aktualizuje bramkę na podstawie stanu z chmury. Zwraca true przy zmianie.</summary>
        public bool Aktualizuj(SubscriptionState stan)
        {
            bool nowa = stan != null && stan.CzyOdblokowana(Funkcja);
            if (nowa == Odblokowana) return false;
            Odblokowana = nowa;
            if (Zmieniona != null) Zmieniona(nowa);
            return true;
        }
    }
}
