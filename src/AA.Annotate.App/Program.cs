using Avalonia;

namespace AA.Annotate.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
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
}
