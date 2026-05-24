using UnityEngine;

/// <summary>
/// Shared runtime contract for anything the mining system can damage and harvest.
/// </summary>
public interface IMineableTarget
{
    Vector3 WorldPosition { get; }
    float MiningProgressNormalized { get; }
    bool HasDamage { get; }
    bool IsDepleted { get; }
    bool IsBeingMined { get; }

    bool CanBeMinedWith(MiningToolState tool);
    void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner spawner);
    void NotifyMiningStarted();
    void NotifyMiningStopped();
    bool IsSameTarget(IMineableTarget other);
    void ShowHigherToolRequiredFeedback();
}
