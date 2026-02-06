using Content.Client._abyss.Health.UI;
using Content.Client.Gameplay;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
namespace Content.Client._abyss.Health;

public sealed class AbyssHealthUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private HealthWindow? _window;
    private double _lastRefreshTime;
    private const double RefreshIntervalSec = 0.35;

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<HealthWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenHealthWindow,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<AbyssHealthUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }
        CommandBinds.Unregister<AbyssHealthUIController>();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;
        if (_window.IsOpen)
            _window.Close();
        else
        {
            var local = _player.LocalEntity;
            _window.UpdateEntity(local);
            _window.Open();
        }
    }

    /// <summary>
    /// Call from a system each frame to refresh the health window in real time when open.
    /// </summary>
    public void RefreshOpenWindowIfNeeded()
    {
        if (_window == null || !_window.IsOpen)
            return;

        var local = _player.LocalEntity;
        if (local != null && _window.CurrentEntity is { } target && target != local)
        {
            var transformSystem = _entityManager.System<SharedTransformSystem>();
            var localPos = transformSystem.GetMapCoordinates(local.Value);
            var targetPos = transformSystem.GetMapCoordinates(target);
            if (!localPos.InRange(targetPos, 1.5f))
            {
                _window.Close();
                return;
            }
        }

        var now = _timing.RealTime.TotalSeconds;
        if (now - _lastRefreshTime < RefreshIntervalSec)
            return;
        _lastRefreshTime = now;
        _window.RefreshAll();
    }

    /// <summary>
    /// Opens the health window for the given entity (e.g. after "Examine health" verb on another player).
    /// </summary>
    public void OpenForEntity(EntityUid entity)
    {
        if (_window == null)
            return;
        _window.UpdateEntity(entity);
        _window.Open();
    }

    public HealthWindow? Window => _window;
}
