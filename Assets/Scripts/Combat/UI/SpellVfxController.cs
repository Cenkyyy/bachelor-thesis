using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellCastingPanelController castingPanel;
    [SerializeField] private SpellVfxLibrary library;

    [Header("Spawning")]
    [SerializeField] private Transform spawnOrigin;
    [SerializeField, Min(0.01f)] private float barrageInterval = 0.08f;

    private void OnEnable()
    {
        if (castingPanel != null)
            castingPanel.OnPhraseCompleted += HandlePhraseCompleted;
    }

    private void OnDisable()
    {
        if (castingPanel != null)
            castingPanel.OnPhraseCompleted -= HandlePhraseCompleted;
    }

    private void HandlePhraseCompleted(SpellPhrase phrase)
    {
        if (!phrase.IsComplete || library == null || library.BaseSpellPrefab == null)
            return;

        var direction = GetForwardDirection();
        var directions = BuildDirections(direction, phrase.Modifier.Value);
        var profile = library.GetProfile(phrase.Form.Value);
        var tint = library.GetElementColor(phrase.Element.Value);

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
            yield return new WaitForSeconds(barrageInterval);
        }
    }

    private void SpawnBatch(IReadOnlyList<Vector2> directions, SpellVfxProfile profile, Color tint)
    {
        for (var i = 0; i < directions.Count; i++)
        {
            var spellObject = Instantiate(library.BaseSpellPrefab, spawnOrigin.position, Quaternion.identity);
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

    private Vector2 GetForwardDirection()
    {
        var mouseWorld = Camera.main != null
            ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : (Vector2)spawnOrigin.position + Vector2.right;

        var forward = (mouseWorld - (Vector2)spawnOrigin.position).normalized;
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
