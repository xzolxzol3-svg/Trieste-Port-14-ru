using Robust.Shared.Serialization;

namespace Content.Shared._abyss.Health;

/// <summary>
/// Abyss-14: how a given damage type should be applied to per-limb health.
/// </summary>
[Serializable, NetSerializable]
public enum AbyssLimbMode
{
    /// <summary>
    /// Default: damage distributed across limbs (current vanilla-style behavior).
    /// </summary>
    AllLimb,

    /// <summary>
    /// Damage goes only to torso limb slot.
    /// </summary>
    TorsoOnly,

    /// <summary>
    /// Only total HP (DamageableComponent) is affected; per-limb health is ignored.
    /// </summary>
    TotalOnly
}

