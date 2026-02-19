using UnityEngine;

public class SpellVfxInstance : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _elapsed;

    public void Initialize(Vector2 direction, float speed, float lifetime, Color tint, Vector3 scale)
    {
        _direction = direction.normalized;
        _speed = Mathf.Max(0f, speed);
        _lifetime = Mathf.Max(0.01f, lifetime);

        transform.localScale = scale;
        transform.right = _direction;

        var renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.color = tint;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));

        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }
}
