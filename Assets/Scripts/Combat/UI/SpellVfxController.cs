using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellCombatController _spellCombatController;
    [SerializeField] private PlayerHeldItemVisual _playerHeldItemVisual;
    [SerializeField] private CombatWordsData _combatWordsData;
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
        if (!phrase.IsComplete || _combatWordsData == null || _vfxData == null)
            return;

        var origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        var direction = GetForwardDirection(origin);
        var directions = BuildDirections(direction, phrase.Modifier.Value);
        var profile = _vfxData.GetProfile(phrase.Form.Value);

        var formData = _combatWordsData.GetForm(phrase.Form.Value);
        if (formData == null || formData.ProjectilePrefab == null)
            return;

        var elementData = _combatWordsData.GetElement(phrase.Element.Value);
        var modifierData = _combatWordsData.GetModifier(phrase.Modifier.Value);

        if (phrase.Form == FormWord.Barrage)
        {
            StartCoroutine(PlayBarrage(directions, profile, formData.ProjectilePrefab, elementData?.Material));
            return;
        }

        SpawnBatch(directions, profile, formData.ProjectilePrefab, elementData?.Material);

        if (modifierData != null && modifierData.OptionalPrefab != null)
            SpawnModifierVisual(modifierData.OptionalPrefab, origin, direction, elementData?.Material);
    }

    private IEnumerator PlayBarrage(IReadOnlyList<Vector2> directions, SpellVfxProfile profile, GameObject projectilePrefab, Material elementMaterial)
    {
        const int defaultShots = 4;
        for (var i = 0; i < defaultShots; i++)
        {
            SpawnBatch(directions, profile, projectilePrefab, elementMaterial);
            yield return new WaitForSeconds(_barrageInterval);
        }
    }

    private void SpawnBatch(IReadOnlyList<Vector2> directions, SpellVfxProfile profile, GameObject projectilePrefab, Material elementMaterial)
    {
        var origin = _playerHeldItemVisual.CurrentHandAnchor.position;
        for (var i = 0; i < directions.Count; i++)
        {
            var spellObject = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var instance = spellObject.GetComponent<SpellVfxInstance>();
            if (instance == null)
                instance = spellObject.AddComponent<SpellVfxInstance>();

            instance.Initialize(directions[i], profile.Speed, profile.Lifetime, elementMaterial);
        }
    }

    private void SpawnModifierVisual(GameObject optionalPrefab, Vector2 origin, Vector2 direction, Material elementMaterial)
    {
        var modifierObject = Instantiate(optionalPrefab, origin, Quaternion.identity);
        modifierObject.transform.right = direction;

        var renderer = modifierObject.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null && elementMaterial != null)
            renderer.material = elementMaterial;
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
