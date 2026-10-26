using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Acrux;
public sealed partial class MainWindow
{
    readonly Dictionary<string,int> activityColumnSelection = new();
    TextBlock ColumnHeading(string text) {var label=Label(text,34);label.FontWeight=Microsoft.UI.Text.FontWeights.Light;return label;}
    TextBlock Eyebrow(string text) {var label=Label(text,11,.6);label.CharacterSpacing=100;return label;}
    sealed record ActivityLayout(Grid Page,StackPanel[] Columns,Action<int> Reveal);
    ActivityLayout ActivityColumns(string view,string[] names,double[] weights)
    {
        SelectTopView(view);workspace.Visibility=Visibility.Collapsed;if(notebookHome!=null)notebookHome.Visibility=Visibility.Collapsed;if(overlayView!=null)root.Children.Remove(overlayView);
        var page=new Grid{Background=palette.Window,Padding=new Thickness(28,24,28,20),RowSpacing=20};page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});page.RowDefinitions.Add(new RowDefinition());overlayView=page;Grid.SetRow(page,1);root.Children.Add(page);
        var selector=new ComboBox{ItemsSource=names,SelectedIndex=activityColumnSelection.GetValueOrDefault(view,view=="capture"?1:0),Header="View",HorizontalAlignment=HorizontalAlignment.Stretch,Visibility=Visibility.Collapsed};page.Children.Add(selector);
        var board=new Grid{ColumnSpacing=32};Grid.SetRow(board,1);page.Children.Add(board);var panels=new StackPanel[names.Length];var scrolls=new ScrollViewer[names.Length];
        for(int i=0;i<names.Length;i++){board.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(weights[i],GridUnitType.Star)});panels[i]=new StackPanel{Spacing=18};panels[i].Children.Add(ColumnHeading(names[i]));scrolls[i]=new ScrollViewer{Content=panels[i],HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalContentAlignment=HorizontalAlignment.Stretch};Grid.SetColumn(scrolls[i],i);board.Children.Add(scrolls[i]);}
        void Adapt(){bool narrow=page.ActualWidth<1050;selector.Visibility=narrow?Visibility.Visible:Visibility.Collapsed;board.ColumnSpacing=narrow?0:32;for(int i=0;i<panels.Length;i++){bool visible=!narrow||selector.SelectedIndex==i;scrolls[i].Visibility=visible?Visibility.Visible:Visibility.Collapsed;board.ColumnDefinitions[i].Width=new GridLength(visible?weights[i]:0,GridUnitType.Star);}}
        selector.SelectionChanged+=(_,_)=>{activityColumnSelection[view]=selector.SelectedIndex;Adapt();};page.SizeChanged+=(_,_)=>Adapt();return new ActivityLayout(page,panels,i=>{if(page.ActualWidth<1050)selector.SelectedIndex=i;});
    }
    Button MetroChoice(string name,string detail,Action action,bool selected=false)
    {
        var content=new StackPanel{Spacing=5};content.Children.Add(Label(name,21));if(detail.Length>0)content.Children.Add(Label(detail,12,.6));var button=Action("",action);button.Content=content;button.Padding=new Thickness(12,12,8,12);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.BorderThickness=new Thickness(selected?3:0,0,0,0);button.BorderBrush=palette.Accent;button.CornerRadius=new CornerRadius(0);button.Background=selected?palette.Selection:palette.Window;return button;
    }
}
