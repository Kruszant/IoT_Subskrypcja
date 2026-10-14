# Symulacja modelu subskrypcyjnego czujników automotive (IoT)

Projekt zaliczeniowy z przedmiotu *Internet Rzeczy i systemy automotive*.
Autor: Alan Mniedlo, Teleinformatyka, ZUT w Szczecinie.

## 1. Cel

Sterowalny model samochodu 3D, którego systemy wspomagania (kamera cofania i czujnik parkowania)
są domyślnie **zablokowane**. Odblokowują się w czasie rzeczywistym, bez restartu aplikacji,
gdy w Firebase Realtime Database flaga subskrypcji zmieni się z `false` na `true`.
Symulacja odwzorowuje model biznesowy "funkcji na żądanie" (Features on Demand), w którym
sprzęt jest fabrycznie zamontowany w aucie, a dostęp do niego włącza się zdalnie z chmury.

## 2. Stos technologiczny

| Warstwa | Technologia |
|---|---|
| Urządzenie końcowe (IoT Edge) | Unity 2022.3 LTS lub Unity 6, C#, Built-in Render Pipeline |
| Chmura | Firebase Realtime Database (dane w formacie JSON) |
| Komunikacja | REST API Firebase przez `UnityWebRequest` (HTTPS GET), bez Firebase SDK i bez zewnętrznych paczek |
| UI | Unity UI (uGUI), zwykły `UI.Text` (bez TextMeshPro, więc nic nie trzeba importować ręcznie) |
| Testy | Unity Test Framework (NUnit), testy EditMode |

## 3. Architektura

```
 +------------------------------ Unity (IoT Edge) ------------------------------+
 |                                                                              |
 |  SubscriptionConfig (.asset)                                                 |
 |        |  databaseUrl, vehicleId, pollIntervalSeconds, requestTimeoutSeconds |
 |        v                                                                     |
 |  CloudSubscriptionClient ("IoT Edge")                                        |
 |    korutyna: co 2 s GET .../vehicles/car01/subscriptions.json                |
 |    SubscriptionParser -> SubscriptionStateTracker -> OnStateChanged          |
 |        |                                   |                    |            |
 |        v                                   v                    v            |
 |  RearCameraSystem                  ParkingSensorSystem         HUD           |
 |  (FeatureGate: rear_camera)        (FeatureGate: parking_      (kontrolki,   |
 |  kamera -> RenderTexture -> PiP     sensor) 4x Raycast ->      toast, pasek  |
 |                                     Debug.Log "[PDC] ..."      PDC, status)  |
 |        ^                                   ^                                 |
 |        +----------- CarController (IsReversing, prędkość) ------------------+ |
 +------------------------------------|-----------------------------------------+
                                      |  HTTPS / REST (JSON), polling
                                      v
 +----------------------- Firebase Realtime Database ---------------------------+
 |  vehicles/                                                                   |
 |    car01/                                                                    |
 |      subscriptions/                                                          |
 |        rear_camera:    false | true                                          |
 |        parking_sensor: false | true                                          |
 +------------------------------------------------------------------------------+
            ^
            |  ręczna zmiana flag w konsoli Firebase (przeglądarka)
        Operator / "sklep z subskrypcjami"
```

Unity działa jak mikrokontroler pokładowy: nie dostaje powiadomień push, tylko cyklicznie
i asynchronicznie odpytuje chmurę (polling). Przy braku sieci zachowuje ostatni znany stan
(na starcie: wszystko zablokowane) i próbuje dalej.

## 4. Struktura projektu

