using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Dynamiczne linie pomocnicze kamery cofania. Co klatkę liczy przewidywany tor kół
    /// z aktualnego skrętu (CarController.KrzywiznaToru + GuideLinePath), rzutuje go przez kamerę cofania na obraz PiP
    /// i rysuje odcinkami UI. Poprzeczki pokazują odległość 0,5 / 1,5 / 3 m od zderzaka.
    /// </summary>
    public class RearCameraGuideLines : MonoBehaviour
    {
        public CarController auto;
        public Camera kameraCofania;
        [Tooltip("RawImage z obrazem kamery (linie są rysowane w jego układzie)")]
        public RectTransform obraz;
        [Tooltip("Czy obraz jest odbity lustrzanie (uvRect z ujemną szerokością)")]
        public bool lustro = true;

        [Header("Geometria auta")]
        [Tooltip("Rozstaw osi (m): tylna oś leży rozstawOsi/2 za środkiem auta")]
        public float rozstawOsi = 2.7f;
        public float szerokoscToru = 1.7f;
        [Tooltip("Odległość od tylnej osi do zderzaka (m)")]
        public float osDoZderzaka = 0.8f;

        [Header("Wygląd")]
        public float zasiegM = 3f;
        public int odcinkowNaLinie = 14;
        public float grubosc = 4f;

        private static readonly float[] Poprzeczki = { 0.5f, 1.5f, 3f };

        private readonly List<Image> _odcinkiLewe = new List<Image>();
        private readonly List<Image> _odcinkiPrawe = new List<Image>();
        private readonly List<Image> _poprzeczki = new List<Image>();

        private void Awake()
        {
            for (int i = 0; i < odcinkowNaLinie; i++)
            {
                _odcinkiLewe.Add(NowyOdcinek("Tor L " + i));
                _odcinkiPrawe.Add(NowyOdcinek("Tor P " + i));
            }
            for (int i = 0; i < Poprzeczki.Length; i++) _poprzeczki.Add(NowyOdcinek("Poprzeczka " + i));
        }

        private Image NowyOdcinek(string nazwa)
        {
            var go = new GameObject(nazwa, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private void LateUpdate()
        {
            if (auto == null || kameraCofania == null || obraz == null) return;

            float kat = auto.KrzywiznaToru;
            float pol = szerokoscToru / 2f;
            float krok = zasiegM / odcinkowNaLinie;

            for (int i = 0; i < odcinkowNaLinie; i++)
            {
                float d0 = i * krok, d1 = (i + 1) * krok;
                Color kolor = ParkingSensorSystem.KolorDlaOdleglosci(d0);
                Rysuj(_odcinkiLewe[i], NaEkran(kat, -pol, d0), NaEkran(kat, -pol, d1), kolor);
                Rysuj(_odcinkiPrawe[i], NaEkran(kat, pol, d0), NaEkran(kat, pol, d1), kolor);
            }
            for (int i = 0; i < Poprzeczki.Length; i++)
            {
                float d = Poprzeczki[i];
                Rysuj(_poprzeczki[i], NaEkran(kat, -pol, d), NaEkran(kat, pol, d),
                    ParkingSensorSystem.KolorDlaOdleglosci(d - 0.01f));
            }
        }

        /// <summary>Punkt toru (d metrów za zderzakiem) w pikselach RawImage; null, gdy za kamerą.</summary>
        private Vector2? NaEkran(float krzywizna, float przesuniecie, float odZderzaka)
        {
            Vector2 p = GuideLinePath.Punkt(krzywizna, przesuniecie, osDoZderzaka + odZderzaka);
            // Tylna oś leży w połowie rozstawu za środkiem auta.
            Vector3 lokalny = new Vector3(p.x, 0.02f, p.y - rozstawOsi / 2f);
            Vector3 vp = kameraCofania.WorldToViewportPoint(auto.transform.TransformPoint(lokalny));
            if (vp.z <= 0f) return null;
            float x = lustro ? 1f - vp.x : vp.x;
            Rect r = obraz.rect;
            return new Vector2((x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);
        }

        private void Rysuj(Image odcinek, Vector2? a, Vector2? b, Color kolor)
        {
            bool widoczny = a.HasValue && b.HasValue;
            if (odcinek.enabled != widoczny) odcinek.enabled = widoczny;
            if (!widoczny) return;

            Vector2 delta = b.Value - a.Value;
            RectTransform rt = odcinek.rectTransform;
            rt.anchoredPosition = a.Value;
            rt.sizeDelta = new Vector2(delta.magnitude, grubosc);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            odcinek.color = new Color(kolor.r, kolor.g, kolor.b, 0.85f);
        }
    }
}
