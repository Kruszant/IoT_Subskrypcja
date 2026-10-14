using UnityEngine;
using UnityEngine.UI;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Sklep subskrypcji w symulacji (klawisz B). Klawisze 1 i 2 kupują albo anulują
    /// subskrypcję. Zakup jest symulowany (bez płatności): zapisuje flagę w Firebase,
    /// a odblokowanie wraca do auta z chmury, tak jak przy zmianie w konsoli Firebase.
    /// </summary>
    public class SubscriptionShop : MonoBehaviour
    {
        public CloudSubscriptionClient cloudClient;

        [Header("UI")]
        public GameObject panel;
        public Text tekstOferty;
        public Text tekstStatusu;

        [Header("Cennik (tylko do wyświetlenia)")]
        public string cenaKamery = "49 zł / mies.";
        public string cenaCzujnika = "29 zł / mies.";

        private bool _wTrakcie;
        private string _status = "";
        private Color _kolorStatusu = Color.white;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (DriverInput.KlawiszSklepu() && panel != null)
            {
                panel.SetActive(!panel.activeSelf);
                if (panel.activeSelf && !_wTrakcie) UstawStatus("Wybierz 1 albo 2. B zamyka sklep.", Color.white);
            }

            if (panel == null || !panel.activeSelf) return;

            if (DriverInput.KlawiszOpcji1()) Przelacz(FeatureKind.KameraCofania);
            else if (DriverInput.KlawiszOpcji2()) Przelacz(FeatureKind.CzujnikParkowania);

            Odswiez();
        }

        private void Przelacz(FeatureKind funkcja)
        {
            if (cloudClient == null || _wTrakcie) return;

            bool aktywna = cloudClient.AktualnyStan.CzyOdblokowana(funkcja);
            bool cel = !aktywna;
            _wTrakcie = true;
            UstawStatus((cel ? "Kupowanie: " : "Anulowanie: ") + funkcja.NazwaPolska() + "...", Color.yellow);

            cloudClient.ZmienSubskrypcje(funkcja, cel, (sukces, blad) =>
            {
                _wTrakcie = false;
                if (sukces)
                    UstawStatus(cel ? "Zakup przyjęty przez chmurę: " + funkcja.NazwaPolska()
                                    : "Subskrypcja anulowana: " + funkcja.NazwaPolska(),
                        new Color(0.4f, 1f, 0.5f));
                else
                    UstawStatus("Błąd: " + blad, new Color(1f, 0.45f, 0.35f));
            });
        }

        private void UstawStatus(string tekst, Color kolor)
        {
            _status = tekst;
            _kolorStatusu = kolor;
        }

        private void Odswiez()
        {
            SubscriptionState stan = cloudClient != null ? cloudClient.AktualnyStan : SubscriptionState.WszystkoZablokowane();
            if (tekstOferty != null)
            {
                tekstOferty.text =
                    Pozycja("1", FeatureKind.KameraCofania, cenaKamery, stan.rear_camera) + "\n\n" +
                    Pozycja("2", FeatureKind.CzujnikParkowania, cenaCzujnika, stan.parking_sensor) +
                    "\n\nZakup symulowany, bez płatności.";
            }
            if (tekstStatusu != null)
            {
                bool offline = cloudClient == null || !cloudClient.MozeZapisywac;
                tekstStatusu.text = offline ? "Tryb offline: sklep wymaga połączenia z Firebase" : _status;
                tekstStatusu.color = offline ? new Color(1f, 0.45f, 0.35f) : _kolorStatusu;
            }
        }

        private static string Pozycja(string klawisz, FeatureKind funkcja, string cena, bool aktywna)
        {
            return "[" + klawisz + "] " + funkcja.NazwaPolska() + "  " + cena + "\n     " +
                   (aktywna ? "AKTYWNA  (" + klawisz + " = anuluj)" : "nieaktywna  (" + klawisz + " = kup)");
        }
    }
}
