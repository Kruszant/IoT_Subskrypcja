using System;
using System.Collections.Generic;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Pamięta ostatni znany stan subskrypcji i wykrywa zmiany.
    /// Zdarzenie <see cref="StanZmieniony"/> jest wywoływane wyłącznie przy ZMIANIE stanu.
    /// </summary>
    public class SubscriptionStateTracker
    {
        private SubscriptionState _aktualny = SubscriptionState.WszystkoZablokowane();

        /// <summary>Nowy stan po zmianie.</summary>
        public event Action<SubscriptionState> StanZmieniony;

        /// <summary>Kopia ostatniego znanego stanu (domyślnie wszystko zablokowane).</summary>
        public SubscriptionState Aktualny
        {
            get { return _aktualny.Kopia(); }
        }

        /// <summary>
        /// Przyjmuje nowy stan z chmury. Zwraca listę linii logu opisujących zmiany
        /// (np. "[CLOUD] rear_camera: false -> true"); pusta lista oznacza brak zmian.
        /// Wartość null jest ignorowana (zachowany ostatni znany stan).
        /// </summary>
        public List<string> Zastosuj(SubscriptionState nowy)
        {
            var zmiany = new List<string>();
            if (nowy == null) return zmiany;

            if (nowy.rear_camera != _aktualny.rear_camera)
                zmiany.Add(FormatujZmiane("rear_camera", _aktualny.rear_camera, nowy.rear_camera));
            if (nowy.parking_sensor != _aktualny.parking_sensor)
                zmiany.Add(FormatujZmiane("parking_sensor", _aktualny.parking_sensor, nowy.parking_sensor));

            if (zmiany.Count > 0)
            {
                _aktualny = nowy.Kopia();
                if (StanZmieniony != null) StanZmieniony(_aktualny.Kopia());
            }
            return zmiany;
        }

        public static string FormatujZmiane(string klucz, bool przed, bool po)
        {
            return "[CLOUD] " + klucz + ": " + SubscriptionState.Bool(przed) + " -> " + SubscriptionState.Bool(po);
        }
    }
}
