using System.Collections.Generic;
using NUnit.Framework;

namespace IoT.Subskrypcja.Testy
{
    public class SubscriptionStateTrackerTests
    {
        [Test]
        public void StanPoczatkowy_WszystkoZablokowane()
        {
            var t = new SubscriptionStateTracker();
            Assert.AreEqual(SubscriptionState.WszystkoZablokowane(), t.Aktualny);
        }

        [Test]
        public void TenSamStan_NieWywolujeZdarzenia()
        {
            var t = new SubscriptionStateTracker();
            int licznik = 0;
            t.StanZmieniony += s => licznik++;

            List<string> zmiany = t.Zastosuj(new SubscriptionState(false, false));
            t.Zastosuj(new SubscriptionState(false, false));

            Assert.AreEqual(0, licznik);
            Assert.IsEmpty(zmiany);
        }

        [Test]
        public void Zmiana_WywolujeZdarzenieRaz_ILogujeZmiane()
        {
            var t = new SubscriptionStateTracker();
            int licznik = 0;
            SubscriptionState otrzymany = null;
            t.StanZmieniony += s => { licznik++; otrzymany = s; };

            List<string> zmiany = t.Zastosuj(new SubscriptionState(true, false));
            t.Zastosuj(new SubscriptionState(true, false));
            t.Zastosuj(new SubscriptionState(true, false));

            Assert.AreEqual(1, licznik);
            Assert.IsTrue(otrzymany.rear_camera);
            Assert.AreEqual(1, zmiany.Count);
            Assert.AreEqual("[CLOUD] rear_camera: false -> true", zmiany[0]);
        }

        [Test]
        public void ZmianaObuFlag_DwieLinieLogu_JednoZdarzenie()
        {
            var t = new SubscriptionStateTracker();
            int licznik = 0;
            t.StanZmieniony += s => licznik++;

            List<string> zmiany = t.Zastosuj(new SubscriptionState(true, true));

            Assert.AreEqual(1, licznik);
            CollectionAssert.AreEqual(new[]
            {
                "[CLOUD] rear_camera: false -> true",
                "[CLOUD] parking_sensor: false -> true"
            }, zmiany);
        }

        [Test]
        public void PowrotDoFalse_TezJestZmiana()
        {
            var t = new SubscriptionStateTracker();
            t.Zastosuj(new SubscriptionState(false, true));
            List<string> zmiany = t.Zastosuj(new SubscriptionState(false, false));
            Assert.AreEqual("[CLOUD] parking_sensor: true -> false", zmiany[0]);
        }

        [Test]
        public void Null_ZachowujeOstatniStan()
        {
            var t = new SubscriptionStateTracker();
            t.Zastosuj(new SubscriptionState(true, true));
            int licznik = 0;
            t.StanZmieniony += s => licznik++;

            t.Zastosuj(null);

            Assert.AreEqual(0, licznik);
            Assert.AreEqual(new SubscriptionState(true, true), t.Aktualny);
        }

        [Test]
        public void Aktualny_ZwracaKopie()
        {
            var t = new SubscriptionStateTracker();
            SubscriptionState kopia = t.Aktualny;
            kopia.rear_camera = true;
            Assert.IsFalse(t.Aktualny.rear_camera);
        }

        [Test]
        public void FeatureGate_ZmieniaSieTylkoDlaSwojejFlagi()
        {
            var kamera = new FeatureGate(FeatureKind.KameraCofania);
            int licznik = 0;
            kamera.Zmieniona += o => licznik++;

            Assert.IsFalse(kamera.Odblokowana);
            Assert.IsFalse(kamera.Aktualizuj(new SubscriptionState(false, true)));
            Assert.IsTrue(kamera.Aktualizuj(new SubscriptionState(true, true)));
            Assert.IsFalse(kamera.Aktualizuj(new SubscriptionState(true, false)));
            Assert.IsTrue(kamera.Odblokowana);
            Assert.IsTrue(kamera.Aktualizuj(null));
            Assert.IsFalse(kamera.Odblokowana);
            Assert.AreEqual(2, licznik);
        }
    }
}
