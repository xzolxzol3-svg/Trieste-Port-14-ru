using System.Numerics;
using Content.Shared._abyss.Health;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client._abyss.Health.UI;

/// <summary>
/// Interactive body doll: sprite + clickable limb zones. Highlights damaged limbs in red.
/// </summary>
public sealed class BodyDollControl : Control
{
    private readonly SpriteView _sprite;
    private string? _selectedSlot;
    private HashSet<string>? _damagedSlots;

    /// <summary> Limb slot ID when a zone is clicked. </summary>
    public event Action<string>? OnLimbClicked;

    /// <summary> Slot IDs in draw order (back to front). </summary>
    private static readonly string[] DrawOrder =
    {
        "LLeg", "RLeg", "LFoot", "RFoot", "Torso", "LArm", "RArm", "LHand", "RHand", "Head"
    };

    /// <summary> Normalized rects (0-1) for each limb: (x, y, w, h). Front-facing humanoid. </summary>
    private static readonly Dictionary<string, (float x, float y, float w, float h)> LimbRects = new()
    {
        { "Head", (0.35f, 0f, 0.3f, 0.22f) },
        { "Torso", (0.3f, 0.22f, 0.4f, 0.38f) },
        { "LArm", (0.12f, 0.25f, 0.18f, 0.35f) },
        { "RArm", (0.7f, 0.25f, 0.18f, 0.35f) },
        { "LHand", (0.05f, 0.58f, 0.12f, 0.15f) },
        { "RHand", (0.83f, 0.58f, 0.12f, 0.15f) },
        { "LLeg", (0.32f, 0.58f, 0.18f, 0.35f) },
        { "RLeg", (0.5f, 0.58f, 0.18f, 0.35f) },
        { "LFoot", (0.28f, 0.9f, 0.14f, 0.1f) },
        { "RFoot", (0.58f, 0.9f, 0.14f, 0.1f) }
    };

    public BodyDollControl()
    {
        _sprite = new SpriteView
        {
            OverrideDirection = Direction.South,
            Scale = new Vector2(3f, 3f),
            SetSize = new Vector2(96, 96),
            MouseFilter = MouseFilterMode.Ignore
        };
        AddChild(_sprite);
        MinSize = new Vector2(96, 96);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetEntity(EntityUid? uid)
    {
        _sprite.SetEntity(uid);
    }

    public void SetEntity(EntityUid uid)
    {
        _sprite.SetEntity(uid);
    }

    public void SetSelectedSlot(string? slotId)
    {
        _selectedSlot = slotId;
    }

    public void SetDamagedSlots(IEnumerable<string>? slotIds)
    {
        _damagedSlots = slotIds != null ? new HashSet<string>(slotIds) : null;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var size = Size;
        if (size.X <= 0 || size.Y <= 0)
            return;

        foreach (var slot in DrawOrder)
        {
            if (!LimbRects.TryGetValue(slot, out var r))
                continue;
            var x = r.x * size.X;
            var y = r.y * size.Y;
            var w = r.w * size.X;
            var h = r.h * size.Y;
            var box = UIBox2.FromDimensions(x, y, w, h);

            var isDamaged = _damagedSlots != null && _damagedSlots.Contains(slot);
            var isSelected = slot == _selectedSlot;

            if (isDamaged)
            {
                handle.DrawRect(box, new Color(180, 0, 0, 80));
            }
            if (isSelected)
            {
                handle.DrawRect(box, new Color(255, 200, 0, 60));
            }
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        var pos = args.RelativePosition;
        var size = Size;
        if (size.X <= 0 || size.Y <= 0)
            return;

        var nx = pos.X / size.X;
        var ny = pos.Y / size.Y;

        foreach (var (slot, r) in LimbRects)
        {
            if (nx >= r.x && nx <= r.x + r.w && ny >= r.y && ny <= r.y + r.h)
            {
                OnLimbClicked?.Invoke(slot);
                break;
            }
        }
    }
}
