using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds and updates a tile-based beam spell that follows the cursor and applies tick damage.
/// </summary>
public sealed class SpellBeam : MonoBehaviour
{
    private enum SegmentType
    {
        Start,
        Middle,
        End
    }

    [Header("References")]
    [SerializeField] private BoxCollider2D _hitbox;
    [SerializeField] private SpriteRenderer _tileTemplate;
    [SerializeField] private RuntimeAnimatorController _tileAnimatorController;

    [Header("Tiles")]
    [SerializeField] private float _tileLength = 1f;
    [SerializeField] private string _startStateName = "Start";
    [SerializeField] private string _middleStateName = "Middle";
    [SerializeField] private string _endStateName = "End";

    private readonly List<GameObject> _tiles = new();
    private readonly List<SegmentType> _tileSegmentTypes = new();
    private readonly HashSet<ICombatTarget> _contactHitTargets = new();
    private readonly HashSet<ICombatTarget> _tickTargets = new();
    private readonly List<Collider2D> _targetBuffer = new(32);

    private PlayerHeldItemVisualController _originProvider;
    private Vector2 _fallbackOrigin;
    private float _angleOffsetDegrees;
    private float _currentLength;
    private float _targetLength;
    private float _elapsed;
    private float _tickAccumulator;
    private LayerMask _targetMask;
    private LayerMask _obstructionMask;
    private Material _elementMaterial;
    private ContactFilter2D _targetFilter;
    private PlayerSpellCombatController _spellCombatController;
    private SpellCastRuntimeData _runtimeData;

    public void Initialize(
        PlayerHeldItemVisualController originProvider,
        Vector2 fallbackOrigin,
        float angleOffsetDegrees,
        LayerMask targetMask,
        LayerMask obstructionMask,
        Material elementMaterial,
        PlayerSpellCombatController spellCombatController,
        SpellCastRuntimeData runtimeData)
    {
        _originProvider = originProvider;
        _fallbackOrigin = fallbackOrigin;
        _angleOffsetDegrees = angleOffsetDegrees;
        _targetMask = targetMask;
        _obstructionMask = obstructionMask;
        _elementMaterial = elementMaterial;
        _spellCombatController = spellCombatController;
        _runtimeData = runtimeData;
        _elapsed = 0f;
        _tickAccumulator = 0f;
        _currentLength = _tileLength;

        _targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _targetMask,
            useTriggers = true
        };

        if (_hitbox == null)
            _hitbox = GetComponent<BoxCollider2D>();

        if (_tileTemplate == null)
            _tileTemplate = GetComponent<SpriteRenderer>();

        if (_tileTemplate != null)
            _tileTemplate.enabled = false;

        if (_hitbox != null)
            _hitbox.isTrigger = true;