```
Assets/
  Scripts/                         (asmdef: IoT.Subskrypcja)
    Logika/                        czyste klasy C#, bez MonoBehaviour (testowalne)
      SubscriptionState.cs
      SubscriptionParser.cs
      SubscriptionUrlBuilder.cs
      SubscriptionStateTracker.cs
      FeatureGate.cs
      ParkingSensorLogic.cs
      ReverseDetector.cs
      SubscriptionPurchase.cs
    SubscriptionConfig.cs
    CloudSubscriptionClient.cs
    SubscriptionFeature.cs
    DriverInput.cs
    CarController.cs
    RearCameraSystem.cs
    ParkingSensorSystem.cs
    HUD.cs
    CameraFollow.cs
    SubscriptionShop.cs
    IoT.Subskrypcja.asmdef
  Editor/
    SceneBuilder.cs                menu "IoT/Zbuduj scenę demo"
  Tests/EditMode/                  (asmdef: IoT.Subskrypcja.Tests.EditMode)
    SubscriptionParserTests.cs
    SubscriptionStateTrackerTests.cs
    ParkingSensorLogicTests.cs
    SubscriptionUrlBuilderTests.cs
    SubscriptionPurchaseTests.cs
  Config/SubscriptionConfig.asset  (tworzony przez SceneBuilder)
  Materials/                       (tworzone przez SceneBuilder)
  Scenes/Demo.unity                (tworzona przez SceneBuilder)
Firebase/
  firebase_import.json             dane startowe do importu
  database.rules.json              reguły testowe
Docs/Zrzuty/                       zrzuty ekranu do dokumentacji
```

## 5. Opis skryptów

### Logika (czyste klasy C#)

- **SubscriptionState**: klasa `[Serializable]` z polami `rear_camera` i `parking_sensor`
  (nazwy identyczne z kluczami JSON, bo `JsonUtility` mapuje po nazwie pola). Zawiera stan
  domyślny "wszystko zablokowane" i porównanie stanów. Plik zawiera też enum `FeatureKind`.
- **SubscriptionParser**: `TryParse(json, out stan, out blad)`. Odrzuca pustą odpowiedź,
  `null` (węzeł nie istnieje w bazie) i błędny JSON. Brakujące pole oznacza `false`.
- **SubscriptionUrlBuilder**: buduje `{databaseUrl}/vehicles/{vehicleId}/subscriptions.json`
  (usuwa końcowe `/`, koduje identyfikator) i sprawdza, czy adres nie jest placeholderem.
- **SubscriptionStateTracker**: pamięta ostatni znany stan, zwraca linie logu
  `[CLOUD] rear_camera: false -> true` i wywołuje zdarzenie tylko przy zmianie.
- **FeatureGate**: wspólna bramka włącz/wyłącz dla jednej funkcji na podstawie jej flagi.
- **ParkingSensorLogic**: strefy PDC (BEZPIECZNIE > 1.5 m, OSTRZEZENIE 0.5–1.5 m,
  STOP < 0.5 m) i format surowej linii logu, zawsze z kropką dziesiętną.
- **SubscriptionPurchase**: treść zapytania PATCH przy zakupie/anulowaniu (zmienia tylko
  jedną flagę, np. `{"rear_camera":true}`) i czytelny opis błędu zapisu (np. brak uprawnień).
- **ReverseDetector**: wykrywanie biegu wstecznego (prędkość wzdłużna ujemna albo
  trzymany S przy niskiej prędkości).

### Komponenty Unity

- **SubscriptionConfig** (ScriptableObject): adres bazy, `vehicleId = car01`,
  odpytywanie co 2 s, timeout 5 s.
- **CloudSubscriptionClient** (obiekt "IoT Edge"): korutyna z `UnityWebRequest.Get`,
  timeout, parsowanie, zdarzenie `OnStateChanged(SubscriptionState)`. Udostępnia
  `Status` (Offline / Łączenie / Połączono / Błąd), czas ostatniej odpowiedzi i ostatni błąd.
  Bez prawidłowego adresu działa w trybie offline. Ostrzeżenia o błędach sieci loguje
  przy pierwszym błędzie i co 10. próbę, żeby nie zaśmiecać konsoli.
- **SubscriptionFeature**: klasa bazowa systemów objętych subskrypcją. Nasłuchuje klienta
  chmury, przepuszcza stan przez `FeatureGate` i wywołuje `PoZmianieDostepu`.
- **DriverInput**: odczyt klawiatury. Gdy aktywny jest *Input Manager (Old)* albo *Both*,
  używa `Input.GetAxis`; gdy tylko nowy *Input System*, używa `Keyboard.current`.
  Wybór następuje automatycznie przy kompilacji, więc kod działa przy każdym ustawieniu.
