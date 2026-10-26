using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    bool captureDraftLoaded;
    string CaptureDraftPath=>smokeMode?Path.Combine(AppContext.BaseDirectory,"capture-draft.txt"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Southlight","Acrux","capture-draft.txt");
    void ShowCapture()
    {
        if(!TrySave())return;SelectTopView("capture");workspace.Visibility=Visibility.Collapsed;if(notebookHome!=null)notebookHome.Visibility=Visibility.Collapsed;if(overlayView!=null)root.Children.Remove(overlayView);
        var page=new Grid{Padding=new Thickness(36,12,36,24),Background=palette.Window};page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});page.RowDefinitions.Add(new RowDefinition());page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});overlayView=page;Grid.SetRow(page,1);root.Children.Add(page);
        var surface=new Grid{MaxWidth=1000,HorizontalAlignment=HorizontalAlignment.Stretch};surface.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});surface.RowDefinitions.Add(new RowDefinition());surface.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});Grid.SetRowSpan(surface,3);page.Children.Add(surface);
        var location=Row(Eyebrow(Path.GetFileName(vault).ToUpperInvariant()+" / "+(captureKind==2?"WEB CLIPPINGS":"INBOX")),Action("change",()=>{},"Capture options"));surface.Children.Add(location);
        if(location.Children[1] is Button options){var menu=new MenuFlyout();foreach(var pair in new[]{("Quick idea",0),("Link",1),("Web clipping",2)}){var kind=pair.Item2;var item=new MenuFlyoutItem{Text=pair.Item1};item.Click+=(_,_)=>{captureKind=kind;ShowCapture();};menu.Items.Add(item);}var explore=new MenuFlyoutItem{Text="Choose notebook in Explore"};explore.Click+=(_,_)=>ShowExplore();menu.Items.Add(explore);options.Flyout=menu;}
        if(!captureDraftLoaded){captureDraftLoaded=true;try{if(File.Exists(CaptureDraftPath))captureText=File.ReadAllText(CaptureDraftPath);}catch(Exception ex){status.Text="Could not restore capture draft: "+ex.Message;}}
        var input=new TextBox{AcceptsReturn=true,Text=captureText,PlaceholderText=captureKind==2?"Keep a passage. Add its source link.":captureKind==1?"Drop a link. Tell yourself why it matters.":"What's on your mind?",TextWrapping=TextWrapping.Wrap,FontSize=26,FontFamily=new FontFamily("Segoe UI"),Foreground=palette.Body,BorderThickness=new Thickness(0),Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent),Padding=new Thickness(0,36,0,24),MinHeight=220,VerticalContentAlignment=VerticalAlignment.Top};
        foreach(var key in new[]{"TextControlBackground","TextControlBackgroundFocused","TextControlBackgroundPointerOver","TextControlBorderBrush","TextControlBorderBrushFocused","TextControlBorderBrushPointerOver"})input.Resources[key]=new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        wordCount.Text=Regex.Matches(captureText,@"\b[\w'-]+\b").Count+" words / capture draft";
        input.TextChanged+=(_,_)=>{captureText=input.Text;wordCount.Text=Regex.Matches(captureText,@"\b[\w'-]+\b").Count+" words / capture draft";try{Directory.CreateDirectory(Path.GetDirectoryName(CaptureDraftPath)!);File.WriteAllText(CaptureDraftPath,captureText);status.Text="Draft saved locally / not yet filed";}catch(Exception ex){status.Text="Capture draft not saved: "+ex.Message;}};Grid.SetRow(input,1);surface.Children.Add(input);
        var bottom=new StackPanel{Spacing=12};var feedback=new InfoBar{IsOpen=lastCapture.Length>0,Message=lastCapture,Severity=InfoBarSeverity.Success,IsClosable=true};bottom.Children.Add(feedback);
        var save=Action(captureKind==2?"keep clipping":"keep in inbox",()=>{try{var name=captureText.Split('\n')[0].Trim().TrimStart('#').Trim();if(name.Length>70)name=name[..70];if(!ValidName(name))name="";var url=captureKind==0?"":Regex.Match(captureText,@"https?://[^\s<>]+",RegexOptions.IgnoreCase).Value;var path=StoreCapture(name,captureText,url,captureKind==2?"Web clippings":"Inbox");captureText=captureTitle=captureUrl="";File.WriteAllText(CaptureDraftPath,"");lastCapture="Saved locally to "+Path.GetRelativePath(vault,path);current=null;LoadVault();status.Text=lastCapture;ShowCapture();}catch(Exception ex){feedback.Severity=InfoBarSeverity.Error;feedback.Message=ex.Message;feedback.IsOpen=true;}});save.FontSize=22;save.Foreground=palette.Accent;save.HorizontalAlignment=HorizontalAlignment.Left;save.Padding=new Thickness(0,8,24,8);
        bottom.Children.Add(Row(save,Action("recent captures",ShowReview)));bottom.Children.Add(Label(captureKind==2?"Keeps the text and link you provide. No automatic page download.":"A thought now. A place for it later.",13,.55));Grid.SetRow(bottom,2);surface.Children.Add(bottom);
    }
}
