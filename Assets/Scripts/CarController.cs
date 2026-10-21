using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Sterowanie autem w stylu arcade na Rigidbody.
    /// W/S: gaz / hamulec / wsteczny, A/D: skręt, Spacja: hamulec ręczny.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Napęd")]
        public float maksPredkoscPrzod = 14f;      // m/s (~50 km/h)
        public float maksPredkoscTyl = 4f;         // m/s
        public float przyspieszenie = 7f;          // m/s^2
        public float przyspieszenieWstecz = 4f;    // m/s^2
        public float hamowanie = 14f;              // m/s^2
        public float hamulecReczny = 20f;          // m/s^2
        public float toczenie = 2.5f;              // wytracanie prędkości bez gazu

        [Header("Skręt")]
        public float predkoscSkretu = 70f;         // stopnie/s przy pełnym skręcie
        public float maksKatKolWizualnie = 30f;

        [Header("Koła (wizualne)")]
        public Transform kolaPrzedniePivot0;
        public Transform kolaPrzedniePivot1;
        public Transform[] kolaObrot;              // obiekty obracane wokół lokalnej osi X
        public float promienKola = 0.35f;

        [Header("Światła tylne")]
        public Renderer[] swiatlaTylne;
        public Material materialSwiatlaTylne;
        public Material materialSwiatlaCofania;

        /// <summary>Prędkość wzdłuż osi auta (m/s), ujemna przy jeździe do tyłu.</summary>
        public float PredkoscWzdluzna { get; private set; }

        public float PredkoscKmh
        {
            get { return Mathf.Abs(PredkoscWzdluzna) * 3.6f; }
        }

        /// <summary>
        /// Krzywizna toru (1/m), po którym auto faktycznie pojedzie przy obecnym skręcie.
        /// Wynika z modelu skrętu w FixedUpdate: prędkość kątowa = skręt * predkoscSkretu
        /// * min(1, |v| / 3), więc promień skrętu nie zależy od prędkości poniżej 3 m/s.
        /// </summary>
        public float KrzywiznaToru
        {
            get { return _skret * predkoscSkretu * Mathf.Deg2Rad / Mathf.Max(3f, Mathf.Abs(PredkoscWzdluzna)); }
        }

        /// <summary>True, gdy auto cofa (bieg R).</summary>
        public bool IsReversing { get; private set; }

        private Rigidbody _rb;
        private float _gaz;
        private float _skret;
        private bool _reczny;
        private float _katObrotuKol;
        private bool _ostatnioCofal;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.centerOfMass = new Vector3(0f, 0.3f, 0f);
            UstawSwiatla(false);
        }

        private void Update()
        {
            _gaz = Mathf.Clamp(DriverInput.Gaz(), -1f, 1f);
            _skret = Mathf.Clamp(DriverInput.Skret(), -1f, 1f);
            _reczny = DriverInput.HamulecReczny();

            IsReversing = ReverseDetector.CzyCofa(PredkoscWzdluzna, _gaz);
            if (IsReversing != _ostatnioCofal)
            {
                UstawSwiatla(IsReversing);
                _ostatnioCofal = IsReversing;
            }

            AnimujKola();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 v = PobierzPredkosc();
            Vector3 przod = transform.forward;
            float wzdluz = Vector3.Dot(v, przod);

            if (_reczny)
            {
                wzdluz = Mathf.MoveTowards(wzdluz, 0f, hamulecReczny * dt);
            }
            else if (_gaz > 0.05f)
            {
                if (wzdluz < -0.1f) wzdluz = Mathf.MoveTowards(wzdluz, 0f, hamowanie * dt);
                else wzdluz = Mathf.MoveTowards(wzdluz, maksPredkoscPrzod * _gaz, przyspieszenie * dt);
            }
            else if (_gaz < -0.05f)
            {
                if (wzdluz > 0.1f) wzdluz = Mathf.MoveTowards(wzdluz, 0f, hamowanie * dt);
                else wzdluz = Mathf.MoveTowards(wzdluz, -maksPredkoscTyl * -_gaz, przyspieszenieWstecz * dt);
            }
            else
            {
                wzdluz = Mathf.MoveTowards(wzdluz, 0f, toczenie * dt);
            }

            // Skręt proporcjonalny do prędkości; przy cofaniu kierunek odwrócony jak w prawdziwym aucie.
            float wspolczynnik = Mathf.Clamp01(Mathf.Abs(wzdluz) / 3f) * Mathf.Sign(wzdluz);
            float kat = _skret * predkoscSkretu * wspolczynnik * dt;
            if (Mathf.Abs(kat) > 0.0001f)
            {
                Quaternion obrot = _rb.rotation * Quaternion.Euler(0f, kat, 0f);
                _rb.MoveRotation(obrot);
                przod = obrot * Vector3.forward;
            }

            // Arcade: bez poślizgu bocznego, zachowujemy tylko składową pionową (grawitacja).
            UstawPredkosc(przod * wzdluz + Vector3.up * v.y);
            PredkoscWzdluzna = wzdluz;
        }

        private Vector3 PobierzPredkosc()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }

        private void UstawPredkosc(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = v;
#else
            _rb.velocity = v;
#endif
        }

        private void AnimujKola()
        {
            float katSkretu = _skret * maksKatKolWizualnie;
            if (kolaPrzedniePivot0 != null) kolaPrzedniePivot0.localRotation = Quaternion.Euler(0f, katSkretu, 0f);
            if (kolaPrzedniePivot1 != null) kolaPrzedniePivot1.localRotation = Quaternion.Euler(0f, katSkretu, 0f);

            if (kolaObrot == null || promienKola <= 0f) return;
            _katObrotuKol += PredkoscWzdluzna / promienKola * Mathf.Rad2Deg * Time.deltaTime;
            _katObrotuKol = Mathf.Repeat(_katObrotuKol, 360f);
            for (int i = 0; i < kolaObrot.Length; i++)
            {
                if (kolaObrot[i] != null) kolaObrot[i].localRotation = Quaternion.Euler(_katObrotuKol, 0f, 0f);
            }
        }

        private void UstawSwiatla(bool cofanie)
        {
            if (swiatlaTylne == null) return;
            Material m = cofanie ? materialSwiatlaCofania : materialSwiatlaTylne;
            if (m == null) return;
            for (int i = 0; i < swiatlaTylne.Length; i++)
            {
                if (swiatlaTylne[i] != null) swiatlaTylne[i].sharedMaterial = m;
            }
        }
    }
}
