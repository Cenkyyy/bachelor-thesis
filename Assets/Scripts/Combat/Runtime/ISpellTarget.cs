using UnityEngine;

public interface ISpellTarget
{
    Vector2 Position { get; }
    bool IsAlive { get; }
    bool HasAnyNegativeStatus { get; }

    void ReceiveSpellDamage(float amount, object source = null);
    void ApplyStatus(CombatStatusEffect effect, float durationSeconds);
    void ApplyDamageOverTime(float damagePerSecond, float durationSeconds, object source = null);
    void AddStunBuildup(float amount, float threshold, float stunDurationSeconds);
}
