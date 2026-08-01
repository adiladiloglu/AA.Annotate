using System.Runtime.InteropServices;

namespace AA.Annotate.Core.Services;

public sealed class AppPathResolver
{
    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly Func<Environment.SpecialFolder, string> _getFolderPath;
    private readonly Func<string> _getTempPath;
    private readonly Func<string> _getUserIdentifier;
    private readonly Func<string, bool> _isValidRuntimeDirectory;
    private readonly bool _isUnix;

    public AppPathResolver(
        Func<string, string?>? getEnvironmentVariable = null,
        Func<Environment.SpecialFolder, string>? getFolderPath = null,
        Func<string>? getTempPath = null,
        Func<string>? getUserIdentifier = null,
        Func<string, bool>? isValidRuntimeDirectory = null,
        bool? isUnix = null)
    {
        _getEnvironmentVariable = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        _getFolderPath = getFolderPath ?? Environment.GetFolderPath;
        _getTempPath = getTempPath ?? Path.GetTempPath;
        _getUserIdentifier = getUserIdentifier ?? GetCurrentUserIdentifier;
        _isValidRuntimeDirectory = isValidRuntimeDirectory ?? IsValidRuntimeDirectory;
        _isUnix = isUnix ?? !OperatingSystem.IsWindows();
    }

    public string GetPrivateRuntimeDirectory()
    {
        if (!_isUnix)
        {
            return Path.Combine(_getTempPath(), "AA.Annotate");
        }

        var xdgRuntimeDirectory = _getEnvironmentVariable("XDG_RUNTIME_DIR");
        var useXdgRuntimeDirectory =
            !string.IsNullOrWhiteSpace(xdgRuntimeDirectory) &&
            Path.IsPathFullyQualified(xdgRuntimeDirectory) &&
            _isValidRuntimeDirectory(xdgRuntimeDirectory);
        if (useXdgRuntimeDirectory)
        {
            var xdgAppRuntimeDirectory = Path.Combine(xdgRuntimeDirectory!, "aa-annotate");
            try
            {
                PrivateFileSystem.CreateDirectory(xdgAppRuntimeDirectory);
                return xdgAppRuntimeDirectory;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                // A runtime directory that cannot host a private child is not usable
                // by this user, even when its permission bits otherwise look valid.
            }
        }

        var root = Path.Combine(
            _getTempPath(),
            $"aa-annotate-runtime-{MakePathSegment(_getUserIdentifier())}");
        PrivateFileSystem.CreateDirectory(root);
        var appRuntimeDirectory = Path.Combine(root, "aa-annotate");
        PrivateFileSystem.CreateDirectory(appRuntimeDirectory);
        return appRuntimeDirectory;
    }

    public string GetConfigDirectory()
    {
        if (!_isUnix)
        {
            return Path.Combine(
                _getFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AA.Annotate");
        }

        var xdgConfigHome = _getEnvironmentVariable("XDG_CONFIG_HOME");
        var configRoot = !string.IsNullOrWhiteSpace(xdgConfigHome) &&
                         Path.IsPathFullyQualified(xdgConfigHome)
            ? xdgConfigHome
            : Path.Combine(
                _getFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");
        return Path.Combine(configRoot, "aa-annotate");
    }

    private static bool IsValidRuntimeDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path) ||
                new DirectoryInfo(path).LinkTarget is not null)
            {
                return false;
            }

            return OperatingSystem.IsWindows() ||
                   File.GetUnixFileMode(path) == PrivateFileSystem.PrivateDirectoryMode;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    private static string GetCurrentUserIdentifier()
    {
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                return GetUserId().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (
                exception is DllNotFoundException or EntryPointNotFoundException)
            {
                // Environment.UserName is a stable per-user fallback on unusual Unix runtimes.
            }
        }

        return Environment.UserName;
    }

    private static string MakePathSegment(string value)
    {
        var characters = value
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray();
        return characters.Length == 0 ? "user" : new string(characters);
    }

    [DllImport("libc", EntryPoint = "getuid")]
    private static extern uint GetUserId();
}
