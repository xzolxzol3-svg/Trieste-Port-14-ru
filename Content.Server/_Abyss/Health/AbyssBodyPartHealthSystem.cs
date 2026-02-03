using System.Collections.Generic;
using Content.Shared._abyss.Health;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;

namespace Content.Server._abyss.Health;

public sealed class AbyssBodyPartHealthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, MapInitEvent>(OnDamageableMapInit);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, ComponentInit>(OnAbyssHealthInit);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, AbyssDamageChangedEvent>(OnAbyssDamageChanged);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<AbyssBodyPartHealthComponent> ent, ref RejuvenateEvent args)
    {
        var comp = ent.Comp;
        if (comp.PartDamage == null)
            return;
        foreach (var slot in AbyssBodyPartHealthComponent.LimbSlots)
            comp.PartDamage[slot] = FixedPoint2.Zero;
        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnDamageableMapInit(Entity<DamageableComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<BodyComponent>(ent) || !HasComp<MobStateComponent>(ent))
            return;
        EnsureComp<AbyssBodyPartHealthComponent>(ent);
    }

    private void OnAbyssDamageChanged(EntityUid uid, AbyssBodyPartHealthComponent component, AbyssDamageChangedEvent args)
    {
        var inner = args.Inner;
        if (inner.DamageDelta == null || inner.DamageDelta.Empty)
            return;

        var delta = inner.DamageDelta.GetTotal();
        if (delta == FixedPoint2.Zero)
            return;

        if (delta > FixedPoint2.Zero)
            AddDamageToRandomLimb(component, delta);
        else
            SubtractHealingFromRandomDamagedLimb(component, -delta);

        Dirty(uid, component);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnAbyssHealthInit(Entity<AbyssBodyPartHealthComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
        comp.PartMaxHealth ??= new Dictionary<string, FixedPoint2>();
        comp.PartDamage ??= new Dictionary<string, FixedPoint2>();
        if (comp.PartMaxHealth.Count > 0)
            return;

        for (var i = 0; i < AbyssBodyPartHealthComponent.LimbSlots.Length; i++)
        {
            var slot = AbyssBodyPartHealthComponent.LimbSlots[i];
            comp.PartMaxHealth[slot] = AbyssBodyPartHealthComponent.DefaultMaxHealth[i];
            comp.PartDamage.TryAdd(slot, FixedPoint2.Zero);
        }

        if (TryComp<DamageableComponent>(ent, out var damageable) && damageable.TotalDamage > FixedPoint2.Zero)
            DistributeDamageToRandomLimbs(ent.Comp, damageable.TotalDamage);

        Dirty(ent, comp);
    }

    private void AddDamageToRandomLimb(AbyssBodyPartHealthComponent comp, FixedPoint2 amount)
    {
        if (comp.PartMaxHealth == null || comp.PartDamage == null)
            return;
        var slots = AbyssBodyPartHealthComponent.LimbSlots;
        var available = new List<string>();
        foreach (var slot in slots)
        {
            var max = comp.PartMaxHealth.GetValueOrDefault(slot);
            var current = comp.PartDamage.GetValueOrDefault(slot);
            if (current < max)
                available.Add(slot);
        }
        if (available.Count == 0)
            available.AddRange(slots);

        var chosen = _random.Pick(available);
        var maxH = comp.PartMaxHealth.GetValueOrDefault(chosen);
        var cur = comp.PartDamage.GetValueOrDefault(chosen);
        comp.PartDamage[chosen] = FixedPoint2.Min(cur + amount, maxH);
    }

    private void SubtractHealingFromRandomDamagedLimb(AbyssBodyPartHealthComponent comp, FixedPoint2 amount)
    {
        if (comp.PartDamage == null)
            return;
        var damaged = new List<string>();
        foreach (var slot in AbyssBodyPartHealthComponent.LimbSlots)
        {
            if ((comp.PartDamage.GetValueOrDefault(slot)) > FixedPoint2.Zero)
                damaged.Add(slot);
        }
        if (damaged.Count == 0)
            return;

        var chosen = _random.Pick(damaged);
        var cur = comp.PartDamage[chosen];
        comp.PartDamage[chosen] = FixedPoint2.Max(FixedPoint2.Zero, cur - amount);
    }

    private void DistributeDamageToRandomLimbs(AbyssBodyPartHealthComponent comp, FixedPoint2 total)
    {
        if (comp.PartMaxHealth == null || comp.PartDamage == null)
            return;
        var remaining = total;
        var maxIterations = AbyssBodyPartHealthComponent.LimbSlots.Length * 4;
        var iterations = 0;
        while (remaining > FixedPoint2.Zero && iterations < maxIterations)
        {
            iterations++;
            var slot = _random.Pick(AbyssBodyPartHealthComponent.LimbSlots);
            var maxH = comp.PartMaxHealth.GetValueOrDefault(slot);
            var cur = comp.PartDamage.GetValueOrDefault(slot);
            var space = maxH - cur;
            if (space <= FixedPoint2.Zero)
                continue;
            var add = FixedPoint2.Min(remaining, space);
            comp.PartDamage[slot] = cur + add;
            remaining -= add;
        }
    }
}
