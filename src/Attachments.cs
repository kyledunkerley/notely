using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
namespace Acrux;
public sealed partial class MainWindow
{
    async Task ImportAttachment(bool picture)
    {
        if (current == null) { status.Text = "Open a note before attaching a file."; return; }
        try
        {
            var picker = new FileOpenPicker(); foreach (var extension in picture ? new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" } : new[] { ".pdf", ".txt", ".md", ".docx", ".xlsx", ".zip" }) picker.FileTypeFilter.Add(extension);
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this)); var file = await picker.PickSingleFileAsync(); if (file == null) return;
            var folder = Path.Combine(Path.GetDirectoryName(current.Path)!, "assets"); Directory.CreateDirectory(folder); var name = file.Name; var count = 2;
            while (File.Exists(Path.Combine(folder, name))) name = Path.GetFileNameWithoutExtension(file.Name) + " " + count++ + Path.GetExtension(file.Name);
            File.Copy(file.Path, Path.Combine(folder, name));
            canvas.InsertMarkdown((picture ? "!" : "") + "[" + name.Replace("]", "\\]") + "](assets/" + Uri.EscapeDataString(name) + ")");
            status.Text = "Attached a copy of " + file.Name + ".";
        }
        catch (Exception ex) { status.Text = "Could not attach file: " + ex.Message; }
    }
    async Task ShowLinkDialog()
    {
        if (current == null) return;
        var text = new TextBox { Header = "Text", Text = canvas.SelectedText, PlaceholderText = "Link text" }; var url = new TextBox { Header = "Address", PlaceholderText = "https://" };
        var dialog = new ContentDialog { Title = "Insert a link", Content = new StackPanel { Width = 420, Spacing = 14, Children = { text, url } }, PrimaryButtonText = "Insert", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot };
        dialog.PrimaryButtonClick += (_, e) => { if (!Uri.TryCreate(url.Text, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "mailto")) { url.Header = "Enter an http, https or mailto address"; e.Cancel = true; } };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) canvas.InsertLink(string.IsNullOrWhiteSpace(text.Text) ? url.Text : text.Text, url.Text);
    }
}
