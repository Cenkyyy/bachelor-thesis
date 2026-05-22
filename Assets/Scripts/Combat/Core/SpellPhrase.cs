using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the selected spell words and, once committed, the cast origin and directions shared by combat and VFX.
/// </summary>
public struct SpellPhrase
{
    private Vector2[] _directions;

    public ModifierWordData Modifier { get; private set; }
    public ElementWordData Element { get; private set; }
    public FormWordData Form { get; private set; }
    public Vector2 Origin { get; private set; }
    public IReadOnlyList<Vector2> Directions => _directions;

    public bool IsComplete => Modifier != null && Element != null && Form != null;
    public bool HasCastContext => _directions != null && _directions.Length > 0;
    public bool IsCommitted => IsComplete && HasCastContext;

    public void SetModifier(ModifierWordData modifier)
    {
        Modifier = modifier;
    }

    public void SetElement(ElementWordData element)
    {
        Element = element;
    }

    public void SetForm(FormWordData form)
    {
        Form = form;
    }

    public void SetCastContext(Vector2 origin, Vector2[] directions)
    {
        Origin = origin;
        _directions = directions;
    }

    public void Clear()
    {
        Modifier = null;
        Element = null;
        Form = null;
        Origin = default;
        _directions = null;
    }

    public override string ToString()
    {
        var modifierLabel = Modifier != null ? Modifier.DisplayName : "--";
        var elementLabel = Element != null ? Element.DisplayName : "--";
        var formLabel = Form != null ? Form.DisplayName : "--";

        return $"{modifierLabel} - {elementLabel} - {formLabel}";
    }
}
