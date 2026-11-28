using System;

namespace JianQi.Ch06
{
    /// <summary>空虛值的規則數值（由 GluttonyConfig 轉成這個純資料類別，測試時可直接 new）。</summary>
    public sealed class EmptinessRules
    {
        public float Max = 100f;
        public float StartValue = 40f;
        public float EatGain = 15f;
        public float ListenLoss = 20f;
        public float DriftPerSecond = 0.5f;
        public float WrongCardGain = 10f;
        public float SelfListenLoss = 30f;
        public float SelfListenCooldown = 10f;
    }

    /// <summary>
    /// 空虛值 E（設計文件 §4.1、§4.4）。
    /// 吃東西 +、聽完回音 −、Zone B/C 自然上升（傾聽時暫停）、E 到頂 = 失控、失控時可「傾聽自己」。
    /// 純 C#，不依賴 Unity。
    /// </summary>
    public sealed class EmptinessModel
    {
        private readonly EmptinessRules rules;
        private float selfListenCooldownLeft;

        public float Value { get; private set; }
        public float Normalized => rules.Max > 0f ? Value / rules.Max : 0f;
        public bool IsBinge => Value >= rules.Max;
        public float SelfListenCooldownLeft => selfListenCooldownLeft;
        public bool CanSelfListen => IsBinge && selfListenCooldownLeft <= 0f;

        /// <summary>新的 E 值。</summary>
        public event Action<float> Changed;
        /// <summary>true = 進入失控，false = 解除失控。</summary>
        public event Action<bool> BingeChanged;

        public EmptinessModel(EmptinessRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Value = Clamp(rules.StartValue);
        }

        /// <summary>吃下一份食物（場上或背包）。E 已經到頂時維持不變。</summary>
        public void Eat() => Add(rules.EatGain);

        /// <summary>聽完一個回音。</summary>
        public void CompleteEcho() => Add(-rules.ListenLoss);

        /// <summary>最終戰選錯草稿。</summary>
        public void WrongCard() => Add(rules.WrongCardGain);

        /// <summary>每幀呼叫。drifting：是否在會自然上升的 Zone；listening：正在傾聽時暫停上升。</summary>
        public void Tick(float deltaTime, bool drifting, bool listening)
        {
            if (deltaTime <= 0f) return;
            if (selfListenCooldownLeft > 0f)
                selfListenCooldownLeft = Math.Max(0f, selfListenCooldownLeft - deltaTime);
            if (drifting && !listening) Add(rules.DriftPerSecond * deltaTime);
        }

        /// <summary>失控時原地按住傾聽完成後呼叫。不在失控或冷卻中時回傳 false。</summary>
        public bool TrySelfListen()
        {
            if (!CanSelfListen) return false;
            selfListenCooldownLeft = rules.SelfListenCooldown;
            Add(-rules.SelfListenLoss);
            return true;
        }

        /// <summary>死亡或讀檔時回到 Zone 起點的值（設計文件 §6）。</summary>
        public void ResetTo(float value)
        {
            selfListenCooldownLeft = 0f;
            Set(Clamp(value));
        }

        private void Add(float delta) => Set(Clamp(Value + delta));

        private void Set(float next)
        {
            if (Math.Abs(next - Value) < 0.0001f) return;
            bool wasBinge = IsBinge;
            Value = next;
            Changed?.Invoke(Value);
            if (wasBinge != IsBinge) BingeChanged?.Invoke(IsBinge);
        }

        private float Clamp(float v) => v < 0f ? 0f : (v > rules.Max ? rules.Max : v);
    }
}
