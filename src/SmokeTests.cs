using Microsoft.UI.Xaml;
using System.Text.Json;
namespace Acrux;

public sealed partial class MainWindow
{
    internal async Task RunSmokeTests()
    {
        var results = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Smoke test failed: " + name); results.Add(name); }
        try
        {
            await Task.Delay(250);
            Check(smokeMode, "Tests use an isolated notebook");
            Check(notebookHome?.Visibility==Visibility.Visible,"Startup opens all notebook live tiles");
            Check(PointerNear(110,80,150,90,200,40)&&!PointerNear(10,400,150,90,200,40),"Command proximity includes approach area and excludes distant pointer");
            foreach (var text in new[] { "", "paragraph", "one\n", "one\n\ntwo", "one\n\n\ntwo", "```cs\nvar a = 1;\n\nvar b = 2;\n```\n\nAfter", "# Heading\n\n> Quote\n> next line\n\n- [ ] Task\n" })
                Check(MarkdownBlocks.Join(MarkdownBlocks.Split(text)) == text, "Markdown round trip: " + results.Count);
            var continuous = new LiveMarkdownEditor("first\n\n## Heading\n\nlast"); continuous.SelectRange(0,22); Check(continuous.SelectedText.Contains("Heading") && continuous.SelectedText.Contains("first"), "Native selection crosses paragraphs and headings");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"editor-range-probe.txt"),continuous.RangeProbe);
            var capturedPath=StoreCapture("Captured fixture","A quick idea","https://example.com","Inbox"); Check(File.Exists(capturedPath)&&File.ReadAllText(capturedPath).Contains("[Source](https://example.com)"),"Capture saves source and content as local Markdown");
            var duplicateCapture=StoreCapture("Captured fixture","Another idea","","Inbox");Check(duplicateCapture!=capturedPath,"Capture never overwrites a same-name note");
            ShowCapture();Check(topView=="capture"&&overlayView?.Visibility==Visibility.Visible,"Capture has its own navigable workspace");ShowWriting();
            ApplyResponsiveLayout(400);Check(side.Visibility==Visibility.Collapsed&&context.Visibility==Visibility.Collapsed,"Sidebar writing hides both permanent rails");ApplyResponsiveLayout(960);Check(side.Visibility==Visibility.Collapsed && workspace.ColumnDefinitions[1].Width.Value==0,"Half-width writing keeps organisation summoned");ApplyResponsiveLayout(1608);
            hideNavigation=true;ApplyResponsiveLayout(1608);Check(side.Visibility==Visibility.Collapsed && workspace.Children[1].Visibility==Visibility.Collapsed,"Desktop navigation can give way to the document");hideNavigation=false;ApplyResponsiveLayout(1608);
            var sampleNotes = all.ToList();
            foreach (var note in sampleNotes) { Select(note); await Task.Delay(35); Check(canvas.ContainerCount == 1 && current?.Path == note.Path, "Open note: " + note.Name); }
            ShowNotebooks(); Check(notebookHome != null && notebookHome.Visibility == Visibility.Visible, "Notebook tile landing page");
            var path = Path.Combine(vault, "Smoke fixture.md");
            var original = "# Smoke fixture\n\nA paragraph with **bold**, *italic*, ~~strike~~, `code`, and [[Small communities]].\n\n## Heading\n\n- [ ] A task\n\n```text\nfirst\n\nsecond\n```\n\n| Name | Value |\n| --- | --- |\n| One | Two |\n";
            File.WriteAllText(path, original); current = null; LoadVault(); var fixture = all.Single(n => n.Path == path); NavigateTo(fixture);
            Check(canvas.Markdown == original, "Opening a formatted note preserves its Markdown");
            Check(!detailsOpen && context.Visibility == Visibility.Collapsed, "Note details do not compete with writing by default");
            Check(workspace.ColumnDefinitions[0].Width.Value==0 && workspace.ColumnDefinitions[1].Width.Value==0,"Write has no permanent navigation columns");
            ShowExplore();Check(overlayView?.Visibility==Visibility.Visible && workspace.Visibility==Visibility.Collapsed,"Explore summons the library without a permanent sidebar");ShowWriting();Check(overlayView?.Visibility==Visibility.Collapsed,"Returning to Write dismisses Explore");
            Check(canvas.PageMode, "Writing opens as a flowing book page");
            Check(topView == "write", "The write navigation state is selected");
            touchControls=false;RevealCommands(writingTools!,false);await Task.Delay(340);Check(writingTools!.Opacity<.2,"Writing commands rest as a faint hint");RevealCommands(writingTools,true);await Task.Delay(200);Check(writingTools.Opacity>.9,"Approach reveal animates writing commands to full visibility");RevealCommands(writingTools,false);
            Check(canvas.SourceMatchesModel, "Multiline source controls preserve complete Markdown blocks");
            canvas.FocusEditor(); var flowBefore = draft; canvas.SplitParagraph(); Check(draft == flowBefore + "\n\n", "Enter continues into a new paragraph"); canvas.MergeParagraph(); Check(draft == flowBefore, "Backspace merge preserves paragraph content");
            var metadataProbe = NoteMeta.Set(original, "tags", new[] { "design", "internet" }); metadataProbe = NoteMeta.Set(metadataProbe, "source_links", new[] { new NoteMeta.Source("Reference", "https://example.com") }); Check(NoteMeta.Tags(metadataProbe).Count == 2 && NoteMeta.Sources(metadataProbe).Count == 1 && NoteMeta.Body(metadataProbe) == original, "Tags and sources preserve the Markdown body");
            var editedSubtitle = NoteMeta.WithSubtitle(metadataProbe, "Smoke fixture", "A revised description."); Check(NoteMeta.Subtitle(editedSubtitle) == "A revised description." && editedSubtitle.Contains("```text\nfirst\n\nsecond\n```"), "The editable subtitle retains metadata and code blocks");
            var spatialBefore = draft; canvas.SetPageMode(false); Check(!canvas.PageMode && canvas.Markdown == spatialBefore, "Freeform canvas retains note content"); canvas.SetPageMode(true); Check(canvas.Markdown == spatialBefore, "Returning to book layout preserves content");
            ShowConnections(); Check(topView == "connect" && overlayView?.Visibility == Visibility.Visible, "Connections is a complete navigable view"); ShowReview(); Check(topView == "review", "Review view is available"); ShowWriting();
            var before = draft; canvas.Format("bold"); Check(draft != before && draft.Contains("**text**"), "Formatting toolbar updates Markdown");
            Check(!draft.Contains('\r'), "Formatting retains portable Markdown line endings");
            canvas.Undo(); Check(draft == before, "Undo restores exact Markdown"); canvas.Redo(); Check(draft.Contains("**text**"), "Redo restores formatting");
            canvas.InsertBlock("task"); Check(draft.Contains("- [ ] To do"), "Insert to-do block"); canvas.InsertBlock("table"); Check(draft.Contains("| Column one |"), "Insert Markdown table");
            canvas.ToggleSource(); canvas.ToggleSource(); Check(canvas.ContainerCount == 1, "Source and live preview switch safely");
            canvas.Add(); Check(canvas.ContainerCount == 2 && draft.Contains("<!-- acrux:container:"), "Create a second Markdown container");
            Check(TrySave(), "Autosave writes the note"); Check(File.ReadAllText(path) == draft, "Saved note matches the editor");
            canvas.SaveLayout(); Check(File.Exists(path + ".acrux.json"), "Canvas layout saved separately");
            var saved = draft; Select(current!); Check(canvas.ContainerCount == 2 && canvas.Markdown == saved, "Reopen both containers without changing Markdown");
            title.Text = "Renamed smoke fixture"; Check(TrySave(), "Rename note"); var renamed = Path.Combine(vault, "Renamed smoke fixture.md"); Check(File.Exists(renamed) && File.Exists(renamed + ".acrux.json"), "Rename moves note and canvas layout");
            var other = all.First(n => n.Path != renamed); Select(other); Select(all.Single(n => n.Path == renamed)); Check(current?.Path == renamed, "Switch notes after renaming");
            var external = draft + "\nExternal update\n"; File.WriteAllText(renamed, external); canvas.InsertBlock("quote"); Check(!TrySave() && File.ReadAllText(renamed) == external, "External file changes are not overwritten"); dirty = false;
            current = null; loading = true; search.Text = "not-a-real-note-7b68"; loading = false; RefreshNotes(); Check(current == null && canvas.ContainerCount == 0, "Empty search clears the previous canvas");
            root.RequestedTheme = ElementTheme.Dark; root.RequestedTheme = ElementTheme.Light; Check(root.ActualTheme == ElementTheme.Light, "Light and dark theme controls");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test-results.json"), JsonSerializer.Serialize(new { passed = true, checks = results.Count, results }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test-results.json"), JsonSerializer.Serialize(new { passed = false, checks = results.Count, results, error = ex.ToString() }, new JsonSerializerOptions { WriteIndented = true })); }
        finally { dirty = false; Application.Current.Exit(); }
    }
}








