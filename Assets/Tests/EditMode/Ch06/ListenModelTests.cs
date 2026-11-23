using JianQi.Ch06;
using NUnit.Framework;

namespace JianQi.Ch06.Tests
{
    /// <summary>設計文件 §8 驗收 2、5：中斷只重來目前這一句、蓋住時不能聽、3 句完成。</summary>
    public class ListenModelTests
    {
        [Test]
        public void ThreeLines_CompleteAfterFourPointFiveSeconds()
        {
            var l = new ListenModel(3, 1.5f);
            int completedEvents = 0;
            l.Completed += () => completedEvents++;
            l.Hold(4.4f);
            Assert.IsFalse(l.IsComplete);
            l.Hold(0.2f);
            Assert.IsTrue(l.IsComplete);
            Assert.AreEqual(1, completedEvents);
        }

        [Test]
        public void Interrupt_RestartsOnlyCurrentLine()
        {
            var l = new ListenModel(3, 1.5f);
            l.Hold(1.5f);   // 第 1 句完成
            l.Hold(1.0f);   // 第 2 句聽到一半
            l.Interrupt();  // 受傷
            Assert.AreEqual(1, l.CompletedLines);
            Assert.AreEqual(0f, l.CurrentLineProgress, 0.001f);
        }

        [Test]
        public void Muted_BlocksProgress_UntilUnmuted()
        {
            var l = new ListenModel(3, 1.5f);
            l.Hold(1f);
            l.SetMuted(true);
            Assert.AreEqual(0f, l.CurrentLineProgress, 0.001f, "muting interrupts the line");
            Assert.IsFalse(l.Hold(5f));
            Assert.AreEqual(0, l.CompletedLines);
            l.SetMuted(false);
            l.Hold(1.5f);
            Assert.AreEqual(1, l.CompletedLines);
        }

        [Test]
        public void LineCompleted_FiresOncePerLine_InOrder()
        {
            var l = new ListenModel(3, 1f);
            var seen = new System.Collections.Generic.List<int>();
            l.LineCompleted += i => seen.Add(i);
            l.Hold(10f);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, seen);
        }

        [Test]
        public void RestoreCompleted_IsCompleteWithoutEvents()
        {
            var l = new ListenModel(3, 1.5f);
            bool fired = false;
            l.Completed += () => fired = true;
            l.RestoreCompleted();
            Assert.IsTrue(l.IsComplete);
            Assert.IsFalse(fired);
        }
    }
}
