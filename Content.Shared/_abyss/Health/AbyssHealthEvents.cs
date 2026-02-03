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

    public AbyssDamageChangedEvent(DamageChangedEvent inner)
    {
        Inner = inner;
    }
}