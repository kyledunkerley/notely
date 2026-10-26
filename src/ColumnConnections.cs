using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System.Text.RegularExpressions;
namespace Acrux;
public sealed partial class MainWindow
{
    void ShowConnections(bool graph=true)
    {
        if(!TrySave())return;var board=ActivityColumns("connect",new[]{"find notes","connections","selected note"},new[]{.8,2.2,1.0});var find=board.Columns[0];var map=board.Columns[1];var details=board.Columns[2];
        var query=new TextBox{PlaceholderText="Search notes"};find.Children.Add(query);var local=new CheckBox{Content="Selected note's neighbourhood"};find.Children.Add(local);var results=new StackPanel{Spacing=4};find.Children.Add(results);
        var counts=Eyebrow("");map.Children.Add(counts);var scene=new Canvas{Width=760,Height=600};var viewport=new ScrollViewer{Content=scene,Height=600,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,ZoomMode=ZoomMode.Enabled,MinZoomFactor=.35f,MaxZoomFactor=2.0f};map.Children.Add(Row(Action("−",()=>viewport.ChangeView(null,null,Math.Max(.35f,viewport.ZoomFactor-.15f))),Action("+",()=>viewport.ChangeView(null,null,Math.Min(2f,viewport.ZoomFactor+.15f))),Action("Reset view",()=>viewport.ChangeView(0,0,1))));map.Children.Add(viewport);
        Note? selected=all.FirstOrDefault(n=>n.Path==current?.Path)??all.FirstOrDefault();
        bool Links(Note from,Note to)=>Regex.Matches(from.Body,@"\[\[([^\]]+)\]\]").Any(m=>{var target=m.Groups[1].Value.Split('|')[0].Split('#')[0];return target.Equals(to.Name,StringComparison.OrdinalIgnoreCase)||target.Equals(System.IO.Path.GetRelativePath(vault,to.Path).Replace('\\','/')[..^3],StringComparison.OrdinalIgnoreCase);});
        void ShowDetails(){while(details.Children.Count>1)details.Children.RemoveAt(1);if(selected==null){details.Children.Add(Label("Choose a note to see its context.",16,.6));return;}var note=selected;details.Children.Add(Label(note.Name,28));details.Children.Add(Eyebrow(note.Group.Length==0?"NOTEBOOK ROOT":note.Group.ToUpperInvariant()));var excerpt=Regex.Replace(NoteMeta.Body(note.Body),@"\A# [^\n]*\n*","");details.Children.Add(Label(excerpt.Length>500?excerpt[..500]+"…":excerpt,15,.7));details.Children.Add(Action("Open note →",()=>NavigateTo(note)));details.Children.Add(Eyebrow("LINKED THOUGHTS"));var related=all.Where(n=>n.Path!=note.Path&&(Links(note,n)||Links(n,note))).ToList();foreach(var n in related){var target=n;details.Children.Add(MetroChoice(n.Name,Links(note,n)?"Outgoing link":"Backlink",()=>{selected=target;Draw();board.Reveal(2);}));}if(related.Count==0)details.Children.Add(Label("No links yet. Add [[Note title]] while writing to make a connection.",14,.6));}
        void Draw(){scene.Children.Clear();results.Children.Clear();var filtered=all.Where(n=>n.Name.Contains(query.Text,StringComparison.OrdinalIgnoreCase)).ToList();foreach(var n in filtered){var note=n;results.Children.Add(MetroChoice(n.Name,n.Group,()=>{selected=note;Draw();board.Reveal(2);},n.Path==selected?.Path));}
            var eligible=local.IsChecked==true&&selected!=null?filtered.Where(n=>n.Path==selected.Path||Links(n,selected)||Links(selected,n)).ToList():filtered;
            var nodes=eligible.Take(80).ToList();counts.Text=nodes.Count+" NODES / "+eligible.Count+" MATCHING NOTES"+(eligible.Count>80?" / GRAPH LIMITED TO 80":"");var positions=nodes.Select((n,i)=>new Windows.Foundation.Point(380+Math.Cos(i*Math.PI*2/Math.Max(1,nodes.Count))*245,290+Math.Sin(i*Math.PI*2/Math.Max(1,nodes.Count))*215)).ToList();
            for(int i=0;i<nodes.Count;i++)for(int j=i+1;j<nodes.Count;j++)if(Links(nodes[i],nodes[j])||Links(nodes[j],nodes[i]))scene.Children.Add(new Line{X1=positions[i].X,Y1=positions[i].Y,X2=positions[j].X,Y2=positions[j].Y,Stroke=palette.Accent,StrokeThickness=1.5,Opacity=.45});
            for(int i=0;i<nodes.Count;i++){var note=nodes[i];var button=Action(note.Name,()=>{selected=note;Draw();board.Reveal(2);});button.MaxWidth=200;button.Background=note.Path==selected?.Path?palette.Selection:palette.Paper;button.BorderBrush=palette.Accent;button.BorderThickness=new Thickness(note.Path==selected?.Path?2:1);button.CornerRadius=new CornerRadius(3);Canvas.SetLeft(button,positions[i].X-75);Canvas.SetTop(button,positions[i].Y-16);scene.Children.Add(button);}ShowDetails();}
        query.TextChanged+=(_,_)=>Draw();local.Checked+=(_,_)=>Draw();local.Unchecked+=(_,_)=>Draw();Draw();
    }
}
