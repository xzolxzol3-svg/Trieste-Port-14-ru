using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Link to Discord server to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksDiscord =
        CVarDef.Create("infolinks.discord", "https://discord.gg/JgDWMvf8WK", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to website to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksForum =
        CVarDef.Create("infolinks.forum", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to GitHub page to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksGithub =
        CVarDef.Create("infolinks.github", "https://github.com/xzolxzol3-svg/Trieste-Port-14-ru", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to website to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksWebsite =
        CVarDef.Create("infolinks.website", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to wiki to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksWiki =
        CVarDef.Create("infolinks.wiki", "http://217.26.25.88:3000/", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to Patreon. Not shown in the launcher currently.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksPatreon =
        CVarDef.Create("infolinks.patreon", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to the bug report form.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksBugReport =
        CVarDef.Create("infolinks.bug_report", "https://discord.gg/JgDWMvf8WK", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to site handling ban appeals. Shown in ban disconnect messages.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksAppeal =
        CVarDef.Create("infolinks.appeal", "", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Link to Telegram channel to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksTelegram =
        CVarDef.Create("infolinks.telegram", "", CVar.SERVER | CVar.REPLICATED);
}
