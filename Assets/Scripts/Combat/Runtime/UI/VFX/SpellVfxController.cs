using System.Collections;
using UnityEngine;

public class SpellVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellCombatController _spellCombatController;

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

    private void HandleSpellCastCommitted(ResolvedSpellCast spell)
    {
        if (!spell.IsValid || spell.Form.ProjectilePrefab == null)
            return;

        if (spell.Form.Type == FormWordType.Barrage)
        {
            StartCoroutine(PlayBarrage(spell));
            return;
        }

        SpawnBatch(spell);

        if (spell.Modifier.OptionalPrefab != null)
            SpawnModifierVisual(spell);
    }

    private IEnumerator PlayBarrage(ResolvedSpellCast spell)
    {
        for (var i = 0; i < spell.Form.BarrageProjectileCount; i++)
        {
            SpawnBatch(spell);
            yield return new WaitForSeconds(spell.Form.BarrageInterval);
        }
    }

    private void SpawnBatch(ResolvedSpellCast spell)
    {
        for (var i = 0; i < spell.Directions.Count; i++)
        {
            var spellObject = Instantiate(spell.Form.ProjectilePrefab, spell.Origin, Quaternion.identity);
            spellObject.transform.localScale = spell.Form.VfxScale;

            var instance = spellObject.GetComponent<SpellVfxInstance>();
            if (instance == null)
                instance = spellObject.AddComponent<SpellVfxInstance>();

            instance.Initialize(spell.Directions[i], spell.Form.VfxSpeed, spell.Form.VfxLifetime, spell.Element.Material);
        }
    }

    private void SpawnModifierVisual(ResolvedSpellCast spell)
    {
        var modifierObject = Instantiate(spell.Modifier.OptionalPrefab, spell.Origin, Quaternion.identity);
        modifierObject.transform.right = spell.Directions[0];

        var renderer = modifierObject.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null && spell.Element.Material != null)
            renderer.material = spell.Element.Material;
    }
}
