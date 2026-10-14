using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IoT.Subskrypcja
{
    public enum CloudStatus
    {
        /// <summary>Brak skonfigurowanego adresu bazy - praca bez chmury.</summary>
        Offline,
        /// <summary>Wysłano pierwsze zapytanie, czekamy na odpowiedź.</summary>
        Laczenie,
        Polaczono,
        Blad
    }

    /// <summary>
    /// "IoT Edge": imitacja mikrokontrolera pokładowego, który cyklicznie i asynchronicznie
    /// odpytuje chmurę (Firebase REST API) o stan subskrypcji pojazdu.
    /// GET {databaseUrl}/vehicles/{vehicleId}/subscriptions.json
    /// </summary>
    public class CloudSubscriptionClient : MonoBehaviour
    {
        [Tooltip("Konfiguracja połączenia (Assets/Config/SubscriptionConfig.asset)")]
        public SubscriptionConfig config;

        /// <summary>Publikowane tylko przy ZMIANIE stanu subskrypcji.</summary>
        public event Action<SubscriptionState> OnStateChanged;

        public CloudStatus Status { get; private set; }

        /// <summary>Ostatni komunikat błędu (lub opis trybu offline).</summary>
        public string OstatniBlad { get; private set; }

        /// <summary>Time.realtimeSinceStartup ostatniej poprawnej odpowiedzi; ujemny = nigdy.</summary>
        public float CzasOstatniejOdpowiedzi { get; private set; }

        /// <summary>Lokalna godzina ostatniej poprawnej odpowiedzi (do HUD).</summary>
        public DateTime GodzinaOstatniejOdpowiedzi { get; private set; }

        /// <summary>Kopia ostatniego znanego stanu (domyślnie wszystko zablokowane).</summary>
        public SubscriptionState AktualnyStan
        {
            get { return _tracker.Aktualny; }
        }

        private readonly SubscriptionStateTracker _tracker = new SubscriptionStateTracker();
        private Coroutine _petla;
        private int _bledyPodRzad;
        private bool _zalogowanoOffline;

        private void Awake()
        {
            Status = CloudStatus.Offline;
            CzasOstatniejOdpowiedzi = -1f;
            _tracker.StanZmieniony += PrzekazZmiane;
        }

        private void OnEnable()
        {
            _petla = StartCoroutine(PetlaOdpytywania());
        }

        private void OnDisable()
        {
            if (_petla != null)
            {
                StopCoroutine(_petla);
                _petla = null;
            }
        }

        private void OnDestroy()
        {
            _tracker.StanZmieniony -= PrzekazZmiane;
        }

        private void PrzekazZmiane(SubscriptionState nowy)
        {
            if (OnStateChanged != null) OnStateChanged(nowy);
        }

        private IEnumerator PetlaOdpytywania()
        {
            while (true)
            {
                if (config == null)
                {
                    UstawOffline("Brak przypisanego SubscriptionConfig w komponencie IoT Edge");
                }
                else if (!SubscriptionUrlBuilder.CzySkonfigurowany(config.databaseUrl))
                {
                    UstawOffline("Nie ustawiono adresu bazy - uzupełnij databaseUrl w Assets/Config/SubscriptionConfig");
                }
                else
                {
                    _zalogowanoOffline = false;
                    yield return WykonajZapytanie();
                }

                float odstep = config != null ? Mathf.Max(0.2f, config.pollIntervalSeconds) : 2f;
                yield return new WaitForSecondsRealtime(odstep);
            }
        }

        /// <summary>True, gdy adres bazy jest uzupełniony i można zapisywać do chmury.</summary>
        public bool MozeZapisywac
        {
            get { return config != null && SubscriptionUrlBuilder.CzySkonfigurowany(config.databaseUrl); }
        }

        /// <summary>
        /// Symulowany zakup (aktywna = true) albo anulowanie (false) subskrypcji.
        /// Wysyła PATCH do Firebase; samo odblokowanie funkcji przychodzi potem z chmury
        /// tą samą drogą co zmiana wykonana w konsoli Firebase.
        /// Callback: (sukces, opis błędu).
        /// </summary>
        public void ZmienSubskrypcje(FeatureKind funkcja, bool aktywna, Action<bool, string> poZakonczeniu)
        {
            if (!MozeZapisywac)
            {
                if (poZakonczeniu != null) poZakonczeniu(false, "Tryb offline - uzupełnij databaseUrl w SubscriptionConfig");
                return;
            }
            StartCoroutine(WyslijZmiane(funkcja, aktywna, poZakonczeniu));
        }

        private IEnumerator WyslijZmiane(FeatureKind funkcja, bool aktywna, Action<bool, string> poZakonczeniu)
        {
            string url = SubscriptionUrlBuilder.ZbudujUrl(config.databaseUrl, config.vehicleId);
            string tresc = SubscriptionPurchase.ZbudujTresc(funkcja, aktywna);
            Debug.Log("[CLOUD] " + (aktywna ? "Zakup" : "Anulowanie") + " subskrypcji: PATCH " + url + " " + tresc);

            using (var zapytanie = new UnityWebRequest(url, "PATCH"))
            {
                zapytanie.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(tresc));
                zapytanie.downloadHandler = new DownloadHandlerBuffer();
                zapytanie.SetRequestHeader("Content-Type", "application/json");
                zapytanie.timeout = Mathf.Max(1, Mathf.CeilToInt(config.requestTimeoutSeconds));
                yield return zapytanie.SendWebRequest();

                if (zapytanie.result != UnityWebRequest.Result.Success)
                {
                    string opis = SubscriptionPurchase.OpisBledu(zapytanie.responseCode, zapytanie.error);
                    Debug.LogWarning("[CLOUD] Zapis nieudany: " + opis);
                    if (poZakonczeniu != null) poZakonczeniu(false, opis);
                    yield break;
                }
            }

            Debug.Log("[CLOUD] Chmura przyjęła zmianę " + funkcja.KluczJson() + " = " + SubscriptionState.Bool(aktywna));
            if (poZakonczeniu != null) poZakonczeniu(true, null);
            // Od razu odpytaj chmurę, zamiast czekać na następny cykl.
            yield return WykonajZapytanie();
        }

        private void UstawOffline(string powod)
        {
            Status = CloudStatus.Offline;
            OstatniBlad = powod;
            if (!_zalogowanoOffline)
            {
                Debug.LogWarning("[CLOUD] TRYB OFFLINE: " + powod + ". Wszystkie funkcje pozostają zablokowane.");
                _zalogowanoOffline = true;
            }
        }

        private IEnumerator WykonajZapytanie()
        {
            string url = SubscriptionUrlBuilder.ZbudujUrl(config.databaseUrl, config.vehicleId);
            if (Status == CloudStatus.Offline) Status = CloudStatus.Laczenie;

            using (UnityWebRequest zapytanie = UnityWebRequest.Get(url))
            {
                zapytanie.timeout = Mathf.Max(1, Mathf.CeilToInt(config.requestTimeoutSeconds));
                yield return zapytanie.SendWebRequest();

                if (zapytanie.result != UnityWebRequest.Result.Success)
                {
                    ZglosBlad("Błąd sieci (" + zapytanie.result + ", HTTP " + zapytanie.responseCode + "): " + zapytanie.error);
                    yield break;
                }

                SubscriptionState stan;
                string blad;
                if (!SubscriptionParser.TryParse(zapytanie.downloadHandler.text, out stan, out blad))
                {
                    ZglosBlad(blad);
                    yield break;
                }

                if (Status != CloudStatus.Polaczono)
                    Debug.Log("[CLOUD] Połączono z chmurą: " + url);

                Status = CloudStatus.Polaczono;
                OstatniBlad = null;
                _bledyPodRzad = 0;
                CzasOstatniejOdpowiedzi = Time.realtimeSinceStartup;
                GodzinaOstatniejOdpowiedzi = DateTime.Now;

                List<string> zmiany = _tracker.Zastosuj(stan);
                for (int i = 0; i < zmiany.Count; i++) Debug.Log(zmiany[i]);
            }
        }

        /// <summary>
        /// Błąd sieci / timeout / null: zachowujemy ostatni znany stan i próbujemy dalej.
        /// Ostrzeżenie logujemy przy pierwszym błędzie i potem co 10. próbę (bez spamu).
        /// </summary>
        private void ZglosBlad(string opis)
        {
            Status = CloudStatus.Blad;
            OstatniBlad = opis;
            _bledyPodRzad++;
            if (_bledyPodRzad == 1 || _bledyPodRzad % 10 == 0)
            {
                Debug.LogWarning("[CLOUD] " + opis + " | próba " + _bledyPodRzad +
                                 ", zachowuję ostatni znany stan (" + _tracker.Aktualny + ") i ponawiam.");
            }
        }
    }
}
