using UnityEngine;

public class SpellVfxInstance : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _elapsed;

    public void Initialize(Vector2 direction, float speed, float lifetime, Material elementMaterial)
    {
        _direction = direction.normalized;
        _speed = Mathf.Max(0f, speed);
        _lifetime = Mathf.Max(0.01f, lifetime);

        transform.right = _direction;

        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && elementMaterial != null)
            renderer.material = elementMaterial;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));

        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }
}
