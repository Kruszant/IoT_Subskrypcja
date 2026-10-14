using System.Collections.Generic;
using IoT.Subskrypcja;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Buduje całą scenę demo skryptem (menu "IoT/Zbuduj scenę demo"):
/// parking z przeszkodami, auto z prymitywów z systemami subskrypcyjnymi, kamery i HUD.
/// Zapisuje Assets/Scenes/Demo.unity i dodaje ją do Build Settings.
/// </summary>
public static class SceneBuilder
{
    private const string SciezkaSceny = "Assets/Scenes/Demo.unity";
    private const string SciezkaConfigu = "Assets/Config/SubscriptionConfig.asset";
    private const string FolderMaterialow = "Assets/Materials";
    private const string NazwaWarstwyAuta = "Pojazd";

#if UNITY_2022_2_OR_NEWER
    private const string NazwaCzcionki = "LegacyRuntime.ttf";
#else
    private const string NazwaCzcionki = "Arial.ttf";
#endif

    private static Font _czcionka;

    // Materiały
    private static Material _mKaroseria, _mSzyby, _mOpony, _mFelgi, _mZderzaki, _mReflektory,
        _mSwiatlaTylne, _mSwiatlaCofania, _mAsfalt, _mLinie, _mBeton, _mSlupek, _mPacholek,
        _mPacholekPas, _mAuto2, _mKamera;

    [MenuItem("IoT/Zbuduj scenę demo", false, 1)]
    public static void ZbudujScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("IoT", "Zatrzymaj tryb Play przed budowaniem sceny.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        UtworzFolder("Assets", "Scenes");
        UtworzFolder("Assets", "Config");
        UtworzFolder("Assets", "Materials");

        SubscriptionConfig config = UpewnijSieConfig();
        UtworzMaterialy();
        int warstwaAuta = UpewnijSieWarstwa(NazwaWarstwyAuta);
        _czcionka = Resources.GetBuiltinResource<Font>(NazwaCzcionki);

        Scene scena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ZbudujSwiatlo();
        ZbudujParking();

        // Chmura / IoT Edge
        var edge = new GameObject("IoT Edge");
        var klient = edge.AddComponent<CloudSubscriptionClient>();
        klient.config = config;

        // Auto
        CarController auto = ZbudujAuto(new Vector3(0f, 0.05f, 0f), warstwaAuta);

        // Kamera główna
        var kameraGlowna = new GameObject("Main Camera");
        kameraGlowna.tag = "MainCamera";
        var cam = kameraGlowna.AddComponent<Camera>();
        cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
        cam.fieldOfView = 60f;
        cam.farClipPlane = 300f;
        kameraGlowna.AddComponent<AudioListener>();
        kameraGlowna.transform.position = auto.transform.TransformPoint(new Vector3(0f, 3.5f, -7.5f));
        kameraGlowna.transform.LookAt(auto.transform.position + Vector3.up);
        var follow = kameraGlowna.AddComponent<CameraFollow>();
        follow.cel = auto.transform;

        // Systemy subskrypcyjne
        ZbudujSystemyIHud(auto, klient, warstwaAuta);

        EditorSceneManager.MarkSceneDirty(scena);
        if (!EditorSceneManager.SaveScene(scena, SciezkaSceny))
        {
            Debug.LogError("[IoT] Nie udało się zapisać sceny " + SciezkaSceny);
            return;
        }
        DodajDoBuildSettings(SciezkaSceny);

        Selection.activeGameObject = auto.gameObject;
        Debug.Log("[IoT] Scena demo zbudowana i zapisana: " + SciezkaSceny +
                  ". Uzupełnij databaseUrl w " + SciezkaConfigu + " i naciśnij Play.");
    }

