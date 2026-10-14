using NUnit.Framework;

namespace IoT.Subskrypcja.Testy
{
    public class SubscriptionParserTests
    {
        [Test]
        public void Parsuje_ObieFlagiFalse()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("{\"rear_camera\":false,\"parking_sensor\":false}", out s, out blad));
            Assert.IsFalse(s.rear_camera);
            Assert.IsFalse(s.parking_sensor);
            Assert.IsNull(blad);
        }

        [Test]
        public void Parsuje_ObieFlagiTrue_ZeSpacjami()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("  { \"rear_camera\" : true, \"parking_sensor\" : true }\n", out s, out blad));
            Assert.IsTrue(s.rear_camera);
            Assert.IsTrue(s.parking_sensor);
        }

        [Test]
        public void Parsuje_FlagiMieszane()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("{\"parking_sensor\":true,\"rear_camera\":false}", out s, out blad));
            Assert.IsFalse(s.rear_camera);
            Assert.IsTrue(s.parking_sensor);
        }

        [Test]
        public void BrakPola_TraktowanyJakoZablokowane()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("{\"rear_camera\":true}", out s, out blad));
            Assert.IsTrue(s.rear_camera);
            Assert.IsFalse(s.parking_sensor);
        }

        [Test]
        public void PustyObiekt_WszystkoZablokowane()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("{}", out s, out blad));
            Assert.AreEqual(SubscriptionState.WszystkoZablokowane(), s);
        }

        [Test]
        public void DodatkowePola_SaIgnorowane()
        {
            SubscriptionState s; string blad;
            Assert.IsTrue(SubscriptionParser.TryParse("{\"rear_camera\":true,\"parking_sensor\":false,\"inne\":123}", out s, out blad));
            Assert.IsTrue(s.rear_camera);
        }

        [TestCase("null")]
        [TestCase(" null ")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void NullLubPusto_ZwracaFalse(string json)
        {
            SubscriptionState s; string blad;
            Assert.IsFalse(SubscriptionParser.TryParse(json, out s, out blad));
            Assert.IsNull(s);
            Assert.IsNotEmpty(blad);
        }

        [TestCase("to nie jest json")]
        [TestCase("{\"rear_camera\":")]
        [TestCase("[true,false]")]
        [TestCase("true")]
        [TestCase("{\"error\" : \"Permission denied\"")]
        public void BlednyJson_ZwracaFalse(string json)
        {
            SubscriptionState s; string blad;
            Assert.IsFalse(SubscriptionParser.TryParse(json, out s, out blad));
            Assert.IsNull(s);
            Assert.IsNotEmpty(blad);
        }
    }
}
