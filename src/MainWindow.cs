using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace Acrux;

public sealed partial class MainWindow : Window
{
    readonly Grid root = new();
    readonly Grid workspace = new();
    readonly NoteCanvas canvas = new();
    readonly StackPanel folders = new() { Spacing = 4 };
    readonly ListView notes = new() { SelectionMode = ListViewSelectionMode.Single };
    readonly StackPanel paper = new() { Spacing = 16, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly StackPanel outline = new() { Spacing = 12 };
    string draft = "";
    readonly TextBox title = new() { FontSize = 36, FontWeight = Microsoft.UI.Text.FontWeights.SemiLight, BorderThickness = new Thickness(0), Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
    readonly TextBlock breadcrumb = new();
    readonly TextBlock status = new();
    readonly TextBlock listTitle = new() { Text = "all notes", FontSize = 36, FontWeight = Microsoft.UI.Text.FontWeights.Light };
    readonly TextBlock folderLabel = new();
    readonly AutoSuggestBox search = new() { PlaceholderText = "Search your notes", Width = 310, QueryIcon = new SymbolIcon(Symbol.Find) };
    readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    readonly List<Note> all = new();
    readonly Border side = new();
    readonly Border context = new();
    string vault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Southlight", "Acrux", "Notebook");
    string activeFolder = "";
    Note? current;
    bool loading, focus, dirty, smokeMode, detailsOpen;
    string noteFilter = "all";
    readonly Dictionary<string, Button> sectionButtons = new();
    readonly Border homePivotLine = new(), writePivotLine = new();
    record Note(string Path, string Group, string Name, string Body);
    SolidColorBrush Accent => palette.Accent;
    TextBlock Label(string text, double size = 14, double opacity = 1) => new() { Text = text, FontSize = size, Opacity = opacity, Foreground = palette.Ink, TextWrapping = TextWrapping.Wrap };
    Button Action(string text, Action click, string tip = "")
    {
        var b = new Button { Foreground = palette.Ink, CornerRadius = new CornerRadius(4), Content = text, Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(0), Padding = new Thickness(12, 8, 12, 8), HorizontalAlignment = HorizontalAlignment.Stretch };
        b.Click += (_, _) => click(); ToolTipService.SetToolTip(b, string.IsNullOrEmpty(tip) ? text : tip); return b;
    }
    StackPanel Row(params UIElement[] items) { var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; foreach (var x in items) p.Children.Add(x); return p; }
    Border Line() => new() { Height = 1, Background = palette.Line, Margin = new Thickness(0, 12, 0, 12) };
    public MainWindow(bool smokeTest = false)
    {
        smokeMode = smokeTest; if (smokeMode) vault = Path.Combine(AppContext.BaseDirectory, "smoke-data-" + Guid.NewGuid().ToString("N"));
        palette.Apply("Dawn"); root.Background = palette.Window;
        Title = "Acrux | Southlight I/O";
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1624, 946));
        root.RequestedTheme = ElementTheme.Light;
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = root;
        BuildHeader(); BuildWorkspace(); BuildStatus();
        Seed(); if (smokeMode) notebooks.Add(vault); else { LoadCatalog(); RegisterNotebook(vault); SeedConceptNotebooks(); } SetConceptTheme(smokeMode ? "Dawn" : SavedConceptTheme()); LoadVault(); if (smokeMode) ShowNotebooks(); else ShowWriting();
        search.TextChanged += (_, _) => { if (!loading) RefreshNotes(); };
        notes.SelectionChanged += (_, _) => { if (notes.SelectedItem is ListViewItem i && i.Tag is Note n) Select(n); };
        canvas.RequestLink += async () => await ShowLinkDialog(); canvas.OpenNote += OpenLinkedNote; canvas.MarkdownChanged += markdown => { draft = markdown; ScheduleSave(); }; canvas.Notice += message => status.Text = message;
        subtitle.LostFocus += (_, _) => { if (!loading && current != null && subtitle.Text.Trim() != NoteMeta.Subtitle(draft)) CommitMetadata(NoteMeta.WithSubtitle(draft, current.Name, subtitle.Text)); };
        title.TextChanged += (_, _) => { if (title.Text != current?.Name) ScheduleSave(); };
        saveTimer.Tick += async (_, _) => { saveTimer.Stop(); await Save(); };
        Closed += (_, _) => { if (current != null) Save().GetAwaiter().GetResult(); };
        root.ActualThemeChanged += (_, _) => { if (themeName == "Windows") { palette.Apply(root.ActualTheme == ElementTheme.Dark ? "Midnight" : "Dawn"); canvas.SetPalette(palette.Ink, palette.Body, palette.Accent, palette.Selection); } };
        root.SizeChanged += (_, e) => ApplyResponsiveLayout(e.NewSize.Width);
        AddAppShortcuts();
        var newKey = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = Windows.System.VirtualKey.N, Modifiers = Windows.System.VirtualKeyModifiers.Control }; newKey.Invoked += async (_, e) => { await NewNote(); e.Handled = true; }; root.KeyboardAccelerators.Add(newKey);
        var findKey = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = Windows.System.VirtualKey.F, Modifiers = Windows.System.VirtualKeyModifiers.Control }; findKey.Invoked += async (_, e) => { e.Handled = true; await FindInNote(); }; root.KeyboardAccelerators.Add(findKey);
    }
    void BuildHeader() => BuildModernHeader();
    void BuildWorkspace() => BuildModernWorkspace();
    void BuildStatus() => BuildModernFooter();
    Button Pivot(string text, Action action, Border indicator) => ConceptPivot(text, action, indicator);
    Button NavAction(string text, Action action) { var button = Action(text, action); button.FontSize = 17; button.HorizontalContentAlignment = HorizontalAlignment.Left; return button; }
    void Seed()
    {
        if (Directory.Exists(vault)) return;
        Directory.CreateDirectory(Path.Combine(vault, "Ideas")); Directory.CreateDirectory(Path.Combine(vault, "The XboxCast")); Directory.CreateDirectory(Path.Combine(vault, "Projects")); Directory.CreateDirectory(Path.Combine(vault, "Journal"));
        File.WriteAllText(Path.Combine(vault, "Ideas", "A calmer internet.md"), "# A calmer internet\n\nNotes on building a more humane, hopeful web.\n\nThe internet doesn't feel broken because of technology. It feels broken because we lost sight of what it's for.\n\nA calmer internet is possible. One designed for depth, kindness, and a longer view of what it means to be human together.\n\nThis note collects ideas, references, and experiments toward that vision. It's not a plan yet, more a constellation of thoughts.\n\n## Principles\n\n- People over metrics. Design for human well-being, not just attention.\n- Depth over speed. Slow is a feature.\n- Ownership over dependency. Give people their data and their voice.\n- Small, kind communities. Make room for meaningful conversation.\n\n## A hopeful future\n\nTechnology should expand what's good in us. The tools we make should leave people with more room to think.\n\n> Build something that feels like taking a breath.\n\n## Next steps\n\n- [ ] Collect examples of thoughtful digital spaces\n- [ ] Sketch a quieter way to capture ideas\n- [x] Make a little room to think\n");
        File.WriteAllText(Path.Combine(vault, "Ideas", "Small communities.md"), "# Small communities\n\nThe best conversations happen when there is room to listen.\n\n## Things worth trying\n\n- Make space for regular conversation\n- Let people arrive at their own pace\n");
        File.WriteAllText(Path.Combine(vault, "The XboxCast", "Next show.md"), "# Next show\n\nFunny first. Factual second.\n\n## Housekeeping\n\n- [ ] Check the recording setup\n- [ ] Community news\n\n## What we've been playing\n\nRoom for stories, tangents, and the occasional actual fact.\n\n## News\n\nCollect links and talking points here.\n");
        File.WriteAllText(Path.Combine(vault, "Projects", "Southlight IO.md"), "# Southlight I/O\n\nSouthern ideas. Brighter thinking.\n\n## The idea\n\nBeautiful Windows apps that give people a better view into data they own.\n\n## Acrux\n\nPortable Markdown. Familiar folders. A quieter place to think.\n");
        File.WriteAllText(Path.Combine(vault, "Journal", "A fresh page.md"), "# A fresh page\n\nA few things on my mind today.\n\n## One thing to focus on\n\nStart small. Leave room for what matters.\n");
    }
    void LoadVault()
    {
        try
        {
            var loaded = new List<Note>();
            foreach (var path in Directory.EnumerateFiles(vault, "*.md", SearchOption.AllDirectories))
            {
                try { var body = File.ReadAllText(path); var relative = Path.GetRelativePath(vault, Path.GetDirectoryName(path)!); loaded.Add(new Note(path, relative == "." ? "" : relative, Path.GetFileNameWithoutExtension(path), body)); }
                catch (IOException) { status.Text = "A note is temporarily unavailable. Refresh the notebook to retry."; }
            }
            all.Clear(); all.AddRange(loaded); folderLabel.Text = Path.GetFileName(vault); folders.Children.Clear(); sectionButtons.Clear(); folders.Children.Add(NavAction("All notes",()=>SetFolder("")));
            foreach (var group in all.Select(n => n.Group).Where(g => g.Length > 0).Concat(Directory.EnumerateDirectories(vault, "*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(vault, p))).Distinct().Order())
            {
                var name = group; var button = NavAction(Path.GetFileName(name) + "  " + all.Count(n => n.Group == name), () => { noteFilter = "all"; SetFolder(name); }); button.FontSize = 14; button.Margin = new Thickness(name.Count(c => c == Path.DirectorySeparatorChar) * 12, 0, 0, 0); folders.Children.Add(button); sectionButtons[name] = button;
            }
            RefreshNotes();
        }
        catch (Exception ex) { status.Text = "Could not read notebook: " + ex.Message; }
    }
    void SetFolder(string folder)
    {
        if (!TrySave()) return;
        activeFolder = folder; listTitle.Text = folder.Length == 0 ? (noteFilter == "recent" ? "recent" : "all notes") : Path.GetFileName(folder).ToLowerInvariant();
        foreach (var pair in sectionButtons) { pair.Value.Foreground = pair.Key == folder ? Accent : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]; pair.Value.FontWeight = pair.Key == folder ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Light; }
        RefreshNotes();
    }
    void RefreshNotes()
    {
        if (!TrySave()) return;
        var previous = current?.Path; loading = true; notes.Items.Clear(); var query = search.Text ?? "";
        foreach (var n in all.OrderByDescending(n => noteFilter == "recent" ? File.GetLastWriteTimeUtc(n.Path).Ticks : 0).ThenBy(n => n.Name).Where(n => (activeFolder == "" || n.Group == activeFolder) && (n.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || n.Body.Contains(query, StringComparison.OrdinalIgnoreCase))))
        {
            var p = new StackPanel { Spacing = 3, Padding = new Thickness(4, 7, 4, 7) }; p.Children.Add(Label(n.Group.Length == 0 ? "NOTEBOOK" : n.Group.ToUpperInvariant(), 10, .4));
            var name = Label(n.Name, 15); name.MaxLines = 2; name.TextTrimming = TextTrimming.CharacterEllipsis; p.Children.Add(name);
            var snippet = Label(string.Join(" ", n.Body.Split('\n').Where(l => l.Length > 0 && !l.StartsWith('#') && !l.StartsWith("<!--")).Take(2)), 12, .55); snippet.MaxLines = 2; snippet.TextTrimming = TextTrimming.CharacterEllipsis; 
            notes.Items.Add(new ListViewItem { Content = p, Tag = n, HorizontalContentAlignment = HorizontalAlignment.Stretch });
        }
        loading = false;
        if (notes.Items.Count > 0)
        {
            var index = Enumerable.Range(0, notes.Items.Count).FirstOrDefault(i => ((Note)((ListViewItem)notes.Items[i]).Tag).Path == previous, -1); notes.SelectedIndex = index < 0 ? 0 : index;
            // SelectionChanged is subscribed after initial construction.
            if (current == null && notes.SelectedItem is ListViewItem item) Select((Note)item.Tag);
        }
        else { canvas.Clear(); current = null; dirty = false; loading = true; title.Text = "A fresh page"; draft = ""; loading = false; outline.Children.Clear(); linkedNotes.Children.Clear(); status.Text = query.Length == 0 ? "No notes in this section. Choose + note to start." : "No matching notes. Try another search."; }
    }
    void Select(Note n)
    {
        if (loading) return;
        saveTimer.Stop(); if (!TrySave()) return;
        try
        {
            var text = File.ReadAllText(n.Path); loading = true; current = n with { Body = text }; dirty = false; title.Text = n.Name; draft = text; loading = false;
            breadcrumb.Text = Path.GetFileName(vault) + "  /  " + (n.Group.Length == 0 ? "notes" : n.Group.Replace("\\", "  /  "));
            canvas.Load(n.Path, text); compactNav = false; noteListOpen = false; ApplyResponsiveLayout(root.ActualWidth); Render(); UpdateStatus("Saved locally");
        }
        catch (Exception ex) { loading = false; canvas.Clear(); status.Text = "Could not open note: " + ex.Message; WriteDiagnostic(ex); }
    }
    void Render()
    {
        outline.Children.Clear();
        foreach (var line in draft.Replace("\r", "").Split('\n')) if (line.StartsWith("## ")) { var heading = line[3..]; outline.Children.Add(Action(heading, () => canvas.ScrollToHeading(heading))); }
        RefreshLinkedNotes(); RefreshConceptInspector();
    }
    void ScheduleSave() { if (loading || current == null) return; dirty = true; status.Text = "Saving..."; saveTimer.Stop(); saveTimer.Start(); }
    Task Save() { TrySave(); return Task.CompletedTask; }
    bool TrySave()
    {
        if (current == null || loading) return true; if (!dirty && title.Text == current.Name) return true;
        try
        {
            var noteName = title.Text.Trim();
            if (!ValidName(noteName)) { status.Text = "Choose a valid note title before saving."; return false; }
            var previousPath = current.Path; var previousName = current.Name; var destination = Path.Combine(Path.GetDirectoryName(previousPath)!, noteName + ".md");
            var rename = !string.Equals(destination, previousPath, StringComparison.OrdinalIgnoreCase);
            if (rename && File.Exists(destination)) { status.Text = "A note with that name already exists."; return false; }
            if (!File.Exists(previousPath) || File.ReadAllText(previousPath) != current.Body) { status.Text = "The file changed outside Acrux. Your draft is still here; copy it before reloading."; return false; }
            var text = draft;
            if (noteName != previousName) { var front = NoteMeta.Frontmatter(text); text = front + System.Text.RegularExpressions.Regex.Replace(text[front.Length..], @"\A# " + System.Text.RegularExpressions.Regex.Escape(previousName) + @"(?=\r?\n|$)", "# " + noteName); }
            var temporary = previousPath + ".acrux-writing"; File.WriteAllText(temporary, text, new System.Text.UTF8Encoding(false)); File.Move(temporary, previousPath, true);
            current = current with { Body = text };
            if (rename) { File.Move(previousPath, destination); current = current with { Path = destination }; canvas.UpdatePath(destination); }
            var updated = current with { Name = noteName }; var i = all.FindIndex(n => n.Path == previousPath); if (i >= 0) all[i] = updated;
            foreach (var item in notes.Items.OfType<ListViewItem>()) if (item.Tag is Note note && note.Path == previousPath) { item.Tag = updated; if (item.Content is StackPanel preview && preview.Children[1] is TextBlock caption) caption.Text = updated.Name; }
            current = updated; dirty = false; loading = true; draft = text; loading = false;
            if (noteName != previousName) canvas.Load(current.Path, text);
            UpdateStatus("Saved locally"); Render(); return true;
        }
        catch (Exception ex) { status.Text = "Could not save. Your draft is still here: " + ex.Message; WriteDiagnostic(ex); return false; }
    }
    static bool ValidName(string name) => name.Length > 0 && name is not "." and not ".." && !name.EndsWith('.') && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !System.Text.RegularExpressions.Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    static void WriteDiagnostic(Exception ex) { try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup-error.txt"), ex.ToString()); } catch { } }
    void UpdateStatus(string message) { status.Text = message; savedCaption.Text = "\u25cf  " + message; wordCount.Text = "Markdown  \u00b7  " + System.Text.RegularExpressions.Regex.Matches(NoteMeta.Body(draft), @"\b[\w'-]+\b").Count + " words"; }
    void ToggleTheme() => root.RequestedTheme = root.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
    void ToggleFocus() { focus = !focus; ApplyResponsiveLayout(root.ActualWidth); }
    async Task NewNote()
    {
        if (!TrySave()) return;
        try { var folder = Path.Combine(vault, activeFolder); Directory.CreateDirectory(folder); var name = "Untitled note"; int count = 2; while (File.Exists(Path.Combine(folder, name + ".md"))) name = "Untitled note " + count++;
            var path = Path.Combine(folder, name + ".md"); await File.WriteAllTextAsync(path, "# " + name + "\n\n"); loading = true; search.Text = ""; loading = false; current = null; LoadVault(); var note = all.First(n => n.Path == path); NavigateTo(note); canvas.FocusEditor();
        } catch (Exception ex) { status.Text = "Could not create note: " + ex.Message; }
    }
    async Task NewFolder()
    {
        var field = new TextBox { PlaceholderText = "Section name" }; var dialog = new ContentDialog { Title = "A new section", Content = field, PrimaryButtonText = "Create", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var name = field.Text.Trim(); if (!ValidName(name)) { status.Text = "Choose a valid section name."; return; }
        try { Directory.CreateDirectory(Path.Combine(vault, activeFolder, name)); LoadVault(); SetFolder(Path.Combine(activeFolder, name)); } catch (Exception ex) { status.Text = "Could not create section: " + ex.Message; }
    }
    async Task OpenVault()
    {
        if (!TrySave()) return;
        try { var picker = new FolderPicker(); picker.FileTypeFilter.Add("*"); InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this)); var folder = await picker.PickSingleFolderAsync(); if (folder == null) return; OpenNotebook(folder.Path); }
        catch (Exception ex) { status.Text = "Could not open notebook: " + ex.Message; }
    }
    void OpenNotebook(string path)
    {
        if (!TrySave()) return;
        current = null; dirty = false; vault = path; activeFolder = ""; noteFilter = "all"; loading = true; search.Text = ""; loading = false;
        if (!smokeMode) RegisterNotebook(vault); LoadVault(); ShowWriting();
    }
    void Share() { if (current == null) return; var package = new DataPackage(); package.SetText(draft); Clipboard.SetContent(package); status.Text = "Markdown copied to clipboard."; }
}









