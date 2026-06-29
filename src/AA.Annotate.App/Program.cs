using Avalonia;
using System.Runtime.InteropServices;

namespace AA.Annotate.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (AppCommandLine.IsHelpRequested(args))
        {
            WriteHelp(AppCommandLine.HelpText);
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            var logFolder = Path.Combine(Path.GetTempPath(), "AA.Annotate");
            Directory.CreateDirectory(logFolder);
            File.WriteAllText(Path.Combine(logFolder, "last-crash.log"), exception.ToString());
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private const uint AttachParentProcess = 0xFFFFFFFF;
    private const int StandardOutputHandle = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        IntPtr hFile,
        byte[] lpBuffer,
        int nNumberOfBytesToWrite,
        out int lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    private static void WriteHelp(string text)
    {
        var output = text.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? text
            : text + Environment.NewLine;
        var bytes = System.Text.Encoding.UTF8.GetBytes(output);
        var handle = GetStdHandle(StandardOutputHandle);

        if (handle != IntPtr.Zero &&
            handle != new IntPtr(-1) &&
            WriteFile(handle, bytes, bytes.Length, out _, IntPtr.Zero))
        {
            return;
        }

        AttachConsole(AttachParentProcess);
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.Write(output);
    }
}
