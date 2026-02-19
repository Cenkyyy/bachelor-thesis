using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Spell VFX Library", fileName = "SpellVfxLibrary")]
public class SpellVfxLibrary : ScriptableObject
{
    [Header("Shared Prefab")]
    [SerializeField] private GameObject baseSpellPrefab;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float shardLifetime = 0.35f;
    [SerializeField, Min(0.01f)] private float beamLifetime = 0.9f;
    [SerializeField, Min(0.01f)] private float waveLifetime = 0.45f;
    [SerializeField, Min(0.01f)] private float barrageLifetime = 0.3f;

    [Header("Speeds")]
    [SerializeField, Min(0f)] private float shardSpeed = 14f;
    [SerializeField, Min(0f)] private float beamSpeed = 0f;
    [SerializeField, Min(0f)] private float waveSpeed = 6f;
    [SerializeField, Min(0f)] private float barrageSpeed = 12f;

    [Header("Scales")]
    [SerializeField] private Vector3 shardScale = new(0.35f, 0.35f, 1f);
    [SerializeField] private Vector3 beamScale = new(3f, 0.2f, 1f);
    [SerializeField] private Vector3 waveScale = new(1.8f, 0.8f, 1f);
    [SerializeField] private Vector3 barrageScale = new(0.25f, 0.25f, 1f);

    [Header("Element Colors")]
    [SerializeField] private Color lightningColor = new(0.65f, 0.9f, 1f, 1f);
    [SerializeField] private Color poisonColor = new(0.45f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color frostColor = new(0.7f, 0.95f, 1f, 1f);
    [SerializeField] private Color emberColor = new(1f, 0.45f, 0.2f, 1f);
    [SerializeField] private Color darkColor = new(0.5f, 0.35f, 0.75f, 1f);

    public GameObject BaseSpellPrefab => baseSpellPrefab;

    public SpellVfxProfile GetProfile(FormWord form)
    {
        return form switch
        {
            FormWord.Beam => new SpellVfxProfile(beamLifetime, beamSpeed, beamScale),
            FormWord.Wave => new SpellVfxProfile(waveLifetime, waveSpeed, waveScale),
            FormWord.Barrage => new SpellVfxProfile(barrageLifetime, barrageSpeed, barrageScale),
            _ => new SpellVfxProfile(shardLifetime, shardSpeed, shardScale)
        };
    }

    public Color GetElementColor(ElementWord element)
    {
        return element switch
        {
            ElementWord.Lightning => lightningColor,
            ElementWord.Poison => poisonColor,
            ElementWord.Frost => frostColor,
            ElementWord.Ember => emberColor,
            ElementWord.Dark => darkColor,
            _ => Color.white
        };
    }
}

[Serializable]
public readonly struct SpellVfxProfile
{
    public float Lifetime { get; }
    public float Speed { get; }
    public Vector3 Scale { get; }

    public SpellVfxProfile(float lifetime, float speed, Vector3 scale)
    {
        Lifetime = lifetime;
        Speed = speed;
        Scale = scale;
    }
}
