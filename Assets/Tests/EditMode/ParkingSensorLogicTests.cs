using NUnit.Framework;

namespace IoT.Subskrypcja.Testy
{
    public class ParkingSensorLogicTests
    {
        [TestCase(3.00f, ParkingZone.BEZPIECZNIE)]
        [TestCase(1.51f, ParkingZone.BEZPIECZNIE)]
        [TestCase(1.50f, ParkingZone.OSTRZEZENIE)]
        [TestCase(0.87f, ParkingZone.OSTRZEZENIE)]
        [TestCase(0.50f, ParkingZone.OSTRZEZENIE)]
        [TestCase(0.49f, ParkingZone.STOP)]
        [TestCase(0.00f, ParkingZone.STOP)]
        public void Klasyfikacja_Stref(float d, ParkingZone oczekiwana)
        {
            Assert.AreEqual(oczekiwana, ParkingSensorLogic.Klasyfikuj(d));
        }

        [Test]
        public void FormatLinii_ZgodnyZeSpecyfikacja()
        {
            string linia = ParkingSensorLogic.FormatujLinie(12.4f, new[] { 1.23f, 0.87f, 0.91f, 2.5f });
            Assert.AreEqual("[PDC] t=12.40s dist_m=[1.23, 0.87, 0.91, 2.50] min=0.87 strefa=OSTRZEZENIE", linia);
        }

        [Test]
        public void FormatLinii_BrakTrafien_ZasiegIBezpiecznie()
        {
            float z = ParkingSensorLogic.ZasiegM;
            string linia = ParkingSensorLogic.FormatujLinie(0f, new[] { z, z, z, z });
            Assert.AreEqual("[PDC] t=0.00s dist_m=[3.00, 3.00, 3.00, 3.00] min=3.00 strefa=BEZPIECZNIE", linia);
        }

        [Test]
        public void FormatLinii_Stop()
        {
            string linia = ParkingSensorLogic.FormatujLinie(5.555f, new[] { 0.3f, 0.45f, 1f, 3f });
            StringAssert.EndsWith("min=0.30 strefa=STOP", linia);
        }

        [Test]
        public void FormatLinii_NiezaleznyOdKulturySystemu()
        {
            var poprzednia = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pl-PL");
                string linia = ParkingSensorLogic.FormatujLinie(1.5f, new[] { 1.25f });
                StringAssert.Contains("t=1.50s", linia);
                StringAssert.Contains("[1.25]", linia);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = poprzednia;
            }
        }

        [Test]
        public void Minimum_PustaTablica_Zasieg()
        {
            Assert.AreEqual(ParkingSensorLogic.ZasiegM, ParkingSensorLogic.Minimum(new float[0]));
            Assert.AreEqual(ParkingSensorLogic.ZasiegM, ParkingSensorLogic.Minimum(null));
        }

        [TestCase(-2f, 0f, true)]    // jedzie do tyłu
        [TestCase(0f, -1f, true)]    // stoi i trzyma S
        [TestCase(0.5f, -1f, true)]  // prawie stoi i trzyma S
        [TestCase(5f, -1f, false)]   // hamuje z dużej prędkości
        [TestCase(0f, 0f, false)]    // stoi
        [TestCase(3f, 1f, false)]    // jedzie do przodu
        public void Wykrywanie_Wstecznego(float predkosc, float gaz, bool oczekiwane)
        {
            Assert.AreEqual(oczekiwane, ReverseDetector.CzyCofa(predkosc, gaz));
        }
    }
}
