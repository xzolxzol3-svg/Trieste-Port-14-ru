using Content.Client.Gameplay;
using Content.Shared._abyss.Audio;
using Content.Shared._TP.WaterInteractions;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._abyss.Audio;

/// <summary>
/// Воспроизводит случайные one-shot эмбиенты по правилам из прототипов abyssSoundRule.
/// Поддержка любых карт (Sweetwater, Trieste и т.д.) через requiredGridNames; в воде / не в воде; рандомный питч и громкость.
/// </summary>
public sealed class AbyssAmbientSoundSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private bool _localPlayerInWater;
    private readonly Dictionary<string, TimeSpan> _nextPlayByRule = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeNetworkEvent<InGasEvent>(OnInGas);
    }

    private void OnInGas(InGasEvent msg, EntitySessionEventArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;
        if (GetNetEntity(player.Value) != msg.Entity)
            return;
        _localPlayerInWater = msg.InWater;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_state.CurrentState is not GameplayState)
        {
            _nextPlayByRule.Clear();
            return;
        }

        var player = _player.LocalEntity;
        if (player == null)
        {
            _nextPlayByRule.Clear();
            return;
        }

        if (!TryComp(player.Value, out TransformComponent? xform) || xform.GridUid == null)
        {
            _nextPlayByRule.Clear();
            return;
        }

        var gridUid = xform.GridUid.Value;
        var gridName = TryComp(gridUid, out MetaDataComponent? meta) ? meta.EntityName : string.Empty;
        var mapId = (int) xform.MapID;

        foreach (var rule in _proto.EnumeratePrototypes<AbyssSoundRulePrototype>())
        {
            if (!ConditionsMatch(rule, gridName, mapId))
            {
                _nextPlayByRule.Remove(rule.ID);
                continue;
            }

            if (!_nextPlayByRule.TryGetValue(rule.ID, out _))
                _nextPlayByRule[rule.ID] = _timing.CurTime + NextInterval(rule);

            if (_timing.CurTime < _nextPlayByRule[rule.ID])
                continue;

            PlayRuleSound(rule);
            _nextPlayByRule[rule.ID] = _timing.CurTime + NextInterval(rule);
        }
    }

    private bool ConditionsMatch(AbyssSoundRulePrototype rule, string gridName, int mapId)
    {
        if (rule.RequiredGridNames.Count > 0 && !rule.RequiredGridNames.Contains(gridName))
            return false;
        if (rule.RequiredMapIds.Count > 0 && !rule.RequiredMapIds.Contains(mapId))
            return false;
        if (rule.RequireNotInWater && _localPlayerInWater)
            return false;
        if (rule.RequireInWater && !_localPlayerInWater)
            return false;
        return true;
    }

    private TimeSpan NextInterval(AbyssSoundRulePrototype rule)
    {
        var min = TimeSpan.FromSeconds(rule.MinIntervalSec);
        var max = TimeSpan.FromSeconds(rule.MaxIntervalSec);
        return _random.Next(min, max);
    }

    private void PlayRuleSound(AbyssSoundRulePrototype rule)
    {
        var pitch = 1f + _random.NextFloat(-rule.PitchVariation, rule.PitchVariation);
        var volume = rule.Volume + _random.NextFloat(-2f, 2f);
        var audioParams = AudioParams.Default
            .WithVolume(volume)
            .WithPitchScale(pitch)
            .WithLoop(false);

        if (_proto.TryIndex<SoundCollectionPrototype>(rule.SoundCollection, out var collection) && collection.PickFiles.Count > 0)
        {
            var path = _random.Pick(collection.PickFiles);
            _audio.PlayGlobal(new ResolvedPathSpecifier(path), Filter.Local(), false, audioParams);
            return;
        }

        if (rule.FallbackPath != null)
            _audio.PlayGlobal(new ResolvedPathSpecifier(rule.FallbackPath.Value), Filter.Local(), false, audioParams);
    }
}