        transform.position = GetOrigin();
        UpdateDirection();
        UpdateTargetLength();
        ApplyLength();
        HitNewContactTargets();
    }

    private void Update()
    {
        if (_runtimeData == null || _runtimeData.Form == null)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed >= _runtimeData.Form.BeamDuration)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = GetOrigin();
        UpdateDirection();
        UpdateTargetLength();
        _currentLength = Mathf.MoveTowards(_currentLength, _targetLength, _runtimeData.Form.VfxSpeed * Time.deltaTime);
        ApplyLength();
        HitNewContactTargets();
        TickTargets();
    }

    private void UpdateDirection()
    {
        Vector2 forward = GetCursorDirection();
        if (!Mathf.Approximately(_angleOffsetDegrees, 0f))
            forward = Rotate(forward, _angleOffsetDegrees);

        transform.up = forward;
    }

    private void UpdateTargetLength()
    {
        int maxTileCount = Mathf.RoundToInt(_runtimeData.Form.Range / _tileLength);
        float maxLength = maxTileCount * _tileLength;
        float distance = maxLength;

        if (_obstructionMask.value != 0)
        {
            RaycastHit2D obstructionHit = Physics2D.Raycast(GetOrigin(), transform.up, maxLength, _obstructionMask);
            if (obstructionHit.collider != null)
                distance = Mathf.Min(distance, obstructionHit.distance);
        }

        if (_runtimeData.Modifier.Type != ModifierWordType.Piercing)
        {
            RaycastHit2D targetHit = Physics2D.CircleCast(GetOrigin(), _runtimeData.Form.HitRadius, transform.up, maxLength, _targetMask);
            if (targetHit.collider != null)
                distance = Mathf.Min(distance, targetHit.distance);
        }

        int targetTiles = Mathf.Clamp(Mathf.CeilToInt(distance / _tileLength), 1, maxTileCount);
        _targetLength = targetTiles * _tileLength;
    }

    private void ApplyLength()
    {
        int maxTileCount = Mathf.RoundToInt(_runtimeData.Form.Range / _tileLength);
        int tileCount = Mathf.Clamp(Mathf.CeilToInt(_currentLength / _tileLength), 1, maxTileCount);
        float visibleLength = tileCount * _tileLength;

        ApplyHitbox(visibleLength);
        ApplyTiles(tileCount);
    }

    private void ApplyHitbox(float visibleLength)
    {
        if (_hitbox == null)
            return;

        _hitbox.size = new Vector2(_hitbox.size.x, visibleLength);
        _hitbox.offset = new Vector2(_hitbox.offset.x, visibleLength * 0.5f);
    }

    private void ApplyTiles(int tileCount)
    {
        EnsureTileCapacity(tileCount);

        for (int i = 0; i < _tiles.Count; i++)
        {
            bool active = i < tileCount;
            _tiles[i].SetActive(active);
            if (!active)
            {
                _tileSegmentTypes[i] = (SegmentType)(-1);
                continue;
            }

            SegmentType segmentType = GetSegmentType(i, tileCount);
            ConfigureTile(_tiles[i], i, segmentType);
        }
    }

    private void EnsureTileCapacity(int tileCount)
    {
        while (_tiles.Count < tileCount)
        {
            GameObject tile = new("BeamTile");
            tile.transform.SetParent(transform, false);

            SpriteRenderer spriteRenderer = tile.AddComponent<SpriteRenderer>();
            ConfigureRenderer(spriteRenderer);

            if (_tileAnimatorController != null)
            {
                Animator animator = tile.AddComponent<Animator>();
                animator.runtimeAnimatorController = _tileAnimatorController;
            }

            _tiles.Add(tile);
            _tileSegmentTypes.Add((SegmentType)(-1));
        }
    }

    private void ConfigureTile(GameObject tile, int index, SegmentType segmentType)
    {
        tile.name = $"Beam{segmentType}Tile";
        tile.transform.localPosition = Vector3.up * (index * _tileLength + _tileLength * 0.5f);
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale = Vector3.one;

        Animator animator = tile.GetComponent<Animator>();
        if (animator != null && _tileSegmentTypes[index] != segmentType)
        {
            PlaySegmentState(animator, segmentType);
            _tileSegmentTypes[index] = segmentType;
        }
    }

    private void ConfigureRenderer(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;

        if (_tileTemplate != null)
        {
            spriteRenderer.sprite = _tileTemplate.sprite;
            spriteRenderer.color = _tileTemplate.color;
            spriteRenderer.sortingLayerID = _tileTemplate.sortingLayerID;
            spriteRenderer.sortingOrder = _tileTemplate.sortingOrder;
        }

        if (_elementMaterial != null)
            spriteRenderer.material = _elementMaterial;
    }

    private void PlaySegmentState(Animator animator, SegmentType segmentType)
    {
        string stateName = GetStateName(segmentType);
        if (string.IsNullOrWhiteSpace(stateName))
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash))
            animator.Play(stateHash, 0, 0f);
    }

    private void TickTargets()
    {
        if (_hitbox == null || _spellCombatController == null)
            return;

        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < _runtimeData.Form.BeamTickInterval)
            return;

        _tickAccumulator -= _runtimeData.Form.BeamTickInterval;
        _tickTargets.Clear();
        _targetBuffer.Clear();
        _hitbox.Overlap(_targetFilter, _targetBuffer);

        for (int i = 0; i < _targetBuffer.Count; i++)
        {
            _spellCombatController.TryApplyProjectileHit(_runtimeData, _targetBuffer[i], _tickTargets);
        }
    }

    private void HitNewContactTargets()
    {
        if (_hitbox == null || _spellCombatController == null)
            return;

        _targetBuffer.Clear();
        _hitbox.Overlap(_targetFilter, _targetBuffer);

        for (int i = 0; i < _targetBuffer.Count; i++)
        {
            _spellCombatController.TryApplyProjectileHit(_runtimeData, _targetBuffer[i], _contactHitTargets);
        }
    }

    private Vector2 GetCursorDirection()
    {
        Vector2 origin = GetOrigin();
        Vector2 mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : origin + Vector2.right;
        Vector2 forward = (mouseWorld - origin).normalized;
        if (forward == Vector2.zero)
            return Vector2.right;

        return forward;
    }

    private Vector2 GetOrigin()
    {
        if (_originProvider != null && _originProvider.CurrentHandAnchor != null)
            return _originProvider.CurrentHandAnchor.position;

        return _fallbackOrigin;
    }

    private SegmentType GetSegmentType(int index, int tileCount)
    {
        if (tileCount == 1)
            return SegmentType.End;

        if (index == 0)
            return SegmentType.Start;

        if (index == tileCount - 1)
            return SegmentType.End;

        return SegmentType.Middle;
    }

    private string GetStateName(SegmentType segmentType)
    {
        return segmentType switch
        {
            SegmentType.Start => _startStateName,
            SegmentType.Middle => _middleStateName,
            _ => _endStateName
        };
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y).normalized;
    }
}
