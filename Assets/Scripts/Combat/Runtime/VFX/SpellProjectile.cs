using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves, expires, and applies collider-driven hits for one spawned spell projectile.
/// </summary>
public sealed class SpellProjectile : MonoBehaviour
{
    public enum HitMode
    {
        SingleImpact,
        Piercing,
        AreaOnce,
        BeamTick
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D[] _hitboxColliders;

    private readonly HashSet<ICombatTarget> _hitTargets = new();
    private readonly HashSet<ICombatTarget> _beamTickTargets = new();
    private readonly List<Collider2D> _beamTargetBuffer = new(32);

    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _maxDistance;
    private float _elapsed;
    private float _traveledDistance;
    private LayerMask _obstructionMask;
    private HitMode _hitMode;
    private PlayerSpellCombatController _spellCombatController;
    private SpellCastRuntimeData _runtimeData;
    private ContactFilter2D _beamTargetFilter;
    private float _beamTickInterval;
    private float _beamTickAccumulator;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float stepDistance = _speed * Time.deltaTime;
        if (_rigidbody == null)
            transform.position += (Vector3)(_direction * stepDistance);

        _traveledDistance += stepDistance;

        if (_hitMode == HitMode.BeamTick)
            TickBeamTargets();

        if (_traveledDistance >= _maxDistance || _elapsed >= _lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_obstructionMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if (_spellCombatController == null || _runtimeData == null || _hitMode == HitMode.BeamTick)
            return;

        bool hit = _spellCombatController.TryApplyProjectileHit(_runtimeData, other, _hitTargets);
        if (hit && _hitMode == HitMode.SingleImpact)
            Destroy(gameObject);
    }

    public void Initialize(
        Vector2 direction,
        float speed,
        float lifetime,
        float maxDistance,
        LayerMask obstructionMask,
        Material elementMaterial,
        PlayerSpellCombatController spellCombatController,
        SpellCastRuntimeData runtimeData,
        HitMode hitMode)
    {
        _direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0f, speed);
        _lifetime = Mathf.Max(0.01f, lifetime);
        _maxDistance = Mathf.Max(0.01f, maxDistance);
        _obstructionMask = obstructionMask;
        _spellCombatController = spellCombatController;
        _runtimeData = runtimeData;
        _hitMode = hitMode;
        _elapsed = 0f;
        _traveledDistance = 0f;
        _hitTargets.Clear();
        _beamTickTargets.Clear();
        _beamTickAccumulator = ResolveInitialBeamTickAccumulator();

        transform.up = _direction;

        ConfigurePhysics();
        ConfigureCollider();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && elementMaterial != null)
            spriteRenderer.material = elementMaterial;
    }

    private void ConfigurePhysics()
    {
        if (_hitboxColliders == null)
            _hitboxColliders = new Collider2D[0];

        for (var i = 0; i < _hitboxColliders.Length; i++)
        {
            if (_hitboxColliders[i] != null)
                _hitboxColliders[i].isTrigger = true;
        }

        if (_rigidbody == null)
            return;

        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody.gravityScale = 0f;
        _rigidbody.linearVelocity = _direction * _speed;
    }

    private void ConfigureCollider()
    {
        if (_runtimeData == null || _runtimeData.Form == null || _hitboxColliders == null || _hitboxColliders.Length == 0)
            return;

        if (_hitboxColliders[0] is not BoxCollider2D boxCollider)
            return;

        FormWordData form = _runtimeData.Form;
        if (form.Type == FormWordType.Wave)
            return;

        float diameter = Mathf.Max(0.01f, form.HitRadius * 2f);

        switch (form.Type)
        {
            case FormWordType.Shard:
            case FormWordType.Barrage:
                boxCollider.size = new Vector2(diameter, diameter);
                boxCollider.offset = Vector2.zero;
                break;

            case FormWordType.Beam:
                boxCollider.size = new Vector2(diameter, _maxDistance);
                boxCollider.offset = Vector2.up * (_maxDistance * 0.5f);
                break;
        }
    }

    private float ResolveInitialBeamTickAccumulator()
    {
        if (_spellCombatController == null || _runtimeData == null || _runtimeData.Form == null || _runtimeData.Form.Type != FormWordType.Beam)
            return 0f;

        _beamTickInterval = Mathf.Max(0.01f, _runtimeData.Form.BeamTickInterval);
        _beamTargetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _spellCombatController.TargetMask,
            useTriggers = true
        };

        return _beamTickInterval;
    }

    private void TickBeamTargets()
    {
        if (_spellCombatController == null || _runtimeData == null || _hitboxColliders == null)
            return;

        _beamTickAccumulator += Time.deltaTime;
        if (_beamTickAccumulator < _beamTickInterval)
            return;

        _beamTickAccumulator -= _beamTickInterval;
        _beamTickTargets.Clear();

        for (var i = 0; i < _hitboxColliders.Length; i++)
        {
            if (_hitboxColliders[i] == null)
                continue;

            _beamTargetBuffer.Clear();
            _hitboxColliders[i].Overlap(_beamTargetFilter, _beamTargetBuffer);

            for (var targetIndex = 0; targetIndex < _beamTargetBuffer.Count; targetIndex++)
                _spellCombatController.TryApplyProjectileHit(_runtimeData, _beamTargetBuffer[targetIndex], _beamTickTargets);
        }
    }
}
