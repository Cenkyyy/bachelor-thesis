using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private float _defaultSpeed = 8f;
    [SerializeField] private float _defaultLifetimeSeconds = 4f;

    [Header("Impact")]
    [SerializeField] private LayerMask _damageMask;

    private EnemyCore _owner;
    private int _damage;
    private float _expireAt;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (Time.time >= _expireAt)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(EnemyCore owner, Vector2 origin, Vector2 direction, int damage, float speed, float lifetimeSeconds)
    {
        _owner = owner;
        _damage = Mathf.Max(0, damage);
        transform.position = origin;

        var finalDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        var finalSpeed = speed > 0f ? speed : _defaultSpeed;
        _expireAt = Time.time + (lifetimeSeconds > 0f ? lifetimeSeconds : _defaultLifetimeSeconds);

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = finalDirection * finalSpeed;
        }

        IgnoreOwnerCollision();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_damageMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.CanReceiveDamage)
        {
            return;
        }

        damageable.ReceiveDamage(_damage, _owner);
        Destroy(gameObject);
    }

    private void IgnoreOwnerCollision()
    {
        if (_owner == null)
        {
            return;
        }

        var ownerColliders = _owner.GetComponentsInChildren<Collider2D>();
        var projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider == null)
        {
            return;
        }

        foreach (var ownerCollider in ownerColliders)
        {
            if (ownerCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
        }
    }
}
