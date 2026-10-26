using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using Windows.System;

namespace Acrux;

public sealed partial class MainWindow
{
    readonly StackPanel linkedNotes = new() { Spacing = 5 };
    CommandBar BuildEditorToolbar()
    {
        var bar = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed, IsDynamicOverflowEnabled = true, HorizontalContentAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), Margin = new Thickness(36, 0, 24, 0) };
        void Button(string label, Symbol symbol, Action action) { var b = new AppBarButton { Label = label, Icon = new SymbolIcon(symbol) }; ToolTipService.SetToolTip(b, label); b.Click += (_, _) => action(); bar.PrimaryCommands.Add(b); }

        var style = new AppBarButton { Label = "Paragraph style", Icon = new FontIcon { Glyph = "H", FontFamily = new FontFamily("Segoe UI"), FontSize = 22 } };
        var styles = new MenuFlyout(); foreach (var type in new[] { ("Text", "paragraph"), ("Heading 1", "heading1"), ("Heading 2", "heading2"), ("Heading 3", "heading3") }) { var command = type.Item2; var item = new MenuFlyoutItem { Text = type.Item1 }; item.Click += (_, _) => canvas.Format(command); styles.Items.Add(item); } style.Flyout = styles; bar.PrimaryCommands.Add(style);
        Button("Bold | Ctrl+B", Symbol.Bold, () => canvas.Format("bold"));  Button("Insert link", Symbol.Link, async () => await ShowLinkDialog()); bar.PrimaryCommands.Add(new AppBarSeparator());
         Button("Insert block", Symbol.Add, async () => await ShowInsertMenu());
        void More(string label, Action action) { var b = new AppBarButton { Label = label }; b.Click += (_, _) => action(); bar.SecondaryCommands.Add(b); }
        More("Undo", canvas.Undo); More("Redo", canvas.Redo); More("Italic | Ctrl+I", () => canvas.Format("italic")); More("Bulleted list", () => canvas.Format("bullet")); More("To-do list", () => canvas.Format("task")); More("Note details", () => { detailsOpen = !detailsOpen; ApplyResponsiveLayout(root.ActualWidth); });
        More("Insert picture", async () => await ImportAttachment(true)); More("Attach a file", async () => await ImportAttachment(false)); More("Strikethrough", () => canvas.Format("strike")); More("Highlight", () => canvas.Format("highlight")); More("Inline code", () => canvas.Format("code")); More("Numbered list", () => canvas.Format("number")); More("Quote", () => canvas.Format("quote")); More("Insert table", () => canvas.InsertBlock("table")); More("Insert code block", () => canvas.InsertBlock("codeblock")); More("Link to a note", () => canvas.Format("wikilink")); More("New container", canvas.Add); More("Markdown source / Live preview", canvas.ToggleSource); More("Zoom in", () => canvas.Zoom(.1f)); More("Zoom out", () => canvas.Zoom(-.1f)); More("Reset view", canvas.Home); More("Focus mode | F11", ToggleFocus);
        return bar;
    }
    void ApplyResponsiveLayout(double width)
    {
        if (workspace.ColumnDefinitions.Count < 4) return;
        AdaptModernHeader(width);
        bool nav = !focus && (width<900 || !hideNavigation) && (width >= 1280 || compactNav);
        bool list = !focus && (width<900 || !hideNavigation) && (width >= 900 || compactNav);
        side.Visibility = nav ? Visibility.Visible : Visibility.Collapsed;
        workspace.Children[1].Visibility = list ? Visibility.Visible : Visibility.Collapsed;
        context.Visibility = !focus && detailsOpen && width >= 1500 ? Visibility.Visible : Visibility.Collapsed;
        workspace.ColumnDefinitions[0].Width = new GridLength(nav ? width < 640 ? 180 : 220 : 0);
        workspace.ColumnDefinitions[1].Width = new GridLength(list ? width < 640 ? Math.Max(180,width-180) : 260 : 0);
        workspace.ColumnDefinitions[3].Width = new GridLength(context.Visibility == Visibility.Visible ? 280 : 0);
        bookColumns.Visibility = width < 900 && list ? Visibility.Collapsed : Visibility.Visible;

    }
    void AddAppShortcuts()
    {
        void Key(VirtualKey key, VirtualKeyModifiers mods, Action action) { var a = new KeyboardAccelerator { Key = key, Modifiers = mods }; a.Invoked += (_, e) => { action(); e.Handled = true; }; root.KeyboardAccelerators.Add(a); }
        Key(VirtualKey.P, VirtualKeyModifiers.Control, async () => await ShowCommandPalette()); Key(VirtualKey.O, VirtualKeyModifiers.Control, async () => await ShowQuickSwitcher()); Key(VirtualKey.S, VirtualKeyModifiers.Control, () => Save().GetAwaiter().GetResult()); Key(VirtualKey.F11, VirtualKeyModifiers.None, ToggleFocus); Key(VirtualKey.E, VirtualKeyModifiers.Control, canvas.ToggleSource);
    }
    private record Command(string Name, string Hint, Action Run);
    async Task ShowCommandPalette()
    {
        var commands = new List<Command> {
            new("New note", "Ctrl+N", async () => await NewNote()), new("New daily note", "Journal", async () => await DailyNote()), new("All notebooks", "Home", ShowNotebooks), new("Open notebook", "Choose a folder", async () => await OpenVault()),
            new("Quick switcher", "Ctrl+O", async () => await ShowQuickSwitcher()), new("Save note", "Ctrl+S", () => Save().GetAwaiter().GetResult()), new("New section", "Folder", async () => await NewFolder()),
            new("Add container", "Canvas", canvas.Add), new("Freeform canvas", "Spatial layout", () => canvas.SetPageMode(false)), new("Book page", "Reading layout", () => canvas.SetPageMode(true)), new("Focus mode", "F11", ToggleFocus), new("Markdown source / Live preview", "Ctrl+E", canvas.ToggleSource), new("Reset canvas", "100%", canvas.Home),
            new("Undo", "Ctrl+Z", canvas.Undo), new("Redo", "Ctrl+Y", canvas.Redo), new("Strikethrough", "Formatting", () => canvas.Format("strike")), new("Highlight", "Formatting", () => canvas.Format("highlight")), new("Inline code", "Formatting", () => canvas.Format("code")), new("Insert picture", "Local asset", async () => await ImportAttachment(true)), new("Attach file", "Local asset", async () => await ImportAttachment(false)), new("Bold", "Ctrl+B", () => canvas.Format("bold")), new("Italic", "Ctrl+I", () => canvas.Format("italic")), new("Insert link", "Ctrl+K", () => canvas.Format("link")), new("Copy Markdown", "Clipboard", Share), new("Settings", "Appearance", async () => await ShowSettings()) };
        foreach (var type in new[] { "heading1", "heading2", "heading3", "task", "bullet", "number", "quote", "callout", "codeblock", "table", "divider" }) { var name = type; commands.Add(new Command("Insert " + name, "Block", () => canvas.InsertBlock(name))); }
        var selected = await PickCommand("What would you like to do?", commands);
        if (selected != null) root.DispatcherQueue.TryEnqueue(() => selected.Run());
    }
    async Task ShowInsertMenu()
    {
        var commands = new List<Command>();
        foreach (var pair in new[] { ("Text", "paragraph"), ("Heading 1", "heading1"), ("Heading 2", "heading2"), ("Heading 3", "heading3"), ("Bulleted list", "bullet"), ("Numbered list", "number"), ("To-do list", "task"), ("Quote", "quote"), ("Callout", "callout"), ("Code block", "codeblock"), ("Table", "table"), ("Divider", "divider"), ("Today's date", "date") }) { var command = pair.Item2; commands.Add(new Command(pair.Item1, "/" + command, () => canvas.InsertBlock(command))); }
        var selected = await PickCommand("Insert a block", commands); selected?.Run();
    }
    async Task<Command?> PickCommand(string heading, List<Command> commands)
    {
        var query = new TextBox { PlaceholderText = "Start typing...", Margin = new Thickness(0, 0, 0, 12) }; var list = new ListView { Height = 340, IsItemClickEnabled = true }; Command? selected = null;
        var dialog = new ContentDialog { Title = heading, CloseButtonText = "Cancel", PrimaryButtonText = "Open", DefaultButton = ContentDialogButton.Primary, XamlRoot = root.XamlRoot };
        void Refresh() { list.Items.Clear(); foreach (var command in commands.Where(c => c.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase))) { var row = new StackPanel { Spacing = 4, Padding = new Thickness(2, 8, 2, 8) }; row.Children.Add(Label(command.Name, 17)); row.Children.Add(Label(command.Hint, 11, .5)); list.Items.Add(new ListViewItem { Content = row, Tag = command }); } if (list.Items.Count > 0) list.SelectedIndex = 0; }
        query.TextChanged += (_, _) => Refresh(); list.ItemClick += (_, e) => { selected = (Command)((ListViewItem)e.ClickedItem).Tag; dialog.Hide(); }; dialog.PrimaryButtonClick += (_, _) => { if (list.SelectedItem is ListViewItem item) selected = (Command)item.Tag; };
        dialog.Content = new StackPanel { Width = 460, Children = { query, list } }; Refresh(); dialog.Opened += (_, _) => query.Focus(FocusState.Programmatic); await dialog.ShowAsync(); return selected;
    }
    async Task ShowQuickSwitcher()
    {
        var selected = await PickCommand("Go to a note", all.Select(n => new Command(n.Name, n.Group.Length == 0 ? Path.GetFileName(vault) : n.Group, () => NavigateTo(n))).ToList()); selected?.Run();
    }
    void NavigateTo(Note note)
    {
        if (!TrySave()) return;
        noteFilter = "all"; activeFolder = ""; loading = true; search.Text = ""; loading = false; RefreshNotes();
        for (var i = 0; i < notes.Items.Count; i++) if (((Note)((ListViewItem)notes.Items[i]).Tag).Path == note.Path) { notes.SelectedIndex = i; break; }
        ShowWriting();
    }
    async void OpenLinkedNote(string target)
    {
        var name = target.Split('|')[0].Split('#')[0].Trim(); var matches = all.Where(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || Path.GetRelativePath(vault, n.Path).Replace('\\', '/').Equals(name + ".md", StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 1) NavigateTo(matches[0]);
        else if (matches.Count > 1) { var command = await PickCommand("Which note?", matches.Select(n => new Command(n.Name, n.Group, () => NavigateTo(n))).ToList()); command?.Run(); }
        else status.Text = "No note named " + name + ". Use + note to create it.";
    }
    void RefreshLinkedNotes()
    {
        linkedNotes.Children.Clear(); linkedNotes.Spacing = 0; if (current == null) return;
        var targets = Regex.Matches(draft, @"\[\[([^\]]+)\]\]").Select(m => m.Groups[1].Value.Split('|')[0].Split('#')[0]).ToList();
        var found = all.Where(n => n.Path != current.Path && (targets.Contains(n.Name, StringComparer.OrdinalIgnoreCase) || n.Body.Contains("[[" + current.Name + "]]", StringComparison.OrdinalIgnoreCase))).DistinctBy(n => n.Path).Take(5).ToList();
        foreach (var note in found)
        {
            var picked = note; var text = new StackPanel { Spacing = 1 }; text.Children.Add(IconText("\uE8A5", note.Name, 12)); text.Children.Add(Label("       " + Path.GetFileName(vault) + " / " + note.Group, 10, .6));
            var card = Action("", () => NavigateTo(picked)); card.Content = text; card.BorderBrush = palette.Line; card.BorderThickness = new Thickness(0, 0, 0, 1); card.CornerRadius = new CornerRadius(0); card.Height = 40; card.Padding = new Thickness(7, 3, 7, 3); card.HorizontalContentAlignment = HorizontalAlignment.Left; linkedNotes.Children.Add(card);
        }
        if (found.Count == 0) linkedNotes.Children.Add(Label("Connections will appear here.", 12, .45));
    }
    async Task DailyNote()
    {
        if (!TrySave()) return;
        var folder = Path.Combine(vault, "Journal"); Directory.CreateDirectory(folder); var path = Path.Combine(folder, DateTime.Now.ToString("yyyy-MM-dd") + ".md");
        if (!File.Exists(path)) await File.WriteAllTextAsync(path, "# " + DateTime.Now.ToString("yyyy-MM-dd") + "\n\n## On my mind\n\n\n\n## One thing that matters\n\n- [ ] \n");
        LoadVault(); var note = all.First(n => n.Path == path); NavigateTo(note);
    }
    async Task ShowSettings()
    {
        var names = new[] { "Dawn", "Midnight", "Sage", "Blossom", "Monochrome", "Windows" }; var themes = new ComboBox { Header = "Theme", ItemsSource = names, SelectedIndex = Math.Max(0, Array.IndexOf(names, themeName)), HorizontalAlignment = HorizontalAlignment.Stretch };
        var fonts = new ComboBox { Header="Document typography", ItemsSource=new[]{"Segoe UI","Georgia","Cascadia Code"}, SelectedIndex=0, HorizontalAlignment=HorizontalAlignment.Stretch };
        var content = new StackPanel { Width = 440, Spacing = 16 }; content.Children.Add(fonts); content.Children.Add(Label("a calmer workspace", 29)); content.Children.Add(themes); content.Children.Add(Label("The five palettes follow the supplied concept. Windows follows your system appearance.", 13, .65)); content.Children.Add(Label("Ctrl+P  Commands\nCtrl+O  Quick switcher\nCtrl+N  New note\nCtrl+S  Save\nCtrl+E  Markdown source\nF11  Focus mode", 13, .7)); content.Children.Add(Label("Your notes remain ordinary Markdown. Tags and source references live in frontmatter. Canvas positions stay in a separate layout file.", 12, .55));
        var dialog = new ContentDialog { Title = "Settings", Content = content, PrimaryButtonText = "Apply", CloseButtonText = "Cancel", XamlRoot = root.XamlRoot }; if (await dialog.ShowAsync() == ContentDialogResult.Primary) { SetConceptTheme(names[themes.SelectedIndex]); SetWritingFont((string)fonts.SelectedItem); }
    }
}
