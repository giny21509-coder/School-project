using System;
using System.Collections.Generic;

namespace JianQi.Ch06
{
    public enum ShadePhase { Attack, Choosing, Finished }

    public enum ChoiceResult
    {
        Correct,   // 正確草稿：進入下一回合
        Fallback,  // 保底卡「我在聽。」：也進入下一回合，但缺一張結局 CG
        Wrong,     // 選錯：E +10，重打一次攻擊段
        Invalid,   // 不在選單上的選項（未解鎖、不是選擇階段）
    }

    /// <summary>
    /// 最終戰「暴食之影」的回合規則（設計文件 §3 S4、§8 驗收 7–8）。
    /// 每回合：攻擊段 → 停頓選草稿。選對或保底 → 下一回合；選錯 → 再打一次攻擊段。
    /// 保底卡只在「這一回合的正確草稿沒有解鎖」時出現。沒有壞結局：三回合後必定結束。
    /// 純 C#，不依賴 Unity。
    /// </summary>
    public sealed class ShadeBossModel
    {
        public const string FallbackId = "FALLBACK";

        private readonly string[] correctPerRound;
        private readonly HashSet<string> unlocked;

        public int RoundCount => correctPerRound.Length;
        /// <summary>目前回合（0 開始）；結束後等於 RoundCount。</summary>
        public int Round { get; private set; }
        public ShadePhase Phase { get; private set; } = ShadePhase.Attack;
        public int WrongCount { get; private set; }
        /// <summary>第 n 位元 = 第 n 回合用了保底卡。</summary>
        public int FallbackMask { get; private set; }
        public bool IsFinished => Phase == ShadePhase.Finished;

        /// <summary>結局 CG 數 = 用正確草稿通過的回合數。</summary>
        public int EndingCgCount
        {
            get
            {
                int passed = Math.Min(Round, RoundCount), fallbacks = 0;
                for (int i = 0; i < passed; i++) if ((FallbackMask & (1 << i)) != 0) fallbacks++;
                return passed - fallbacks;
            }
        }

        public event Action<int> RoundCleared;   // 剛通過的回合（0 開始）
        public event Action Finished;

        /// <param name="correctDraftPerRound">每回合的正確草稿 ID，例如 { "D1", "D2", "D3" }。</param>
        /// <param name="unlockedDraftIds">現場調查時解鎖的草稿 ID。</param>
        public ShadeBossModel(IList<string> correctDraftPerRound, IEnumerable<string> unlockedDraftIds)
        {
            if (correctDraftPerRound == null || correctDraftPerRound.Count == 0)
                throw new ArgumentException("至少需要一個回合", nameof(correctDraftPerRound));
            if (correctDraftPerRound.Count > 30)
                throw new ArgumentException("回合數過多（FallbackMask 為 int）", nameof(correctDraftPerRound));
            correctPerRound = new string[correctDraftPerRound.Count];
            correctDraftPerRound.CopyTo(correctPerRound, 0);
            unlocked = new HashSet<string>(unlockedDraftIds ?? Array.Empty<string>());
        }

        /// <summary>攻擊段結束、牠停下來啜泣時呼叫。</summary>
        public bool BeginChoosing()
        {
            if (Phase != ShadePhase.Attack) return false;
            Phase = ShadePhase.Choosing;
            return true;
        }

        /// <summary>這一回合選單上的選項：所有已解鎖草稿，加上（需要時）保底卡。</summary>
        public List<string> GetOptions()
        {
            var options = new List<string>();
            if (Phase != ShadePhase.Choosing) return options;
            foreach (var id in correctPerRound)
                if (unlocked.Contains(id) && !options.Contains(id)) options.Add(id);
            foreach (var id in unlocked)
                if (!options.Contains(id)) options.Add(id);
            if (!unlocked.Contains(correctPerRound[Round])) options.Add(FallbackId);
            return options;
        }

        public ChoiceResult Choose(string optionId)
        {
            if (Phase != ShadePhase.Choosing || optionId == null) return ChoiceResult.Invalid;
            if (!GetOptions().Contains(optionId)) return ChoiceResult.Invalid;

            if (optionId == FallbackId)
            {
                FallbackMask |= 1 << Round;
                ClearRound();
                return ChoiceResult.Fallback;
            }
            if (optionId == correctPerRound[Round])
            {
                ClearRound();
                return ChoiceResult.Correct;
            }
            WrongCount++;
            Phase = ShadePhase.Attack;
            return ChoiceResult.Wrong;
        }

        /// <summary>讀檔或死亡後回到目前回合的攻擊段（已完成的回合保留）。</summary>
        public void Restore(int round, int fallbackMask)
        {
            Round = Math.Max(0, Math.Min(round, RoundCount));
            FallbackMask = fallbackMask;
            Phase = Round >= RoundCount ? ShadePhase.Finished : ShadePhase.Attack;
        }

        private void ClearRound()
        {
            int cleared = Round;
            Round++;
            RoundCleared?.Invoke(cleared);
            if (Round >= RoundCount)
            {
                Phase = ShadePhase.Finished;
                Finished?.Invoke();
            }
            else Phase = ShadePhase.Attack;
        }
    }
}
