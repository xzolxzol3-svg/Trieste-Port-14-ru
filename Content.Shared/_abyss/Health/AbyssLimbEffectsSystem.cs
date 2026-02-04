using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;
using System; // Добавлено для Math.Abs

namespace Content.Shared._abyss.Health;

public sealed class AbyssLimbEffectsSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private static readonly string[] LegSlots = { "LLeg", "RLeg" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AbyssBodyPartHealthComponent, DamageableComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var abyss, out var damageable, out var bloodstream))
        {
            // Получаем значение твоего урона "Bleeding" из панели
            if (damageable.Damage.DamageDict.TryGetValue("Bleeding", out var bleedDamage) && bleedDamage > 0)
            {
                // СИНХРОНИЗАЦИЯ С ВАНИЛЬНОЙ КРОВЬЮ:
                // Мы вычисляем разницу между тем, что в панели, и тем, что сейчас в системе Bloodstream
                var targetBleed = bleedDamage.Float();
                var delta = targetBleed - bloodstream.BleedAmount;
                
                if (Math.Abs(delta) > 0.1f)
                {
                    // ИСПРАВЛЕНО: Убран 3-й аргумент. Метод принимает (uid, amount).
                    _bloodstream.TryModifyBleedAmount(uid, delta);
                }

                // ТИКАЮЩИЙ УРОН: Постепенно превращаем Кровотечение в Потерю крови (Bloodloss)
                var damageToApply = bleedDamage * 0.05f * frameTime;

                if (damageToApply > 0)
                {
                    var damageSpec = new DamageSpecifier();
                    damageSpec.DamageDict.Add("Bloodloss", (FixedPoint2)damageToApply);
                    _damageable.TryChangeDamage(uid, damageSpec, ignoreResistances: true);
                }
            }
            else
            {
                // Если в панели рана забинтована (0), то и кровь на пол капать должна перестать
                if (bloodstream.BleedAmount > 0)
                {
                    // ИСПРАВЛЕНО: Убран 3-й аргумент.
                    _bloodstream.TryModifyBleedAmount(uid, -bloodstream.BleedAmount);
                }
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, AbyssBodyPartHealthComponent component, DamageChangedEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);

        // Автоматическое начисление Bleeding при получении физического урона
        if (args.DamageIncreased && args.DamageDelta != null)
        {
            var slash = args.DamageDelta.DamageDict.GetValueOrDefault("Slash");
            var piercing = args.DamageDelta.DamageDict.GetValueOrDefault("Piercing");
            var bullet = args.DamageDelta.DamageDict.GetValueOrDefault("Bullet");

            var total = (slash + piercing + bullet).Float();
            if (total > 0)
            {
                var bleedSpec = new DamageSpecifier();
                bleedSpec.DamageDict.Add("Bleeding", (FixedPoint2)(total * 1.0f));
                _damageable.TryChangeDamage(uid, bleedSpec, ignoreResistances: true);
            }
        }

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