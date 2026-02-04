using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._abyss.Health;

/// <summary>
/// Barotrauma-style per-limb health. Each limb has its own damage and max health.
/// When total damage is applied, it is distributed to a random limb by the server.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AbyssBodyPartHealthComponent : Component
{
    /// <summary> Limb slot IDs for UI and distribution. </summary>
    public static readonly string[] LimbSlots =
    {
        // Keep the system simple: no hands/feet, only major limbs.
        "Head", "Torso", "LArm", "RArm", "LLeg", "RLeg"
    };

    /// <summary> Max health per limb (same order as LimbSlots). Total ~100. </summary>
    public static readonly FixedPoint2[] DefaultMaxHealth =
    {
        (FixedPoint2) 15,  // Head
        (FixedPoint2) 50, // Torso
        (FixedPoint2) 12, (FixedPoint2) 12, // Arms
        (FixedPoint2) 15, (FixedPoint2) 15 // Legs
    };

    /// <summary> Current damage per limb slot. Key = slot id (LimbSlots). </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, FixedPoint2> PartDamage { get; set; } = new();

    /// <summary> Max health per limb slot. Filled from DefaultMaxHealth on init. </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, FixedPoint2> PartMaxHealth { get; set; } = new();

    /// <summary> Whether this limb is considered "broken" (damage >= max). </summary>
    [ViewVariables]
    public bool IsLimbBroken(string slotId)
    {
        if (PartMaxHealth == null || PartDamage == null)
            return false;
        if (!PartMaxHealth.TryGetValue(slotId, out var max) || max <= FixedPoint2.Zero)
            return false;
        return PartDamage.GetValueOrDefault(slotId) >= max;
    }
}
