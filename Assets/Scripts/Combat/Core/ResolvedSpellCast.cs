using System.Collections.Generic;
using UnityEngine;

public readonly struct ResolvedSpellCast
{
    private readonly Vector2[] _directions;

    public ResolvedSpellCast(ModifierWordData modifier, ElementWordData element, FormWordData form, Vector2 origin, Vector2[] directions)
    {
        Modifier = modifier;
        Element = element;
        Form = form;
        Origin = origin;
        _directions = directions;
    }

    public ModifierWordData Modifier { get; }
    public ElementWordData Element { get; }
    public FormWordData Form { get; }
    public Vector2 Origin { get; }
    public IReadOnlyList<Vector2> Directions => _directions;
    public bool IsValid => Modifier != null && Element != null && Form != null && _directions != null && _directions.Length > 0;
}
