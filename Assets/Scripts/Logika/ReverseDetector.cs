namespace IoT.Subskrypcja
{
    /// <summary>Wykrywanie biegu wstecznego (czysta logika, testowalna).</summary>
    public static class ReverseDetector
    {
        /// <summary>
        /// Cofanie: prędkość wzdłuż osi auta jest ujemna (poniżej -progRuchu)
        /// albo kierowca trzyma S (gaz &lt; 0) przy niskiej prędkości.
        /// </summary>
        public static bool CzyCofa(float predkoscWzdluzna, float gaz, float niskaPredkosc = 1f, float progRuchu = 0.1f)
        {
            if (predkoscWzdluzna < -progRuchu) return true;
            return gaz < -0.1f && predkoscWzdluzna < niskaPredkosc;
        }
    }
}
