using System.Numerics;
using Content.Shared._abyss.Health;
using Content.Shared.FixedPoint;
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
    private string? _hoverSlot;
    private Dictionary<string, float>? _limbHealthFrac;

    /// <summary> Limb slot ID when a zone is clicked. </summary>
    public event Action<string>? OnLimbClicked;

    /// <summary> Slot IDs in draw order (back to front). </summary>
    private static readonly string[] DrawOrder =
    {
        "LLeg", "RLeg", "Torso", "LArm", "RArm", "Head"
    };

    /// <summary> Normalized rects (0-1) for each limb: (x, y, w, h). Front-facing humanoid. </summary>
    private static readonly Dictionary<string, (float x, float y, float w, float h)> LimbRects = new()
    {
        { "Head", (0.35f, 0f, 0.3f, 0.22f) },
        { "Torso", (0.3f, 0.22f, 0.4f, 0.38f) },
        { "LArm", (0.12f, 0.25f, 0.18f, 0.35f) },
        { "RArm", (0.7f, 0.25f, 0.18f, 0.35f) },
        { "LLeg", (0.32f, 0.58f, 0.18f, 0.35f) },
        { "RLeg", (0.5f, 0.58f, 0.18f, 0.35f) },
    };

    public BodyDollControl()
    {
        _sprite = new SpriteView
        {
            OverrideDirection = Direction.South,
            // Let the parent size the control; SpriteView will scale to fit.
            Stretch = SpriteView.StretchMode.Fit,
            Scale = Vector2.One,
            MouseFilter = MouseFilterMode.Ignore
        };
        AddChild(_sprite);
        MinSize = new Vector2(96, 96);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetEntity(EntityUid? uid)
    {
        // SpriteView больше не используется для отрисовки, оставляем метод пустым
    }

    public void SetEntity(EntityUid uid)
    {
        // SpriteView больше не используется для отрисовки, оставляем метод пустым
    }

    public void SetSelectedSlot(string? slotId)
    {
        _selectedSlot = slotId;
    }

    /// <summary>
    /// Sets per-limb health fraction (0..1). Used for coloring the doll.
    /// </summary>
    public void SetLimbHealthFractions(Dictionary<string, float>? fracBySlot)
    {
        _limbHealthFrac = fracBySlot;
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

            var isSelected = slot == _selectedSlot;
            var isHovered = slot == _hoverSlot;

            // Limb state fill (green/yellow/red) based on remaining HP fraction.
            var frac = 1f;
            if (_limbHealthFrac != null && _limbHealthFrac.TryGetValue(slot, out var f))
                frac = Math.Clamp(f, 0f, 1f);

            var fill = frac >= 0.85f
                ? new Color(0, 180, 0, 70)
                : frac >= 0.4f
                    ? new Color(230, 200, 0, 85)
                    : new Color(200, 0, 0, 90);

            handle.DrawRect(box, fill);

            // Selection / hover outline (subtle, doesn't overpower fill).
            if (isHovered)
                DrawOutline(handle, box, new Color(255, 255, 255, 140), 2);

            if (isSelected)
                DrawOutline(handle, box, new Color(120, 200, 255, 200), 3);
        }
    }

    private static void DrawOutline(DrawingHandleScreen handle, UIBox2 box, Color color, float thickness)
    {
        // Top
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Top, box.Width, thickness), color);
        // Bottom
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Bottom - thickness, box.Width, thickness), color);
        // Left
        handle.DrawRect(UIBox2.FromDimensions(box.Left, box.Top, thickness, box.Height), color);
        // Right
        handle.DrawRect(UIBox2.FromDimensions(box.Right - thickness, box.Top, thickness, box.Height), color);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        var newHover = GetSlotAt(args.RelativePosition);
        if (newHover == _hoverSlot)
            return;

        _hoverSlot = newHover;
        InvalidateArrange();
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        if (_hoverSlot == null)
            return;

        _hoverSlot = null;
        InvalidateArrange();
    }

    private string? GetSlotAt(Vector2 pos)
    {
        var size = Size;
        if (size.X <= 0 || size.Y <= 0)
            return null;

        var nx = pos.X / size.X;
        var ny = pos.Y / size.Y;

        foreach (var (slot, r) in LimbRects)
        {
            if (nx >= r.x && nx <= r.x + r.w && ny >= r.y && ny <= r.y + r.h)
                return slot;
        }

        return null;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        var slot = GetSlotAt(args.RelativePosition);
        if (slot != null)
            OnLimbClicked?.Invoke(slot);
    }
}
