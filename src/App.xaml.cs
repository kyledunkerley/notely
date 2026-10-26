using Microsoft.UI.Xaml;
namespace Acrux;
public partial class App : Application
{
    private Window? window;
    public App() { InitializeComponent(); UnhandledException += (_, e) => Log(e.Exception); }
    private static void Log(Exception ex) { try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup-error.txt"), ex.ToString()); } catch { } }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var smoke = Environment.GetCommandLineArgs().Contains("--smoke-test");
            var proof = Environment.GetCommandLineArgs().Contains("--design-proof");
            var main = new MainWindow(smoke || proof); window = main; main.Activate();
            if (proof) main.DispatcherQueue.TryEnqueue(async () => await main.RenderDesignProof());
            else if (smoke) main.DispatcherQueue.TryEnqueue(async () => await main.RunSmokeTests());
        }
        catch (Exception ex) { Log(ex); throw; }
    }
}