    [MenuItem("IoT/Pokaż konfigurację subskrypcji", false, 2)]
    public static void PokazConfig()
    {
        UtworzFolder("Assets", "Config");
        SubscriptionConfig config = UpewnijSieConfig();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    // ------------------------------------------------------------------ zasoby

    private static void UtworzFolder(string rodzic, string nazwa)
    {
        if (!AssetDatabase.IsValidFolder(rodzic + "/" + nazwa)) AssetDatabase.CreateFolder(rodzic, nazwa);
    }

    private static SubscriptionConfig UpewnijSieConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SubscriptionConfig>(SciezkaConfigu);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SubscriptionConfig>();
            AssetDatabase.CreateAsset(config, SciezkaConfigu);
            AssetDatabase.SaveAssets();
            Debug.Log("[IoT] Utworzono " + SciezkaConfigu + " (wklej tam adres swojej bazy Firebase).");
        }
        return config;
    }

    private static int UpewnijSieWarstwa(string nazwa)
    {
        int istniejaca = LayerMask.NameToLayer(nazwa);
        if (istniejaca >= 0) return istniejaca;

        Object[] zasoby = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (zasoby != null && zasoby.Length > 0)
        {
            var tagManager = new SerializedObject(zasoby[0]);
            SerializedProperty warstwy = tagManager.FindProperty("layers");
            for (int i = 8; warstwy != null && i < warstwy.arraySize; i++)
            {
                SerializedProperty p = warstwy.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue))
                {
                    p.stringValue = nazwa;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[IoT] Dodano warstwę '" + nazwa + "' (nr " + i + ").");
                    return i;
                }
            }
        }
        Debug.LogWarning("[IoT] Brak wolnej warstwy - auto użyje warstwy 'Ignore Raycast'.");
        return 2;
    }

    private static Material Mat(string nazwa, Color kolor, float gladkosc = 0.3f, float metalicznosc = 0f, Color? emisja = null)
    {
        string sciezka = FolderMaterialow + "/" + nazwa + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(sciezka);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, sciezka);
        }
        m.color = kolor;
        m.SetFloat("_Glossiness", gladkosc);
        m.SetFloat("_Metallic", metalicznosc);
        if (emisja.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emisja.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void UtworzMaterialy()
    {
        _mKaroseria = Mat("Karoseria", new Color(0.05f, 0.3f, 0.75f), 0.75f, 0.4f);
        _mSzyby = Mat("Szyby", new Color(0.08f, 0.12f, 0.18f), 0.95f, 0.1f);
        _mOpony = Mat("Opony", new Color(0.06f, 0.06f, 0.06f), 0.1f);
        _mFelgi = Mat("Felgi", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.8f);
        _mZderzaki = Mat("Zderzaki", new Color(0.15f, 0.15f, 0.17f), 0.3f);
        _mReflektory = Mat("Reflektory", new Color(1f, 0.97f, 0.85f), 0.9f, 0f, new Color(1f, 0.95f, 0.75f) * 1.2f);
        _mSwiatlaTylne = Mat("SwiatlaTylne", new Color(0.55f, 0.02f, 0.02f), 0.8f, 0f, new Color(0.6f, 0f, 0f));
        _mSwiatlaCofania = Mat("SwiatlaCofania", Color.white, 0.9f, 0f, Color.white * 2.5f);
        _mAsfalt = Mat("Asfalt", new Color(0.22f, 0.22f, 0.24f), 0.15f);
        _mLinie = Mat("LinieParkingowe", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        _mBeton = Mat("Beton", new Color(0.62f, 0.6f, 0.56f), 0.1f);
        _mSlupek = Mat("Slupek", new Color(0.95f, 0.75f, 0.05f), 0.4f);
        _mPacholek = Mat("Pacholek", new Color(1f, 0.4f, 0.05f), 0.3f);
        _mPacholekPas = Mat("PacholekPas", Color.white, 0.3f);
        _mAuto2 = Mat("Auto2", new Color(0.6f, 0.08f, 0.08f), 0.7f, 0.3f);
        _mKamera = Mat("KameraObudowa", new Color(0.05f, 0.05f, 0.05f), 0.5f);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ geometria

    private static GameObject Prymityw(PrimitiveType typ, string nazwa, Transform rodzic, Vector3 pozycja,
        Vector3 skala, Material material, bool zColliderem, Vector3 obrot = default(Vector3))
    {
        GameObject go = GameObject.CreatePrimitive(typ);
        go.name = nazwa;
        if (rodzic != null) go.transform.SetParent(rodzic, false);
        go.transform.localPosition = pozycja;
        go.transform.localRotation = Quaternion.Euler(obrot);
        go.transform.localScale = skala;
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
        if (!zColliderem)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }

    private static void ZbudujSwiatlo()
    {
        var swiatlo = new GameObject("Directional Light");
        var l = swiatlo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        l.color = new Color(1f, 0.96f, 0.88f);
        l.shadows = LightShadows.Soft;
        swiatlo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void ZbudujParking()
    {
        var parking = new GameObject("Parking").transform;

        // Plane ma 10x10 j., skala 6 => 60x60 m
        Prymityw(PrimitiveType.Plane, "Nawierzchnia", parking, Vector3.zero, new Vector3(6f, 1f, 6f), _mAsfalt, true);

        // Rząd miejsc parkingowych przy ścianie (za autem)
        var linie = new GameObject("Linie miejsc").transform;
        linie.SetParent(parking, false);
        for (int i = 0; i < 7; i++)
        {
            float x = -9f + i * 3f;
            Prymityw(PrimitiveType.Cube, "Linia_" + i, linie, new Vector3(x, 0.006f, -7.5f),
                new Vector3(0.12f, 0.01f, 5f), _mLinie, false);
        }
        // Drugi rząd miejsc, po przeciwnej stronie placu
        for (int i = 0; i < 7; i++)
        {
            float x = -9f + i * 3f;
            Prymityw(PrimitiveType.Cube, "Linia_B_" + i, linie, new Vector3(x, 0.006f, 12f),
                new Vector3(0.12f, 0.01f, 5f), _mLinie, false);
        }
        // Linia jezdni
        Prymityw(PrimitiveType.Cube, "Linia_jezdni", linie, new Vector3(0f, 0.006f, 2.5f),
            new Vector3(24f, 0.01f, 0.12f), _mLinie, false);

        // Przeszkody (wszystkie z colliderami)
        var przeszkody = new GameObject("Przeszkody").transform;
        przeszkody.SetParent(parking, false);

        // Ściana za miejscami parkingowymi
        Prymityw(PrimitiveType.Cube, "Sciana", przeszkody, new Vector3(0f, 1.25f, -10.4f),
            new Vector3(24f, 2.5f, 0.4f), _mBeton, true);

        // Słupki
        Slupek(przeszkody, "Slupek_1", new Vector3(-1.4f, 0f, -6.2f));
        Slupek(przeszkody, "Slupek_2", new Vector3(1.6f, 0f, -7.8f));
        Slupek(przeszkody, "Slupek_3", new Vector3(4.6f, 0f, -6.5f));

        // Pachołki
        Pacholek(przeszkody, "Pacholek_1", new Vector3(0.4f, 0f, -4.6f));
        Pacholek(przeszkody, "Pacholek_2", new Vector3(3.2f, 0f, -2.8f));
        Pacholek(przeszkody, "Pacholek_3", new Vector3(-3.0f, 0f, -1.0f));
        Pacholek(przeszkody, "Pacholek_4", new Vector3(5.5f, 0f, 1.5f));

        // Drugie auto zaparkowane obok miejsca startu
        ZbudujZaparkowaneAuto(przeszkody, new Vector3(-4.5f, 0f, -6.8f));
    }

    private static void Slupek(Transform rodzic, string nazwa, Vector3 pozycja)
    {
        var s = Prymityw(PrimitiveType.Cylinder, nazwa, rodzic, pozycja + new Vector3(0f, 0.5f, 0f),
            new Vector3(0.25f, 0.5f, 0.25f), _mSlupek, true);
        Prymityw(PrimitiveType.Cylinder, "Pas", s.transform, new Vector3(0f, 0.6f, 0f),
            new Vector3(1.02f, 0.12f, 1.02f), _mZderzaki, false);
    }

    private static void Pacholek(Transform rodzic, string nazwa, Vector3 pozycja)
    {
        var p = new GameObject(nazwa).transform;
        p.SetParent(rodzic, false);
        p.localPosition = pozycja;
        Prymityw(PrimitiveType.Cube, "Podstawa", p, new Vector3(0f, 0.025f, 0f), new Vector3(0.45f, 0.05f, 0.45f), _mPacholek, false);
        Prymityw(PrimitiveType.Cylinder, "Dol", p, new Vector3(0f, 0.25f, 0f), new Vector3(0.32f, 0.2f, 0.32f), _mPacholek, false);
        Prymityw(PrimitiveType.Cylinder, "Pas", p, new Vector3(0f, 0.5f, 0f), new Vector3(0.24f, 0.05f, 0.24f), _mPacholekPas, false);
        Prymityw(PrimitiveType.Cylinder, "Gora", p, new Vector3(0f, 0.65f, 0f), new Vector3(0.16f, 0.1f, 0.16f), _mPacholek, false);
        // Jeden prosty collider na cały pachołek (pewne trafienie promieniem PDC na wysokości 0,45 m)
        var kolider = p.gameObject.AddComponent<BoxCollider>();
        kolider.center = new Vector3(0f, 0.375f, 0f);
        kolider.size = new Vector3(0.36f, 0.75f, 0.36f);
    }

    private static void ZbudujZaparkowaneAuto(Transform rodzic, Vector3 pozycja)
    {
        var a = new GameObject("Zaparkowane auto").transform;
        a.SetParent(rodzic, false);
        a.localPosition = pozycja;
        Prymityw(PrimitiveType.Cube, "Nadwozie", a, new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 0.7f, 4.2f), _mAuto2, true);
        Prymityw(PrimitiveType.Cube, "Kabina", a, new Vector3(0f, 1.25f, 0.2f), new Vector3(1.6f, 0.55f, 2.2f), _mSzyby, true);
        float[] xs = { -0.92f, 0.92f };
        float[] zs = { -1.35f, 1.35f };
        foreach (float x in xs)
            foreach (float z in zs)
                Prymityw(PrimitiveType.Cylinder, "Kolo", a, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f),
                    _mOpony, false, new Vector3(0f, 0f, 90f));
    }

    private static CarController ZbudujAuto(Vector3 pozycja, int warstwa)
    {
        // Auto stoi przodem (+Z) od ściany, tyłem do przeszkód.
        var korzen = new GameObject("Auto");
        Transform t = korzen.transform;
        t.position = pozycja;

        var rb = korzen.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var kolider = korzen.AddComponent<BoxCollider>();
        kolider.center = new Vector3(0f, 0.65f, 0f);
        kolider.size = new Vector3(1.8f, 1.3f, 4.3f);
        kolider.sharedMaterial = MaterialFizycznyBezTarcia();

        // Nadwozie
        Prymityw(PrimitiveType.Cube, "Nadwozie", t, new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 0.6f, 4.2f), _mKaroseria, false);
        Prymityw(PrimitiveType.Cube, "Maska", t, new Vector3(0f, 1.0f, 1.45f), new Vector3(1.7f, 0.06f, 1.2f), _mKaroseria, false, new Vector3(-4f, 0f, 0f));
        Prymityw(PrimitiveType.Cube, "Kabina", t, new Vector3(0f, 1.28f, -0.35f), new Vector3(1.6f, 0.56f, 2.1f), _mKaroseria, false);

        // Szyby: przednia szeroka i pochylona, tylna mniejsza => widać kierunek jazdy
        Prymityw(PrimitiveType.Cube, "Szyba przednia", t, new Vector3(0f, 1.28f, 0.72f), new Vector3(1.5f, 0.5f, 0.05f), _mSzyby, false, new Vector3(-25f, 0f, 0f));
        Prymityw(PrimitiveType.Cube, "Szyba tylna", t, new Vector3(0f, 1.3f, -1.41f), new Vector3(1.3f, 0.36f, 0.05f), _mSzyby, false);
        Prymityw(PrimitiveType.Cube, "Szyba lewa", t, new Vector3(-0.81f, 1.3f, -0.35f), new Vector3(0.03f, 0.36f, 1.8f), _mSzyby, false);
        Prymityw(PrimitiveType.Cube, "Szyba prawa", t, new Vector3(0.81f, 1.3f, -0.35f), new Vector3(0.03f, 0.36f, 1.8f), _mSzyby, false);

        // Zderzaki, reflektory, grill
        Prymityw(PrimitiveType.Cube, "Zderzak przedni", t, new Vector3(0f, 0.45f, 2.15f), new Vector3(1.85f, 0.25f, 0.15f), _mZderzaki, false);
        Prymityw(PrimitiveType.Cube, "Zderzak tylny", t, new Vector3(0f, 0.45f, -2.15f), new Vector3(1.85f, 0.25f, 0.15f), _mZderzaki, false);
        Prymityw(PrimitiveType.Cube, "Grill", t, new Vector3(0f, 0.72f, 2.11f), new Vector3(0.8f, 0.2f, 0.03f), _mZderzaki, false);
        Prymityw(PrimitiveType.Cube, "Reflektor L", t, new Vector3(-0.6f, 0.78f, 2.11f), new Vector3(0.42f, 0.16f, 0.04f), _mReflektory, false);
        Prymityw(PrimitiveType.Cube, "Reflektor P", t, new Vector3(0.6f, 0.78f, 2.11f), new Vector3(0.42f, 0.16f, 0.04f), _mReflektory, false);

        var tylneL = Prymityw(PrimitiveType.Cube, "Swiatlo tylne L", t, new Vector3(-0.62f, 0.8f, -2.11f), new Vector3(0.4f, 0.16f, 0.04f), _mSwiatlaTylne, false);
        var tylneP = Prymityw(PrimitiveType.Cube, "Swiatlo tylne P", t, new Vector3(0.62f, 0.8f, -2.11f), new Vector3(0.4f, 0.16f, 0.04f), _mSwiatlaTylne, false);

        // Koła: Pivot (skręt wokół Y) -> Obrot (toczenie wokół X) -> Opona (cylinder obrócony o 90° wokół Z)
        var pivoty = new List<Transform>();
        var obroty = new List<Transform>();
        float[] xs = { -0.95f, 0.95f };
        float[] zs = { 1.35f, -1.35f };
        foreach (float z in zs)
        {
            foreach (float x in xs)
            {
                string strona = (x < 0 ? "L" : "P") + (z > 0 ? "P" : "T");
                var pivot = new GameObject("Kolo_" + strona).transform;
                pivot.SetParent(t, false);
                pivot.localPosition = new Vector3(x, 0.35f, z);
                var obrot = new GameObject("Obrot").transform;
                obrot.SetParent(pivot, false);
                Prymityw(PrimitiveType.Cylinder, "Opona", obrot, Vector3.zero, new Vector3(0.7f, 0.12f, 0.7f), _mOpony, false, new Vector3(0f, 0f, 90f));
                Prymityw(PrimitiveType.Cube, "Felga", obrot, new Vector3(x < 0 ? -0.125f : 0.125f, 0f, 0f), new Vector3(0.02f, 0.5f, 0.12f), _mFelgi, false);
                if (z > 0) pivoty.Add(pivot);
                obroty.Add(obrot);
            }
        }

        // Kamera cofania na tylnym zderzaku: skierowana do tyłu i lekko w dół
        Prymityw(PrimitiveType.Cube, "Obudowa kamery", t, new Vector3(0f, 0.95f, -2.13f), new Vector3(0.12f, 0.07f, 0.06f), _mKamera, false);
        var kameraGo = new GameObject("Kamera cofania");
        kameraGo.transform.SetParent(t, false);
        kameraGo.transform.localPosition = new Vector3(0f, 0.95f, -2.2f);
        kameraGo.transform.localRotation = Quaternion.Euler(22f, 180f, 0f);
        var kamera = kameraGo.AddComponent<Camera>();
        kamera.fieldOfView = 100f;
        kamera.nearClipPlane = 0.05f;
        kamera.farClipPlane = 60f;
        kamera.depth = -2f;
        kamera.enabled = false;

        // Punkty czujników PDC (lewy, środek-lewy, środek-prawy, prawy)
        var czujnikiRoot = new GameObject("Czujniki PDC").transform;
        czujnikiRoot.SetParent(t, false);
        float[] xsCzujnikow = { -0.75f, -0.25f, 0.25f, 0.75f };
        string[] nazwy = { "PDC_Lewy", "PDC_SrodekLewy", "PDC_SrodekPrawy", "PDC_Prawy" };
        var czujniki = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var c = new GameObject(nazwy[i]).transform;
            c.SetParent(czujnikiRoot, false);
            c.localPosition = new Vector3(xsCzujnikow[i], 0.45f, -2.25f);
            czujniki[i] = c;
        }

        UstawWarstwe(korzen, warstwa);

        var auto = korzen.AddComponent<CarController>();
        auto.kolaPrzedniePivot0 = pivoty[0];
        auto.kolaPrzedniePivot1 = pivoty[1];
        auto.kolaObrot = obroty.ToArray();
        auto.swiatlaTylne = new Renderer[] { tylneL.GetComponent<Renderer>(), tylneP.GetComponent<Renderer>() };
        auto.materialSwiatlaTylne = _mSwiatlaTylne;
        auto.materialSwiatlaCofania = _mSwiatlaCofania;

        var pdc = korzen.AddComponent<ParkingSensorSystem>();
        pdc.auto = auto;
        pdc.czujniki = czujniki;
        pdc.maskaWykrywania = ~(1 << warstwa) & ~(1 << 2);

        var rear = korzen.AddComponent<RearCameraSystem>();
        rear.kameraCofania = kamera;
        rear.auto = auto;

        return auto;
    }

