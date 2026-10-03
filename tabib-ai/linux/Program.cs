using Avalonia;
using TabibAI.Linux.Core;

namespace TabibAI.Linux;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].StartsWith("--"))
        {
            switch (args[0])
            {
                case "--selftest":
                    return SelfTest.Run();
                case "--check":
                    return Cli.CheckAsync().GetAwaiter().GetResult();
                case "--ask":
                    return Cli.AskAsync(args.Skip(1).ToArray()).GetAwaiter().GetResult();
                case "--help":
                case "-h":
                    Cli.PrintHelp();
                    return 0;
            }
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
