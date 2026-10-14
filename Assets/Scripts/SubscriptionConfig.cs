using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Konfiguracja połączenia z chmurą (Assets/Config/SubscriptionConfig.asset).
    /// Po utworzeniu bazy w Firebase wklej jej adres w polu databaseUrl.
    /// </summary>
    [CreateAssetMenu(fileName = "SubscriptionConfig", menuName = "IoT/Konfiguracja subskrypcji")]
    public class SubscriptionConfig : ScriptableObject
    {
        public const string AdresWzorcowy = "https://TWOJ-PROJEKT-default-rtdb.firebaseio.com";

        [Tooltip("Adres Realtime Database, np. https://xxx-default-rtdb.europe-west1.firebasedatabase.app")]
        public string databaseUrl = AdresWzorcowy;

        [Tooltip("Identyfikator pojazdu (węzeł vehicles/<id> w bazie)")]
        public string vehicleId = "car01";

        [Tooltip("Co ile sekund odpytywać chmurę")]
        [Min(0.2f)]
        public float pollIntervalSeconds = 2f;

        [Tooltip("Limit czasu pojedynczego zapytania HTTP (s)")]
        [Min(1f)]
        public float requestTimeoutSeconds = 5f;
    }
}