#if UNITY_6000_0_OR_NEWER
    private static PhysicsMaterial MaterialFizycznyBezTarcia()
    {
        const string sciezka = FolderMaterialow + "/AutoBezTarcia.asset";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(sciezka);
        if (m == null)
        {
            m = new PhysicsMaterial("AutoBezTarcia");
            AssetDatabase.CreateAsset(m, sciezka);
        }
        m.frictionCombine = PhysicsMaterialCombine.Minimum;
#else
    private static PhysicMaterial MaterialFizycznyBezTarcia()
    {
        const string sciezka = FolderMaterialow + "/AutoBezTarcia.asset";
        var m = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(sciezka);
        if (m == null)
        {
            m = new PhysicMaterial("AutoBezTarcia");
            AssetDatabase.CreateAsset(m, sciezka);
        }
        m.frictionCombine = PhysicMaterialCombine.Minimum;
#endif
        // Tarcie z podłożem zerowe: prędkość auta i tak ustawia CarController (model arcade).
        m.dynamicFriction = 0f;
        m.staticFriction = 0f;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void UstawWarstwe(GameObject go, int warstwa)
    {
        go.layer = warstwa;
        foreach (Transform dziecko in go.transform) UstawWarstwe(dziecko.gameObject, warstwa);
    }

    // ------------------------------------------------------------------ UI

    private static void ZbudujSystemyIHud(CarController auto, CloudSubscriptionClient klient, int warstwaAuta)
    {
        var pdc = auto.GetComponent<ParkingSensorSystem>();
        var rear = auto.GetComponent<RearCameraSystem>();
        pdc.cloudClient = klient;
        rear.cloudClient = klient;

        // Canvas
        var canvasGo = new GameObject("HUD", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var skaler = canvasGo.AddComponent<CanvasScaler>();
        skaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        skaler.referenceResolution = new Vector2(1920f, 1080f);
        skaler.matchWidthOrHeight = 0.5f;
        RectTransform root = (RectTransform)canvasGo.transform;

        var hud = canvasGo.AddComponent<HUD>();
        hud.cloudClient = klient;
        hud.auto = auto;
        hud.czujnikParkowania = pdc;

        Color tloPaneli = new Color(0f, 0f, 0f, 0.55f);

        // --- Lewy górny: status chmury i kontrolki subskrypcji
        RectTransform panelStatus = Obraz(root, "Panel statusu", tloPaneli).rectTransform;
        Zakotwicz(panelStatus, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(620f, 210f));

        hud.tekstChmury = Tekst(panelStatus, "Status chmury", "Chmura: ...", 20, TextAnchor.UpperLeft, Color.white);
        Zakotwicz(hud.tekstChmury.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15f, -10f), new Vector2(-30f, 90f));

        hud.kontrolkaKamery = Obraz(panelStatus, "Kontrolka kamery", Color.red);
        Zakotwicz(hud.kontrolkaKamery.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(15f, -118f), new Vector2(26f, 26f));
        hud.tekstKamery = Tekst(panelStatus, "Tekst kamery", "Kamera cofania: ZABLOKOWANA", 24, TextAnchor.MiddleLeft, Color.white);
        Zakotwicz(hud.tekstKamery.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(52f, -114f), new Vector2(-60f, 34f));

        hud.kontrolkaCzujnika = Obraz(panelStatus, "Kontrolka czujnika", Color.red);
        Zakotwicz(hud.kontrolkaCzujnika.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(15f, -162f), new Vector2(26f, 26f));
        hud.tekstCzujnika = Tekst(panelStatus, "Tekst czujnika", "Czujnik parkowania: ZABLOKOWANY", 24, TextAnchor.MiddleLeft, Color.white);
        Zakotwicz(hud.tekstCzujnika.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(52f, -158f), new Vector2(-60f, 34f));

        // --- Lewy dolny: prędkość i bieg
        RectTransform panelJazdy = Obraz(root, "Panel jazdy", tloPaneli).rectTransform;
        Zakotwicz(panelJazdy, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 20f), new Vector2(380f, 120f));
        hud.tekstPredkosci = Tekst(panelJazdy, "Predkosc", "0 km/h", 56, TextAnchor.MiddleLeft, Color.white);
        Zakotwicz(hud.tekstPredkosci.rectTransform, Vector2.zero, new Vector2(0.72f, 1f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
        hud.tekstBiegu = Tekst(panelJazdy, "Bieg", "D", 72, TextAnchor.MiddleCenter, Color.white);
        Zakotwicz(hud.tekstBiegu.rectTransform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Text pomoc = Tekst(root, "Sterowanie", "W/S - gaz, hamulec, wsteczny   A/D - skręt   Spacja - hamulec ręczny   B - sklep subskrypcji", 18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.8f));
        Zakotwicz(pomoc.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 145f), new Vector2(1100f, 30f));

        // --- Góra środek: toast
        Image toastTlo = Obraz(root, "Toast", new Color(0.1f, 0.45f, 0.15f, 0.92f));
        Zakotwicz(toastTlo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 70f));
        hud.toast = toastTlo.gameObject.AddComponent<CanvasGroup>();
        hud.toast.alpha = 0f;
        hud.toast.blocksRaycasts = false;
        hud.tekstToastu = Tekst(toastTlo.rectTransform, "Tekst toastu", "Subskrypcja aktywowana", 28, TextAnchor.MiddleCenter, Color.white);
        Rozciagnij(hud.tekstToastu.rectTransform);

        // --- Dół środek: pasek PDC
        Image pdcTlo = Obraz(root, "Panel PDC", tloPaneli);
        Zakotwicz(pdcTlo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(560f, 90f));
        hud.panelPdc = pdcTlo.gameObject;
        hud.tekstPdc = Tekst(pdcTlo.rectTransform, "Tekst PDC", "PDC", 24, TextAnchor.MiddleCenter, Color.white);
        Zakotwicz(hud.tekstPdc.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(0f, 34f));
        Image pasekTlo = Obraz(pdcTlo.rectTransform, "Pasek tlo", new Color(0.25f, 0.25f, 0.25f, 1f));
        Zakotwicz(pasekTlo.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-40f, 28f));
        hud.obrazWypelnieniaPdc = Obraz(pasekTlo.rectTransform, "Pasek wypelnienie", Color.green);
        hud.wypelnieniePdc = hud.obrazWypelnieniaPdc.rectTransform;
        hud.wypelnieniePdc.anchorMin = Vector2.zero;
        hud.wypelnieniePdc.anchorMax = new Vector2(0f, 1f);
        hud.wypelnieniePdc.pivot = new Vector2(0f, 0.5f);
        hud.wypelnieniePdc.offsetMin = Vector2.zero;
        hud.wypelnieniePdc.offsetMax = Vector2.zero;
        pdcTlo.gameObject.SetActive(false);

        // --- Prawy górny róg: Picture-in-Picture kamery cofania
        RectTransform pip = Nowy(root, "Kamera cofania PiP");
        Zakotwicz(pip, Vector2.one, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(560f, 350f));

        // Podgląd (ramka + obraz + podpis + linie pomocnicze)
        Image ramka = Obraz(pip, "Panel podgladu", new Color(0.05f, 0.05f, 0.05f, 0.95f));
        Rozciagnij(ramka.rectTransform);
        var obraz = Nowy(ramka.rectTransform, "Obraz kamery").gameObject.AddComponent<RawImage>();
        Zakotwicz(obraz.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(-16f, -44f));
        obraz.color = Color.white;
        obraz.uvRect = new Rect(1f, 0f, -1f, 1f); // odbicie lustrzane jak w prawdziwej kamerze cofania
        Text podpis = Tekst(ramka.rectTransform, "Podpis", "KAMERA COFANIA", 22, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f));
        Zakotwicz(podpis.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(10f, -2f), new Vector2(-20f, 28f));
        ZbudujLiniePomocnicze(obraz.rectTransform);

        // Blokada (szary panel z kłódką)
        Image blokada = Obraz(pip, "Panel blokady", new Color(0.35f, 0.35f, 0.37f, 0.95f));
        Rozciagnij(blokada.rectTransform);
        ZbudujKlodke(blokada.rectTransform);
        Text tytul = Tekst(blokada.rectTransform, "Tytul", "KAMERA COFANIA", 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f));
        Zakotwicz(tytul.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 30f));
        Text info = Tekst(blokada.rectTransform, "Informacja", "Funkcja wymaga subskrypcji", 28, TextAnchor.MiddleCenter, Color.white);
        Zakotwicz(info.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(0f, 40f));

        // --- Prawa strona, pod kamerą: sklep subskrypcji (klawisz B)
        var sklep = canvasGo.AddComponent<SubscriptionShop>();
        sklep.cloudClient = klient;
        Image sklepTlo = Obraz(root, "Sklep subskrypcji", new Color(0.05f, 0.08f, 0.15f, 0.92f));
        Zakotwicz(sklepTlo.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, -110f), new Vector2(560f, 340f));
        Text sklepTytul = Tekst(sklepTlo.rectTransform, "Tytul", "SKLEP SUBSKRYPCJI", 28, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        Zakotwicz(sklepTytul.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 40f));
        sklep.tekstOferty = Tekst(sklepTlo.rectTransform, "Oferta", "", 22, TextAnchor.UpperLeft, Color.white);
        Zakotwicz(sklep.tekstOferty.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(-40f, -110f));
        sklep.tekstStatusu = Tekst(sklepTlo.rectTransform, "Status", "", 20, TextAnchor.MiddleLeft, Color.white);
        Zakotwicz(sklep.tekstStatusu.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-40f, 50f));
        sklep.panel = sklepTlo.gameObject;
        sklepTlo.gameObject.SetActive(false);

        rear.panelPodgladu = ramka.gameObject;
        rear.obrazKamery = obraz;
        rear.panelBlokady = blokada.gameObject;
        ramka.gameObject.SetActive(false);
    }

    private static void ZbudujLiniePomocnicze(RectTransform rodzic)
    {
        // Dwie zbieżne linie toru jazdy + poprzeczki odległości (zielona / żółta / czerwona)
        Color[] kolory = { new Color(0.1f, 0.9f, 0.2f, 0.8f), new Color(1f, 0.85f, 0.1f, 0.8f), new Color(1f, 0.15f, 0.1f, 0.85f) };
        float[] wysokosci = { 0.55f, 0.35f, 0.15f };
        float[] szerokosci = { 0.34f, 0.44f, 0.56f };
        for (int i = 0; i < 3; i++)
        {
            Image poprzeczka = Obraz(rodzic, "Linia odleglosci " + i, kolory[i]);
            RectTransform rt = poprzeczka.rectTransform;
            rt.anchorMin = new Vector2(0.5f - szerokosci[i] / 2f, wysokosci[i]);
            rt.anchorMax = new Vector2(0.5f + szerokosci[i] / 2f, wysokosci[i]);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 4f);
        }
        for (int s = -1; s <= 1; s += 2)
        {
            Image bok = Obraz(rodzic, s < 0 ? "Tor lewy" : "Tor prawy", new Color(1f, 0.85f, 0.1f, 0.8f));
            RectTransform rt = bok.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f + s * 0.235f, 0.05f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(4f, 190f);
            rt.localRotation = Quaternion.Euler(0f, 0f, s * 22f);
        }
    }

    private static void ZbudujKlodke(RectTransform rodzic)
    {
        Color kolor = new Color(0.9f, 0.9f, 0.9f);
        RectTransform klodka = Nowy(rodzic, "Klodka");
        Zakotwicz(klodka, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(90f, 110f));
        Image korpus = Obraz(klodka, "Korpus", kolor);
        Zakotwicz(korpus.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 62f));
        Image dziurka = Obraz(korpus.rectTransform, "Dziurka", new Color(0.3f, 0.3f, 0.32f));
        Zakotwicz(dziurka.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 26f));
        Image lewy = Obraz(klodka, "Pałąk L", kolor);
        Zakotwicz(lewy.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-28f, 60f), new Vector2(12f, 46f));
        Image prawy = Obraz(klodka, "Pałąk P", kolor);
        Zakotwicz(prawy.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(28f, 60f), new Vector2(12f, 46f));
        Image gora = Obraz(klodka, "Pałąk G", kolor);
        Zakotwicz(gora.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 98f), new Vector2(68f, 12f));
    }

    private static RectTransform Nowy(Transform rodzic, string nazwa)
    {
        var go = new GameObject(nazwa, typeof(RectTransform));
        go.transform.SetParent(rodzic, false);
        return (RectTransform)go.transform;
    }

    private static Image Obraz(Transform rodzic, string nazwa, Color kolor)
    {
        RectTransform rt = Nowy(rodzic, nazwa);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = kolor;
        img.raycastTarget = false;
        return img;
    }

    private static Text Tekst(Transform rodzic, string nazwa, string tresc, int rozmiar, TextAnchor wyrownanie, Color kolor)
    {
        RectTransform rt = Nowy(rodzic, nazwa);
        var txt = rt.gameObject.AddComponent<Text>();
        txt.font = _czcionka;
        txt.text = tresc;
        txt.fontSize = rozmiar;
        txt.alignment = wyrownanie;
        txt.color = kolor;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        var cien = rt.gameObject.AddComponent<Shadow>();
        cien.effectColor = new Color(0f, 0f, 0f, 0.8f);
        cien.effectDistance = new Vector2(1.5f, -1.5f);
        return txt;
    }

    private static void Zakotwicz(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pozycja, Vector2 rozmiar)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pozycja;
        rt.sizeDelta = rozmiar;
    }

    private static void Rozciagnij(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------------ build settings

    private static void DodajDoBuildSettings(string sciezka)
    {
        var sceny = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        sceny.RemoveAll(s => s.path == sciezka);
        sceny.Insert(0, new EditorBuildSettingsScene(sciezka, true));
        EditorBuildSettings.scenes = sceny.ToArray();
    }
}
