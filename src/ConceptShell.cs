using Path = System.IO.Path;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Text.RegularExpressions;
using System.Numerics;
namespace Acrux;
public sealed partial class MainWindow
{
    readonly ConceptPalette palette = new();
    readonly StackPanel notebookStrip = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
    readonly VariableSizedWrapGrid sectionTiles = new() { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = 3, ItemWidth = 96, ItemHeight = 40 };
    readonly StackPanel tagChips = new() { Spacing = 5 };
    readonly StackPanel sourceCards = new() { Spacing = 8 };
    readonly StackPanel properties = new() { Spacing = 8 };
    readonly StackPanel relatedCards = new() { Orientation = Orientation.Horizontal, Spacing = 10 };
    readonly TextBox subtitle = new() { BorderThickness = new Thickness(0), Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent), MinHeight = 0, Height = 27 };
    readonly TextBlock wordCount = new();
    readonly TextBlock savedCaption = new();
    readonly List<(string Name, Border Ring)> themeRings = new();
    readonly Border connectPivotLine = new(), reviewPivotLine = new();
    readonly Grid bookColumns = new();
    readonly TextBox sourcePreview = new() { AcceptsReturn = true, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Cascadia Code"), FontSize = 14, Padding = new Thickness(20), BorderThickness = new Thickness(0) };
    Border? bookFrame;
    Grid? overlayView;
    string themeName = "Dawn", topView = "capture";
    bool noteListOpen, splitView;
    Button? sourceModeButton;
    void BuildConceptHeader()
    {
        var header = new Grid { Height = 104, Padding = new Thickness(25, 20, 24, 10), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = palette.Line };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(369) }); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 13, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(ConceptArtwork.Star("33347a", 43)); var branding = new StackPanel { Spacing = 0 }; branding.Children.Add(Label("acrux.", 35)); branding.Children.Add(Label("Beautiful notes for a brighter tomorrow.", 12, .7)); brand.Children.Add(branding); header.Children.Add(brand);
        var pivots = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 76, VerticalAlignment = VerticalAlignment.Center };
        pivots.Children.Add(ConceptPivot("capture", ShowNotebooks, homePivotLine)); pivots.Children.Add(ConceptPivot("write", ShowWriting, writePivotLine)); pivots.Children.Add(ConceptPivot("connect", () => ShowConnections(), connectPivotLine)); pivots.Children.Add(ConceptPivot("review", () => ShowReview(), reviewPivotLine)); Grid.SetColumn(pivots, 1); header.Children.Add(pivots);
        var commands = Row(Action("Search  \u2315", () => { ToggleNoteList(); search.Focus(FocusState.Programmatic); }, "Search notes | Ctrl+O"), Action("+  New note", async () => await NewNote()), Action("\u2699", async () => await ShowSettings(), "Settings")); commands.VerticalAlignment = VerticalAlignment.Center; commands.Spacing = 6; commands.Width = 300; Grid.SetColumn(commands, 2); header.Children.Add(commands); root.Children.Add(header);
    }
    Button ConceptPivot(string text, Action action, Border indicator)
    {
        var stack = new StackPanel { Spacing = 9 }; var heading = Label(text, 52); heading.FontWeight = Microsoft.UI.Text.FontWeights.Light; stack.Children.Add(heading); indicator.Height = 0; indicator.Background = palette.Ink; stack.Children.Add(indicator);
        var button = Action("", action); button.Padding = new Thickness(0); button.Content = stack; return button;
    }
    void SelectTopView(string view)
    {
        var changed = topView != view; topView = view;
        var allPivots = new[] { ("capture", homePivotLine), ("write", writePivotLine), ("connect", connectPivotLine), ("review", reviewPivotLine) };
        foreach (var (name, line) in allPivots) { line.Opacity = 0; if (line.Parent is StackPanel p && p.Children[0] is TextBlock text) { text.Opacity = name == view ? 1 : .45; if(changed && name == view) Sweep(text); text.FontWeight = name == view ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Light; } }
        AdaptFocusHeader(root.ActualWidth>0?root.ActualWidth:1608);
        if (overlayView != null) overlayView.Visibility = Visibility.Collapsed;
    }
    void BuildConceptWorkspace()
    {
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(335) }); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) }); workspace.ColumnDefinitions.Add(new ColumnDefinition()); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(327) });
        Grid.SetRow(workspace, 1); root.Children.Add(workspace);
        var rail = new StackPanel { Padding = new Thickness(18, 16, 18, 14), Spacing = 9 };
        rail.Children.Add(RailLabel("Notebook")); rail.Children.Add(notebookStrip);
        rail.Children.Add(RailLabel("Section")); rail.Children.Add(sectionTiles);
        rail.Children.Add(RailLabel("Note mode"));
        rail.Children.Add(OptionRow(new[] { ("Free writing", "\uE8A5", (Action)(() => canvas.SetPageMode(true))), ("Structured", "\uE8A9", (Action)(() => canvas.SetPageMode(false))), ("Daily note", "\uE787", (Action)(async () => await DailyNote())) }, 0));
        rail.Children.Add(RailLabel("Format")); rail.Children.Add(OptionRow(new[] { ("Markdown", "\uE943", (Action)(() => canvas.SetSourceMode(false))), ("Plain text", "\uE8E9", (Action)(() => canvas.SetSourceMode(true))) }, 0, 42));
        rail.Children.Add(RailLabel("Focus level")); rail.Children.Add(OptionRow(new[] { ("Low", "\uE708", (Action)(() => SetFocus(0))), ("Standard", "\uE7F4", (Action)(() => SetFocus(1))), ("Deep", "\uE7F7", (Action)(() => SetFocus(2))) }, 0));
        rail.Children.Add(RailLabel("Writing style")); rail.Children.Add(OptionRow(new[] { ("Book", "\uE82D", (Action)(() => SetWritingFont("Georgia"))), ("Sans", "\uE8D2", (Action)(() => SetWritingFont("Segoe UI"))), ("Mono", "\uE943", (Action)(() => SetWritingFont("Cascadia Code"))) }, 0));
        rail.Children.Add(RailLabel("Theme")); rail.Children.Add(BuildThemeChoices());
        side.Child = new ScrollViewer { Content = rail, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; side.Background = palette.Panel; workspace.Children.Add(side);
        var drawer = new Grid { Padding = new Thickness(14, 18, 12, 12), Background = palette.Panel, BorderThickness = new Thickness(0, 0, 1, 0), BorderBrush = palette.Line, Visibility = Visibility.Collapsed };
        drawer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); drawer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); drawer.RowDefinitions.Add(new RowDefinition());
        listTitle.FontSize = 26; drawer.Children.Add(listTitle); search.Width = double.NaN; search.Margin = new Thickness(0, 12, 0, 12); Grid.SetRow(search, 1); drawer.Children.Add(search); Grid.SetRow(notes, 2); drawer.Children.Add(notes); Grid.SetColumn(drawer, 1); workspace.Children.Add(drawer);
        bookColumns.ColumnDefinitions.Add(new ColumnDefinition()); bookColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
        var book = new Grid { Background = palette.Paper };
        book.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); book.RowDefinitions.Add(new RowDefinition()); book.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); book.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var heading = new Grid { Height = 178, Padding = new Thickness(67, 40, 32, 8) };
        var art = ConceptArtwork.Landscape(2); if (art is FrameworkElement artwork) { artwork.Width = 379; artwork.Height = 178; artwork.Margin = new Thickness(0, -35, -15, -10); artwork.HorizontalAlignment = HorizontalAlignment.Right; artwork.VerticalAlignment = VerticalAlignment.Top; artwork.Opacity = .85; } heading.Children.Add(art);
        var headText = new StackPanel { Spacing = 7, VerticalAlignment = VerticalAlignment.Top };
        breadcrumb.FontSize = 12; breadcrumb.Foreground = palette.Muted; headText.Children.Add(breadcrumb);
        title.FontFamily = new FontFamily("Georgia"); title.FontSize = 52; title.FontWeight = Microsoft.UI.Text.FontWeights.Normal; title.Foreground = palette.Ink; title.MaxWidth = 600; title.HorizontalAlignment = HorizontalAlignment.Left; title.Margin = new Thickness(0, 2, 0, 0); headText.Children.Add(title);
        subtitle.FontFamily = new FontFamily("Georgia"); subtitle.FontSize = 20; subtitle.HorizontalAlignment = HorizontalAlignment.Left; subtitle.Foreground = palette.Muted; subtitle.MaxWidth = 640; subtitle.TextWrapping = TextWrapping.Wrap; headText.Children.Add(subtitle); heading.Children.Add(headText); heading.SizeChanged += (_, e) => { if (art is FrameworkElement drawing) { drawing.Width = Math.Min(379, Math.Max(230, e.NewSize.Width * .43)); drawing.Height = drawing.Width * 178 / 379; } title.FontSize = e.NewSize.Width < 800 ? 38 : 52; }; book.Children.Add(heading);
        canvas.Margin = new Thickness(43, 8, 50, 0); Grid.SetRow(canvas, 1); book.Children.Add(canvas);
        var related = new StackPanel { Spacing = 10, Margin = new Thickness(67, 0, 50, 12) }; related.Children.Add(new Border { Height = 1, Background = palette.Line }); related.Children.Add(IconText("\uE71B", "Related notes", 12)); related.Children.Add(relatedCards); Grid.SetRow(related, 2); book.Children.Add(related);
        var readingStatus = new Grid { Margin = new Thickness(67, 0, 50, 9) }; readingStatus.ColumnDefinitions.Add(new ColumnDefinition()); readingStatus.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); wordCount.FontSize = 10; wordCount.Foreground = palette.Muted; savedCaption.FontSize = 10; savedCaption.Foreground = palette.Muted; readingStatus.Children.Add(wordCount); Grid.SetColumn(savedCaption, 1); readingStatus.Children.Add(savedCaption); Grid.SetRow(readingStatus, 3); readingStatus.Children.Remove(savedCaption); readingStatus.Visibility = Visibility.Collapsed; book.Children.Add(readingStatus);
        bookFrame = new Border { Child = book, CornerRadius = new CornerRadius(0), BorderThickness = new Thickness(0), Margin = new Thickness(12, 16, 21, 10) };
        bookColumns.Children.Add(bookFrame); sourcePreview.Foreground = palette.Body; sourcePreview.Background = palette.Paper; Grid.SetColumn(sourcePreview, 1); bookColumns.Children.Add(sourcePreview); Grid.SetColumn(bookColumns, 2); workspace.Children.Add(bookColumns);
        var inspector = new StackPanel { Spacing = 4, Padding = new Thickness(16, 4, 18, 8) };
        inspector.Children.Add(InspectorGroup("Linked notes", new Border { Child = linkedNotes, BorderThickness = new Thickness(1), BorderBrush = palette.Line, CornerRadius = new CornerRadius(4) })); var linkNote = Action("+  Link another note", async () => await LinkAnotherNote()); linkNote.FontSize = 12; linkNote.MinHeight = 24; linkNote.Padding = new Thickness(0, 3, 0, 3); linkNote.HorizontalContentAlignment = HorizontalAlignment.Left; inspector.Children.Add(linkNote);
        inspector.Children.Add(InspectorGroup("Tags", tagChips));
        inspector.Children.Add(InspectorGroup("Sources & references", sourceCards)); var addSource = Action("+  Add source", async () => await AddSource()); addSource.FontSize = 12; addSource.MinHeight = 24; addSource.Padding = new Thickness(0, 3, 0, 3); addSource.HorizontalContentAlignment = HorizontalAlignment.Left; inspector.Children.Add(addSource);
        inspector.Children.Add(InspectorGroup("Note properties", properties));
        var inserts = new Grid(); for (var i = 0; i < 5; i++) inserts.ColumnDefinitions.Add(new ColumnDefinition());
        var commands = new[] { ("Heading", "\uE8D2", "heading2"), ("List", "\uE8FD", "bullet"), ("Quote", "\uE994", "quote"), ("Code", "\uE943", "codeblock"), ("Image", "\uEB9F", "image") };
        for (int i = 0; i < commands.Length; i++) { var item = commands[i]; var button = ToolTile(item.Item1, item.Item2, () => { if (item.Item3 == "image") _ = ImportAttachment(true); else canvas.InsertBlock(item.Item3); }); Grid.SetColumn(button, i); inserts.Children.Add(button); }
        inspector.Children.Add(InspectorGroup("Insert block", inserts));
        var arrange = new Grid(); for (int i = 0; i < 5; i++) arrange.ColumnDefinitions.Add(new ColumnDefinition());
        var actions = new[] { ("Backlinks", "\uE71B", (Action)(() => ShowConnections())), ("Graph view", "\uE8D4", (Action)(() => ShowConnections(true))), ("Split view", "\uE89F", (Action)ToggleSplitView), ("Focus", "\uE8A7", (Action)ToggleFocus), ("More", "\uE712", (Action)(async () => await ShowCommandPalette())) };
        for (int i = 0; i < actions.Length; i++) { var action = actions[i]; var button = ToolTile(action.Item1, action.Item2, action.Item3); button.Padding = new Thickness(2); button.MinHeight = 32; if (button.Content is StackPanel contents) { contents.Spacing = 3; if (contents.Children[0] is FontIcon icon) icon.FontSize = 16; } Grid.SetColumn(button, i); arrange.Children.Add(button); }
        inspector.Children.Add(InspectorGroup("Arrange", arrange));
        context.Child = new ScrollViewer { Content = inspector, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; context.Background = palette.Panel; Grid.SetColumn(context, 3); workspace.Children.Add(context);
        detailsOpen = true;
    }
    UIElement IconText(string glyph, string text, double size = 12)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9 }; row.Children.Add(new FontIcon { Glyph = glyph, FontSize = size + 3, Foreground = palette.Ink, VerticalAlignment = VerticalAlignment.Center }); row.Children.Add(Label(text, size)); return row;
    }
    UIElement RailLabel(string text)
    {
        if (text is "Notebook" or "Section")
        {
            var button = Action(text + "  \u2304", () => { if (text == "Section") sectionTiles.Visibility = sectionTiles.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }); button.FontSize = 13; button.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; button.HorizontalContentAlignment = HorizontalAlignment.Left; button.Padding = new Thickness(0); button.MinHeight = 0; button.Height = 18;
            if (text == "Notebook") { var menu = new MenuFlyout(); menu.Opening += (_, _) => { menu.Items.Clear(); foreach (var path in notebooks) { var notebook = path; var item = new MenuFlyoutItem { Text = Path.GetFileName(path) }; item.Click += (_, _) => OpenNotebook(notebook); menu.Items.Add(item); } menu.Items.Add(new MenuFlyoutSeparator()); var add = new MenuFlyoutItem { Text = "New notebook..." }; add.Click += async (_, _) => await CreateNotebook(); menu.Items.Add(add); }; button.Flyout = menu; }
            return button;
        }
        var label = Label(text, 13); label.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; return label;
    }
    UIElement OptionRow((string Label, string Glyph, Action Run)[] items, int selected, double height = 65)
    {
        var grid = new Grid(); var buttons = new List<Button>(); for (int i = 0; i < items.Length; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < items.Length; i++)
        {
            var index = i; var item = items[i]; var stack = new StackPanel { Spacing = 5, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            if (height > 45) stack.Children.Add(new FontIcon { Glyph = item.Glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 19, Foreground = palette.Ink }); stack.Children.Add(Label(item.Label, 11));
            var button = new Button { Content = stack, Height = height, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, i + 1 < items.Length ? 6 : 0, 0), Padding = new Thickness(3), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), BorderBrush = i == selected ? palette.Accent : palette.Line, Background = i == selected ? palette.Selection : palette.Panel, Foreground = palette.Ink };
            button.Click += (_, _) => { foreach (var b in buttons) { b.Background = palette.Panel; b.BorderBrush = palette.Line; } button.Background = palette.Selection; button.BorderBrush = palette.Accent; item.Run(); }; ToolTipService.SetToolTip(button, item.Label); buttons.Add(button); Grid.SetColumn(button, index); grid.Children.Add(button);
        }
        return grid;
    }
    Button ToolTile(string caption, string glyph, Action run)
    {
        var stack = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center }; stack.Children.Add(new FontIcon { Glyph = glyph, FontSize = 18, Foreground = palette.Ink }); stack.Children.Add(Label(caption, 10));
        var button = Action("", run, caption); button.Content = stack; button.Padding = new Thickness(2, 6, 2, 6); button.BorderThickness = new Thickness(1); button.BorderBrush = palette.Line; button.CornerRadius = new CornerRadius(4); button.Margin = new Thickness(0, 0, 4, 0); return button;
    }
    UIElement InspectorGroup(string caption, UIElement content)
    {
        var stack = new StackPanel { Spacing = 4, Padding = new Thickness(0, 2, 0, 3) }; var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); var name = Label(caption, 12); name.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; header.Children.Add(name); var arrow = Label("\u2304", 12); Grid.SetColumn(arrow, 1); header.Children.Add(arrow);
        var heading = Action("", () => content.Visibility = content.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible); heading.Content = header; heading.HorizontalContentAlignment = HorizontalAlignment.Stretch; heading.Padding = new Thickness(0); heading.MinHeight = 18; heading.Height = 20; stack.Children.Add(heading); stack.Children.Add(content); return new Border { BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = palette.Line, Child = stack };
    }
    UIElement BuildThemeChoices()
    {
        themeRings.Clear(); var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9 };
        foreach (var pair in new[] { ("Dawn", "e8c994"), ("Midnight", "252467"), ("Sage", "9eafa5"), ("Blossom", "ceadc0"), ("Monochrome", "9b9ba9") })
        {
            var name = pair.Item1; var stack = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center }; var circle = new Ellipse { Width = 29, Height = 29, Fill = new SolidColorBrush(ConceptPalette.Color(pair.Item2)) }; var ring = new Border { Child = circle, CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(2), BorderBrush = name == themeName ? palette.Accent : new SolidColorBrush(Colors.Transparent), Padding = new Thickness(2) }; themeRings.Add((name, ring)); stack.Children.Add(ring); stack.Children.Add(Label(name == "Monochrome" ? "Mono" : name, 10)); var button = Action("", () => SetConceptTheme(name)); button.Content = stack; button.Padding = new Thickness(0); row.Children.Add(button);
        }
        return row;
    }
    UIElement PlanetGlyph()
    {
        var scene = new Canvas { Width = 44, Height = 36 }; var planet = new Ellipse { Width = 27, Height = 27, Stroke = palette.Ink, StrokeThickness = 1.5 }; Canvas.SetLeft(planet, 8); Canvas.SetTop(planet, 4); scene.Children.Add(planet); var orbit = new Ellipse { Width = 44, Height = 16, Stroke = palette.Ink, StrokeThickness = 1.5, RenderTransform = new RotateTransform { Angle = -35, CenterX = 22, CenterY = 8 } }; Canvas.SetTop(orbit, 10); scene.Children.Add(orbit); return scene;
    }
    void BuildConceptFooter()
    {
        var footer = new Grid { Height = 60, Padding = new Thickness(24, 4, 20, 8) }; footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) }); footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 13, VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(ConceptArtwork.Star("c0a16e", 31)); var caption = new StackPanel(); caption.Children.Add(Label("Portable Markdown.", 14)); caption.Children.Add(Label("Your notes are yours, always.", 11, .65)); left.Children.Add(caption); footer.Children.Add(left);
        var middle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center }; middle.Children.Add(PlanetGlyph()); var text = new StackPanel(); text.Children.Add(Label("Relationships preserved quietly.", 14)); text.Children.Add(Label("Ideas stay connected, even offline.", 11, .65)); middle.Children.Add(text); Grid.SetColumn(middle, 1); footer.Children.Add(middle);
        var save = Action("", () => { TrySave(); RefreshConceptInspector(); }); save.Content = FooterLabel("\uE74E", "Save note", "Markdown (.md)"); if (save.Content is StackPanel saveRow && saveRow.Children[1] is StackPanel saveLabels) { saveLabels.Children.RemoveAt(1); savedCaption.FontSize = 10; savedCaption.MaxWidth = 130; savedCaption.TextTrimming = TextTrimming.CharacterEllipsis; savedCaption.Text = "Markdown (.md)"; saveLabels.Children.Add(savedCaption); } status.RegisterPropertyChangedCallback(TextBlock.TextProperty, (_, _) => { savedCaption.Text = status.Text; ToolTipService.SetToolTip(save, status.Text + " | " + wordCount.Text); }); save.BorderBrush = palette.Line; save.BorderThickness = new Thickness(1); save.CornerRadius = new CornerRadius(5); save.MinWidth = 175;
        sourceModeButton = Action("", canvas.ToggleSource); sourceModeButton.Content = FooterLabel("\uE8E5", "Open source", "Markdown + canvas"); sourceModeButton.Background = palette.Ink; sourceModeButton.Foreground = palette.Paper; sourceModeButton.CornerRadius = new CornerRadius(5); sourceModeButton.MinWidth = 225;
        if (sourceModeButton.Content is StackPanel s) foreach (var child in s.Children) if (child is TextBlock b) b.Foreground = palette.Paper; else if (child is StackPanel labels) foreach (var label in labels.Children.OfType<TextBlock>()) label.Foreground = palette.Paper;
        var buttons = Row(save, sourceModeButton); Grid.SetColumn(buttons, 2); footer.Children.Add(buttons); Grid.SetRow(footer, 2); root.Children.Add(footer);
    }
    UIElement FooterLabel(string glyph, string titleText, string detail)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }; row.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 24, VerticalAlignment = VerticalAlignment.Center, Foreground = palette.Ink }); var labels = new StackPanel(); labels.Children.Add(Label(titleText, 14)); labels.Children.Add(Label(detail, 10, .7)); row.Children.Add(labels); return row;
    }
    void ToggleNoteList() { focus = false; ShowWriting(); noteListOpen = !noteListOpen; ApplyResponsiveLayout(root.ActualWidth); }
    void ToggleSplitView() { splitView = !splitView; bookColumns.ColumnDefinitions[1].Width = new GridLength(splitView ? 1 : 0, GridUnitType.Star); sourcePreview.Text = draft; }
    void SetFocus(int level) { focus = level == 2; detailsOpen = level == 0; ApplyResponsiveLayout(root.ActualWidth); }
    void SetWritingFont(string font) { canvas.SetTypography(font); title.FontFamily = new FontFamily(font); subtitle.FontFamily = new FontFamily(font); }
    void SetConceptTheme(string name)
    {
        themeName = name; root.RequestedTheme = name == "Windows" ? ElementTheme.Default : name == "Midnight" ? ElementTheme.Dark : ElementTheme.Light; palette.Apply(name == "Windows" ? root.ActualTheme == ElementTheme.Dark ? "Midnight" : "Dawn" : name); canvas.SetPalette(palette.Ink, palette.Body, palette.Accent, palette.Selection);
        foreach (var choice in themeRings) choice.Ring.BorderBrush = choice.Name == name ? palette.Accent : new SolidColorBrush(Colors.Transparent);
        if (!smokeMode) { try { File.WriteAllText(Path.Combine(Path.GetDirectoryName(CatalogPath)!, "concept-theme.txt"), name); } catch { } }
    }
    string SavedConceptTheme() { try { var file = Path.Combine(Path.GetDirectoryName(CatalogPath)!, "concept-theme.txt"); if (File.Exists(file)) { var name = File.ReadAllText(file).Trim(); if (new[] { "Dawn", "Midnight", "Sage", "Blossom", "Monochrome", "Windows" }.Contains(name)) return name; } } catch { } return "Dawn"; }
}