- **CarController**: jazda arcade na `Rigidbody`. W/S gaz, hamulec i wsteczny, A/D skręt,
  Spacja hamulec ręczny. Koła toczą się, przednie skręcają. Właściwość `IsReversing`;
  tylne światła zmieniają się na białe przy cofaniu.
- **RearCameraSystem**: kamera na tylnym zderzaku (do tyłu, 22° w dół) renderuje do
  `RenderTexture`, a `RawImage` w prawym górnym rogu pokazuje obraz (Picture-in-Picture,
  z ramką, podpisem "KAMERA COFANIA" i liniami toru jazdy; obraz odbity lustrzanie jak
  w prawdziwym aucie). Zablokowana: `camera.enabled = false` i szary panel z kłódką
  "Funkcja wymaga subskrypcji". Pole *Tryb Podgladu*: `TylkoPodczasCofania` albo `Zawsze`.
- **ParkingSensorSystem**: 4 promienie `Physics.Raycast` z tylnego zderzaka (lewy,
  środek-lewy, środek-prawy, prawy, rozchylone o ±6° i ±20°), zasięg 3 m, maska warstw
  pomija auto (warstwa "Pojazd"). Odblokowany i podczas cofania: co 0,2 s linia
  `[PDC] t=12.40s dist_m=[1.23, 0.87, 0.91, 2.50] min=0.87 strefa=OSTRZEZENIE`, plus
  `Debug.DrawRay` i Gizmos w kolorze strefy. Zablokowany: jeden komunikat
  `[PDC] Czujnik parkowania zablokowany - brak subskrypcji` na każdą próbę cofania.
- **HUD**: prędkość, bieg D/R, status chmury z godziną ostatniej aktualizacji, kontrolki
  "Kamera cofania: ZABLOKOWANA/AKTYWNA" i "Czujnik parkowania: ZABLOKOWANY/AKTYWNY"
  (czerwona/zielona), toast "Subskrypcja aktywowana: ..." i pasek odległości PDC.
- **SubscriptionShop**: sklep subskrypcji w symulacji (klawisz **B**). Klawisz **1**
  kupuje/anuluje kamerę cofania, **2** czujnik parkowania. Zakup jest symulowany (bez
  płatności): `CloudSubscriptionClient.ZmienSubskrypcje` wysyła `PATCH` do
  `vehicles/car01/subscriptions.json`, a odblokowanie wraca do auta z chmury przy najbliższym
  odczycie, tą samą drogą co zmiana w konsoli Firebase. Chmura pozostaje jedynym źródłem
  prawdy o subskrypcji.
- **CameraFollow**: kamera główna płynnie podąża za autem z tyłu i z góry.
- **SceneBuilder** (edytor): buduje całą scenę z prymitywów: parking 60x60 m z liniami
  miejsc, ściana, słupki, pachołki, zaparkowane drugie auto (wszystko z colliderami),
  światło kierunkowe, auto z systemami, kamery i HUD. Tworzy materiały, konfigurację,
  warstwę "Pojazd", zapisuje `Assets/Scenes/Demo.unity` i dodaje ją do Build Settings.

## 6. Uruchomienie krok po kroku

### 6.1. Projekt Unity

1. W Unity Hub utwórz projekt z szablonu **3D (Built-In Render Pipeline)**
   (Unity 2022.3 LTS albo Unity 6). W Unity 6 nie wybieraj "Universal 3D", bo materiały
   sceny używają shadera *Standard* z Built-in RP.
2. Skopiuj do folderu projektu foldery `Assets/`, `Firebase/`, `Docs/` i plik `README.md`
   (scal z istniejącym `Assets/`).
3. Sprawdź w *Window > Package Manager*, że są zainstalowane **Test Framework** i
   **Unity UI** (w szablonie 3D są domyślnie).
4. Poczekaj na kompilację. Konsola (*Window > General > Console*) powinna być pusta.

### 6.2. Firebase

1. Wejdź na https://console.firebase.google.com i utwórz projekt (Analytics nie jest potrzebne).
2. *Build > Realtime Database > Create Database*, wybierz lokalizację (np. `europe-west1`)
   i **Start in test mode**.
3. Skopiuj adres bazy z góry zakładki *Data*, np.
   `https://xxx-default-rtdb.europe-west1.firebasedatabase.app`.
