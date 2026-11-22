using System;

/// <summary>One exploration's normal-monster drop counter and stored healing items.</summary>
public sealed class HealingSuppliesState
{
    public const int Capacity = 3;
    private readonly int minimumKills;
    private readonly int maximumKills;
    private readonly Func<int, int, int> chooseThreshold;

    public int StoredCount { get; private set; }
    public int KillsSinceDrop { get; private set; }
    public int NextDropAt { get; private set; }

    public HealingSuppliesState(int minimumKills, int maximumKills,
        Func<int, int, int> chooseThreshold)
    {
        if (minimumKills < 1 || maximumKills < minimumKills || maximumKills == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(minimumKills));
        this.minimumKills = minimumKills;
        this.maximumKills = maximumKills;
        this.chooseThreshold = chooseThreshold ?? throw new ArgumentNullException(nameof(chooseThreshold));
        ChooseNextThreshold();
    }

    public bool RegisterNormalDefeat()
    {
        KillsSinceDrop++;
        if (KillsSinceDrop < NextDropAt) return false;
        KillsSinceDrop = 0;
        ChooseNextThreshold();
        return true;
    }

    public bool Store(int amount)
    {
        if (amount <= 0 || amount > Capacity - StoredCount) return false;
        StoredCount += amount;
        return true;
    }

    public bool TryConsume()
    {
        if (StoredCount == 0) return false;
        StoredCount--;
        return true;
    }

    private void ChooseNextThreshold()
    {
        NextDropAt = Math.Max(minimumKills,
            Math.Min(maximumKills, chooseThreshold(minimumKills, maximumKills + 1)));
    }
}
