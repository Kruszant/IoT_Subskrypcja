using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Czujnik parkowania (PDC): 4 promienie Physics.Raycast z tylnego zderzaka.
    /// Po odblokowaniu, podczas cofania, co 0,2 s wysyła surowe odczyty do konsoli silnika
    /// (symulacja terminala diagnostycznego pojazdu).
    /// </summary>
    public class ParkingSensorSystem : SubscriptionFeature
    {
        [Header("Czujniki")]
        public CarController auto;
        [Tooltip("Punkty startowe promieni (lewy, środek-lewy, środek-prawy, prawy)")]
        public Transform[] czujniki = new Transform[4];
        [Tooltip("Odchylenie promieni od osi tylnej w stopniach (dodatnie = w lewo)")]
        public float[] katyOdchylenia = { 20f, 6f, -6f, -20f };
        public float zasiegM = ParkingSensorLogic.ZasiegM;
        [Tooltip("Warstwy wykrywane przez czujnik (bez warstwy samego auta)")]
        public LayerMask maskaWykrywania = ~0;
        public float okresLogowaniaS = 0.2f;

        /// <summary>Ostatnie odczyty w metrach (brak trafienia = zasięg).</summary>
        public float[] Odleglosci { get; private set; }
        public float MinimalnaOdleglosc { get; private set; }
        public ParkingZone Strefa { get; private set; }

        /// <summary>True, gdy czujnik jest odblokowany i auto cofa (dane są aktualne).</summary>
        public bool Aktywny { get; private set; }

        protected override FeatureKind Funkcja
        {
            get { return FeatureKind.CzujnikParkowania; }
        }

        private float _nastepnyLog;
        private bool _poprzednioCofal;

        protected override void Awake()
        {
            base.Awake();
            Odleglosci = new float[czujniki != null ? czujniki.Length : 0];
            ResetujOdczyty();
        }

        protected override void PoZmianieDostepu(bool odblokowana)
        {
            if (!odblokowana)
            {
                Aktywny = false;
                ResetujOdczyty();
            }
            _nastepnyLog = 0f;
        }

        private void Update()
        {
            bool cofa = auto != null && auto.IsReversing;

            if (!Odblokowana)
            {
                Aktywny = false;
                // Jeden komunikat na każdą próbę cofania, bez spamu.
                if (cofa && !_poprzednioCofal)
                    Debug.Log("[PDC] Czujnik parkowania zablokowany - brak subskrypcji");
                _poprzednioCofal = cofa;
                return;
            }

            _poprzednioCofal = cofa;
            Aktywny = cofa;
            if (!cofa) return;

            Pomiar();

            if (Time.time >= _nastepnyLog)
            {
                Debug.Log(ParkingSensorLogic.FormatujLinie(Time.time, Odleglosci));
                _nastepnyLog = Time.time + okresLogowaniaS;
            }
        }

        private void Pomiar()
        {
            for (int i = 0; i < Odleglosci.Length; i++)
            {
                Transform t = czujniki[i];
                if (t == null)
                {
                    Odleglosci[i] = zasiegM;
                    continue;
                }
                Vector3 kierunek = KierunekPromienia(i);
                RaycastHit trafienie;
                float d = zasiegM;
                if (Physics.Raycast(t.position, kierunek, out trafienie, zasiegM, maskaWykrywania, QueryTriggerInteraction.Ignore))
                    d = trafienie.distance;
                Odleglosci[i] = d;
                Debug.DrawRay(t.position, kierunek * d, KolorDlaOdleglosci(d));
            }
            MinimalnaOdleglosc = ParkingSensorLogic.Minimum(Odleglosci);
            Strefa = ParkingSensorLogic.Klasyfikuj(MinimalnaOdleglosc);
        }

        private Vector3 KierunekPromienia(int i)
        {
            float kat = (katyOdchylenia != null && i < katyOdchylenia.Length) ? katyOdchylenia[i] : 0f;
            return transform.rotation * (Quaternion.Euler(0f, kat, 0f) * Vector3.back);
        }

        private void ResetujOdczyty()
        {
            if (Odleglosci == null) return;
            for (int i = 0; i < Odleglosci.Length; i++) Odleglosci[i] = zasiegM;
            MinimalnaOdleglosc = zasiegM;
            Strefa = ParkingZone.BEZPIECZNIE;
        }

        public static Color KolorDlaOdleglosci(float d)
        {
            switch (ParkingSensorLogic.Klasyfikuj(d))
            {
                case ParkingZone.STOP: return Color.red;
                case ParkingZone.OSTRZEZENIE: return new Color(1f, 0.6f, 0f);
                default: return Color.green;
            }
        }

        private void OnDrawGizmos()
        {
            if (czujniki == null) return;
            bool maOdczyty = Application.isPlaying && Odleglosci != null && Odleglosci.Length == czujniki.Length;
            for (int i = 0; i < czujniki.Length; i++)
            {
                if (czujniki[i] == null) continue;
                float d = maOdczyty ? Odleglosci[i] : zasiegM;
                Gizmos.color = maOdczyty && Aktywny ? KolorDlaOdleglosci(d) : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                Gizmos.DrawRay(czujniki[i].position, KierunekPromienia(i) * d);
                Gizmos.DrawWireSphere(czujniki[i].position, 0.04f);
            }
        }
    }
}
