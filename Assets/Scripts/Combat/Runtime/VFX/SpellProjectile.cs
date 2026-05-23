using UnityEngine;

/// <summary>
/// Moves and expires one spawned spell projectile visual.
/// </summary>
public sealed class SpellProjectile : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private LayerMask _despawnLayerMask;

    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _maxDistance;
    private float _elapsed;
    private float _traveledDistance;

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float stepDistance = _speed * Time.deltaTime;
        transform.position += (Vector3)(_direction * stepDistance);
        _traveledDistance += stepDistance;

        if (_traveledDistance >= _maxDistance || _elapsed >= _lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_despawnLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        Destroy(gameObject);
    }

    public void Initialize(Vector2 direction, float speed, float lifetime, float maxDistance, LayerMask despawnLayerMask, Material elementMaterial)
    {
        _direction = direction.normalized;
        _speed = Mathf.Max(0f, speed);
        _lifetime = Mathf.Max(0.01f, lifetime);
        _maxDistance = Mathf.Max(0.01f, maxDistance);
        _despawnLayerMask = despawnLayerMask;
        _elapsed = 0f;
        _traveledDistance = 0f;

        transform.up = _direction;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && elementMaterial != null)
            spriteRenderer.material = elementMaterial;
    }
}
