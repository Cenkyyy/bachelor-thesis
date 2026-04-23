using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellCombatController _spellCombatController;
    [SerializeField] private PlayerHeldItemVisual _playerHeldItemVisual;
    [SerializeField] private SpellVfxData _vfxData;

    [Header("Spawning")]
    [SerializeField, Min(0.01f)] private float _barrageInterval = 0.08f;

    private void Awake()
    {
        if (_playerHeldItemVisual == null)
            _playerHeldItemVisual = GetComponentInChildren<PlayerHeldItemVisual>();
    }

    private void OnEnable()
    {
        if (_spellCombatController != null)
            _spellCombatController.OnSpellCastCommitted += HandleSpellCastCommitted;
    }

    private void OnDisable()
    {
        if (_spellCombatController != null)
            _spellCombatController.OnSpellCastCommitted -= HandleSpellCastCommitted;
    }

    private void HandleSpellCastCommitted(SpellPhrase phrase)
    {
        if (!phrase.IsComplete || _vfxData == null || _vfxData.BaseSpellPrefab == null)
            return;

        var origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        var direction = GetForwardDirection(origin);
        var directions = BuildDirections(direction, phrase.Modifier.Value);
        var profile = _vfxData.GetProfile(phrase.Form.Value);
        var tint = _vfxData.GetElementColor(phrase.Element.Value);

        if (phrase.Form == FormWord.Barrage)
        {
            StartCoroutine(PlayBarrage(directions, profile, tint));
            return;
        }

        SpawnBatch(directions, profile, tint);
    }

    private IEnumerator PlayBarrage(IReadOnlyList<Vector2> directions, SpellVfxProfile profile, Color tint)
    {
        const int defaultShots = 4;
        for (var i = 0; i < defaultShots; i++)
        {
            SpawnBatch(directions, profile, tint);
            yield return new WaitForSeconds(_barrageInterval);
        }
    }

    private void SpawnBatch(IReadOnlyList<Vector2> directions, SpellVfxProfile profile, Color tint)
    {
        var origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        for (var i = 0; i < directions.Count; i++)
        {
            var spellObject = Instantiate(_vfxData.BaseSpellPrefab, origin, Quaternion.identity);
            var instance = spellObject.GetComponent<SpellVfxInstance>();
            if (instance == null)
                instance = spellObject.AddComponent<SpellVfxInstance>();

            instance.Initialize(directions[i], profile.Speed, profile.Lifetime, tint, profile.Scale);
        }
    }

    private List<Vector2> BuildDirections(Vector2 forward, ModifierWord modifier)
    {
        if (modifier != ModifierWord.Splitting)
            return new List<Vector2> { forward };

        return new List<Vector2>
        {
            forward,
            Rotate(forward, -45f),
            Rotate(forward, 45f)
        };
    }

    private Vector2 GetForwardDirection(Vector2 origin)
    {
        var mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : origin + Vector2.right;

        var forward = (mouseWorld - origin).normalized;
        return forward == Vector2.zero ? Vector2.right : forward;
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y).normalized;
    }
}
