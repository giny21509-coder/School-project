using System;

namespace JianQi.Chapters
{
    /// <summary>
    /// 純 C# 的章節狀態機：只能往前（或停在原地），不依賴 Unity，可直接做單元測試。
    /// 存讀檔由 ChapterRunner 透過 ChapterProgress 處理，這裡只管規則。
    /// </summary>
    public sealed class ChapterStateMachine
    {
        public ChapterStage Current { get; private set; }

        /// <summary>(from, to)：階段改變時觸發。</summary>
        public event Action<ChapterStage, ChapterStage> Changed;

        public ChapterStateMachine(ChapterStage start = ChapterStage.Commission)
        {
            Current = start;
        }

        public bool IsComplete => Current == ChapterStage.Complete;

        /// <summary>前進到指定階段。往回或原地不動時回傳 false，不觸發事件。</summary>
        public bool TryAdvance(ChapterStage to)
        {
            if (to <= Current) return false;
            ChapterStage from = Current;
            Current = to;
            Changed?.Invoke(from, to);
            return true;
        }

        /// <summary>前進到下一個階段；已完成時回傳 false。</summary>
        public bool AdvanceNext()
        {
            return !IsComplete && TryAdvance(Current + 1);
        }

        /// <summary>讀檔用：直接還原（不觸發事件）。超出範圍的值會被夾回合法範圍。</summary>
        public void Restore(int savedStage)
        {
            if (savedStage < (int)ChapterStage.Commission) savedStage = (int)ChapterStage.Commission;
            if (savedStage > (int)ChapterStage.Complete) savedStage = (int)ChapterStage.Complete;
            Current = (ChapterStage)savedStage;
        }
    }
}
