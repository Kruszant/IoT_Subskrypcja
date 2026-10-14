using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Klasa bazowa systemu pojazdu objętego subskrypcją. Nasłuchuje klienta chmury,
    /// przepuszcza stan przez <see cref="FeatureGate"/> i wywołuje
    /// <see cref="PoZmianieDostepu"/> przy odblokowaniu / zablokowaniu.
    /// </summary>
    public abstract class SubscriptionFeature : MonoBehaviour
    {
        [Tooltip("Klient chmury (obiekt IoT Edge)")]
        public CloudSubscriptionClient cloudClient;

        protected FeatureGate Bramka { get; private set; }

        public bool Odblokowana
        {
            get { return Bramka != null && Bramka.Odblokowana; }
        }

        protected abstract FeatureKind Funkcja { get; }

        protected virtual void Awake()
        {
            Bramka = new FeatureGate(Funkcja);
        }

        protected virtual void OnEnable()
        {
            if (cloudClient != null)
            {
                cloudClient.OnStateChanged += NaZmianeStanu;
                Bramka.Aktualizuj(cloudClient.AktualnyStan);
            }
            else
            {
                Debug.LogWarning("[" + name + "] Brak referencji do CloudSubscriptionClient - funkcja zostaje zablokowana.");
            }
        }

        protected virtual void Start()
        {
            // Wymuszenie stanu początkowego (zablokowany albo ostatni znany).
            PoZmianieDostepu(Odblokowana);
        }

        protected virtual void OnDisable()
        {
            if (cloudClient != null) cloudClient.OnStateChanged -= NaZmianeStanu;
        }

        private void NaZmianeStanu(SubscriptionState stan)
        {
            if (Bramka.Aktualizuj(stan))
            {
                Debug.Log("[" + Funkcja.NazwaPolska() + "] " + (Bramka.Odblokowana ? "ODBLOKOWANA" : "ZABLOKOWANA") + " (subskrypcja)");
                PoZmianieDostepu(Bramka.Odblokowana);
            }
        }

        /// <summary>Wywoływane przy starcie i przy każdej zmianie dostępu.</summary>
        protected abstract void PoZmianieDostepu(bool odblokowana);
    }
}
