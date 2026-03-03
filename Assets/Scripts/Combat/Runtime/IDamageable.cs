public interface IDamageable
{
    bool CanReceiveDamage { get; }
    void ReceiveDamage(int amount, object source = null);
}
