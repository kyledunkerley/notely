using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    void ShowConnections(bool graph=true)
    {
        if(!TrySave())return;SelectTopView("connect");workspace.Visibility=Visibility.Collapsed;if(notebookHome!=null)notebookHome.Visibility=Visibility.Collapsed;if(overlayView!=null)root.Children.Remove(overlayView);
        var page=new Grid{Background=palette.Window,Padding=new Thickness(36,0,36,16),RowSpacing=12};page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});page.RowDefinitions.Add(new RowDefinition());page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});overlayView=page;Grid.SetRow(page,1);root.Children.Add(page);
        var query=new TextBox{PlaceholderText="Find a thought in the map",Width=260,HorizontalAlignment=HorizontalAlignment.Left};page.Children.Add(query);
        var scene=new Canvas{Width=1200,Height=620,Background=new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)};var viewport=new ScrollViewer{Content=scene,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,ZoomMode=ZoomMode.Enabled,MinZoomFactor=.25f,MaxZoomFactor=2f,HorizontalContentAlignment=HorizontalAlignment.Center};Grid.SetRow(viewport,1);page.Children.Add(viewport);
        var summary=Eyebrow("");var commands=Row(summary,Action("−",()=>viewport.ChangeView(null,null,Math.Max(.25f,viewport.ZoomFactor-.15f))),Action("+",()=>viewport.ChangeView(null,null,Math.Min(2f,viewport.ZoomFactor+.15f))),Action("home",()=>viewport.ChangeView(0,0,1)),Action("explore notes",ShowExplore));Grid.SetRow(commands,2);page.Children.Add(commands);
        bool Links(Note a,Note b)=>Regex.Matches(a.Body,@"\[\[([^\]]+)\]\]").Any(m=>m.Groups[1].Value.Split('|')[0].Split('#')[0].Equals(b.Name,StringComparison.OrdinalIgnoreCase));
        void Draw(){scene.Children.Clear();var matching=all.Where(n=>n.Name.Contains(query.Text,StringComparison.OrdinalIgnoreCase)).ToList();var nodes=matching.Take(80).ToList();summary.Text=nodes.Count+" OF "+matching.Count+" MATCHING NOTES / SELECT A THOUGHT";var positions=nodes.Select((n,i)=>new Windows.Foundation.Point(600+Math.Cos(i*Math.PI*2/Math.Max(1,nodes.Count))*420,300+Math.Sin(i*Math.PI*2/Math.Max(1,nodes.Count))*230)).ToList();
            for(int i=0;i<nodes.Count;i++)for(int j=i+1;j<nodes.Count;j++)if(Links(nodes[i],nodes[j])||Links(nodes[j],nodes[i]))scene.Children.Add(new Line{X1=positions[i].X,Y1=positions[i].Y,X2=positions[j].X,Y2=positions[j].Y,Stroke=palette.Accent,StrokeThickness=1,Opacity=.4});
            for(int i=0;i<nodes.Count;i++){var note=nodes[i];var button=Action(note.Name,()=>{});button.FontSize=20;button.Background=palette.Window;button.Padding=new Thickness(12);button.CornerRadius=new CornerRadius(0);Canvas.SetLeft(button,positions[i].X-100);Canvas.SetTop(button,positions[i].Y-20);var content=new StackPanel{Spacing=16,Width=280};content.Children.Add(Label(note.Name,26));content.Children.Add(Label(note.Group,12,.6));var body=NoteMeta.Body(note.Body);content.Children.Add(Label(body.Length>260?body[..260]+"…":body,14,.7));var flyout=new Flyout{Content=content};content.Children.Add(Action("Open note",()=>{flyout.Hide();NavigateTo(note);}));button.Flyout=flyout;scene.Children.Add(button);}}
        query.TextChanged+=(_,_)=>Draw();Draw();
    }
}
