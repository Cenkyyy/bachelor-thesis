using UnityEngine;

public interface ICombatTarget
{
    // TODO(combat/enemies): Keep this interface as the contract between spell runtime and
    // enemy actors architecture once FSM combat actors are implemented
    Vector2 Position { get; }
    bool IsAlive { get; }
    bool HasAnyNegativeStatus { get; }

    void ReceiveSpellDamage(float amount);
    void ApplyStatus(CombatStatusEffect effect, float durationSeconds);
    void ApplyDamageOverTime(float damagePerSecond, float durationSeconds);
    void AddStunBuildup(float amount, float threshold, float stunDurationSeconds);
}
