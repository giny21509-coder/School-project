using System;

namespace JianQi.Ch06
{
    /// <summary>
    /// 一個回音的傾聽進度（設計文件 §4.3）。
    /// 按住時累積；受傷只讓「目前這一句」重來，已聽完的句子保留；被雜訊怪蓋住時不能聽。
    /// 純 C#，不依賴 Unity。
    /// </summary>
    public sealed class ListenModel
    {
        private readonly float lineDuration;
        private float lineProgress;

        public int LineCount { get; }
        public int CompletedLines { get; private set; }
        public bool IsComplete => CompletedLines >= LineCount;
        public bool Muted { get; private set; }
        /// <summary>目前這一句的進度 0..1。</summary>
        public float CurrentLineProgress => IsComplete ? 1f : lineProgress / lineDuration;
        /// <summary>整個回音的進度 0..1（給 UI 用）。</summary>
        public float TotalProgress => IsComplete ? 1f : (CompletedLines + CurrentLineProgress) / LineCount;

        /// <summary>參數：剛聽完的句子索引（0 開始）。</summary>
        public event Action<int> LineCompleted;
        public event Action Completed;

        public ListenModel(int lineCount, float lineDuration)
        {
            if (lineCount < 1) throw new ArgumentOutOfRangeException(nameof(lineCount));
            if (lineDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(lineDuration));
            LineCount = lineCount;
            this.lineDuration = lineDuration;
        }

        /// <summary>按住傾聽時每幀呼叫。回傳這次是否有進度。</summary>
        public bool Hold(float deltaTime)
        {
            if (IsComplete || Muted || deltaTime <= 0f) return false;
            lineProgress += deltaTime;
            while (lineProgress >= lineDuration && !IsComplete)
            {
                lineProgress -= lineDuration;
                CompletedLines++;
                LineCompleted?.Invoke(CompletedLines - 1);
                if (IsComplete)
                {
                    lineProgress = 0f;
                    Completed?.Invoke();
                }
            }
            return true;
        }

        /// <summary>受傷或放開按鍵：目前這一句從頭開始。</summary>
        public void Interrupt() => lineProgress = 0f;

        /// <summary>被雜訊怪蓋住（true）或解除（false）。蓋住時也會中斷目前這一句。</summary>
        public void SetMuted(bool muted)
        {
            Muted = muted;
            if (muted) Interrupt();
        }

        /// <summary>讀檔：已經聽完的回音直接標成完成（不觸發事件）。</summary>
        public void RestoreCompleted()
        {
            CompletedLines = LineCount;
            lineProgress = 0f;
        }
    }
}
