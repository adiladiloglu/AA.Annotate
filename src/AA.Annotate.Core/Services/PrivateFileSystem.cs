using System.Text;

namespace AA.Annotate.Core.Services;

public static class PrivateFileSystem
{
    public const UnixFileMode PrivateDirectoryMode =
        UnixFileMode.UserRead |
        UnixFileMode.UserWrite |
        UnixFileMode.UserExecute;

    public const UnixFileMode PrivateFileMode =
        UnixFileMode.UserRead |
        UnixFileMode.UserWrite;

    public static void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!OperatingSystem.IsWindows() &&
            Directory.Exists(path) &&
            new DirectoryInfo(path).LinkTarget is not null)
        {
            throw new IOException($"Refusing to use a symbolic link as a private directory: {path}");
        }

        Directory.CreateDirectory(path);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, PrivateDirectoryMode);
        }
    }

    public static FileStream CreateFile(string path, bool useAsync = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 4096,
            Options = useAsync ? FileOptions.Asynchronous : FileOptions.None
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = PrivateFileMode;
        }

        var stream = new FileStream(path, options);
        try
        {
            ProtectFile(path);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public static void ProtectFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, PrivateFileMode);
        }
    }

    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        using var stream = CreateFile(path, useAsync: false);
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(contents);
    }
}
