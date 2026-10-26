using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
namespace Acrux;
public sealed partial class MainWindow
{
    Grid? modernHeader; StackPanel? modernPivots; FrameworkElement? modernBrand, modernCommands; ComboBox? notebookPicker; bool compactNav, hideNavigation;
    record NotebookChoice(string Path,string Name);
    void BuildModernHeader()
    {
        modernHeader=new Grid {Padding=new Thickness(24,16,24,12),Height=104,BorderThickness=new Thickness(0,0,0,1),BorderBrush=palette.Line};
        modernHeader.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(200)});modernHeader.ColumnDefinitions.Add(new ColumnDefinition());modernHeader.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var brandText=new StackPanel{Spacing=2};brandText.Children.Add(Label("acrux.",27));brandText.Children.Add(Label("by Southlight I/O",11,.6));var brand=Row(ConceptArtwork.Star("33347a",30),brandText);brand.VerticalAlignment=VerticalAlignment.Center;modernBrand=brand;modernHeader.Children.Add(brand);
        modernPivots=new StackPanel{Orientation=Orientation.Horizontal,Spacing=40,VerticalAlignment=VerticalAlignment.Center};
        modernPivots.Children.Add(ConceptPivot("capture",ShowCapture,homePivotLine));modernPivots.Children.Add(ConceptPivot("write",ShowWriting,writePivotLine));modernPivots.Children.Add(ConceptPivot("connect",()=>ShowConnections(true),connectPivotLine));modernPivots.Children.Add(ConceptPivot("review",ShowReview,reviewPivotLine));
        Grid.SetColumn(modernPivots,1);modernHeader.Children.Add(modernPivots);
        var commands=Row(Action("\u2630",()=>{if(root.ActualWidth>=900)hideNavigation=!hideNavigation;else compactNav=!compactNav;ApplyResponsiveLayout(root.ActualWidth);},"Show navigation"),Action("Search",async()=>await ShowQuickSwitcher()),Action("+",async()=>await NewNote(),"New note"));
        var more=new Button{Content="\u2026",Background=palette.Window,BorderThickness=new Thickness(0)};var menu=new MenuFlyout();
        void Item(string label,Action run){var i=new MenuFlyoutItem{Text=label};i.Click+=(_,_)=>run();menu.Items.Add(i);}
        Item("Notebooks",ShowNotebooks);Item("Note details",async()=>await ShowColumnDetails());Item("Markdown source / Live preview",canvas.ToggleSource);Item("Document",()=>canvas.SetPageMode(true));Item("Canvas",()=>canvas.SetPageMode(false));Item("New container",canvas.Add);Item("Daily note",async()=>await DailyNote());Item("Insert",async()=>await ShowInsertMenu());Item("Mark for review",()=>{if(current!=null)CommitMetadata(NoteMeta.Set(draft,"review",true));});Item("Focus writing",ToggleFocus);Item("Open containing folder",()=>{if(current!=null)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","/select,\""+current.Path+"\""){UseShellExecute=true});});Item("Settings",async()=>await ShowSettings());more.Flyout=menu;commands.Children.Add(more);modernCommands=commands;commands.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(commands,2);modernHeader.Children.Add(commands);root.Children.Add(modernHeader);
    }
    void BuildModernWorkspace()
    {
        workspace.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(240)});workspace.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(0)});workspace.ColumnDefinitions.Add(new ColumnDefinition());workspace.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(280)});Grid.SetRow(workspace,1);root.Children.Add(workspace);
        var nav=new Grid{Padding=new Thickness(28,24,16,16),RowSpacing=20};nav.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});nav.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});nav.RowDefinitions.Add(new RowDefinition());nav.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        nav.Children.Add(ColumnHeading("notebooks"));
        notebookPicker=new ComboBox{HorizontalAlignment=HorizontalAlignment.Stretch};notebookPicker.SelectionChanged+=(_,_)=>{if(notebookPicker.SelectedItem is NotebookChoice p&&!p.Path.Equals(vault,StringComparison.OrdinalIgnoreCase))OpenNotebook(p.Path);};Grid.SetRow(notebookPicker,1);nav.Children.Add(notebookPicker);
        var sections=new StackPanel{Spacing=14};sections.Children.Add(Eyebrow("SECTIONS"));sections.Children.Add(folders);var sectionScroll=new ScrollViewer{Content=sections};Grid.SetRow(sectionScroll,2);nav.Children.Add(sectionScroll);
        var actions=new StackPanel{Spacing=4};actions.Children.Add(Action("+ New section",async()=>await NewFolder()));actions.Children.Add(Action("All notebooks",ShowNotebooks));Grid.SetRow(actions,3);nav.Children.Add(actions);side.Child=nav;side.BorderThickness=new Thickness(0);workspace.Children.Add(side);
        var notePane=new Grid{Padding=new Thickness(16,24,20,12),RowSpacing=20};notePane.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});notePane.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});notePane.RowDefinitions.Add(new RowDefinition());
        var noteHeader=new Grid();noteHeader.ColumnDefinitions.Add(new ColumnDefinition());noteHeader.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});noteHeader.Children.Add(ColumnHeading("notes"));var addNote=Action("+",async()=>await NewNote(),"New note");Grid.SetColumn(addNote,1);noteHeader.Children.Add(addNote);notePane.Children.Add(noteHeader);
        search.Width=double.NaN;Grid.SetRow(search,1);notePane.Children.Add(search);Grid.SetRow(notes,2);notePane.Children.Add(notes);Grid.SetColumn(notePane,1);workspace.Children.Add(notePane);
        bookColumns.ColumnDefinitions.Add(new ColumnDefinition());bookColumns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(0)});
        var sheet=new Grid{Background=palette.Paper,MaxWidth=980,HorizontalAlignment=HorizontalAlignment.Stretch};sheet.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});sheet.RowDefinitions.Add(new RowDefinition());
        title.AcceptsReturn=true; title.KeyDown+=(_,e)=>{if(e.Key==Windows.System.VirtualKey.Enter){e.Handled=true;canvas.FocusEditor();}}; var head=new StackPanel{Spacing=12,Margin=new Thickness(36,28,36,16)};head.Children.Add(breadcrumb);title.FontFamily=new FontFamily("Segoe UI");title.Foreground=palette.Ink;title.FontSize=44;title.FontWeight=Microsoft.UI.Text.FontWeights.SemiBold;title.TextWrapping=TextWrapping.Wrap;title.MaxWidth=850;head.Children.Add(title);subtitle.FontFamily=new FontFamily("Segoe UI");subtitle.FontSize=16;subtitle.Foreground=palette.Muted;head.Children.Add(subtitle);sheet.Children.Add(head);
        canvas.Margin=new Thickness(36,0,36,0);canvas.SetTypography("Segoe UI");Grid.SetRow(canvas,1);sheet.Children.Add(canvas);bookFrame=new Border{Child=sheet};bookColumns.Children.Add(bookFrame);Grid.SetColumn(sourcePreview,1);bookColumns.Children.Add(sourcePreview);Grid.SetColumn(bookColumns,2);workspace.Children.Add(bookColumns);
        var details=new StackPanel{Padding=new Thickness(20,20,16,16),Spacing=20};details.Children.Add(Action("details  \u00d7",()=>{detailsOpen=false;ApplyResponsiveLayout(root.ActualWidth);}));details.Children.Add(InspectorGroup("Tags",tagChips));details.Children.Add(InspectorGroup("Backlinks & links",linkedNotes));details.Children.Add(Action("+ Link note",async()=>await LinkAnotherNote()));details.Children.Add(InspectorGroup("References",sourceCards));details.Children.Add(Action("+ Add reference",async()=>await AddSource()));details.Children.Add(InspectorGroup("File details",properties));context.Child=new ScrollViewer{Content=details};context.BorderBrush=palette.Line;context.BorderThickness=new Thickness(1,0,0,0);Grid.SetColumn(context,3);workspace.Children.Add(context);detailsOpen=false;
    }
    void BuildModernFooter()
    {
        var footer=new Grid{Height=36,Padding=new Thickness(20,4,20,4)};footer.ColumnDefinitions.Add(new ColumnDefinition());footer.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});savedCaption.FontSize=12;savedCaption.Foreground=palette.Muted;footer.Children.Add(savedCaption);wordCount.FontSize=12;wordCount.Foreground=palette.Muted;Grid.SetColumn(wordCount,1);footer.Children.Add(wordCount);status.RegisterPropertyChangedCallback(TextBlock.TextProperty,(_,_)=>savedCaption.Text=status.Text);Grid.SetRow(footer,2);root.Children.Add(footer);
    }
    void AdaptModernHeader(double width)
    {
        if(modernHeader==null||modernPivots==null)return;
        modernBrand!.Visibility=width<1100?Visibility.Collapsed:Visibility.Visible;modernHeader.ColumnDefinitions[0].Width=new GridLength(width<1100?0:200);modernHeader.Height=width<700?130:104; title.FontSize=width<640?28:44; subtitle.Height=double.NaN; subtitle.AcceptsReturn=true; subtitle.TextWrapping=TextWrapping.Wrap;
        modernPivots.Spacing=width<1100?20:40;modernCommands!.VerticalAlignment=width<700?VerticalAlignment.Bottom:VerticalAlignment.Center;
        foreach(var button in modernPivots.Children.OfType<Button>())if(button.Content is StackPanel p&&p.Children[0] is TextBlock t)t.FontSize=width<700?25:width<1100?38:48;
        if(width<700){modernPivots.VerticalAlignment=VerticalAlignment.Top;Grid.SetColumn(modernCommands,1);modernCommands.HorizontalAlignment=HorizontalAlignment.Right;modernHeader.ColumnDefinitions[2].Width=new GridLength(0);}else{modernPivots.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(modernCommands,2);modernHeader.ColumnDefinitions[2].Width=GridLength.Auto;}
    }
    Task ShowColumnDetails()
    {
        if(current==null)return Task.CompletedTask;

        var board=ActivityColumns("write",new[]{"details","references","links"},new[]{1.0,1.0,1.0});
        board.Columns[0].Children.Add(Label(current.Name,26));board.Columns[0].Children.Add(Label(current.Group,14,.6));board.Columns[0].Children.Add(Eyebrow("TAGS"));
        foreach(var tag in NoteMeta.Tags(draft)){var value=tag;board.Columns[0].Children.Add(Action(tag+"  ×",()=>{var tags=NoteMeta.Tags(draft);tags.Remove(value);CommitMetadata(NoteMeta.Set(draft,"tags",tags));_ = ShowColumnDetails();}));}
        board.Columns[0].Children.Add(Action("Add tag",async()=>{await AddTag();await ShowColumnDetails();}));board.Columns[0].Children.Add(Action("Back to note",ShowWriting));
        foreach(var source in NoteMeta.Sources(draft)){var reference=source;board.Columns[1].Children.Add(Label(source.Title,21));board.Columns[1].Children.Add(Label(source.Detail,13,.6));if(source.Url.Length>0)board.Columns[1].Children.Add(Action("Open reference",async()=>await Windows.System.Launcher.LaunchUriAsync(new Uri(reference.Url))));board.Columns[1].Children.Add(Action("Edit reference",async()=>{await EditReference(reference);await ShowColumnDetails();}));}
        board.Columns[1].Children.Add(Action("Add reference",async()=>{await AddSource();await ShowColumnDetails();}));
        foreach(var note in all.Where(n=>n.Path!=current.Path&&n.Body.Contains("[["+current.Name+"]]",StringComparison.OrdinalIgnoreCase))){var target=note;board.Columns[2].Children.Add(MetroChoice(note.Name,"Backlink",()=>NavigateTo(target)));}board.Columns[2].Children.Add(Action("Link a note",async()=>{await LinkAnotherNote();await ShowColumnDetails();}));return Task.CompletedTask;
    }
    async Task FindInNote()
    {
        var query=new TextBox{PlaceholderText="Find in this note"};var dialog=new ContentDialog{Title="Find in note",Content=query,PrimaryButtonText="Find",CloseButtonText="Cancel",XamlRoot=root.XamlRoot};if(await dialog.ShowAsync()==ContentDialogResult.Primary&&query.Text.Length>0){ShowWriting();if(!canvas.Find(query.Text))status.Text="No matching text in this note.";}
    }
    void Sweep(FrameworkElement element)
    {
        if(!new UISettings().AnimationsEnabled)return;var transform=new TranslateTransform();element.RenderTransform=transform;var storyboard=new Storyboard();var movement=new DoubleAnimation{From=24,To=0,Duration=TimeSpan.FromMilliseconds(250),EnableDependentAnimation=true,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};Storyboard.SetTarget(movement,transform);Storyboard.SetTargetProperty(movement,"X");storyboard.Children.Add(movement);storyboard.Begin();
    }
}
