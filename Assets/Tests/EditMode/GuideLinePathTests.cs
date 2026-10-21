using NUnit.Framework;
using UnityEngine;

namespace IoT.Subskrypcja.Testy
{
    public class GuideLinePathTests
    {
        [Test]
        public void BezSkretu_ProstaDoTylu()
        {
            Vector2 p = GuideLinePath.Punkt(0f, 0.85f, 3f);
            Assert.AreEqual(0.85f, p.x, 1e-4f);
            Assert.AreEqual(-3f, p.y, 1e-4f);
        }

        [Test]
        public void SkretWPrawo_TorUciekaWPrawo()
        {
            Vector2 p = GuideLinePath.Punkt(0.3f, 0f, 3f);
            Assert.Greater(p.x, 0.1f);
            Assert.Less(p.y, 0f);
        }

        [Test]
        public void SkretWLewo_LustrzanyDoPrawego()
        {
            Vector2 prawy = GuideLinePath.Punkt(0.2f, 0.85f, 2f);
            Vector2 lewy = GuideLinePath.Punkt(-0.2f, -0.85f, 2f);
            Assert.AreEqual(prawy.x, -lewy.x, 1e-4f);
            Assert.AreEqual(prawy.y, lewy.y, 1e-4f);
        }

        [Test]
        public void ZeroDystansu_PunktStartowy()
        {
            Vector2 p = GuideLinePath.Punkt(0.4f, -0.85f, 0f);
            Assert.AreEqual(-0.85f, p.x, 1e-4f);
            Assert.AreEqual(0f, p.y, 1e-4f);
        }

        [Test]
        public void PunktyLezaNaOkregu()
        {
            float k = 0.25f;
            Vector2 srodek = new Vector2(1f / k, 0f);
            for (float d = 0.5f; d <= 4f; d += 0.5f)
                Assert.AreEqual(1f / k - 0.85f, Vector2.Distance(srodek, GuideLinePath.Punkt(k, 0.85f, d)), 1e-3f);
        }

        [Test]
        public void KrzywiznaZKata_ModelRowerowy()
        {
            Assert.AreEqual(0f, GuideLinePath.KrzywiznaZKata(0f, 2.7f), 1e-6f);
            Assert.AreEqual(Mathf.Tan(30f * Mathf.Deg2Rad) / 2.7f, GuideLinePath.KrzywiznaZKata(30f, 2.7f), 1e-6f);
        }
    }
}
