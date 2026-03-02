using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Spell VFX Data", fileName = "SpellVfxData")]
public class SpellVfxData : ScriptableObject
{
    [field: Header("Shared Prefab")]
    [field: SerializeField] public GameObject BaseSpellPrefab { get; private set; }

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float _shardLifetime = 0.35f;
    [SerializeField, Min(0.01f)] private float _beamLifetime = 0.9f;
    [SerializeField, Min(0.01f)] private float _waveLifetime = 0.45f;
    [SerializeField, Min(0.01f)] private float _barrageLifetime = 0.3f;

    [Header("Speeds")]
    [SerializeField, Min(0f)] private float _shardSpeed = 14f;
    [SerializeField, Min(0f)] private float _beamSpeed = 0f;
    [SerializeField, Min(0f)] private float _waveSpeed = 6f;
    [SerializeField, Min(0f)] private float _barrageSpeed = 12f;

    [Header("Scales")]
    [SerializeField] private Vector3 _shardScale = new(0.35f, 0.35f, 1f);
    [SerializeField] private Vector3 _beamScale = new(3f, 0.2f, 1f);
    [SerializeField] private Vector3 _waveScale = new(1.8f, 0.8f, 1f);
    [SerializeField] private Vector3 _barrageScale = new(0.25f, 0.25f, 1f);

    [Header("Element Colors")]
    [SerializeField] private Color _lightningColor = new(0.65f, 0.9f, 1f, 1f);
    [SerializeField] private Color _poisonColor = new(0.45f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color _frostColor = new(0.7f, 0.95f, 1f, 1f);
    [SerializeField] private Color _emberColor = new(1f, 0.45f, 0.2f, 1f);
    [SerializeField] private Color _darkColor = new(0.5f, 0.35f, 0.75f, 1f);

    public SpellVfxProfile GetProfile(FormWord form)
    {
        return form switch
        {
            FormWord.Beam => new SpellVfxProfile(_beamLifetime, _beamSpeed, _beamScale),
            FormWord.Wave => new SpellVfxProfile(_waveLifetime, _waveSpeed, _waveScale),
            FormWord.Barrage => new SpellVfxProfile(_barrageLifetime, _barrageSpeed, _barrageScale),
            _ => new SpellVfxProfile(_shardLifetime, _shardSpeed, _shardScale)
        };
    }

    public Color GetElementColor(ElementWord element)
    {
        return element switch
        {
            ElementWord.Lightning => _lightningColor,
            ElementWord.Poison => _poisonColor,
            ElementWord.Frost => _frostColor,
            ElementWord.Ember => _emberColor,
            ElementWord.Dark => _darkColor,
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
