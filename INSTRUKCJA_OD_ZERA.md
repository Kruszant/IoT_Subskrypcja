# Instrukcja od zera: od instalacji do działającego demo

Potrzebujesz komputera z Windows albo macOS, internetu i konta Google. Całość zajmuje
ok. 30-60 minut, z czego większość to instalacja Unity.

## Krok 1. Zainstaluj Unity Hub

1. Wejdź na https://unity.com/download i pobierz **Unity Hub**.
2. Zainstaluj go, uruchom i zaloguj się (albo załóż darmowe konto Unity).
3. Gdy Hub zapyta o licencję, wybierz darmową **Personal**.

## Krok 2. Zainstaluj edytor Unity

1. W Unity Hub kliknij po lewej **Installs**, a potem **Install Editor**.
2. Wybierz **Unity 6 (LTS)** albo **2022.3 LTS** (oba działają).
3. Moduły dodatkowe możesz pominąć. Kliknij **Install** i poczekaj (kilka GB).

## Krok 3. Utwórz projekt

1. W Unity Hub kliknij **Projects**, a potem **New project**.
2. Wybierz szablon **3D (Built-In Render Pipeline)**. Nie wybieraj "Universal 3D"/URP.
   Jeśli szablon ma przycisk Download, najpierw go kliknij.
3. W polu **Project name** wpisz `IoT_Subskrypcja`, wybierz folder (np. Dokumenty)
   i kliknij **Create project**. Otworzy się edytor Unity.
4. Zamknij na razie edytor Unity (File > Exit), żeby spokojnie skopiować pliki.

## Krok 4. Skopiuj pliki projektu

1. Pobierz `IoT_Subskrypcja.zip` z tego wątku i rozpakuj go.
2. Otwórz folder projektu utworzony w kroku 3 (ten, w którym są `Assets`, `Packages`,
   `ProjectSettings`).
3. Z rozpakowanego zipa przeciągnij do niego foldery `Assets`, `Firebase`, `Docs` oraz
   pliki `README.md` i ta instrukcja. Na pytanie o scalenie folderu `Assets` odpowiedz
   **Tak / Scal**.
4. Otwórz projekt ponownie z Unity Hub (Projects > IoT_Subskrypcja). Poczekaj,
   aż zniknie pasek importu w prawym dolnym rogu.

## Krok 5. Sprawdź, że wszystko się kompiluje

1. Menu **Window > General > Console**. Jeśli widać czerwone błędy, skopiuj je i wklej
   w wątku.
2. Na górnym pasku menu powinno pojawić się menu **IoT**.
3. Menu **Window > General > Test Runner**, zakładka **EditMode**, przycisk **Run All**.
   Wszystkie testy mają mieć zielony znaczek.

## Krok 6. Zbuduj scenę i jedź bez chmury

1. Kliknij **IoT > Zbuduj scenę demo**. Pojawi się parking z niebieskim autem.
2. Kliknij trójkąt **Play** na górze edytora, a potem kliknij w okno gry (Game),
   żeby łapało klawiaturę.
3. Jedź: **W** gaz, **S** hamulec/wsteczny, **A/D** skręt, **Spacja** hamulec ręczny.
4. Oczekiwane: napis "TRYB OFFLINE" w HUD, czerwone kontrolki, kłódka w prawym górnym rogu.
5. Kliknij **Play** ponownie, żeby zatrzymać grę.

## Krok 7. Utwórz bazę w Firebase

1. Wejdź na https://console.firebase.google.com i zaloguj się kontem Google.
2. **Create a project** (Utwórz projekt), dowolna nazwa, Google Analytics możesz wyłączyć.
3. W menu po lewej: **Build > Realtime Database > Create Database**.
4. Lokalizacja: **Belgium (europe-west1)**, potem **Start in test mode** > **Enable**.
5. Na górze zakładki **Data** zobaczysz adres bazy, np.
   `https://iot-xxxx-default-rtdb.europe-west1.firebasedatabase.app`. Skopiuj go.
6. Kliknij **⋮** (trzy kropki po prawej) > **Import JSON** i wskaż plik
   `Firebase/firebase_import.json`. Pojawi się `vehicles > car01 > subscriptions`.
7. Zakładka **Rules**: usuń wszystko, wklej zawartość `Firebase/database.rules.json`,
   kliknij **Publish**.
8. Test: wklej w przeglądarce adres bazy z dopiskiem
   `/vehicles/car01/subscriptions.json`. Ma się pokazać
   `{"parking_sensor":false,"rear_camera":false}`.

## Krok 8. Połącz Unity z Firebase i pokaż odblokowanie

1. W Unity kliknij **IoT > Pokaż konfigurację subskrypcji**. Po prawej (Inspector)
   w polu **Database Url** wklej adres bazy (bez `/vehicles/...`). Zapisz: Ctrl+S.
2. Kliknij **Play**. HUD powinien pokazać "Chmura: POŁĄCZONO".
3. Cofnij (S): kamera i czujnik są zablokowane.
4. Obok otwórz przeglądarkę z Firebase. Kliknij wartość `false` przy `rear_camera`,
   wpisz `true`, Enter.
5. W ciągu ok. 2 s: toast "Subskrypcja aktywowana: Kamera cofania", zielona kontrolka,
   przy cofaniu obraz z kamery w prawym górnym rogu.
6. To samo dla `parking_sensor`. Cofaj na pomarańczowy pachołek: w konsoli Unity
   pojawią się linie `[PDC] t=... dist_m=[...] strefa=...`, a na dole ekranu pasek odległości.

7. Sklep w symulacji: naciśnij **B**, potem **1** albo **2**. Flaga w Firebase zmieni się
   sama na `true` (albo z powrotem na `false`), a funkcja odblokuje się po chwili.

To jest dokładnie scenariusz prezentacji. Zrzuty ekranu do dokumentacji: lista w README,
sekcja 8.
