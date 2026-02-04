

namespace Content.Shared._abyss.Health;

/// <summary>
/// Abyss-14: context flag to distinguish panel-driven healing from normal world interactions.
/// </summary>
public static class AbyssHealingContext
{
    // Заменили AsyncLocal на обычный bool
    public static bool FromHealthPanel = false; 
}
