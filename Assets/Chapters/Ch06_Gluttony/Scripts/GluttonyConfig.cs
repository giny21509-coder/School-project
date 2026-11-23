using JianQi.Ch06;
using UnityEngine;

/// <summary>第六章所有可調數值（設計文件 §7）。建立：Create → JianQi/Ch06/Gluttony Config。</summary>
[CreateAssetMenu(menuName = "JianQi/Ch06/Gluttony Config", fileName = "GluttonyConfig")]
public class GluttonyConfig : ScriptableObject
{
    [Header("空虛值 E")]
    [Range(0, 100)] public float startEmptiness = 40f;
    [Range(0, 50)] public float eatEmptinessGain = 15f;
    [Range(0, 60)] public float listenEmptinessLoss = 20f;
    [Range(0, 3)] public float driftPerSecond = 0.5f;
    [Range(0, 40)] public float wrongCardEmptinessGain = 10f;

    [Header("進食")]
    [Range(0, 100)] public float eatHeal = 20f;
    [Range(1, 9)] public int pouchCapacity = 3;

    [Header("傾聽")]
    [Range(0.5f, 4f)] public float lineDuration = 1.5f;
    [Min(0.1f)] public float listenRadius = 1.6f;

    [Header("失控（E = 100）")]
    [Range(1, 6)] public float selfListenHold = 2.5f;
    [Range(10, 60)] public float selfListenLoss = 30f;
    [Range(0, 30)] public float selfListenCooldown = 10f;
    [Range(0.3f, 1f)] public float bingeMoveMultiplier = 0.7f;
    [Min(0)] public float bingePullRadius = 4f;
    [Min(0)] public float bingePullSpeed = 2f;

    [Header("填充怪生成（§5）")]
    [Range(0.5f, 8f)] public float spawnIntervalBase = 3f;
    [Range(1, 10)] public int maxAliveBase = 3;

    [Header("黑霧與嘴")]
    [Range(0, 1)] public float mistMin = 0.3f;
    [Range(0, 1)] public float mistMax = 0.8f;
    [Range(0, 100)] public float mawPullThreshold = 60f;
    [Min(0)] public float mawPullSpeed = 1.5f;

    [Header("最終戰")]
    [Range(5, 30)] public float bossAttackBase = 10f;

    [Header("S2 顯形拍攝")]
    [Range(0.5f, 3f)] public float pauseHitWindow = 1.2f;
    [Min(0.1f)] public float eatingDurationMin = 2.5f;
    [Min(0.1f)] public float eatingDurationMax = 4f;

    public EmptinessRules ToRules() => new EmptinessRules
    {
        Max = 100f,
        StartValue = startEmptiness,
        EatGain = eatEmptinessGain,
        ListenLoss = listenEmptinessLoss,
        DriftPerSecond = driftPerSecond,
        WrongCardGain = wrongCardEmptinessGain,
        SelfListenLoss = selfListenLoss,
        SelfListenCooldown = selfListenCooldown,
    };

    // ---- §5 公式（e01 = E / 100） ----
    public float SpawnInterval(float e01) => spawnIntervalBase * (1f - 0.5f * Mathf.Clamp01(e01));
    public int MaxAlive(float emptiness) => maxAliveBase + Mathf.FloorToInt(Mathf.Clamp(emptiness, 0f, 100f) / 25f);
    public float MistDensity(float e01) => Mathf.Lerp(mistMin, mistMax, Mathf.Clamp01(e01));
    public float MawPull(float emptiness) =>
        emptiness < mawPullThreshold ? 0f : mawPullSpeed * Mathf.Clamp01((emptiness - mawPullThreshold) / Mathf.Max(1f, 100f - mawPullThreshold));
    public float BossAttackDuration(float e01) => bossAttackBase + 5f * Mathf.Clamp01(e01);

    private void OnValidate()
    {
        if (mistMax < mistMin) mistMax = mistMin;
        if (eatingDurationMax < eatingDurationMin) eatingDurationMax = eatingDurationMin;
    }
}
