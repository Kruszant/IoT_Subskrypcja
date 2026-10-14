using NUnit.Framework;

namespace IoT.Subskrypcja.Testy
{
    public class SubscriptionPurchaseTests
    {
        [Test]
        public void TrescZakupu_ZmieniaTylkoJednaFlage()
        {
            Assert.AreEqual("{\"rear_camera\":true}", SubscriptionPurchase.ZbudujTresc(FeatureKind.KameraCofania, true));
            Assert.AreEqual("{\"parking_sensor\":false}", SubscriptionPurchase.ZbudujTresc(FeatureKind.CzujnikParkowania, false));
        }

        [Test]
        public void TrescZakupu_JestPoprawnymJsonemDlaParsera()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse(SubscriptionPurchase.ZbudujTresc(FeatureKind.CzujnikParkowania, true), out s, out blad));
            Assert.IsTrue(s.parking_sensor);
            Assert.IsFalse(s.rear_camera);
        }

        [TestCase(401L)]
        [TestCase(403L)]
        public void BrakUprawnien_PodpowiadaReguly(long kod)
        {
            StringAssert.Contains("database.rules.json", SubscriptionPurchase.OpisBledu(kod, "HTTP/1.1 401 Unauthorized"));
        }

        [Test]
        public void InnyBladHttp_PodajeKod()
        {
            StringAssert.Contains("HTTP 500", SubscriptionPurchase.OpisBledu(500, "x"));
        }

        [Test]
        public void BladSieci_BrakPolaczenia()
        {
            StringAssert.Contains("Brak połączenia", SubscriptionPurchase.OpisBledu(0, "Cannot resolve destination host"));
        }
    }
}
