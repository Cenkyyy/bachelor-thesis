using UnityEngine;

/// <summary>
/// Moves an enemy projectile, applies damage once on impact, and expires it after its lifetime.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D _collider;

    [Header("Movement")]
    [SerializeField] private float _defaultSpeed = 8f;
    [SerializeField] private float _defaultLifetimeSeconds = 4f;

    [Header("Impact")]
    [SerializeField] private LayerMask _damageMask;
    [SerializeField] private LayerMask _blockerMask;

    private EnemyCore _owner;
    private int _damage;
    private float _expireAt;
    private bool _hasHit;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();

        if (_collider == null)
            _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Time.time >= _expireAt)
            Destroy(gameObject);
    }

    public void Launch(EnemyCore owner, Vector2 origin, Vector2 direction, int damage, float speed, float lifetimeSeconds)
    {
        _owner = owner;
        _damage = Mathf.Max(0, damage);
        _hasHit = false;
        if (_collider != null)
            _collider.enabled = true;

        transform.position = origin;

        Vector2 finalDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        float finalSpeed = speed > 0f ? speed : _defaultSpeed;
        _expireAt = Time.time + (lifetimeSeconds > 0f ? lifetimeSeconds : _defaultLifetimeSeconds);
        transform.up = finalDirection;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = finalDirection * finalSpeed;

        IgnoreOwnerCollision();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit)
            return;

        if (IsInLayerMask(other.gameObject.layer, _blockerMask))
        {
            DestroyProjectile();
            return;
        }

        if ((_damageMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (_owner != null && other.transform.IsChildOf(_owner.transform))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.CanReceiveDamage)
            return;

        _hasHit = true;
        damageable.ReceiveDamage(_damage, _owner);
        DestroyProjectile();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasHit)
            return;

        if (IsInLayerMask(collision.collider.gameObject.layer, _blockerMask))
            DestroyProjectile();
    }

    private void IgnoreOwnerCollision()
    {
        if (_owner == null)
            return;

        Collider2D[] ownerColliders = _owner.GetComponentsInChildren<Collider2D>();
        Collider2D projectileCollider = _collider;
        if (projectileCollider == null)
            return;

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider == null)
                continue;

            Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
        }
    }

    private void DestroyProjectile()
    {
        _hasHit = true;
        if (_collider != null)
            _collider.enabled = false;

        Destroy(gameObject);
    }

    private static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
