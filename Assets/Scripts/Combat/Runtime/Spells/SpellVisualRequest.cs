using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes one batch of projectile visuals requested by spell combat execution.
/// </summary>
public readonly struct SpellVisualRequest
{
    public SpellPhrase Spell { get; }
    public IReadOnlyList<float> TravelDistances { get; }
    public LayerMask ObstructionMask { get; }

    public SpellVisualRequest(SpellPhrase spell, IReadOnlyList<float> travelDistances, LayerMask obstructionMask)
    {
        Spell = spell;
        TravelDistances = travelDistances;
        ObstructionMask = obstructionMask;
    }
}
