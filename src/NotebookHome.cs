using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
namespace Acrux;
public sealed partial class MainWindow
{
    readonly List<string> notebooks = new(); readonly List<Action> tileRefreshers = new(); ScrollViewer? notebookHome;
    readonly DispatcherTimer tileTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    string CatalogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Southlight", "Acrux", "notebooks.json");
    void LoadCatalog()
    {
        try { if (File.Exists(CatalogPath)) notebooks.AddRange(JsonSerializer.Deserialize<List<string>>(File.ReadAllText(CatalogPath)) ?? new()); } catch (Exception ex) { status.Text = "Could not load notebooks: " + ex.Message; }
        tileTimer.Tick += (_, _) => { if (notebookHome?.Visibility == Visibility.Visible) foreach (var refresh in tileRefreshers) refresh(); }; tileTimer.Start();
    }
    void RegisterNotebook(string path)
    {
        path = Path.GetFullPath(path); if (!notebooks.Contains(path, StringComparer.OrdinalIgnoreCase)) notebooks.Add(path);
        try { Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!); File.WriteAllText(CatalogPath, JsonSerializer.Serialize(notebooks, new JsonSerializerOptions { WriteIndented = true })); } catch (Exception ex) { status.Text = "Could not save notebook list: " + ex.Message; }
    }
    void ShowNotebooks()
    {
        if (!TrySave()) return; SelectTopView("home"); wordCount.Text=notebooks.Count+" notebooks"; workspace.Visibility = Visibility.Collapsed; BuildNotebookHome(); if (notebookHome != null) notebookHome.Visibility = Visibility.Visible; if(modernCommands!=null)RevealCommands(modernCommands,true);
    }
    void ShowWriting()
    {
        wordCount.Text="Markdown / "+System.Text.RegularExpressions.Regex.Matches(NoteMeta.Body(draft),@"\b[\w'-]+\b").Count+" words"; SelectTopView("write"); if (notebookHome != null) notebookHome.Visibility = Visibility.Collapsed; workspace.Visibility = Visibility.Visible; if(modernCommands!=null)RevealCommands(modernCommands,false);if(writingTools!=null)RevealCommands(writingTools,false); ApplyResponsiveLayout(root.ActualWidth);
    }
    void BuildNotebookHome()
    {
        tileRefreshers.Clear(); var page = new Grid { Padding = new Thickness(30, 25, 35, 24), Background = palette.Window }; page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); page.ColumnDefinitions.Add(new ColumnDefinition());
        var sidePanel = new Grid { Margin = new Thickness(0, 0, 32, 0) }; sidePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); sidePanel.RowDefinitions.Add(new RowDefinition());
        var intro = new StackPanel { Spacing = 14, Margin = new Thickness(8, 10, 0, 18) }; var words = Label("Your\nideas,\nin the\nright place.", 34); words.FontFamily = new FontFamily("Georgia"); intro.Children.Add(words); var promise = Label("Beautiful notes\nfor a brighter\ntomorrow.", 18, .7); promise.FontFamily = new FontFamily("Georgia"); intro.Children.Add(promise); sidePanel.Children.Add(intro);
        var art = ConceptArtwork.SideLandscape(); if (art is FrameworkElement a) { a.Opacity = .65; a.VerticalAlignment = VerticalAlignment.Stretch; } Grid.SetRow((FrameworkElement)art, 1); sidePanel.Children.Add(art); page.Children.Add(sidePanel);
        var content = new StackPanel { Spacing = 18 }; var top = new Grid(); top.ColumnDefinitions.Add(new ColumnDefinition()); top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); top.Children.Add(Label("notebooks", 48)); var open = Action("Open a notebook", async () => await OpenVault()); open.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(open, 1); top.Children.Add(open); content.Children.Add(top);
        var tiles = new ItemsControl(); tiles.ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ItemsWrapGrid Orientation='Horizontal' MaximumRowsOrColumns='4'/></ItemsPanelTemplate>"); int index = 0;
        foreach (var notebook in notebooks.OrderBy(n => n.Contains("Concept Workspace", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(n => Path.GetFileName(n) == "Personal" ? 0 : 1))
        {
            var path = notebook; var variant = index++ % 4; var cover = new Grid { Width = 258, Height = 340 }; var scene = ConceptArtwork.Landscape(variant, true); cover.Children.Add(scene);
            var overlay = new Border { Background = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1), GradientStops = { new GradientStop { Color = Windows.UI.Color.FromArgb(0, 0, 0, 0), Offset = .28 }, new GradientStop { Color = Windows.UI.Color.FromArgb(variant is 1 or 3 ? (byte)120 : (byte)190, variant is 1 or 3 ? (byte)250 : (byte)18, variant is 1 or 3 ? (byte)240 : (byte)38, variant is 1 or 3 ? (byte)222 : (byte)45), Offset = 1 } } } }; cover.Children.Add(overlay);
            var labels = new StackPanel { Spacing = 8, Padding = new Thickness(20), VerticalAlignment = VerticalAlignment.Bottom }; var ink = new SolidColorBrush(ConceptPalette.Color(variant is 1 or 3 ? "343138" : "f6f4e9")); var name = Label(Path.GetFileName(path), 28); name.FontFamily = new FontFamily("Georgia"); name.Foreground = ink; labels.Children.Add(name); var description = Label(Path.GetFileName(path) switch { "Personal" => "Ideas, plans and the things\nthat make a life.", "Work" => "Projects, reference and\ndecisions.", "Projects" => "Curiosities and things\nworth exploring.", "Reading" => "Words worth keeping.\nIdeas worth returning to.", _ => "Stories, notes and\nunfinished thoughts." }, 12); description.Foreground = ink; labels.Children.Add(description); labels.Children.Add(new Border { Height = 1, Margin = new Thickness(0, 9, 0, 5), Background = ink, Opacity = .2 }); var counts = Label("", 10); counts.Foreground = ink; labels.Children.Add(counts); var edited = Label("", 9, .7); edited.Foreground = ink; labels.Children.Add(edited); cover.Children.Add(labels);
            var tile = new Button { Content = cover, Padding = new Thickness(0), BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(9), Margin = new Thickness(0, 0, 17, 18), Background = palette.Panel };
            Action refresh = () => { try { var files = Directory.EnumerateFiles(path, "*.md", SearchOption.AllDirectories).Select(p => new FileInfo(p)).ToList(); var folders = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Count(); counts.Text = folders + (folders==1?" section   ":" sections   ") + files.Count + (files.Count==1?" note":" notes"); var last = files.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault(); edited.Text = last == null ? "A fresh start" : "Last edited " + last.LastWriteTime.ToString("d MMM"); Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(tile, Path.GetFileName(path) + ", " + counts.Text); } catch { counts.Text = "Folder currently unavailable"; } }; refresh(); tileRefreshers.Add(refresh); tile.Click += (_, _) => OpenNotebook(path); ToolTipService.SetToolTip(tile, path); tiles.Items.Add(tile);
        }
        content.Children.Add(tiles); var newNotebook = Action("\u2295  New notebook", async () => await CreateNotebook()); newNotebook.HorizontalAlignment = HorizontalAlignment.Left; newNotebook.FontSize = 16; content.Children.Add(newNotebook); Grid.SetColumn(content, 1); page.Children.Add(content);
        page.SizeChanged += (_, e) => { var narrow = e.NewSize.Width < 1050; page.ColumnDefinitions[0].Width = new GridLength(narrow ? 0 : 240); sidePanel.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible; };
        if (notebookHome == null) { notebookHome = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; Grid.SetRow(notebookHome, 1); root.Children.Add(notebookHome); } notebookHome.Content = page;
    }
    StackPanel Metric(int number, string caption) { var panel = new StackPanel(); panel.Children.Add(Label(number.ToString(), 34)); panel.Children.Add(Label(caption, 11)); return panel; }
    async Task CreateNotebook()
    {
        var input = new TextBox { PlaceholderText = "Notebook name" }; var dialog = new ContentDialog { Title = "A new notebook", Content = input, PrimaryButtonText = "Create", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot }; if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (!ValidName(input.Text.Trim())) { status.Text = "Choose a valid notebook name."; return; } try { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Southlight", "Acrux", "Notebooks", input.Text.Trim()); Directory.CreateDirectory(path); RegisterNotebook(path); ShowNotebooks(); } catch (Exception ex) { status.Text = "Could not create notebook: " + ex.Message; }
    }
}


