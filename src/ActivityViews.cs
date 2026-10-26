using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    string captureText="",captureTitle="",captureUrl=""; int captureKind;
    void ShowLegacyCapture()
    {
        if(!TrySave())return;SelectTopView("capture");workspace.Visibility=Visibility.Collapsed;if(notebookHome!=null)notebookHome.Visibility=Visibility.Collapsed;
        var page=Overlay("capture", "Save the thought now. Find its place later.");
        var panel=new StackPanel{MaxWidth=800,HorizontalAlignment=HorizontalAlignment.Stretch,Spacing=20};
        var types=new ComboBox{Header="Capture type",ItemsSource=new[]{"Quick idea","Link","Web clipping"},SelectedIndex=captureKind,HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(types);
        var destination=new ComboBox{Header="Add to",ItemsSource=new[]{"Inbox","Web clippings"},SelectedIndex=captureKind==2?1:0,HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(destination);
        var heading=new TextBox{Header="Title (optional)",Text=captureTitle,PlaceholderText="A thought worth keeping"};heading.TextChanged+=(_,_)=>captureTitle=heading.Text;panel.Children.Add(heading);
        var url=new TextBox{Header="Source address",Text=captureUrl,PlaceholderText="https://",Visibility=captureKind==0?Visibility.Collapsed:Visibility.Visible};url.TextChanged+=(_,_)=>captureUrl=url.Text;panel.Children.Add(url);
        var body=new TextBox{Header=captureKind==2?"Paste the excerpt you want to keep":"Note",Text=captureText,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=220,PlaceholderText="Jot something down…"};body.TextChanged+=(_,_)=>captureText=body.Text;panel.Children.Add(body);
        var hint=Label("Saved as Markdown in this notebook. A web clipping keeps the text you paste and its source link; it does not download the page.",12,.6);panel.Children.Add(hint);
        types.SelectionChanged+=(_,_)=>{captureKind=types.SelectedIndex;url.Visibility=captureKind==0?Visibility.Collapsed:Visibility.Visible;body.Header=captureKind==2?"Paste the excerpt you want to keep":"Note";destination.SelectedIndex=captureKind==2?1:0;};
        var feedback=new InfoBar{IsOpen=false,IsClosable=true};panel.Children.Add(feedback);
        panel.Children.Add(Action("Save capture",()=>{try{var path=StoreCapture(heading.Text,body.Text,url.Visibility==Visibility.Visible?url.Text:"",destination.SelectedIndex==1?"Web clippings":"Inbox");captureText=captureTitle=captureUrl="";body.Text=heading.Text=url.Text="";current=null;LoadVault();SelectTopView("capture");workspace.Visibility=Visibility.Collapsed;feedback.Severity=InfoBarSeverity.Success;feedback.Message="Saved to "+Path.GetRelativePath(vault,path);feedback.IsOpen=true;}catch(Exception ex){feedback.Severity=InfoBarSeverity.Error;feedback.Message=ex.Message;feedback.IsOpen=true;}}));
        var scroll=new ScrollViewer{Content=panel};Grid.SetRow(scroll,1);page.Children.Add(scroll);
    }
    string StoreCapture(string name,string body,string url,string folder)
    {
        if(string.IsNullOrWhiteSpace(body)&&string.IsNullOrWhiteSpace(url))throw new InvalidOperationException("Write a thought or add a link before saving.");
        if(url.Trim().Length>0&&(!Uri.TryCreate(url.Trim(),UriKind.Absolute,out var uri)||uri.Scheme is not ("https" or "http")))throw new InvalidOperationException("Use an http or https source address.");
        name=name.Trim();if(name.Length==0)name="Captured "+DateTime.Now.ToString("yyyy-MM-dd HHmmss");if(!ValidName(name))throw new InvalidOperationException("Choose a title without filename characters.");
        if(folder is not ("Inbox" or "Web clippings"))throw new InvalidOperationException("Choose a capture destination.");
        var directory=Path.Combine(vault,folder);Directory.CreateDirectory(directory);var path=Path.Combine(directory,name+".md");int number=2;while(File.Exists(path))path=Path.Combine(directory,name+" "+(number++)+".md");
        var markdown="# "+Path.GetFileNameWithoutExtension(path)+"\n\n"+body.Trim()+"\n";if(url.Trim().Length>0)markdown+="\n[Source]("+url.Trim().Replace(")","%29")+")\n";markdown=NoteMeta.Set(markdown,"captured",DateTimeOffset.Now.ToString("O"));using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(markdown);return path;
    }
    void ShowLegacyColumnReview()
    {
        if(!TrySave())return;SelectTopView("review");workspace.Visibility=Visibility.Collapsed;if(notebookHome!=null)notebookHome.Visibility=Visibility.Collapsed;
        var page=Overlay("review","File captured thoughts, finish tasks, and revisit what matters.");var items=new StackPanel{Spacing=24,MaxWidth=1000,HorizontalAlignment=HorizontalAlignment.Stretch};
        items.Children.Add(Label("inbox",28));var captured=all.Where(n=>n.Group is "Inbox" or "Web clippings").ToList();
        foreach(var n in captured){var note=n;var row=Row(Action(note.Name,()=>NavigateTo(note)),Action("File…",async()=>await FileCapturedNote(note)));items.Children.Add(row);}if(captured.Count==0)items.Children.Add(Label("Everything has a place. Your inbox is clear.",15,.6));
        items.Children.Add(Label("unfinished tasks",28));int count=0;
        foreach(var n in all)foreach(Match m in Regex.Matches(n.Body,@"(?m)^- \[ \] (.+)$")){var note=n;int position=m.Index;var check=new CheckBox{Content=m.Groups[1].Value+"  ·  "+n.Name};check.Checked+=(_,_)=>{if(WriteReviewedNote(note,note.Body[..(position+3)]+"x"+note.Body[(position+4)..]))ShowReview();else check.IsChecked=false;};items.Children.Add(check);count++;}
        if(count==0)items.Children.Add(Label("No unfinished Markdown tasks.",15,.6));
        items.Children.Add(Label("marked for review",28));var flagged=all.Where(n=>Regex.IsMatch(NoteMeta.Frontmatter(n.Body),@"(?m)^review: true\s*$")).ToList();
        foreach(var n in flagged){var note=n;items.Children.Add(Row(Action(note.Name,()=>NavigateTo(note)),Action("Mark reviewed",()=>{if(WriteReviewedNote(note,NoteMeta.Set(note.Body,"review",false)))ShowReview();})));}if(flagged.Count==0)items.Children.Add(Label("Use a note's menu to mark it for review.",15,.6));
        items.Width=Math.Max(260,Math.Min(1000,root.ActualWidth-96)); items.HorizontalAlignment=HorizontalAlignment.Left; var scroll=new ScrollViewer{Content=items,HorizontalContentAlignment=HorizontalAlignment.Left};Grid.SetRow(scroll,1);page.Children.Add(scroll);
    }
    bool WriteReviewedNote(Note note,string text)
    {
        if(!TrySave())return false;
        try{if(File.ReadAllText(note.Path)!=note.Body)throw new IOException("This note changed on disk. Reopen it before reviewing.");var tmp=note.Path+".acrux-review";File.WriteAllText(tmp,text);File.Move(tmp,note.Path,true);current=null;LoadVault();return true;}catch(Exception ex){status.Text=ex.Message;return false;}
    }
    async Task FileCapturedNote(Note note)
    {
        var groups=Directory.EnumerateDirectories(vault,"*",SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(vault,p)).Where(p=>p is not ("Inbox" or "Web clippings")).Prepend("Notebook root").ToList();var pick=new ComboBox{ItemsSource=groups,SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};var dialog=new ContentDialog{Title="File captured note",Content=pick,PrimaryButtonText="Move",CloseButtonText="Cancel",XamlRoot=root.XamlRoot};
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary||!TrySave())return;
        try{if(File.ReadAllText(note.Path)!=note.Body)throw new IOException("The captured note changed on disk.");var target=Path.Combine(vault,pick.SelectedIndex==0?"":groups[pick.SelectedIndex],Path.GetFileName(note.Path));if(File.Exists(target))throw new IOException("A note with that name already exists in this folder.");File.Move(note.Path,target);if(File.Exists(note.Path+".acrux.json"))File.Move(note.Path+".acrux.json",target+".acrux.json");current=null;LoadVault();ShowReview();}catch(Exception ex){status.Text=ex.Message;}
    }
}
