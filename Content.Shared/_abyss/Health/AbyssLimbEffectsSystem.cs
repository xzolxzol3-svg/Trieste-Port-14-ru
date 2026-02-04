using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;

namespace Content.Shared._abyss.Health;

/// <summary>
/// Applies movement speed modifiers based on leg/foot damage (limp when legs injured, crawl when both legs broken).
/// Subscribe to RefreshMovementSpeedModifiersEvent and DamageChangedEvent to refresh.
/// </summary>
public sealed class AbyssLimbEffectsSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    private static readonly string[] LegSlots = { "LLeg", "RLeg" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, AbyssBodyPartHealthComponent component, DamageChangedEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        RaiseLocalEvent(uid, new AbyssDamageChangedEvent(args));
    }

    private void OnRefreshSpeed(EntityUid uid, AbyssBodyPartHealthComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.PartMaxHealth == null || component.PartDamage == null)
            return;

        int brokenCount = 0;
        int damagedCount = 0;
        foreach (var slot in LegSlots)
        {
            var max = component.PartMaxHealth.GetValueOrDefault(slot);
            var dmg = component.PartDamage.GetValueOrDefault(slot);
            if (max <= FixedPoint2.Zero) continue;
            if (component.IsLimbBroken(slot))
                brokenCount++;
            else if (dmg > FixedPoint2.Zero)
                damagedCount++;
        }

        if (brokenCount >= 2)
        {
            args.ModifySpeed(0.2f, 0.2f);
            return;
        }
        if (brokenCount >= 1 || damagedCount >= 2)
        {
            args.ModifySpeed(0.55f, 0.55f);
            return;
        }
        if (damagedCount >= 1)
            args.ModifySpeed(0.8f, 0.8f);
    }
}
