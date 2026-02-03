using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._abyss.Health;

/// <summary>
/// Periodically refreshes the open health window so that health/stats update in real time
/// (e.g. after healing via admin or natural regeneration).
/// </summary>
public sealed class AbyssHealthUIRefreshSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private double _lastTick;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var now = _timing.RealTime.TotalSeconds;
        if (now - _lastTick < 0.1)
            return;
        _lastTick = now;

        var controller = _ui.GetUIController<AbyssHealthUIController>();
        controller.RefreshOpenWindowIfNeeded();
    }
}