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

        /// <summary>Krzywizna z kąta skrętu kół (model rowerowy): tan(kąt) / rozstaw osi.</summary>
        public static float KrzywiznaZKata(float katSkretuDeg, float rozstawOsi)
        {
            return Mathf.Tan(katSkretuDeg * Mathf.Deg2Rad) / Mathf.Max(0.1f, rozstawOsi);
        }
    }
}
