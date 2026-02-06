using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared._abyss.Health;

/// <summary>
/// Raised when damage changes on an entity that has AbyssBodyPartHealthComponent.
/// Used so only one system subscribes to DamageChangedEvent (AbyssLimbEffectsSystem),
/// while the server-only part damage update (AbyssBodyPartHealthSystem) listens to this.
/// </summary>
public sealed class AbyssDamageChangedEvent : EntityEventArgs
{
    public readonly DamageChangedEvent Inner;
    public readonly string? TargetLimb;

    public AbyssDamageChangedEvent(DamageChangedEvent inner, string? targetLimb = null)
    {
        Inner = inner;
        TargetLimb = targetLimb;
    }
}