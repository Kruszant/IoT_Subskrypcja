using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Przewidywany tor jazdy przy cofaniu (model rowerowy, czysta logika).
    /// Układ współrzędnych auta: x w prawo, z do przodu, początek w środku tylnej osi.
    /// </summary>
    public static class GuideLinePath
    {
        /// <summary>
        /// Punkt toru na ziemi (x, z) w układzie tylnej osi po przejechaniu do tyłu
        /// <paramref name="dystansWstecz"/> metrów, dla punktu przesuniętego bocznie o
        /// <paramref name="przesuniecieBoczne"/> (np. ±połowa szerokości auta).
        /// Krzywizna w 1/m, dodatnia = koła skręcone w prawo (klawisz D).
        /// </summary>
        public static Vector2 Punkt(float krzywizna, float przesuniecieBoczne, float dystansWstecz)
        {
            if (Mathf.Abs(krzywizna) < 1e-4f)
                return new Vector2(przesuniecieBoczne, -dystansWstecz);

            // Środek obrotu leży na osi tylnej, w odległości R w stronę skrętu.
            float r = 1f / krzywizna;
            float kat = dystansWstecz * krzywizna;          // kąt obrotu auta po przejechaniu dystansu
            float promien = r - przesuniecieBoczne;          // promień toru danego punktu
            return new Vector2(r - promien * Mathf.Cos(kat), -promien * Mathf.Sin(kat));
        }

        /// <summary>
        /// Przycina odcinek a–b do prostokąta (algorytm Lianga–Barsky'ego).
        /// Zwraca false, gdy odcinek leży w całości poza prostokątem.
        /// </summary>
        public static bool PrzytnijOdcinek(ref Vector2 a, ref Vector2 b, Rect prostokat)
        {
            Vector2 d = b - a;
            float t0 = 0f, t1 = 1f;
            if (!Ogranicz(-d.x, a.x - prostokat.xMin, ref t0, ref t1)) return false;
            if (!Ogranicz(d.x, prostokat.xMax - a.x, ref t0, ref t1)) return false;
            if (!Ogranicz(-d.y, a.y - prostokat.yMin, ref t0, ref t1)) return false;
            if (!Ogranicz(d.y, prostokat.yMax - a.y, ref t0, ref t1)) return false;
            Vector2 start = a;
            a = start + d * t0;
            b = start + d * t1;
            return true;
        }

        private static bool Ogranicz(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f)) return q >= 0f;
            float t = q / p;
            if (p < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
            return true;
        }

        /// <summary>Krzywizna z kąta skrętu kół (model rowerowy): tan(kąt) / rozstaw osi.</summary>
        public static float KrzywiznaZKata(float katSkretuDeg, float rozstawOsi)
        {
            return Mathf.Tan(katSkretuDeg * Mathf.Deg2Rad) / Mathf.Max(0.1f, rozstawOsi);
        }
    }
}
