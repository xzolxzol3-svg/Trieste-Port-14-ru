using Content.Shared._abyss.Health;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A single damage type. These types are grouped together in <see cref="DamageGroupPrototype"/>s.
    /// </summary>
    [Prototype]
    public sealed partial class DamageTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField(required: true)]
        private LocId Name { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        /// The price for each 1% damage reduction in armors
        /// </summary>
        [DataField("armorCoefficientPrice")]
        public double ArmorPriceCoefficient { get; set; }

        /// <summary>
        /// The price for each flat damage reduction in armors
        /// </summary>
        [DataField("armorFlatPrice")]
        public double ArmorPriceFlat { get; set; }

        /// <summary>
        /// Abyss-14: how this damage type should be applied to limbs.
        /// Defaults to AllLimb to preserve vanilla behavior.
        /// </summary>
        [DataField("abyssLimbMode")]
        public AbyssLimbMode AbyssLimbMode { get; set; } = AbyssLimbMode.AllLimb;
    }
}
