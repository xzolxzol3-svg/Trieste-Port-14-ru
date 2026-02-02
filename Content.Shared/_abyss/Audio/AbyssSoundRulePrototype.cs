using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._abyss.Audio;

/// 
/// Правило для случайного one-shot эмбиента / звука (любая карта: Sweetwater, Trieste, море и т.д.).
/// Настраивается через прототипы: для какой карты (имя грида), в воде/не в воде, интервал, коллекция звуков.
/// 
[Prototype("abyssSoundRule")]
public sealed partial class AbyssSoundRulePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// Коллекция звуков (SoundCollectionPrototype id).
    [DataField(required: true)]
    public string SoundCollection { get; private set; } = string.Empty;

    /// Запасной путь к файлу, если коллекция не загружена.
    [DataField]
    public ResPath? FallbackPath { get; private set; }

    /// Минимальный интервал между звуками, секунды.
    [DataField]
    public float MinIntervalSec { get; private set; } = 45f;

    ///  Максимальный интервал между звуками, секунды.
    [DataField]
    public float MaxIntervalSec { get; private set; } = 120f;

    /// <Базовая громкость (dB).
    [DataField]
    public float Volume { get; private set; } = -8f;

    /// Разброс высоты тона: 0.08 = ±8% (например 0.92–1.08).
    [DataField]
    public float PitchVariation { get; private set; } = 0.08f;

    /// 
    /// Имена гридов карты, на которых играет звук (MetaData.EntityName грида).
    /// Например: ["Sweetwater"], ["Trieste"], ["Waste Zone"].
    /// Пустой список = на любом гриде (если остальные условия подходят).
    /// 
    [DataField]
    public List<string> RequiredGridNames { get; private set; } = new();

    /// 
    /// ID карт (MapId), на которых играет звук. Например Waste Zone = 7: [7].
    /// Пустой список = на любой карте. Можно комбинировать с requiredGridNames.
    /// 
    [DataField]
    public List<int> RequiredMapIds { get; private set; } = new();

    /// Играть только если игрок НЕ в воде (в воздухе/на станции).
    [DataField]
    public bool RequireNotInWater { get; private set; } = true;

    /// Играть только если игрок в воде (открытое море и т.п.).
    [DataField]
    public bool RequireInWater { get; private set; } = false;
}
