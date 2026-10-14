using UnityEngine;

namespace IoT.Subskrypcja
{
    /// <summary>Kamera główna płynnie podąża za autem z tyłu i z góry.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform cel;
        [Tooltip("Pozycja kamery w układzie auta (z tyłu i wyżej)")]
        public Vector3 przesuniecie = new Vector3(0f, 3.5f, -7.5f);
        [Tooltip("Punkt, na który patrzy kamera, względem auta")]
        public Vector3 punktPatrzenia = new Vector3(0f, 1f, 1.5f);
        public float czasWygladzania = 0.25f;
        public float szybkoscObrotu = 6f;

        private Vector3 _predkosc;

        private void Start()
        {
            if (cel == null) return;
            transform.position = cel.TransformPoint(przesuniecie);
            transform.LookAt(cel.TransformPoint(punktPatrzenia));
        }

        private void LateUpdate()
        {
            if (cel == null) return;
            Vector3 docelowa = cel.TransformPoint(przesuniecie);
            transform.position = Vector3.SmoothDamp(transform.position, docelowa, ref _predkosc, czasWygladzania);
            Quaternion docelowyObrot = Quaternion.LookRotation(cel.TransformPoint(punktPatrzenia) - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, docelowyObrot, 1f - Mathf.Exp(-szybkoscObrotu * Time.deltaTime));
        }
    }
}
