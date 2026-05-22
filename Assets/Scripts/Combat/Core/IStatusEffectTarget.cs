/// <summary>
/// Contract for combat targets that can receive and expose combat status effects.
/// </summary>
public interface IStatusEffectTarget
{
    bool HasAnyNegativeStatus { get; }
    bool HasStatus(CombatStatusEffectType effect);
    void ApplyStatus(CombatStatusEffectType effect, float durationSeconds);
}
