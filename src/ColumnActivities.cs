using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    string lastCapture="";
    void ShowCapture()
    {
        if(!TrySave())return;var board=ActivityColumns("capture",new[]{"capture","a quick thought","recent captures"},new[]{.7,1.65,1.0});var types=board.Columns[0];var entry=board.Columns[1];var recent=board.Columns[2];
        types.Children.Add(Label("Save now. Find its place later.",15,.6));var labels=new[]{"quick idea","link","web clipping"};var hints=new[]{"A thought, without the filing.","Keep an address and your context.","Keep a pasted excerpt and its source."};
        for(int i=0;i<3;i++){var index=i;types.Children.Add(MetroChoice(labels[i],hints[i],()=>{captureKind=index;activityColumnSelection["capture"]=1;ShowCapture();},captureKind==i));}
        types.Children.Add(Eyebrow("CURRENT NOTEBOOK"));types.Children.Add(Label(Path.GetFileName(vault),20));types.Children.Add(Action("Choose notebook",ShowNotebooks));
        var destination=new ComboBox{Header="Add to",ItemsSource=new[]{"Inbox","Web clippings"},SelectedIndex=captureKind==2?1:0,HorizontalAlignment=HorizontalAlignment.Stretch};entry.Children.Add(destination);
        var heading=new TextBox{Header="Title (optional)",Text=captureTitle,PlaceholderText="A thought worth keeping"};heading.TextChanged+=(_,_)=>captureTitle=heading.Text;entry.Children.Add(heading);
        var url=new TextBox{Header="Source address",Text=captureUrl,PlaceholderText="https://",Visibility=captureKind==0?Visibility.Collapsed:Visibility.Visible};url.TextChanged+=(_,_)=>captureUrl=url.Text;entry.Children.Add(url);
        var body=new TextBox{Header=captureKind==2?"Paste an excerpt":"Note",AcceptsReturn=true,Text=captureText,TextWrapping=TextWrapping.Wrap,MinHeight=260,PlaceholderText="Jot something down"};body.TextChanged+=(_,_)=>captureText=body.Text;entry.Children.Add(body);
        entry.Children.Add(Label(captureKind==2?"Keeps the excerpt you paste and its source. Does not download the web page.":"An ordinary Markdown file. You can organise it later.",12,.6));
        var feedback=new InfoBar{IsOpen=lastCapture.Length>0,Message=lastCapture,Severity=InfoBarSeverity.Success,IsClosable=true};entry.Children.Add(feedback);
        var save=Action("Save to inbox",()=>{try{var path=StoreCapture(heading.Text,body.Text,url.Visibility==Visibility.Visible?url.Text:"",destination.SelectedIndex==1?"Web clippings":"Inbox");captureTitle=captureText=captureUrl="";lastCapture="Saved to "+Path.GetRelativePath(vault,path);current=null;LoadVault();ShowCapture();}catch(Exception ex){feedback.Severity=InfoBarSeverity.Error;feedback.Message=ex.Message;feedback.IsOpen=true;}});save.Background=palette.Accent;save.Foreground=new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);save.HorizontalAlignment=HorizontalAlignment.Left;save.Padding=new Thickness(24,12,24,12);destination.SelectionChanged+=(_,_)=>save.Content=destination.SelectedIndex==1?"Save clipping":"Save to inbox";if(destination.SelectedIndex==1)save.Content="Save clipping";entry.Children.Add(save);
        var captured=all.Where(n=>n.Group is "Inbox" or "Web clippings").OrderByDescending(n=>File.GetLastWriteTime(n.Path)).Take(12).ToList();recent.Children.Add(Eyebrow(captured.Count+" RECENT ITEMS"));foreach(var n in captured){var note=n;recent.Children.Add(MetroChoice(n.Name,n.Group+" / "+File.GetLastWriteTime(n.Path).ToString("d MMM"),()=>NavigateTo(note)));}if(captured.Count==0)recent.Children.Add(Label("Nothing captured yet. Your next idea starts here.",16,.6));recent.Children.Add(Action("Process inbox in review",ShowReview));
    }
    void ShowReview()
    {
        if(!TrySave())return;var board=ActivityColumns("review",new[]{"inbox","unfinished tasks","revisit"},new[]{1.0,1.15,1.0});var inbox=board.Columns[0];var tasks=board.Columns[1];var revisit=board.Columns[2];
        var captured=all.Where(n=>n.Group is "Inbox" or "Web clippings").OrderByDescending(n=>File.GetLastWriteTime(n.Path)).ToList();inbox.Children.Add(Eyebrow(captured.Count+" TO FILE"));foreach(var n in captured){var note=n;inbox.Children.Add(MetroChoice(n.Name,n.Group,()=>NavigateTo(note)));var file=Action("File into a section",async()=>await FileCapturedNote(note));file.HorizontalAlignment=HorizontalAlignment.Left;inbox.Children.Add(file);}if(captured.Count==0)inbox.Children.Add(Label("Everything has a place. Your inbox is clear.",16,.6));
        int count=0;foreach(var n in all)foreach(Match match in Regex.Matches(n.Body,@"(?m)^- \[ \] (.+)$")){var note=n;int position=match.Index;var text=new TextBlock{Text=match.Groups[1].Value,TextWrapping=TextWrapping.Wrap,Foreground=palette.Ink,FontSize=17};var check=new CheckBox{Content=text,HorizontalContentAlignment=HorizontalAlignment.Stretch};check.Checked+=(_,_)=>{if(WriteReviewedNote(note,note.Body[..(position+3)]+"x"+note.Body[(position+4)..]))ShowReview();else check.IsChecked=false;};tasks.Children.Add(check);var open=Action(note.Name,()=>NavigateTo(note));open.FontSize=12;open.Opacity=.6;open.HorizontalAlignment=HorizontalAlignment.Left;tasks.Children.Add(open);count++;}if(count==0)tasks.Children.Add(Label("No unfinished Markdown tasks. Room for the next thing.",16,.6));
        var flagged=all.Where(n=>Regex.IsMatch(NoteMeta.Frontmatter(n.Body),@"(?m)^review: true\s*$")).ToList();revisit.Children.Add(Eyebrow(flagged.Count+" MARKED FOR REVIEW"));foreach(var n in flagged){var note=n;revisit.Children.Add(MetroChoice(note.Name,note.Group,()=>NavigateTo(note)));revisit.Children.Add(Action("Mark reviewed",()=>{if(WriteReviewedNote(note,NoteMeta.Set(note.Body,"review",false)))ShowReview();}));}if(flagged.Count==0)revisit.Children.Add(Label("Mark a note for review from its menu. It will wait here until you act on it.",16,.6));
    }
}
