using Content.Shared._abyss.Health;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._abyss.Health;

/// <summary>
/// Adds "Examine health in detail" verb to entities with AbyssBodyPartHealthComponent.
/// On use: 1.5s delay then opens the health panel for that entity (self or other player).
/// </summary>
public sealed class AbyssExamineHealthVerbSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private (EntityUid Target, double OpenAtTime)? _pendingExamine;
    private const double ExamineDelaySec = 1.5;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbyssBodyPartHealthComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, AbyssBodyPartHealthComponent comp, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        ExamineVerb verb = new()
        {
            Category = VerbCategory.Examine,
            Priority = 5,
            Text = Loc.GetString("abyss-health-verb-examine-detail"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            ClientExclusive = true,
            Act = () =>
            {
                var target = args.Target;
                _pendingExamine = (target, _timing.RealTime.TotalSeconds + ExamineDelaySec);
            }
        };
        args.Verbs.Add(verb);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_pendingExamine is not { } pending)
            return;
        if (_timing.RealTime.TotalSeconds < pending.OpenAtTime)
            return;

        _pendingExamine = null;
        if (!Exists(pending.Target))
            return;

        var controller = _ui.GetUIController<AbyssHealthUIController>();
        controller.OpenForEntity(pending.Target);
    }
}