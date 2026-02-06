using System.Collections.Generic;
using Content.Shared._abyss.Health;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
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

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        // Получаем целевую конечность из аргументов события (если есть)
        string? targetedLimb = args.TargetLimb;

        // Модульное распределение по типам урона: для каждого DamageType смотрим его AbyssLimbMode.
        foreach (var (typeId, delta) in inner.DamageDelta.DamageDict)
        {
            if (delta == FixedPoint2.Zero)
                continue;

            if (!damageable.Damage.DamageDict.TryGetValue(typeId, out _))
                continue;

            if (!IoCManager.Resolve<Robust.Shared.Prototypes.IPrototypeManager>()
                    .TryIndex<DamageTypePrototype>(typeId, out var damageType))
            {
                // Если задана конкретная конечность для лечения/урона - используем её
                if (targetedLimb != null)
                {
                    ApplyToSingleLimb(component, targetedLimb, delta);
                }
                else
                {
                    // Неизвестный тип урона - по умолчанию как AllLimb.
                    ApplyAllLimb(component, delta);
                }
                continue;
            }

            // Если задана конкретная конечность для лечения/урона - используем её, ИГНОРИРУЯ AbyssLimbMode
            if (targetedLimb != null)
            {
                ApplyToSingleLimb(component, targetedLimb, delta);
                continue;
            }

            switch (damageType.AbyssLimbMode)
            {
                case AbyssLimbMode.AllLimb:
                    if (delta > FixedPoint2.Zero)
                        AddDamageToRandomLimb(component, delta);
                    else
                        SubtractHealingFromRandomDamagedLimb(component, -delta);
                    break;

                case AbyssLimbMode.TorsoOnly:
                    ApplyToSingleLimb(component, "Torso", delta);
                    break;

                case AbyssLimbMode.TotalOnly:
                    // Только total HP, не трогаем конечности.
                    break;
            }
        }

        Dirty(uid, component);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void ApplyAllLimb(AbyssBodyPartHealthComponent comp, FixedPoint2 delta)
    {
        if (delta > FixedPoint2.Zero)
            AddDamageToRandomLimb(comp, delta);
        else if (delta < FixedPoint2.Zero)
            SubtractHealingFromRandomDamagedLimb(comp, -delta);
    }

    private void ApplyToSingleLimb(AbyssBodyPartHealthComponent comp, string slot, FixedPoint2 delta)
    {
        if (comp.PartMaxHealth == null || comp.PartDamage == null)
            return;

        var max = comp.PartMaxHealth.GetValueOrDefault(slot);
        var cur = comp.PartDamage.GetValueOrDefault(slot);
        var newVal = cur + delta;

        if (delta > FixedPoint2.Zero && max > FixedPoint2.Zero)
            newVal = FixedPoint2.Min(newVal, max);
        if (delta < FixedPoint2.Zero)
            newVal = FixedPoint2.Max(FixedPoint2.Zero, newVal);

        comp.PartDamage[slot] = newVal;
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
        // Previously healing was applied to a random damaged limb which often felt like a "desync"
        // (total HP looks fully healed while a specific limb stays damaged).
        // Heal the most damaged limb(s) first for more intuitive behavior.
        var remaining = amount;
        while (remaining > FixedPoint2.Zero)
        {
            string? chosen = null;
            var chosenDamage = FixedPoint2.Zero;

            foreach (var slot in AbyssBodyPartHealthComponent.LimbSlots)
            {
                var dmg = comp.PartDamage.GetValueOrDefault(slot);
                if (dmg <= FixedPoint2.Zero)
                    continue;

                if (chosen == null || dmg > chosenDamage)
                {
                    chosen = slot;
                    chosenDamage = dmg;
                }
            }

            if (chosen == null)
                return;

            var cur = comp.PartDamage.GetValueOrDefault(chosen);
            var sub = FixedPoint2.Min(cur, remaining);
            comp.PartDamage[chosen] = FixedPoint2.Max(FixedPoint2.Zero, cur - sub);
            remaining -= sub;
        }
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
