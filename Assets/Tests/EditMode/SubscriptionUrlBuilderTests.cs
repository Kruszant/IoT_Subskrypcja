using System;
using NUnit.Framework;

namespace IoT.Subskrypcja.Testy
{
    public class SubscriptionUrlBuilderTests
    {
        private const string Oczekiwany =
            "https://abc-default-rtdb.europe-west1.firebasedatabase.app/vehicles/car01/subscriptions.json";

        [TestCase("https://abc-default-rtdb.europe-west1.firebasedatabase.app")]
        [TestCase("https://abc-default-rtdb.europe-west1.firebasedatabase.app/")]
        [TestCase("https://abc-default-rtdb.europe-west1.firebasedatabase.app///")]
        [TestCase("  https://abc-default-rtdb.europe-west1.firebasedatabase.app/  ")]
        [TestCase("https://abc-default-rtdb.europe-west1.firebasedatabase.app/.json")]
        public void BudujeUrl_NormalizujeAdres(string baza)
        {
            Assert.AreEqual(Oczekiwany, SubscriptionUrlBuilder.ZbudujUrl(baza, "car01"));
        }

        [Test]
        public void KodujeIdentyfikatorPojazdu()
        {
            Assert.AreEqual("https://x.firebaseio.com/vehicles/auto%20testowe/subscriptions.json",
                SubscriptionUrlBuilder.ZbudujUrl("https://x.firebaseio.com", "auto testowe"));
        }

        [Test]
        public void PustyAdresLubId_RzucaWyjatek()
        {
            Assert.Throws<ArgumentException>(() => SubscriptionUrlBuilder.ZbudujUrl("", "car01"));
            Assert.Throws<ArgumentException>(() => SubscriptionUrlBuilder.ZbudujUrl("https://x.firebaseio.com", " "));
        }

        [TestCase("https://TWOJ-PROJEKT-default-rtdb.firebaseio.com", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("http://abc.firebaseio.com", false)]
        [TestCase("https://", false)]
        [TestCase("https://abc-default-rtdb.firebaseio.com", true)]
        [TestCase("https://abc-default-rtdb.europe-west1.firebasedatabase.app", true)]
        public void RozpoznajeSkonfigurowanyAdres(string url, bool oczekiwane)
        {
            Assert.AreEqual(oczekiwane, SubscriptionUrlBuilder.CzySkonfigurowany(url));
        }

        [Test]
        public void AdresWzorcowyZConfigu_JestNieskonfigurowany()
        {
            Assert.IsFalse(SubscriptionUrlBuilder.CzySkonfigurowany(SubscriptionConfig.AdresWzorcowy));
        }
    }
}
