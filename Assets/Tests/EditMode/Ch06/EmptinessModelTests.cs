using JianQi.Ch06;
using NUnit.Framework;

namespace JianQi.Ch06.Tests
{
    /// <summary>設計文件 §8 驗收 1、4：吃東西、上下限、自然上升、失控與「傾聽自己」。</summary>
    public class EmptinessModelTests
    {
        private static EmptinessModel Make(float start = 40f) =>
            new EmptinessModel(new EmptinessRules { StartValue = start });

        [Test]
        public void Eat_AddsFifteen()
        {
            var m = Make(40f);
            m.Eat();
            Assert.AreEqual(55f, m.Value, 0.001f);
        }

        [Test]
        public void Eat_NeverExceedsMax()
        {
            var m = Make(95f);
            m.Eat();
            Assert.AreEqual(100f, m.Value, 0.001f);
            m.Eat();
            Assert.AreEqual(100f, m.Value, 0.001f);
        }

        [Test]
        public void CompleteEcho_SubtractsTwenty_NotBelowZero()
        {
            var m = Make(15f);
            m.CompleteEcho();
            Assert.AreEqual(0f, m.Value, 0.001f);
        }

        [Test]
        public void Drift_OnlyWhenDriftingAndNotListening()
        {
            var m = Make(40f);
            m.Tick(2f, drifting: false, listening: false);
            Assert.AreEqual(40f, m.Value, 0.001f);
            m.Tick(2f, drifting: true, listening: true);
            Assert.AreEqual(40f, m.Value, 0.001f);
            m.Tick(2f, drifting: true, listening: false);
            Assert.AreEqual(41f, m.Value, 0.001f);
        }

        [Test]
        public void ReachingMax_EntersBinge_AndRaisesEvent()
        {
            var m = Make(90f);
            bool? binge = null;
            m.BingeChanged += b => binge = b;
            m.Eat();
            Assert.IsTrue(m.IsBinge);
            Assert.AreEqual(true, binge);
        }

        [Test]
        public void SelfListen_OnlyInBinge_DropsToSeventy_ThenCoolsDown()
        {
            var m = Make(50f);
            Assert.IsFalse(m.TrySelfListen(), "not in binge");

            m.ResetTo(100f);
            Assert.IsTrue(m.TrySelfListen());
            Assert.AreEqual(70f, m.Value, 0.001f);
            Assert.IsFalse(m.IsBinge);

            m.Eat(); m.Eat();                    // 70 → 100：吃回失控（ResetTo 是死亡重試用，會清冷卻）
            Assert.IsTrue(m.IsBinge);
            Assert.IsFalse(m.TrySelfListen(), "still cooling down");
            m.Tick(10f, drifting: false, listening: false);
            Assert.IsTrue(m.TrySelfListen(), "cooldown over");
        }

        [Test]
        public void WrongCard_AddsTen()
        {
            var m = Make(40f);
            m.WrongCard();
            Assert.AreEqual(50f, m.Value, 0.001f);
        }
    }
}
