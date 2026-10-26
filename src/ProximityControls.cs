using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
namespace Acrux;
public sealed partial class MainWindow
{
    FrameworkElement? writingTools;
    bool touchControls; int openCommandMenus;
    readonly Dictionary<FrameworkElement,bool> revealStates=new();
    bool HasFocus(FrameworkElement surface)
    {
        if(root.XamlRoot==null || !root.IsLoaded)return false;
        var focused=FocusManager.GetFocusedElement(root.XamlRoot) as DependencyObject;
        while(focused!=null){if(ReferenceEquals(focused,surface))return true;focused=VisualTreeHelper.GetParent(focused);}return false;
    }
    internal static bool PointerNear(double x,double y,double left,double top,double width,double height,double radius=80)=>x>=left-radius&&x<=left+width+radius&&y>=top-radius&&y<=top+height+radius;
    void RevealCommands(FrameworkElement surface,bool visible)
    {
        visible|=touchControls||openCommandMenus>0||(surface is CommandBar bar && bar.IsOpen)||HasFocus(surface)||notebookHome?.Visibility==Visibility.Visible;
        if(revealStates.TryGetValue(surface,out var old)&&old==visible)return;revealStates[surface]=visible;
        surface.IsHitTestVisible=visible;
        var target=visible?1:.12;
        if(!surface.IsLoaded || !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled){surface.Opacity=target;return;}
        var animation=new DoubleAnimation{To=target,Duration=TimeSpan.FromMilliseconds(visible?140:280),EnableDependentAnimation=true};Storyboard.SetTarget(animation,surface);Storyboard.SetTargetProperty(animation,"Opacity");var storyboard=new Storyboard();storyboard.Children.Add(animation);storyboard.Begin();
    }
    void WireProximityControls()
    {
        if(writingTools is CommandBar commandBar)foreach(var button in commandBar.PrimaryCommands.Concat(commandBar.SecondaryCommands).OfType<AppBarButton>())if(button.Flyout is {} flyout){flyout.Opened+=(_,_)=>{openCommandMenus++;RevealCommands(commandBar,true);};flyout.Closed+=(_,_)=>{openCommandMenus=Math.Max(0,openCommandMenus-1);RevealCommands(commandBar,false);};}
        root.PointerMoved+=(_,e)=>{touchControls=e.Pointer.PointerDeviceType.ToString()!="Mouse";var p=e.GetCurrentPoint(root).Position;foreach(var surface in new[]{modernCommands,writingTools})if(surface!=null&&surface.IsLoaded){var origin=surface.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0,0));RevealCommands(surface,PointerNear(p.X,p.Y,origin.X,origin.Y,surface.ActualWidth,surface.ActualHeight));}};
        root.PointerExited+=(_,_)=>{foreach(var surface in new[]{modernCommands,writingTools})if(surface!=null)RevealCommands(surface,false);};
        root.PointerPressed+=(_,e)=>{if(e.Pointer.PointerDeviceType.ToString()!="Mouse"){touchControls=true;foreach(var surface in new[]{modernCommands,writingTools})if(surface!=null)RevealCommands(surface,true);}};
        root.AddHandler(UIElement.KeyDownEvent,new KeyEventHandler((_,e)=>{if(e.Key is not (Windows.System.VirtualKey.Tab or Windows.System.VirtualKey.Menu or Windows.System.VirtualKey.F10))return;touchControls=true;foreach(var surface in new[]{modernCommands,writingTools})if(surface!=null)RevealCommands(surface,true);}),true);
        foreach(var surface in new[]{modernCommands,writingTools})if(surface!=null){surface.GotFocus+=(_,_)=>RevealCommands(surface,true);surface.LostFocus+=(_,_)=>root.DispatcherQueue.TryEnqueue(()=>RevealCommands(surface,false));}
    }
}
