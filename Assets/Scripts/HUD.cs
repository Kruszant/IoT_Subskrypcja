using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Interfejs kierowcy: prędkość, bieg, status chmury, kontrolki subskrypcji,
    /// komunikaty (toast) przy aktywacji oraz pasek odległości PDC.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Źródła danych")]
        public CloudSubscriptionClient cloudClient;
        public CarController auto;
        public ParkingSensorSystem czujnikParkowania;

        [Header("Teksty")]
        public Text tekstPredkosci;
        public Text tekstBiegu;
        public Text tekstChmury;

        [Header("Kontrolki subskrypcji")]
        public Image kontrolkaKamery;
        public Text tekstKamery;
        public Image kontrolkaCzujnika;
        public Text tekstCzujnika;

        [Header("Toast")]
        public CanvasGroup toast;
        public Text tekstToastu;
        public float czasToastuS = 3.5f;

        [Header("Pasek PDC")]
        public GameObject panelPdc;
        public RectTransform wypelnieniePdc;
        public Image obrazWypelnieniaPdc;
        public Text tekstPdc;

        private static readonly Color Czerwony = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color Zielony = new Color(0.15f, 0.75f, 0.25f);

        private readonly Queue<string> _kolejkaToastow = new Queue<string>();
        private float _toastDo;
        private SubscriptionState _poprzedni = SubscriptionState.WszystkoZablokowane();

        private void OnEnable()
        {
            if (cloudClient != null) cloudClient.OnStateChanged += NaZmianeSubskrypcji;
        }

        private void OnDisable()
        {
            if (cloudClient != null) cloudClient.OnStateChanged -= NaZmianeSubskrypcji;
        }

        private void Start()
        {
            if (toast != null) toast.alpha = 0f;
        }

        private void NaZmianeSubskrypcji(SubscriptionState nowy)
        {
            DodajToastJesliZmiana(FeatureKind.KameraCofania, _poprzedni, nowy);
            DodajToastJesliZmiana(FeatureKind.CzujnikParkowania, _poprzedni, nowy);
            _poprzedni = nowy.Kopia();
        }

        private void DodajToastJesliZmiana(FeatureKind f, SubscriptionState przed, SubscriptionState po)
        {
            bool a = przed.CzyOdblokowana(f);
            bool b = po.CzyOdblokowana(f);
            if (a == b) return;
            _kolejkaToastow.Enqueue(b
                ? "Subskrypcja aktywowana: " + f.NazwaPolska()
                : "Subskrypcja wygasła: " + f.NazwaPolska());
        }

        private void Update()
        {
            OdswiezJazde();
            OdswiezChmure();
            OdswiezKontrolki();
            OdswiezToast();
            OdswiezPdc();
        }

        private void OdswiezJazde()
        {
            if (auto == null) return;
            if (tekstPredkosci != null) tekstPredkosci.text = Mathf.RoundToInt(auto.PredkoscKmh) + " km/h";
            if (tekstBiegu != null)
            {
                tekstBiegu.text = auto.IsReversing ? "R" : "D";
                tekstBiegu.color = auto.IsReversing ? Color.white : new Color(0.6f, 0.9f, 1f);
            }
        }

        private void OdswiezChmure()
        {
            if (tekstChmury == null) return;
            if (cloudClient == null)
            {
                tekstChmury.text = "Chmura: brak klienta IoT Edge";
                tekstChmury.color = Czerwony;
                return;
            }

            string ostatnia = cloudClient.CzasOstatniejOdpowiedzi < 0f
                ? "brak"
                : cloudClient.GodzinaOstatniejOdpowiedzi.ToString("HH:mm:ss") + " (" +
                  Mathf.FloorToInt(Time.realtimeSinceStartup - cloudClient.CzasOstatniejOdpowiedzi) + " s temu)";

            switch (cloudClient.Status)
            {
                case CloudStatus.Polaczono:
                    tekstChmury.text = "Chmura: POŁĄCZONO\nOstatnia aktualizacja: " + ostatnia;
                    tekstChmury.color = Zielony;
                    break;
                case CloudStatus.Laczenie:
                    tekstChmury.text = "Chmura: łączenie...";
                    tekstChmury.color = Color.yellow;
                    break;
                case CloudStatus.Blad:
                    tekstChmury.text = "Chmura: BŁĄD (" + cloudClient.OstatniBlad + ")\nOstatnia aktualizacja: " + ostatnia +
                                       "\nZachowano ostatni znany stan";
                    tekstChmury.color = new Color(1f, 0.55f, 0.1f);
                    break;
                default:
                    tekstChmury.text = "TRYB OFFLINE: " + cloudClient.OstatniBlad + "\nWszystkie funkcje zablokowane";
                    tekstChmury.color = Czerwony;
                    break;
            }
        }

        private void OdswiezKontrolki()
        {
            SubscriptionState stan = cloudClient != null ? cloudClient.AktualnyStan : SubscriptionState.WszystkoZablokowane();
            UstawKontrolke(kontrolkaKamery, tekstKamery, "Kamera cofania", stan.rear_camera, "ZABLOKOWANA", "AKTYWNA");
            UstawKontrolke(kontrolkaCzujnika, tekstCzujnika, "Czujnik parkowania", stan.parking_sensor, "ZABLOKOWANY", "AKTYWNY");
        }

        private static void UstawKontrolke(Image lampka, Text tekst, string nazwa, bool aktywna, string zablokowana, string aktywnaTekst)
        {
            if (lampka != null) lampka.color = aktywna ? Zielony : Czerwony;
            if (tekst != null) tekst.text = nazwa + ": " + (aktywna ? aktywnaTekst : zablokowana);
        }

        private void OdswiezToast()
        {
            if (toast == null) return;
            if (Time.unscaledTime >= _toastDo && _kolejkaToastow.Count > 0)
            {
                if (tekstToastu != null) tekstToastu.text = _kolejkaToastow.Dequeue();
                _toastDo = Time.unscaledTime + czasToastuS;
            }
            float pozostalo = _toastDo - Time.unscaledTime;
            // Pełna widoczność, a w ostatnich 0,5 s płynne wygaszanie.
            toast.alpha = pozostalo <= 0f ? 0f : Mathf.Clamp01(pozostalo / 0.5f);
        }

        private void OdswiezPdc()
        {
            if (panelPdc == null) return;
            bool pokaz = czujnikParkowania != null && czujnikParkowania.Odblokowana;
            if (panelPdc.activeSelf != pokaz) panelPdc.SetActive(pokaz);
            if (!pokaz) return;

            if (!czujnikParkowania.Aktywny)
            {
                UstawPasek(0f, Zielony);
                if (tekstPdc != null) tekstPdc.text = "PDC: gotowy (włącz wsteczny)";
                return;
            }

            float min = czujnikParkowania.MinimalnaOdleglosc;
            float zasieg = Mathf.Max(0.01f, czujnikParkowania.zasiegM);
            // Im bliżej przeszkody, tym dłuższy pasek.
            UstawPasek(1f - Mathf.Clamp01(min / zasieg), ParkingSensorSystem.KolorDlaOdleglosci(min));
            if (tekstPdc != null)
                tekstPdc.text = "PDC: " + min.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                                " m  " + czujnikParkowania.Strefa;
        }

        private void UstawPasek(float czesc, Color kolor)
        {
            if (wypelnieniePdc != null) wypelnieniePdc.anchorMax = new Vector2(Mathf.Clamp01(czesc), 1f);
            if (obrazWypelnieniaPdc != null) obrazWypelnieniaPdc.color = kolor;
        }
    }
}
