using UnityEngine;
using UnityEngine.UI;

namespace IoT.Subskrypcja
{
    public enum TrybPodgladu
    {
        TylkoPodczasCofania,
        Zawsze
    }

    /// <summary>
    /// Kamera cofania (Picture-in-Picture). Kamera na tylnym zderzaku renderuje do RenderTexture,
    /// który wyświetla RawImage w prawym górnym rogu ekranu. Bez subskrypcji kamera jest
    /// wyłączona (enabled = false), a w rogu widać szary panel z kłódką.
    /// </summary>
    public class RearCameraSystem : SubscriptionFeature
    {
        [Header("Kamera")]
        public Camera kameraCofania;
        public int szerokoscObrazu = 640;
        public int wysokoscObrazu = 360;

        [Header("UI")]
        [Tooltip("Ramka z obrazem kamery, podpisem i liniami pomocniczymi")]
        public GameObject panelPodgladu;
        public RawImage obrazKamery;
        [Tooltip("Szary panel z kłódką i napisem 'Funkcja wymaga subskrypcji'")]
        public GameObject panelBlokady;

        [Header("Zachowanie")]
        public TrybPodgladu trybPodgladu = TrybPodgladu.TylkoPodczasCofania;
        public CarController auto;

        protected override FeatureKind Funkcja
        {
            get { return FeatureKind.KameraCofania; }
        }

        private RenderTexture _tekstura;

        protected override void Awake()
        {
            base.Awake();
            if (kameraCofania != null)
            {
                _tekstura = new RenderTexture(szerokoscObrazu, wysokoscObrazu, 16);
                _tekstura.name = "KameraCofania_RT";
                kameraCofania.targetTexture = _tekstura;
                kameraCofania.enabled = false;
            }
            if (obrazKamery != null) obrazKamery.texture = _tekstura;
        }

        private void Update()
        {
            OdswiezWidok();
        }

        protected override void PoZmianieDostepu(bool odblokowana)
        {
            OdswiezWidok();
        }

        private void OdswiezWidok()
        {
            bool odblokowana = Odblokowana;
            bool cofa = auto != null && auto.IsReversing;
            bool pokazObraz = odblokowana && (trybPodgladu == TrybPodgladu.Zawsze || cofa);

            if (kameraCofania != null && kameraCofania.enabled != pokazObraz) kameraCofania.enabled = pokazObraz;
            if (panelPodgladu != null && panelPodgladu.activeSelf != pokazObraz) panelPodgladu.SetActive(pokazObraz);
            if (panelBlokady != null && panelBlokady.activeSelf == odblokowana) panelBlokady.SetActive(!odblokowana);
        }

        private void OnDestroy()
        {
            if (_tekstura != null)
            {
                if (kameraCofania != null) kameraCofania.targetTexture = null;
                _tekstura.Release();
                Destroy(_tekstura);
            }
        }
    }
}
