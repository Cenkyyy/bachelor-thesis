using UnityEngine;

public interface IMineableTarget
{
    Vector3 WorldPosition { get; }

    bool CanBeMinedWith(MiningToolContext tool);
    void ShowHigherToolRequiredFeedback();
    void NotifyMiningStarted();
    void ApplyMiningDamage(float basePower, Player miner, WorldItemSpawner dropSpawner);
    void NotifyMiningStopped();
    bool IsSameTarget(IMineableTarget other);
}
