using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
namespace Acrux;
public sealed class NoteCanvas : Grid
{
    public event Action<string>? MarkdownChanged;
    public event Action<string>? Notice;
    public event Action<string>? OpenNote;
    public event Action? RequestLink;
    readonly Canvas surface = new() { Width = 780, Height = 1000, Background = new SolidColorBrush(Colors.Transparent) };
    readonly ScrollViewer viewport = new() { VerticalContentAlignment = VerticalAlignment.Top, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollMode = ScrollMode.Disabled, VerticalScrollMode = ScrollMode.Enabled, ZoomMode = ZoomMode.Enabled, MinZoomFactor = .35f, MaxZoomFactor = 2f };
    readonly List<Container> containers = new(); readonly List<LiveMarkdownEditor> editors = new(); readonly Dictionary<string, Border> visuals = new();
    LiveMarkdownEditor? activeEditor; string path = "", font = "Georgia"; bool loading, sourceMode, pageMode = true, relayout;
    Brush ink = new SolidColorBrush(ConceptPalette.Color("222364")), bodyBrush = new SolidColorBrush(ConceptPalette.Color("32303d")), accent = new SolidColorBrush(ConceptPalette.Color("7565d8")), selection = new SolidColorBrush(ConceptPalette.Color("ece7fb"));
    public sealed class Container { public string Id { get; set; } = Guid.NewGuid().ToString("N"); public string Markdown { get; set; } = ""; public string Prefix { get; set; } = ""; public double X { get; set; } = 48; public double Y { get; set; } = 48; public double Width { get; set; } = 620; }
    public sealed class Position { public string Id { get; set; } = ""; public double X { get; set; } public double Y { get; set; } public double Width { get; set; } = 620; }
    public NoteCanvas() { viewport.Content = surface; Children.Add(viewport); viewport.SizeChanged += (_, _) => LayoutCards(); surface.DoubleTapped += (_, e) => { if (!pageMode && ReferenceEquals(e.OriginalSource, surface)) { var p = e.GetPosition(surface); Add(p.X, p.Y); e.Handled = true; } }; }
    public void Load(string notePath, string markdown)
    {
        loading = true; path = notePath; containers.Clear(); editors.Clear(); surface.Children.Clear(); visuals.Clear(); activeEditor = null;
        var chunks = System.Text.RegularExpressions.Regex.Split(markdown, @"\n\n<!-- acrux:container:([a-f0-9]+) -->\n\n"); containers.Add(new Container { Id = "main", Markdown = chunks[0] });
        for (int i = 1; i + 1 < chunks.Length; i += 2) containers.Add(new Container { Id = chunks[i], Markdown = chunks[i + 1], X = 740, Y = 48 + 280 * ((i - 1) / 2), Width = 400 });
        try { if (File.Exists(path + ".acrux.json")) { var positions = JsonSerializer.Deserialize<List<Position>>(File.ReadAllText(path + ".acrux.json")) ?? new(); foreach (var c in containers) if (positions.FirstOrDefault(p => p.Id == c.Id) is { } p) { c.X = double.IsFinite(p.X) ? Math.Max(0, p.X) : 48; c.Y = double.IsFinite(p.Y) ? Math.Max(0, p.Y) : 48; c.Width = double.IsFinite(p.Width) ? Math.Clamp(p.Width, 280, 1600) : 620; } } } catch (Exception ex) { Notice?.Invoke("Canvas layout could not be loaded: " + ex.Message); }
        foreach (var container in containers) AddVisual(container); loading = false; LayoutCards(); viewport.ChangeView(0, 0, 1f, true);
    }
    public string Markdown => containers.Count == 0 ? "" : containers[0].Markdown + string.Concat(containers.Skip(1).Select(c => "\n\n<!-- acrux:container:" + c.Id + " -->\n\n" + c.Markdown));
    public int ContainerCount => containers.Count;
    internal bool SourceMatchesModel => editors.All(e => e.SourceMatchesModel);
    public bool PageMode => pageMode;
    public void Clear() { path = ""; containers.Clear(); editors.Clear(); visuals.Clear(); surface.Children.Clear(); activeEditor = null; }
    public void UpdatePath(string newPath) { var oldPath = path; path = newPath; try { if (oldPath != newPath && File.Exists(oldPath + ".acrux.json")) File.Move(oldPath + ".acrux.json", newPath + ".acrux.json", true); } catch (Exception ex) { Notice?.Invoke("Layout rename: " + ex.Message); SaveLayout(); } }
    public void SetPageMode(bool value) { pageMode = value; viewport.HorizontalScrollBarVisibility = value ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto; viewport.HorizontalScrollMode = value ? ScrollMode.Disabled : ScrollMode.Enabled; var text = Markdown; if (path.Length > 0) Load(path, text); }
    public void SetSourceMode(bool value) { sourceMode = value; if (path.Length > 0) { var markdown = Markdown; Load(path, markdown); } }
    public void ToggleSource() { SetSourceMode(!sourceMode); Notice?.Invoke(sourceMode ? "Markdown source" : "Live preview"); }
    public bool UpdateMetadata(string markdown)
    {
        if(containers.Count==0||editors.Count==0)return false;
        var prefix=sourceMode?"":NoteMeta.HeaderPrefix(markdown,Path.GetFileNameWithoutExtension(path));
        if(markdown[prefix.Length..]!=editors[0].Markdown)return false;
        containers[0].Prefix=prefix;containers[0].Markdown=markdown;return true;
    }
    public void SetTypography(string family) { font = family; foreach (var editor in editors) editor.Configure(font, bodyBrush, accent, selection); }
    public void SetPalette(Brush text, Brush body, Brush highlight, Brush selected) { ink = text; bodyBrush = body; accent = highlight; selection = selected; foreach (var editor in editors) editor.Configure(font, bodyBrush, accent, selection); }
    public void Format(string command) => activeEditor?.Format(command);
    public void InsertBlock(string command) => activeEditor?.InsertBlock(command);
    public void InsertMarkdown(string markdown) => activeEditor?.InsertMarkdown(markdown);
    public void InsertLink(string label, string url) => activeEditor?.InsertLink(label, url);
    public string SelectedText => activeEditor?.SelectedText ?? "";
    public void SplitParagraph() => activeEditor?.SplitActive();
    public void MergeParagraph() => activeEditor?.MergePrevious();
    public void Undo() => activeEditor?.Undo(); public void Redo() => activeEditor?.Redo();
    public bool Find(string text) => activeEditor?.Find(text) ?? false;
    public void FocusEditor() => activeEditor?.FocusEnd();
    public void Home() => viewport.ChangeView(0, 0, 1f);
    public void Zoom(float delta) => viewport.ChangeView(null, null, Math.Clamp(viewport.ZoomFactor + delta, viewport.MinZoomFactor, viewport.MaxZoomFactor));
    public void ScrollToHeading(string heading) { var c = containers.FirstOrDefault(c => c.Markdown.Contains(heading)); if (c != null && visuals.TryGetValue(c.Id, out var card)) viewport.ChangeView(null, Canvas.GetTop(card) * viewport.ZoomFactor, null); }
    public void Add() => Add(viewport.HorizontalOffset / viewport.ZoomFactor + 80 + containers.Count * 40, viewport.VerticalOffset / viewport.ZoomFactor + 80 + containers.Count * 32);
    void Add(double x, double y) { if (path.Length == 0) return; var c = new Container { X = x, Y = y, Width = 420, Markdown = "## A new thought\n\nClick here and start writing." }; containers.Add(c); AddVisual(c); LayoutCards(); Emit(); SaveLayout(); }
    void Emit() { if (!loading) MarkdownChanged?.Invoke(Markdown); }
    public void SaveLayout() { if (path.Length == 0 || loading) return; try { File.WriteAllText(path + ".acrux.json", JsonSerializer.Serialize(containers.Select(c => new Position { Id = c.Id, X = c.X, Y = c.Y, Width = c.Width }), new JsonSerializerOptions { WriteIndented = true })); } catch (Exception ex) { Notice?.Invoke("Could not save canvas layout: " + ex.Message); } }
    void LayoutCards()
    {
        if (relayout) return; relayout = true; double top = 0;
        foreach (var c in containers) if (visuals.TryGetValue(c.Id, out var card))
        {
            card.Width = pageMode ? Math.Max(280, viewport.ActualWidth > 0 ? viewport.ActualWidth - 10 : 750) : c.Width; Canvas.SetLeft(card, pageMode ? 0 : c.X); Canvas.SetTop(card, pageMode ? top : c.Y); top += Math.Max(40, card.ActualHeight) + 16;
        }
        surface.Width = pageMode ? Math.Max(280, viewport.ActualWidth) : Math.Max(4800, containers.Select(c => c.X + c.Width + 1000).DefaultIfEmpty(4800).Max());
        surface.Height = pageMode ? Math.Max(viewport.ActualHeight, top + 20) : Math.Max(3200, containers.Select(c => c.Y + (visuals.TryGetValue(c.Id, out var v) ? v.ActualHeight : 500) + 1000).DefaultIfEmpty(3200).Max()); relayout = false;
    }
    void AddVisual(Container c)
    {
        var body = new StackPanel { Spacing = 0, Padding = new Thickness(pageMode ? 0 : 12, 0, pageMode ? 0 : 18, pageMode ? 0 : 15) };
        var header = new Border { Height = 25, Background = new SolidColorBrush(Colors.Transparent), Visibility = pageMode ? Visibility.Collapsed : Visibility.Visible, Child = new TextBlock { Text = "\u283f  " + (c.Id == "main" ? "note" : "thought"), FontSize = 10, Opacity = .4, Foreground = ink } }; body.Children.Add(header);
        var prefix = c.Id == "main" && !sourceMode ? NoteMeta.HeaderPrefix(c.Markdown, Path.GetFileNameWithoutExtension(path)) : "";
        c.Prefix = prefix; var live = new LiveMarkdownEditor(c.Markdown[prefix.Length..], Path.GetDirectoryName(path) ?? ""); live.Configure(font, bodyBrush, accent, selection); live.SetSourceView(sourceMode); body.Children.Add(live); editors.Add(live); activeEditor ??= live;
        live.Activated += e => { if (activeEditor != e) activeEditor?.Commit(); activeEditor = e; }; live.Changed += () => { c.Markdown = c.Prefix + live.Markdown; Emit(); LayoutCards(); }; live.OpenNote += n => OpenNote?.Invoke(n); live.RequestLink += () => RequestLink?.Invoke();
        var resize = new Border { Width = 32, Height = 16, Background = new SolidColorBrush(Colors.Transparent), HorizontalAlignment = HorizontalAlignment.Right, Visibility = pageMode ? Visibility.Collapsed : Visibility.Visible, Child = new TextBlock { Text = "\u2194", Foreground = ink, Opacity = .3 } }; body.Children.Add(resize);
        var card = new Border { Width = pageMode ? 750 : c.Width, Child = body, Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(pageMode ? 0 : 1), BorderBrush = new SolidColorBrush(Colors.Transparent) }; visuals[c.Id] = card; surface.Children.Add(card); card.SizeChanged += (_, _) => LayoutCards();
        if (!pageMode) { card.PointerEntered += (_, _) => card.BorderBrush = accent; card.PointerExited += (_, _) => card.BorderBrush = new SolidColorBrush(Colors.Transparent); }
        bool dragging = false, resizing = false; Windows.Foundation.Point start = default; double x = 0, y = 0, width = 0;
        header.PointerPressed += (_, e) => { if (!e.GetCurrentPoint(header).Properties.IsLeftButtonPressed) return; dragging = header.CapturePointer(e.Pointer); start = e.GetCurrentPoint(surface).Position; x = c.X; y = c.Y; e.Handled = true; };
        header.PointerMoved += (_, e) => { if (!dragging) return; var p = e.GetCurrentPoint(surface).Position; c.X = Math.Max(0, x + p.X - start.X); c.Y = Math.Max(0, y + p.Y - start.Y); LayoutCards(); e.Handled = true; };
        header.PointerReleased += (_, e) => { dragging = false; header.ReleasePointerCapture(e.Pointer); SaveLayout(); e.Handled = true; }; header.PointerCaptureLost += (_, _) => { if (dragging) { dragging = false; SaveLayout(); } };
        resize.PointerPressed += (_, e) => { resizing = resize.CapturePointer(e.Pointer); start = e.GetCurrentPoint(surface).Position; width = c.Width; e.Handled = true; };
        resize.PointerMoved += (_, e) => { if (resizing) { c.Width = Math.Clamp(width + e.GetCurrentPoint(surface).Position.X - start.X, 280, 1600); LayoutCards(); e.Handled = true; } };
        resize.PointerReleased += (_, e) => { resizing = false; resize.ReleasePointerCapture(e.Pointer); SaveLayout(); e.Handled = true; }; resize.PointerCaptureLost += (_, _) => { if (resizing) { resizing = false; SaveLayout(); } };
    }
}

