using System;

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Stan subskrypcji pojazdu, tak jak jest zapisany w Firebase:
    /// {"rear_camera": false, "parking_sensor": false}.
    /// Nazwy pól muszą odpowiadać kluczom JSON (JsonUtility mapuje po nazwie pola).
    /// </summary>
    [Serializable]
    public class SubscriptionState : IEquatable<SubscriptionState>
    {
        public bool rear_camera;
        public bool parking_sensor;

        public SubscriptionState() { }

        public SubscriptionState(bool rearCamera, bool parkingSensor)
        {
            rear_camera = rearCamera;
            parking_sensor = parkingSensor;
        }

        /// <summary>Stan domyślny: wszystkie funkcje zablokowane.</summary>
        public static SubscriptionState WszystkoZablokowane()
        {
            return new SubscriptionState(false, false);
        }

        public SubscriptionState Kopia()
        {
            return new SubscriptionState(rear_camera, parking_sensor);
        }

        /// <summary>Zwraca wartość flagi dla wskazanej funkcji.</summary>
        public bool CzyOdblokowana(FeatureKind funkcja)
        {
            switch (funkcja)
            {
                case FeatureKind.KameraCofania: return rear_camera;
                case FeatureKind.CzujnikParkowania: return parking_sensor;
                default: return false;
            }
        }

        public bool Equals(SubscriptionState other)
        {
            if (ReferenceEquals(other, null)) return false;
            return rear_camera == other.rear_camera && parking_sensor == other.parking_sensor;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SubscriptionState);
        }

        public override int GetHashCode()
        {
            return (rear_camera ? 1 : 0) | (parking_sensor ? 2 : 0);
        }

        public override string ToString()
        {
            return "rear_camera=" + Bool(rear_camera) + ", parking_sensor=" + Bool(parking_sensor);
        }

        internal static string Bool(bool wartosc)
        {
            return wartosc ? "true" : "false";
        }
    }

    /// <summary>Funkcje pojazdu objęte subskrypcją.</summary>
    public enum FeatureKind
    {
        KameraCofania,
        CzujnikParkowania
    }

    public static class FeatureKindExtensions
    {
        /// <summary>Klucz flagi w bazie Firebase.</summary>
        public static string KluczJson(this FeatureKind funkcja)
        {
            return funkcja == FeatureKind.KameraCofania ? "rear_camera" : "parking_sensor";
        }

        /// <summary>Nazwa funkcji do wyświetlenia w UI.</summary>
        public static string NazwaPolska(this FeatureKind funkcja)
        {
            return funkcja == FeatureKind.KameraCofania ? "Kamera cofania" : "Czujnik parkowania";
        }
    }
}
