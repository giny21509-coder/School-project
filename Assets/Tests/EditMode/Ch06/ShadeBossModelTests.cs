using JianQi.Ch06;
using NUnit.Framework;

namespace JianQi.Ch06.Tests
{
    /// <summary>設計文件 §8 驗收 7、8：正確／錯誤／保底三條路徑、三回合必定結束、結局 CG 數。</summary>
    public class ShadeBossModelTests
    {
        private static readonly string[] Correct = { "D1", "D2", "D3" };

        private static ShadeBossModel Make(params string[] unlocked) => new ShadeBossModel(Correct, unlocked);

        [Test]
        public void AllCorrect_FinishesWithThreeCgs()
        {
            var b = Make("D1", "D2", "D3");
            foreach (var id in Correct)
            {
                Assert.IsTrue(b.BeginChoosing());
                Assert.AreEqual(ChoiceResult.Correct, b.Choose(id));
            }
            Assert.IsTrue(b.IsFinished);
            Assert.AreEqual(3, b.EndingCgCount);
        }

        [Test]
        public void Wrong_RepeatsAttack_SameRound()
        {
            var b = Make("D1", "D2", "D3");
            b.BeginChoosing();
            Assert.AreEqual(ChoiceResult.Wrong, b.Choose("D2"));
            Assert.AreEqual(ShadePhase.Attack, b.Phase);
            Assert.AreEqual(0, b.Round);
            Assert.AreEqual(1, b.WrongCount);
        }

        [Test]
        public void Fallback_OnlyOfferedWhenCorrectDraftMissing()
        {
            var b = Make("D2");
            b.BeginChoosing();
            CollectionAssert.Contains(b.GetOptions(), ShadeBossModel.FallbackId, "round 1 needs D1, missing");
            Assert.AreEqual(ChoiceResult.Fallback, b.Choose(ShadeBossModel.FallbackId));

            b.BeginChoosing();
            CollectionAssert.DoesNotContain(b.GetOptions(), ShadeBossModel.FallbackId, "round 2 has D2");
            Assert.AreEqual(ChoiceResult.Invalid, b.Choose(ShadeBossModel.FallbackId));
        }

        [Test]
        public void NoDrafts_StillFinishes_WithZeroCgs()
        {
            var b = Make();
            for (int i = 0; i < 3; i++)
            {
                b.BeginChoosing();
                Assert.AreEqual(ChoiceResult.Fallback, b.Choose(ShadeBossModel.FallbackId));
            }
            Assert.IsTrue(b.IsFinished);
            Assert.AreEqual(0, b.EndingCgCount);
        }

        [Test]
        public void LockedDraft_IsInvalid()
        {
            var b = Make("D1");
            b.BeginChoosing();
            Assert.AreEqual(ChoiceResult.Invalid, b.Choose("D3"));
            Assert.AreEqual(ShadePhase.Choosing, b.Phase);
        }

        [Test]
        public void ChooseOutsideChoosingPhase_IsInvalid()
        {
            var b = Make("D1");
            Assert.AreEqual(ChoiceResult.Invalid, b.Choose("D1"));
        }

        [Test]
        public void Restore_KeepsClearedRounds_AndFallbackMask()
        {
            var b = Make("D1", "D2", "D3");
            b.Restore(2, 0b01);
            Assert.AreEqual(2, b.Round);
            Assert.AreEqual(ShadePhase.Attack, b.Phase);
            b.BeginChoosing();
            b.Choose("D3");
            Assert.IsTrue(b.IsFinished);
            Assert.AreEqual(2, b.EndingCgCount, "round 1 used fallback");
        }
    }
}
