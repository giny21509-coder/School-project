using JianQi.Chapters;
using NUnit.Framework;

namespace JianQi.Ch06.Tests
{
    /// <summary>章節共用層：只能往前、讀檔還原、範圍夾限。</summary>
    public class ChapterStateMachineTests
    {
        [Test]
        public void OnlyAdvancesForward()
        {
            var m = new ChapterStateMachine();
            Assert.IsTrue(m.TryAdvance(ChapterStage.InnerWorld));
            Assert.IsFalse(m.TryAdvance(ChapterStage.Investigation));
            Assert.IsFalse(m.TryAdvance(ChapterStage.InnerWorld));
            Assert.AreEqual(ChapterStage.InnerWorld, m.Current);
        }

        [Test]
        public void ChangedEvent_ReportsFromAndTo()
        {
            var m = new ChapterStateMachine();
            ChapterStage from = ChapterStage.Complete, to = ChapterStage.Commission;
            m.Changed += (f, t) => { from = f; to = t; };
            m.AdvanceNext();
            Assert.AreEqual(ChapterStage.Commission, from);
            Assert.AreEqual(ChapterStage.Investigation, to);
        }

        [Test]
        public void Restore_ClampsOutOfRange_AndRaisesNoEvent()
        {
            var m = new ChapterStateMachine();
            bool fired = false;
            m.Changed += (f, t) => fired = true;
            m.Restore(99);
            Assert.AreEqual(ChapterStage.Complete, m.Current);
            Assert.IsFalse(m.AdvanceNext());
            m.Restore(-3);
            Assert.AreEqual(ChapterStage.Commission, m.Current);
            Assert.IsFalse(fired);
        }
    }
}
