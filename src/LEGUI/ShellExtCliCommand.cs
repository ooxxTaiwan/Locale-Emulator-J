#nullable enable

namespace LEGUI;

/// <summary>
/// Parser for --shell-ext command-line arguments.
///
/// Usage:
///   LEGUI.exe --shell-ext install current-user
///   LEGUI.exe --shell-ext install all-users
///   LEGUI.exe --shell-ext uninstall current-user
///   LEGUI.exe --shell-ext uninstall all-users
///   LEGUI.exe --shell-ext cleanup-old
/// </summary>
public sealed class ShellExtCliCommand
{
    /// <summary>
    /// Verb (action) for Shell Extension command.
    /// </summary>
    public enum Verb { Install, Uninstall, CleanupOld }

    /// <summary>
    /// Parsed verb.
    /// </summary>
    public Verb ActionVerb { get; }

    /// <summary>
    /// Parsed install mode. For CleanupOld, defaults to AllUsers (cleanup both HKCU and HKLM).
    /// </summary>
    public ShellExtensionRegistrar.InstallMode Mode { get; }

    private ShellExtCliCommand(Verb verb, ShellExtensionRegistrar.InstallMode mode)
    {
        ActionVerb = verb;
        Mode = mode;
    }

    /// <summary>
    /// Parse command-line arguments for --shell-ext command.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Parsed command, or null if args do not match --shell-ext pattern.</returns>
    public static ShellExtCliCommand? Parse(string[] args)
    {
        if (args.Length < 2 || args[0] != "--shell-ext") return null;

        return args[1] switch
        {
            "install" when args.Length == 3 && TryParseMode(args[2], out var im)
                => new ShellExtCliCommand(Verb.Install, im),
            "uninstall" when args.Length == 3 && TryParseMode(args[2], out var um)
                => new ShellExtCliCommand(Verb.Uninstall, um),
            "cleanup-old" when args.Length == 2
                => new ShellExtCliCommand(Verb.CleanupOld, ShellExtensionRegistrar.InstallMode.AllUsers),
            _ => null
        };
    }

    private static bool TryParseMode(string s, out ShellExtensionRegistrar.InstallMode mode)
    {
        mode = default;
        if (s == "current-user") { mode = ShellExtensionRegistrar.InstallMode.CurrentUser; return true; }
        if (s == "all-users")    { mode = ShellExtensionRegistrar.InstallMode.AllUsers; return true; }
        return false;
    }
}
