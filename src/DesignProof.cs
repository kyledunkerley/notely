using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
namespace Acrux;
public sealed partial class MainWindow
{
    internal async Task RenderDesignProof()
    {
        try
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "proof-trace.txt"), "Entered proof renderer. Root " + root.ActualWidth + "x" + root.ActualHeight);
            if (!root.IsLoaded) { var loaded = new TaskCompletionSource(); root.Loaded += (_, _) => loaded.TrySetResult(); await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
            current = null; dirty = false; notebooks.Clear(); SeedConceptNotebooks(); LoadVault(); ShowWriting(); await Task.Delay(700); await ExportVisual("concept-write.png");
            ShowCapture(); await Task.Delay(250); await ExportVisual("concept-capture.png");
            ShowNotebooks(); await Task.Delay(250); await ExportVisual("concept-notebooks.png");
            ShowConnections(); await Task.Delay(250); await ExportVisual("concept-connect.png");
            ShowReview(); await Task.Delay(250); await ExportVisual("concept-review.png");
            ShowWriting(); AppWindow.Resize(new Windows.Graphics.SizeInt32(976,850)); await Task.Delay(600); await ExportVisual("concept-half.png");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(416,850)); await Task.Delay(600); await ExportVisual("concept-sidebar.png");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "design-proof-result.txt"), "Rendered writing, notebooks, connections and review using the real WinUI visual tree. Size: " + root.ActualWidth + " x " + root.ActualHeight);
        }
        catch (Exception ex) { WriteDiagnostic(ex); }
        finally { dirty = false; Application.Current.Exit(); }
    }
    async Task ExportVisual(string name)
    {
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "proof-trace.txt"), "\nRendering " + name + " " + root.ActualWidth + "x" + root.ActualHeight);
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0); using var output = new DataReader(stream.GetInputStreamAt(0)); await output.LoadAsync((uint)stream.Size); var png = new byte[stream.Size]; output.ReadBytes(png); File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, name), png);
    }
}



