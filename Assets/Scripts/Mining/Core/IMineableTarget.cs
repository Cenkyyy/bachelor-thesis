using UnityEngine;

public interface IMineableTarget
{
    Vector3 WorldPosition { get; }

    bool CanBeMinedWith(MiningToolState tool);
    void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner spawner);
    void NotifyMiningStarted();
    void NotifyMiningStopped();
    bool IsSameTarget(IMineableTarget other);
    void ShowHigherToolRequiredFeedback();
}
