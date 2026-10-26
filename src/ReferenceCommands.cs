using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Acrux;
public sealed partial class MainWindow
{
    void AttachReferenceMenu(Button button,NoteMeta.Source source)
    {
        var menu=new MenuFlyout();
        void Item(string name,Action run){var i=new MenuFlyoutItem{Text=name};i.Click+=(_,_)=>run();menu.Items.Add(i);}
        Item("Edit reference",async()=>await EditReference(source));
        if(source.Url.Length>0)Item("Insert link at cursor",()=>canvas.InsertLink(source.Title,source.Url));
        Item("Remove reference",()=>{var list=NoteMeta.Sources(draft);list.Remove(source);CommitMetadata(NoteMeta.Set(draft,"source_links",list));});button.ContextFlyout=menu;ToolTipService.SetToolTip(button,source.Title+" — right-click to edit, insert or remove");
    }
    async Task EditReference(NoteMeta.Source source)
    {
        if(current==null)return;var notePath=current.Path;var name=new TextBox{Header="Title",Text=source.Title};var address=new TextBox{Header="Address (optional)",Text=source.Url};var detail=new TextBox{Header="Author or description",Text=source.Detail};var dialog=new ContentDialog{Title="Edit reference",Content=new StackPanel{Spacing=12,Children={name,address,detail}},PrimaryButtonText="Save",CloseButtonText="Cancel",XamlRoot=root.XamlRoot};
        dialog.PrimaryButtonClick+=(_,e)=>{if(name.Text.Trim().Length==0||(address.Text.Length>0&&(!Uri.TryCreate(address.Text,UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https")))){e.Cancel=true;address.Header="Use an http or https address, or leave blank";}};
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary||current?.Path!=notePath)return;var list=NoteMeta.Sources(draft);var index=list.IndexOf(source);if(index<0)return;list[index]=new NoteMeta.Source(name.Text.Trim(),address.Text.Trim(),detail.Text.Trim());CommitMetadata(NoteMeta.Set(draft,"source_links",list));
    }
}
