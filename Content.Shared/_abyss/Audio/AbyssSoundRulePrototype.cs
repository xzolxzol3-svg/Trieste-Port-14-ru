using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._abyss.Audio;

/// <summary>
/// Правило для случайного one-shot эмбиента (любая карта: Sweetwater, Trieste, море и т.д.).
/// Настраивается через прототипы: для какой карты (имя грида), в воде/не в воде, интервал, коллекция звуков.
/// </summary>
[Prototype("abyssSoundRule")]
public sealed partial class AbyssSoundRulePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>Коллекция звуков (SoundCollectionPrototype id).</summary>
    [DataField(required: true)]
    public string SoundCollection { get; private set; } = string.Empty;

    /// <summary>Запасной путь к файлу, если коллекция не загружена.</summary>
    [DataField]
    public ResPath? FallbackPath { get; private set; }

    /// <summary>Минимальный интервал между звуками, секунды.</summary>
    [DataField]
    public float MinIntervalSec { get; private set; } = 45f;

    /// <summary>Максимальный интервал между звуками, секунды.</summary>
    [DataField]
    public float MaxIntervalSec { get; private set; } = 120f;

    /// <summary>Базовая громкость (dB).</summary>
    [DataField]
    public float Volume { get; private set; } = -8f;

    /// <summary>Разброс высоты тона: 0.08 = ±8% (например 0.92–1.08).</summary>
    [DataField]
    public float PitchVariation { get; private set; } = 0.08f;

    /// <summary>
    /// Имена гридов карты, на которых играет звук (MetaData.EntityName грида).
    /// Например: ["Sweetwater"], ["Trieste"], ["Waste Zone"].
    /// Пустой список = на любом гриде (если остальные условия подходят).
    /// </summary>
    [DataField]
    public List<string> RequiredGridNames { get; private set; } = new();

    /// <summary>
    /// ID карт (MapId), на которых играет звук. Например Waste Zone = 7: [7].
    /// Пустой список = на любой карте. Можно комбинировать с requiredGridNames.
    /// </summary>
    [DataField]
    public List<int> RequiredMapIds { get; private set; } = new();

    /// <summary>Играть только если игрок НЕ в воде (в воздухе/на станции).</summary>
    [DataField]
    public bool RequireNotInWater { get; private set; } = true;

    /// <summary>Играть только если игрок в воде (открытое море и т.п.).</summary>
    [DataField]
    public bool RequireInWater { get; private set; } = false;
}
