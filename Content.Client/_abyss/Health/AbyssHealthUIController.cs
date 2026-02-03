using Content.Client._abyss.Health.UI;
using Content.Client.Gameplay;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._abyss.Health;

public sealed class AbyssHealthUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private HealthWindow? _window;

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
            _window.UpdateEntity(_player.LocalEntity);
            _window.Open();
        }
    }
}
