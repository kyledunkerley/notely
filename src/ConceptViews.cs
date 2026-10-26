using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    void RefreshConceptInspector()
    {
        if (current == null) { tagChips.Children.Clear(); sourceCards.Children.Clear(); properties.Children.Clear(); relatedCards.Children.Clear(); return; }
        subtitle.Text = NoteMeta.Subtitle(draft); sourcePreview.Text = draft;
        tagChips.Children.Clear(); var tags = NoteMeta.Tags(draft); var tagGrid = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = 3, ItemWidth = 86, ItemHeight = 32 };
        foreach (var tag in tags) { var value = tag; var chip = Action(tag + "  \u00d7", () => { var next = NoteMeta.Tags(draft); next.Remove(value); CommitMetadata(NoteMeta.Set(draft, "tags", next)); }); chip.Background = palette.Selection; chip.CornerRadius = new CornerRadius(4); chip.FontSize = 11; chip.Padding = new Thickness(9, 5, 9, 5); chip.Margin = new Thickness(0, 0, 5, 5); tagGrid.Children.Add(chip); }
        var addTag = Action("+ Add tag", async () => await AddTag()); addTag.FontSize = 11; addTag.Padding = new Thickness(5); tagGrid.Children.Add(addTag); tagChips.Children.Add(tagGrid);
        sourceCards.Children.Clear(); sourceCards.Spacing = 3; foreach (var source in NoteMeta.Sources(draft))
        {
            var value = source; var text = new StackPanel { Spacing = 1 }; text.Children.Add(IconText("\uE8A5", source.Title, 12)); text.Children.Add(Label("     " + (source.Detail.Length > 0 ? source.Detail : source.Url), 10, .65)); var button = Action("", async () => { if (Uri.TryCreate(value.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") await Windows.System.Launcher.LaunchUriAsync(uri); }); button.Content = text; button.Padding = new Thickness(0); button.MinHeight = 0; button.Height = 29; button.HorizontalContentAlignment = HorizontalAlignment.Left; AttachReferenceMenu(button,value); sourceCards.Children.Add(button);
        }
        if (sourceCards.Children.Count == 0) sourceCards.Children.Add(Label("Keep your references close.", 12, .45));
        properties.Children.Clear(); properties.Children.Add(IconText("\uE787", File.GetLastWriteTime(current.Path).ToString("MMM d, yyyy  h:mm tt"), 11)); properties.Children.Add(IconText("\uE8B7", Path.GetFileName(vault) + " / " + (current.Group.Length == 0 ? "Notes" : current.Group), 11)); properties.Children.Add(IconText("\uE8EC", tags.Count + " tags", 11)); var links = Regex.Matches(draft, @"\[\[([^\]]+)\]\]").Select(m => m.Groups[1].Value).Distinct().ToList(); properties.Children.Add(IconText("\uE71B", all.Count(n => n.Path != current.Path && (links.Contains(n.Name, StringComparer.OrdinalIgnoreCase) || n.Body.Contains("[[" + current.Name + "]]", StringComparison.OrdinalIgnoreCase))) + " linked notes", 11));
        relatedCards.Children.Clear(); var related = all.Where(n => n.Path != current.Path && links.Any(l => l.Split('|')[0].Split('#')[0].Equals(n.Name, StringComparison.OrdinalIgnoreCase))).DistinctBy(n => n.Path).OrderBy(n => n.Name == "The Attention Economy" ? 0 : n.Name == "Designing for Well-Being" ? 1 : 2).Take(3).ToList();
        foreach (var note in related)
        {
            var picked = note; var content = new StackPanel { Spacing = 5, Width = 225 }; content.Children.Add(IconText("\uE8A5", note.Name, 13)); var summary = Label(NoteMeta.Subtitle(note.Body), 10, .65); summary.MaxLines = 2; summary.TextTrimming = TextTrimming.CharacterEllipsis; content.Children.Add(summary);
            var card = Action("", () => NavigateTo(picked)); card.Content = content; card.BorderBrush = palette.Line; card.BorderThickness = new Thickness(1); card.CornerRadius = new CornerRadius(4); card.Padding = new Thickness(10, 8, 10, 8); card.HorizontalContentAlignment = HorizontalAlignment.Left; relatedCards.Children.Add(card);
        }
        wordCount.Text = "Markdown  \u00b7  " + Regex.Matches(NoteMeta.Body(draft), @"\b[\w'-]+\b").Count + " words"; savedCaption.Text = dirty ? "Saving..." : "\u25cf  Saved locally";
        RefreshNotebookStrip(); RefreshRailSections();
    }
    void RefreshNotebookStrip()
    {
        if(notebookPicker != null) { notebookPicker.DisplayMemberPath = "Name"; notebookPicker.ItemsSource = notebooks.Select(p => new NotebookChoice(p,Path.GetFileName(p))).ToList(); notebookPicker.SelectedIndex = notebooks.FindIndex(p => p.Equals(vault,StringComparison.OrdinalIgnoreCase)); }
        notebookStrip.Children.Clear(); var ordered = new[] { vault }.Concat(notebooks).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList(); var icons = new[] { "\uE734", "\uE70F", "\uE774", "\uE8A5" }; int i = 0;
        foreach (var path in ordered)
        {
            var folder = path; int count; try { count = Directory.EnumerateFiles(path, "*.md", SearchOption.AllDirectories).Count(); } catch { count = 0; }
            var content = new StackPanel { Spacing = 7, Width = 52 }; content.Children.Add(new FontIcon { Glyph = icons[i++ % icons.Length], FontSize = 25, Foreground = palette.Ink, Margin = new Thickness(0, 5, 0, 5) }); var name = Label(Path.GetFileName(path), 11); name.MaxLines = 1; name.TextTrimming = TextTrimming.CharacterEllipsis; content.Children.Add(name); content.Children.Add(Label(count + (count == 1 ? " note" : " notes"), 9, .65));
            var button = Action("", () => OpenNotebook(folder)); button.Content = content; button.Height = 108; button.Padding = new Thickness(7, 8, 7, 7); button.BorderThickness = new Thickness(1); button.BorderBrush = path.Equals(vault, StringComparison.OrdinalIgnoreCase) ? palette.Accent : palette.Line; button.Background = path.Equals(vault, StringComparison.OrdinalIgnoreCase) ? palette.Selection : palette.Panel; button.CornerRadius = new CornerRadius(5); notebookStrip.Children.Add(button);
        }
    }
    void RefreshRailSections()
    {
        sectionTiles.Children.Clear(); var sections = new[] { "" }.Concat(all.Select(n => n.Group).Where(g => g.Length > 0)).Concat(Directory.Exists(vault) ? Directory.EnumerateDirectories(vault).Select(p => Path.GetRelativePath(vault, p)) : Array.Empty<string>()).Distinct().Take(6).ToList();
        foreach (var section in sections)
        {
            var group = section; var button = Action((section.Length == 0 ? "All notes" : Path.GetFileName(section)), () => { SetFolder(group); noteListOpen = true; ApplyResponsiveLayout(root.ActualWidth); }); button.Width = 91; button.Height = 35; button.FontSize = 11; button.Padding = new Thickness(4); button.Margin = new Thickness(0, 0, 5, 5); button.CornerRadius = new CornerRadius(4); button.BorderThickness = new Thickness(1); button.BorderBrush = section == activeFolder ? palette.Accent : palette.Line; button.Background = section == activeFolder ? palette.Selection : palette.Panel; sectionTiles.Children.Add(button);
        }
    }
    void CommitMetadata(string text) { draft = text; if(!canvas.UpdateMetadata(text))canvas.Load(current!.Path, text); ScheduleSave(); RefreshConceptInspector(); }
    async Task AddTag()
    {
        if (current == null) return; var input = new TextBox { PlaceholderText = "Tag name" }; var dialog = new ContentDialog { Title = "Add a tag", Content = input, PrimaryButtonText = "Add", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && input.Text.Trim().Length > 0) { var tags = NoteMeta.Tags(draft); if (!tags.Contains(input.Text.Trim(), StringComparer.OrdinalIgnoreCase)) tags.Add(input.Text.Trim()); CommitMetadata(NoteMeta.Set(draft, "tags", tags)); }
    }
    async Task AddSource()
    {
        if (current == null) return; var name = new TextBox { Header = "Title" }; var url = new TextBox { Header = "Address", PlaceholderText = "https://" }; var detail = new TextBox { Header = "Author or description" };
        var dialog = new ContentDialog { Title = "Add a reference", Content = new StackPanel { Spacing = 12, Width = 420, Children = { Label("Saved with this note. Not embedded or inserted into its text.",13,.65), name, url, detail } }, PrimaryButtonText = "Add", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot };
        dialog.PrimaryButtonClick += (_, e) => { if (name.Text.Trim().Length == 0 || (url.Text.Length > 0 && (!Uri.TryCreate(url.Text, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")))) { e.Cancel = true; url.Header = "Use an http or https address, or leave blank"; } };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) { var sources = NoteMeta.Sources(draft); sources.Add(new NoteMeta.Source(name.Text.Trim(), url.Text.Trim(), detail.Text.Trim())); CommitMetadata(NoteMeta.Set(draft, "source_links", sources)); }
    }
    async Task LinkAnotherNote()
    {
        if (current == null) return; var chosen = await PickCommand("Link another note", all.Where(n => n.Path != current.Path).Select(n => new Command(n.Name, n.Group, () => canvas.InsertMarkdown("[[" + n.Name + "]]"))).ToList()); chosen?.Run();
    }
    Grid Overlay(string titleText, string description)
    {
        if (overlayView != null) root.Children.Remove(overlayView);
        overlayView = new Grid { Background = palette.Window, Padding = new Thickness(48, 26, 48, 28) }; overlayView.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); overlayView.RowDefinitions.Add(new RowDefinition()); var header = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 25) }; header.Children.Add(Label(titleText, 48)); header.Children.Add(Label(description, 15, .6)); overlayView.Children.Add(header); Grid.SetRow(overlayView, 1); root.Children.Add(overlayView); return overlayView;
    }
    void ShowLegacyConnections(bool graph = true)
    {
        if (!TrySave()) return; SelectTopView("connect"); workspace.Visibility = Visibility.Collapsed; if (notebookHome != null) notebookHome.Visibility = Visibility.Collapsed;
        var page = Overlay(graph ? "a constellation of ideas" : "connections", "Follow a thought. See where it leads.");
        if (graph)
        {
            var scene = new Canvas { Width = 1000, Height = 600 }; var nodes = all.Take(20).ToList(); var positions = nodes.Select((n, i) => new Windows.Foundation.Point(500 + Math.Cos(i * Math.PI * 2 / Math.Max(1, nodes.Count)) * 330, 290 + Math.Sin(i * Math.PI * 2 / Math.Max(1, nodes.Count)) * 220)).ToList();
            for (int i = 0; i < nodes.Count; i++) foreach (var link in Regex.Matches(nodes[i].Body, @"\[\[([^\]]+)\]\]").Select(m => m.Groups[1].Value)) { var target = nodes.FindIndex(n => n.Name.Equals(link.Split('|')[0].Split('#')[0], StringComparison.OrdinalIgnoreCase)); if (target >= 0) scene.Children.Add(new Microsoft.UI.Xaml.Shapes.Line { X1 = positions[i].X, Y1 = positions[i].Y, X2 = positions[target].X, Y2 = positions[target].Y, Stroke = palette.Accent, StrokeThickness = 1, Opacity = .4 }); }
            for (int i = 0; i < nodes.Count; i++) { var note = nodes[i]; var node = Action(note.Name, () => NavigateTo(note)); node.Background = palette.Selection; node.BorderBrush = palette.Accent; node.BorderThickness = new Thickness(1); node.CornerRadius = new CornerRadius(12); Canvas.SetLeft(node, positions[i].X - 80); Canvas.SetTop(node, positions[i].Y - 15); scene.Children.Add(node); }
            var scroll = new ScrollViewer { Content = scene, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, ZoomMode = ZoomMode.Enabled, MinZoomFactor=.4f, MaxZoomFactor=2f }; Grid.SetRow(scroll, 1); page.Children.Add(scroll);
        }
        else
        {
            var list = new StackPanel { Spacing = 12 }; foreach (var note in all)
            {
                var picked = note; var links = Regex.Matches(note.Body, @"\[\[([^\]]+)\]\]").Select(m => m.Groups[1].Value).Distinct().ToList(); if (links.Count == 0) continue;
                var content = new StackPanel { Spacing = 7 }; content.Children.Add(Label(note.Name, 24)); content.Children.Add(Label(string.Join("   \u2192   ", links), 14, .65)); var button = Action("", () => NavigateTo(picked)); button.Content = content; button.HorizontalContentAlignment = HorizontalAlignment.Left; button.BorderThickness = new Thickness(0, 0, 0, 1); button.BorderBrush = palette.Line; button.Padding = new Thickness(18); list.Children.Add(button);
            }
            if (list.Children.Count == 0) list.Children.Add(Label("Link two notes with [[Note title]] to begin a connection.", 20, .5)); var scroll = new ScrollViewer { Content = list }; Grid.SetRow(scroll, 1); page.Children.Add(scroll);
        }
    }
    void ShowLegacyReview()
    {
        if (!TrySave()) return; SelectTopView("review"); workspace.Visibility = Visibility.Collapsed; if (notebookHome != null) notebookHome.Visibility = Visibility.Collapsed;
        var page = Overlay("a moment to review", "Recent thoughts and unfinished things, gathered in one place."); var columns = new Grid(); columns.ColumnDefinitions.Add(new ColumnDefinition()); columns.ColumnDefinitions.Add(new ColumnDefinition()); var recent = new StackPanel { Spacing = 12, Margin = new Thickness(0, 0, 30, 0) }; recent.Children.Add(Label("recent notes", 28));
        foreach (var note in all.OrderByDescending(n => File.GetLastWriteTime(n.Path)).Take(10)) { var picked = note; var b = Action(note.Name, () => NavigateTo(picked)); b.FontSize = 20; b.HorizontalContentAlignment = HorizontalAlignment.Left; recent.Children.Add(b); }
        columns.Children.Add(recent); var tasks = new StackPanel { Spacing = 12 }; tasks.Children.Add(Label("still to do", 28));
        foreach (var note in all) foreach (var task in Regex.Matches(note.Body, @"(?m)^- \[ \] (.+)$").Take(12)) { var picked = note; var row = new StackPanel { Spacing = 4 }; row.Children.Add(Label("\u25a1   " + task.Groups[1].Value.Trim(), 17)); row.Children.Add(Label("     " + note.Name, 11, .6)); var button = Action("", () => NavigateTo(picked)); button.Content = row; button.HorizontalContentAlignment = HorizontalAlignment.Left; tasks.Children.Add(button); }
        if (tasks.Children.Count == 1) tasks.Children.Add(Label("Nothing waiting. Room for something new.", 16, .5)); Grid.SetColumn(tasks, 1); columns.Children.Add(tasks); var scroll = new ScrollViewer { Content = columns }; Grid.SetRow(scroll, 1); page.Children.Add(scroll);
    }
    void SeedConceptNotebooks()
    {
        var workspaceRoot = smokeMode ? Path.Combine(AppContext.BaseDirectory, "concept-fixture") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Southlight", "Acrux", "Concept Workspace");
        foreach (var name in new[] { "Personal", "Work", "Projects", "Reading" })
        {
            var path = Path.Combine(workspaceRoot, name); if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                if (name == "Personal")
                {
                    foreach (var section in new[] { "Ideas", "Journal", "Plans", "References", "Archive" }) Directory.CreateDirectory(Path.Combine(path, section));
                    var text = "# A Calmer Internet\n\nNotes on building a more humane, hopeful web.\n\nThe internet doesn't feel broken because of technology.\nIt feels broken because we lost sight of what it's for.\n\n> A calmer internet is possible. One designed for depth, kindness,\n> and a longer view of what it means to be human together.\n\nThis note collects ideas, references, and experiments toward that vision.\nIt's not a plan yet, more a constellation of thoughts.\n\n## Principles\n\n1. **People over metrics.** Design for human well-being, not just attention.\n2. **Depth over speed.** Slow is a feature.\n3. **Ownership over dependency.** Give people their data and their voice.\n4. **Small, kind communities.** Meaningful conversation scales differently.\n5. **A hopeful future.** Technology should expand what's good in us.\n";
                    text = NoteMeta.Set(text, "tags", new[] { "internet", "design", "humanity", "well-being", "community" }); text = NoteMeta.Set(text, "source_links", new[] { new NoteMeta.Source("The Shallows", "", "N. Carr (2010)"), new NoteMeta.Source("Center for Humane Tech", "https://www.humanetech.com"), new NoteMeta.Source("A More Human Web", "", "J. Harris (2023)") });
                    File.WriteAllText(Path.Combine(path, "Ideas", "A Calmer Internet.md"), text);
                    File.WriteAllText(Path.Combine(path, "Ideas", "Designing for Well-Being.md"), "# Designing for Well-Being\n\nA collection of principles and case studies.\n\nGood tools should leave us with more time and attention for what matters.\n\n[[A Calmer Internet]]\n");
                    File.WriteAllText(Path.Combine(path, "Plans", "Small Communities.md"), "# Small Communities\n\nExamples of thriving, human-scale online spaces.\n\nSmall communities make room for meaningful conversation.\n\n[[A Calmer Internet]]\n");
                    File.WriteAllText(Path.Combine(path, "References", "The Attention Economy.md"), "# The Attention Economy\n\nWhy our attention is valuable and how to protect it.\n\nA place to collect ideas about attention, habits, and deliberate technology.\n\n[[A Calmer Internet]]\n");
                }
                else { Directory.CreateDirectory(Path.Combine(path, "Ideas")); File.WriteAllText(Path.Combine(path, "Ideas", name == "Work" ? "A thoughtful workday.md" : name == "Projects" ? "Things worth making.md" : "The reading list.md"), "# " + (name == "Work" ? "A thoughtful workday" : name == "Projects" ? "Things worth making" : "The reading list") + "\n\nA fresh notebook for " + name.ToLowerInvariant() + ".\n\n## A place to begin\n\n- [ ] Make room for the next idea\n"); }
            }
            if (smokeMode) { if (!notebooks.Contains(path)) notebooks.Add(path); } else RegisterNotebook(path);
        }
        vault = Path.Combine(workspaceRoot, "Personal");
    }
}





