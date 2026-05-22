/// <summary>
/// Contract for objects that can receive direct health damage.
/// </summary>
public interface IDamageable
{
    bool CanReceiveDamage { get; }
    void ReceiveDamage(int amount, object source = null);
}
