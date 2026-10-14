using System.Globalization;
using System.Text;

namespace IoT.Subskrypcja
{
    public enum ParkingZone
    {
        BEZPIECZNIE,
        OSTRZEZENIE,
        STOP
    }

    /// <summary>Czysta logika czujnika parkowania (PDC): strefy i format logu.</summary>
    public static class ParkingSensorLogic
    {
        public const float ZasiegM = 3f;
        public const float ProgBezpiecznieM = 1.5f;
        public const float ProgStopM = 0.5f;

        /// <summary>BEZPIECZNIE &gt; 1.5 m, OSTRZEZENIE 0.5–1.5 m, STOP &lt; 0.5 m.</summary>
        public static ParkingZone Klasyfikuj(float odlegloscM)
        {
            if (odlegloscM < ProgStopM) return ParkingZone.STOP;
            if (odlegloscM > ProgBezpiecznieM) return ParkingZone.BEZPIECZNIE;
            return ParkingZone.OSTRZEZENIE;
        }

        /// <summary>Najmniejsza odległość z tablicy (pusta tablica = zasięg czujnika).</summary>
        public static float Minimum(float[] odleglosci)
        {
            if (odleglosci == null || odleglosci.Length == 0) return ZasiegM;
            float min = odleglosci[0];
            for (int i = 1; i < odleglosci.Length; i++)
                if (odleglosci[i] < min) min = odleglosci[i];
            return min;
        }

        /// <summary>
        /// "[PDC] t=12.40s dist_m=[1.23, 0.87, 0.91, 2.50] min=0.87 strefa=OSTRZEZENIE"
        /// Zawsze kropka dziesiętna, niezależnie od ustawień regionalnych systemu.
        /// </summary>
        public static string FormatujLinie(float czasS, float[] odleglosci)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            float min = Minimum(odleglosci);
            var sb = new StringBuilder(96);
            sb.Append("[PDC] t=").Append(czasS.ToString("0.00", inv)).Append("s dist_m=[");
            if (odleglosci != null)
            {
                for (int i = 0; i < odleglosci.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(odleglosci[i].ToString("0.00", inv));
                }
            }
            sb.Append("] min=").Append(min.ToString("0.00", inv));
            sb.Append(" strefa=").Append(Klasyfikuj(min).ToString());
            return sb.ToString();
        }
    }
}
