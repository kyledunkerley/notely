using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace Acrux;
public sealed partial class MainWindow
{
    void BuildFocusHeader()
    {
        modernHeader=new Grid{Padding=new Thickness(36,16,36,12),RowSpacing=16};modernHeader.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});modernHeader.RowDefinitions.Add(new RowDefinition());
        var tools=new Grid();tools.ColumnDefinitions.Add(new ColumnDefinition());tools.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var brand=Row(Action("acrux. / home",ShowNotebooks,"Home: all notebook live tiles"),Action("explore",ShowExplore,"Notebooks, sections and notes"));tools.Children.Add(brand);
        var commands=Row(Action("+ note",async()=>await NewNote()),Action("context",async()=>await ShowColumnDetails()),Action("commands",async()=>await ShowCommandPalette()));Grid.SetColumn(commands,1);tools.Children.Add(commands);modernCommands=commands;modernHeader.Children.Add(tools);
        modernPivots=new StackPanel{Orientation=Orientation.Horizontal,Spacing=36,VerticalAlignment=VerticalAlignment.Bottom};
        modernPivots.Children.Add(ConceptPivot("capture",ShowCapture,homePivotLine));modernPivots.Children.Add(ConceptPivot("write",ShowWriting,writePivotLine));modernPivots.Children.Add(ConceptPivot("connect",()=>ShowConnections(true),connectPivotLine));modernPivots.Children.Add(ConceptPivot("review",ShowReview,reviewPivotLine));
        var landmarks=new ScrollViewer{Content=modernPivots,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollMode=ScrollMode.Enabled};Grid.SetRow(landmarks,1);modernHeader.Children.Add(landmarks);root.Children.Add(modernHeader);
    }
    void BuildFocusWorkspace()
    {
        for(int i=0;i<4;i++)workspace.ColumnDefinitions.Add(new ColumnDefinition{Width=i==2?new GridLength(1,GridUnitType.Star):new GridLength(0)});Grid.SetRow(workspace,1);root.Children.Add(workspace);
        side.Visibility=Visibility.Collapsed;workspace.Children.Add(side);workspace.Children.Add(new Grid{Visibility=Visibility.Collapsed});
        bookColumns.ColumnDefinitions.Add(new ColumnDefinition());bookColumns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(0)});
        var sheet=new Grid{MaxWidth=960,Margin=new Thickness(28,16,28,0),HorizontalAlignment=HorizontalAlignment.Center};sheet.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});sheet.RowDefinitions.Add(new RowDefinition());
        var head=new StackPanel{Spacing=12,Margin=new Thickness(28,0,28,16)};breadcrumb.FontSize=12;breadcrumb.Foreground=palette.Muted;head.Children.Add(breadcrumb);title.FontFamily=new FontFamily("Segoe UI");title.FontSize=44;title.Foreground=palette.Ink;title.FontWeight=Microsoft.UI.Text.FontWeights.SemiBold;title.BorderThickness=new Thickness(0);title.AcceptsReturn=true;title.TextWrapping=TextWrapping.Wrap;title.KeyDown+=(_,e)=>{if(e.Key==Windows.System.VirtualKey.Enter){e.Handled=true;canvas.FocusEditor();}};head.Children.Add(title);subtitle.FontFamily=new FontFamily("Segoe UI");subtitle.FontSize=16;subtitle.Foreground=palette.Muted;subtitle.Height=double.NaN;subtitle.AcceptsReturn=true;subtitle.TextWrapping=TextWrapping.Wrap;head.Children.Add(subtitle);sheet.Children.Add(head);
        foreach(var field in new[]{title,subtitle})foreach(var key in new[]{"TextControlBackground","TextControlBackgroundFocused","TextControlBackgroundPointerOver","TextControlBorderBrushFocused"})field.Resources[key]=new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        sheet.RowDefinitions.Insert(1,new RowDefinition{Height=GridLength.Auto});var dock=BuildEditorToolbar();writingTools=dock;Grid.SetRow(dock,1);sheet.Children.Add(dock);canvas.SetTypography("Segoe UI");canvas.Margin=new Thickness(28,0,28,0);Grid.SetRow(canvas,2);sheet.Children.Add(canvas);bookFrame=new Border{Child=sheet};bookColumns.Children.Add(bookFrame);Grid.SetColumn(sourcePreview,1);bookColumns.Children.Add(sourcePreview);Grid.SetColumn(bookColumns,2);workspace.Children.Add(bookColumns);context.Visibility=Visibility.Collapsed;workspace.Children.Add(context);detailsOpen=false;
        sheet.SizeChanged+=(_,e)=>{title.MaxWidth=Math.Max(240,e.NewSize.Width-56);};bookColumns.SizeChanged+=(_,e)=>{sheet.Width=Math.Min(960,Math.Max(280,e.NewSize.Width-56));};
    }
    void ShowExplore()
    {
        if(!TrySave())return;var remembered=topView;var board=ActivityColumns(remembered,new[]{"your notebooks","sections","find a thought"},new[]{1.0,1.0,1.8});
        foreach(var path in notebooks){var folder=path;board.Columns[0].Children.Add(MetroChoice(System.IO.Path.GetFileName(path),Directory.Exists(path)?Directory.EnumerateFiles(path,"*.md",SearchOption.AllDirectories).Count()+" notes":"Unavailable",()=>{OpenNotebook(folder);ShowExplore();},path==vault));}
        board.Columns[0].Children.Add(Action("Notebook overview",ShowNotebooks));board.Columns[0].Children.Add(Action("Open a folder",async()=>await OpenVault()));
        var results=new StackPanel{Spacing=5};var query=new TextBox{PlaceholderText="Find a note"};board.Columns[2].Children.Add(query);board.Columns[2].Children.Add(results);
        void Refresh(string group=""){results.Children.Clear();foreach(var n in all.Where(n=>(group.Length==0||n.Group==group)&&(n.Name.Contains(query.Text,StringComparison.OrdinalIgnoreCase)||n.Body.Contains(query.Text,StringComparison.OrdinalIgnoreCase)))){var note=n;results.Children.Add(MetroChoice(n.Name,n.Group,()=>NavigateTo(note)));}}
        board.Columns[1].Children.Add(MetroChoice("all notes","The whole notebook",()=>Refresh()));foreach(var group in all.Select(n=>n.Group).Where(g=>g.Length>0).Distinct().Order()){var folder=group;board.Columns[1].Children.Add(MetroChoice(group,"",()=>{Refresh(folder);board.Reveal(2);}));}board.Columns[1].Children.Add(Action("+ section",async()=>{await NewFolder();ShowExplore();}));query.TextChanged+=(_,_)=>Refresh();Refresh();board.Columns[0].Children.Add(Action("Back to "+remembered,()=>{if(remembered=="home")ShowNotebooks();else if(remembered=="capture")ShowCapture();else if(remembered=="connect")ShowConnections();else if(remembered=="review")ShowReview();else ShowWriting();}));
    }
    void AdaptFocusHeader(double width)
    {
        if(modernHeader==null||modernPivots==null)return;modernHeader.Height=width<640?128:148;modernHeader.Padding=new Thickness(width<640?20:36,12,width<640?20:36,8);modernPivots.Spacing=width<640?16:32;title.FontSize=width<640?28:44;
        foreach(var button in modernPivots.Children.OfType<Button>()){button.VerticalAlignment=VerticalAlignment.Bottom;if(button.Content is StackPanel panel&&panel.Children[0] is TextBlock label){var active=label.Text==topView;label.FontSize=active?(width<640?30:width<1000?42:52):(width<640?24:width<1000?34:42);label.FontWeight=active?Microsoft.UI.Text.FontWeights.SemiBold:Microsoft.UI.Text.FontWeights.Light;label.Opacity=active?1:.45;panel.Spacing=0;}}
    }
}
