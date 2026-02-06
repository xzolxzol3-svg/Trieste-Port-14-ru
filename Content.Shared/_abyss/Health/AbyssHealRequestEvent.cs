using Robust.Shared.Serialization;

namespace Content.Shared._abyss.Health;

/// <summary>
/// Событие, которое отправляется от клиента к серверу, когда игрок хочет похилить через панель.
/// </summary>
[Serializable, NetSerializable]
public sealed class AbyssHealRequestEvent : EntityEventArgs
{
    public NetEntity Item;
    public NetEntity Target;
    public string? TargetLimb;

    public AbyssHealRequestEvent(NetEntity item, NetEntity target, string? targetLimb = null)
    {
        Item = item;
        Target = target;
        TargetLimb = targetLimb;
    }
}
