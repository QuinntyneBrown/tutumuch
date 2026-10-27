namespace TuTuMuch.Domain.Launch;

/// <summary>The launch phase chosen by configuration (L2-089, ADR platform/0014).</summary>
public enum LaunchPhase
{
    /// <summary>Only administrators sell; no category menus; fewer listings than the Phase 1 limit.</summary>
    One = 1,

    /// <summary>Only administrators sell; every category menu shows.</summary>
    Two = 2,

    /// <summary>Every verified member sells.</summary>
    Three = 3,
}

/// <summary>What each launch phase allows (L2-091 to L2-093).</summary>
public static class LaunchRules
{
    /// <summary>The six category links, the menu and the category pages show from Phase 2 (L2-093).</summary>
    public static bool CategoryMenusVisible(LaunchPhase phase) => phase != LaunchPhase.One;

    /// <summary>Members other than administrators sell only in Phase 3 (L2-091).</summary>
    public static bool MemberSellingOpen(LaunchPhase phase) => phase == LaunchPhase.Three;

    public static bool CanSell(LaunchPhase phase, bool isAdministrator) => MemberSellingOpen(phase) || isAdministrator;

    /// <summary>In Phase 1 a new listing must leave the catalog below the limit (L2-092).</summary>
    public static bool CanAddListing(LaunchPhase phase, int listings, int limit) => phase != LaunchPhase.One || listings + 1 < limit;

    /// <summary>In Phase 1 a draft publishes only while the catalog, the draft included, is below the limit (L2-092).</summary>
    public static bool CanPublish(LaunchPhase phase, int listings, int limit) => phase != LaunchPhase.One || listings < limit;
}