4. W zakładce *Data* kliknij **⋮ > Import JSON** i wskaż `Firebase/firebase_import.json`.
   W bazie pojawi się `vehicles/car01/subscriptions` z dwiema flagami `false`.
5. Zakładka *Rules*: wklej zawartość `Firebase/database.rules.json` i kliknij *Publish*.
   Reguły pozwalają każdemu **czytać** gałąź `vehicles` i **zapisywać** tylko flagi
   `rear_camera` / `parking_sensor` (wartości logiczne) w `subscriptions`, czego potrzebuje
   sklep w symulacji. Wszystko inne jest zablokowane. **To ustawienie tylko na potrzeby demo**:
   w prawdziwym systemie zakup przechodziłby przez serwer płatności, pojazd uwierzytelniałby się
   (np. tokenem), a zapis flag byłby dostępny tylko dla backendu.
   (Domyślne reguły "test mode" wygasają po 30 dniach, reguły z pliku nie wygasają.)
6. Szybki test w przeglądarce: otwórz
   `https://xxx-default-rtdb.europe-west1.firebasedatabase.app/vehicles/car01/subscriptions.json`.
   Powinno się pokazać `{"parking_sensor":false,"rear_camera":false}`.

### 6.3. Scena i konfiguracja

1. W Unity kliknij menu **IoT > Zbuduj scenę demo**. Powstanie i otworzy się
   `Assets/Scenes/Demo.unity`.
2. Kliknij **IoT > Pokaż konfigurację subskrypcji** (albo zaznacz
   `Assets/Config/SubscriptionConfig`) i w Inspectorze wklej adres bazy w **Database Url**.
3. Naciśnij **Play**.

Bez zmiany adresu projekt działa w **trybie offline**: wszystko zablokowane, w HUD napis
"TRYB OFFLINE: Nie ustawiono adresu bazy...", a w konsoli jedno ostrzeżenie `[CLOUD] TRYB OFFLINE`.

### 6.4. Sterowanie

| Klawisz | Działanie |
|---|---|
| W / strzałka w górę | gaz (podczas cofania: hamulec) |
| S / strzałka w dół | hamulec, a po zatrzymaniu bieg wsteczny |
| A / D | skręt |
| Spacja | hamulec ręczny |
| B | otwórz / zamknij sklep subskrypcji |
| 1 / 2 (przy otwartym sklepie) | kup albo anuluj kamerę cofania / czujnik parkowania |

Auto stoi przodem od ściany, więc przytrzymanie **S** od razu cofa w stronę pachołka,
słupków i ściany.

### 6.5. Testy

*Window > General > Test Runner > EditMode > Run All*. Testy obejmują parsowanie JSON
z Firebase (true/false, brak pola, null, błędny JSON), wykrywanie zmiany stanu, strefy PDC
i format linii logu, wykrywanie wstecznego oraz budowanie URL.

## 7. Scenariusz prezentacji

**Przygotowanie:** obie flagi w Firebase na `false`, okno Unity i konsola Firebase
(zakładka *Data*) obok siebie, widoczna konsola Unity.

1. **Jazda testowa bez czujników.** Naciśnij Play. HUD pokazuje "Chmura: POŁĄCZONO" oraz
   dwie czerwone kontrolki ZABLOKOWANA / ZABLOKOWANY, w prawym górnym rogu szary panel
   z kłódką. Przejedź się (W, A/D), potem cofnij (S) w stronę pachołka. Światła tylne
   świecą na biało, bieg zmienia się na R, ale nie ma obrazu z kamery, a w konsoli pojawia
   się tylko jeden komunikat `[PDC] Czujnik parkowania zablokowany - brak subskrypcji`.
2. **Zakup subskrypcji w chmurze.** Nie zatrzymując Play, w konsoli Firebase kliknij
   wartość `rear_camera` i zmień `false` na `true`, potem to samo dla `parking_sensor`.
