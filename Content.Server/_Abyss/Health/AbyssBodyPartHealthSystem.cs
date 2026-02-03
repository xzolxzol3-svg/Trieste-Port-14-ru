using Content.Shared._abyss.Health;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Server._abyss.Health;

public sealed class AbyssBodyPartHealthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, MapInitEvent>(OnDamageableMapInit);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, ComponentInit>(OnAbyssHealthInit);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageableMapInit(Entity<DamageableComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<BodyComponent>(ent))
            return;
        EnsureComp<AbyssBodyPartHealthComponent>(ent);
    }

    private void OnDamageChanged(EntityUid uid, AbyssBodyPartHealthComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty)
            return;

        var delta = args.DamageDelta.GetTotal();
        if (delta == FixedPoint2.Zero)
            return;

        if (delta > FixedPoint2.Zero)
            AddDamageToRandomLimb(component, delta);
        else
            SubtractHealingFromRandomDamagedLimb(component, -delta);

        Dirty(uid, component);
    }

    private void OnAbyssHealthInit(Entity<AbyssBodyPartHealthComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
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
        var remaining = total;
        while (remaining > FixedPoint2.Zero)
        {
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
