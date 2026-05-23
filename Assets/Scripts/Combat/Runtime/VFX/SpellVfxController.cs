using UnityEngine;

/// <summary>
/// Spawns world-space spell visuals requested by the player spell combat flow.
/// </summary>
public sealed class SpellVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSpellCombatController _spellCombatController;
    [SerializeField] private Transform _spellContainer;

    private void OnEnable()
    {
        if (_spellCombatController != null)
            _spellCombatController.OnSpellVisualRequested += HandleSpellVisualRequested;
    }

    private void OnDisable()
    {
        if (_spellCombatController != null)
            _spellCombatController.OnSpellVisualRequested -= HandleSpellVisualRequested;
    }

    private void HandleSpellVisualRequested(SpellVisualRequest request)
    {
        if (!request.Spell.IsCommitted || request.Spell.Form.ProjectilePrefab == null)
            return;

        SpawnBatch(request);
    }

    private void SpawnBatch(SpellVisualRequest request)
    {
        SpellPhrase spell = request.Spell;
        for (int i = 0; i < spell.Directions.Count; i++)
        {
            GameObject spellObject = Instantiate(spell.Form.ProjectilePrefab, spell.Origin, Quaternion.identity, _spellContainer);
            spellObject.transform.localScale = spell.Form.VfxScale;

            SpellProjectile projectileVfx = spellObject.GetComponent<SpellProjectile>();
            if (projectileVfx == null)
                projectileVfx = spellObject.AddComponent<SpellProjectile>();

            float travelDistance = request.TravelDistances != null && i < request.TravelDistances.Count ? request.TravelDistances[i] : spell.Form.Range;
            float lifetime = ResolveLifetime(spell.Form.VfxLifetime, spell.Form.VfxSpeed, travelDistance);
            projectileVfx.Initialize(spell.Directions[i], spell.Form.VfxSpeed, lifetime, travelDistance, request.ObstructionMask, spell.Element.Material);
        }
    }

    private static float ResolveLifetime(float configuredLifetime, float speed, float travelDistance)
    {
        if (speed <= 0f)
            return configuredLifetime;

        return Mathf.Max(configuredLifetime, travelDistance / speed);
    }
}