3. **Natychmiastowe odblokowanie bez restartu.** W ciągu ok. 2 s w konsoli Unity pojawia się
   `[CLOUD] rear_camera: false -> true`, na HUD toast "Subskrypcja aktywowana: Kamera
   cofania", kontrolka zmienia się na zieloną AKTYWNA. Przy cofaniu w rogu widać obraz
   z kamery cofania z liniami toru, a po odblokowaniu czujnika konsola co 0,2 s wypisuje
   linie `[PDC] t=... dist_m=[...] min=... strefa=...`, pasek PDC rośnie i zmienia kolor
   przy zbliżaniu do przeszkody.
4. **Zakup z poziomu auta.** Ustaw flagi z powrotem na `false`. W symulacji naciśnij **B**,
   potem **1**: w konsoli Unity widać `[CLOUD] Zakup subskrypcji: PATCH ...`, w konsoli
   Firebase flaga sama zmienia się na `true`, a kamera odblokowuje się jak w punkcie 3.
   To pokazuje pełną pętlę: pojazd -> chmura -> pojazd.
5. *(Opcjonalnie)* Zmień flagę z powrotem na `false`: funkcja blokuje się, toast
   "Subskrypcja wygasła". Wyłącz Wi-Fi: HUD pokazuje BŁĄD, ale funkcje zachowują
   ostatni znany stan.

## 8. Zrzuty ekranu do dokumentacji

Zapisuj w `Docs/Zrzuty/`:

| Plik | Co ma być widać |
|---|---|
| `01_scena_edytor.png` | Okno Scene po zbudowaniu sceny: parking, auto, przeszkody, hierarchia obiektów |
| `02_menu_iot.png` | Rozwinięte menu **IoT > Zbuduj scenę demo** |
| `03_subscription_config.png` | Inspector z `SubscriptionConfig` i wklejonym adresem bazy |
| `04_firebase_import.png` | Konsola Firebase po imporcie JSON: `vehicles/car01/subscriptions` z flagami `false` |
| `05_firebase_rules.png` | Zakładka *Rules* z regułami z `database.rules.json` |
| `06_tryb_offline.png` | Gra bez adresu bazy: HUD "TRYB OFFLINE", ostrzeżenie w konsoli |
| `07_jazda_zablokowane.png` | Play, cofanie z zablokowanymi funkcjami: panel z kłódką, czerwone kontrolki, bieg R, białe światła |
| `08_konsola_pdc_zablokowany.png` | Konsola Unity z jednym komunikatem `[PDC] Czujnik parkowania zablokowany` |
| `09_firebase_zmiana_flag.png` | Konsola Firebase w trakcie zmiany `rear_camera` / `parking_sensor` na `true` |
| `10_toast_aktywacja.png` | HUD z toastem "Subskrypcja aktywowana: Kamera cofania" i zielonymi kontrolkami |
| `11_kamera_cofania_pip.png` | Cofanie z działającym obrazem PiP w prawym górnym rogu |
| `12_konsola_pdc_odczyty.png` | Konsola Unity z liniami `[CLOUD] ... false -> true` i surowymi danymi `[PDC] t=...` |
| `13_pasek_pdc_stop.png` | Auto tuż przy przeszkodzie: czerwony pasek PDC, strefa STOP |
| `14_promienie_scene_view.png` | Okno Scene w trakcie gry: kolorowe promienie czujnika (Gizmos) |
| `15_test_runner.png` | Test Runner, zakładka EditMode, wszystkie testy na zielono |
| `16_sklep_subskrypcji.png` | Otwarty sklep (B) w trakcie zakupu oraz konsola Firebase z flagą zmienioną na `true` |

## 9. Rozwiązywanie problemów

- **HUD: BŁĄD (... HTTP 401)**: reguły bazy nie pozwalają na odczyt, wklej
  `database.rules.json` w zakładce *Rules*.
- **Sklep: "Brak uprawnień do zapisu (HTTP 401)"**: opublikuj aktualne reguły
  z `database.rules.json` (pozwalają zapisywać flagi subskrypcji).
- **HUD: BŁĄD (Brak danych w bazie (odpowiedź null))**: ścieżka nie istnieje; sprawdź
  import JSON i `vehicleId`.
- **Różowe obiekty**: projekt używa URP zamiast Built-in RP; utwórz projekt z szablonu
  3D (Built-In Render Pipeline).
- **Brak menu IoT**: w konsoli są błędy kompilacji, które blokują skrypty edytora.
