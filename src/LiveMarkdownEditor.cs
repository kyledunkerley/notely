using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System.Text.RegularExpressions;
using Windows.System;
namespace Acrux;

// One native document and selection per canvas container. Markdown remains the file format.
public sealed class LiveMarkdownEditor : StackPanel
{
    public event Action? Changed;
    public event Action<LiveMarkdownEditor>? Activated;
    public event Action<string>? OpenNote;
    public event Action? RequestLink;
    readonly RichEditBox input = new() { AcceptsReturn = true, VerticalContentAlignment = VerticalAlignment.Top, TextWrapping = TextWrapping.Wrap, BorderThickness = new Thickness(0), Padding = new Thickness(0, 4, 0, 24), Background = new SolidColorBrush(Colors.Transparent), MinHeight = 160, IsSpellCheckEnabled = true };
    readonly Stack<string> undo = new(), redo = new();
    string text = "", family = "Segoe UI"; Brush foreground = new SolidColorBrush(Colors.Black); bool updating, source;
    DateTime lastHistory;
    internal string RangeProbe => string.Join(" | ",Regex.Matches(text,@"(?m)^#{1,6} .+$").Cast<Match>().Select(m=>m.Value+" => "+input.Document.GetRange(m.Index,m.Index+m.Length).Text));
    internal void SelectRange(int start,int end)=>input.Document.Selection.SetRange(start,end);
    public string Markdown => text;
    public int BlockCount => MarkdownBlocks.Split(text).Count();
    internal bool SourceMatchesModel => Read() == text;
    public string SelectedText => input.Document.Selection.Text.Replace("\r", "\n");
    public LiveMarkdownEditor(string markdown, string directory = "")
    {
        Children.Add(input);
        var clear = new SolidColorBrush(Colors.Transparent);
        foreach (var key in new[] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused", "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused" }) input.Resources[key] = clear;
        input.TextChanged += (_, _) => { if (updating) return; var next = Read(); if (next == text) return; if ((DateTime.UtcNow-lastHistory).TotalMilliseconds > 600) { undo.Push(text); lastHistory=DateTime.UtcNow; } redo.Clear(); text=next; Restyle(); Changed?.Invoke(); };
        input.GotFocus += (_, _) => Activated?.Invoke(this);
        Shortcut(VirtualKey.B, () => Format("bold")); Shortcut(VirtualKey.I, () => Format("italic")); Shortcut(VirtualKey.K, () => RequestLink?.Invoke()); Shortcut(VirtualKey.Z, Undo); Shortcut(VirtualKey.Y, Redo);
        var commands = new TextCommandBarFlyout();
        foreach (var item in new[] { ("Bold",Symbol.Bold,"bold"), ("Italic",Symbol.Italic,"italic"), ("Link",Symbol.Link,"link"), ("To-do",Symbol.Accept,"task") }) { var command=item.Item3; var b=new AppBarButton { Label=item.Item1, Icon=new SymbolIcon(item.Item2) }; b.Click+=(_,_)=> { if(command=="link") RequestLink?.Invoke(); else Format(command); }; commands.PrimaryCommands.Add(b); }
        var follow=new AppBarButton{Label="Open linked note",Icon=new SymbolIcon(Symbol.OpenFile)};follow.Click+=(_,_)=>{var cursor=input.Document.Selection.StartPosition;var link=Regex.Matches(text,@"\[\[([^\]]+)\]\]").Cast<Match>().FirstOrDefault(m=>cursor>=m.Index&&cursor<=m.Index+m.Length);if(link!=null)OpenNote?.Invoke(link.Groups[1].Value);};commands.SecondaryCommands.Add(follow);
        input.ContextFlyout=commands;
        SetMarkdown(markdown);
    }
    string Read() { input.Document.GetText(TextGetOptions.None, out var value); value=value.Replace("\r\n","\n").Replace('\r','\n'); return value.EndsWith('\n') ? value[..^1] : value; }
    void Shortcut(VirtualKey key, Action action) { var a=new KeyboardAccelerator {Key=key,Modifiers=VirtualKeyModifiers.Control}; a.Invoked+=(_,e)=> {action();e.Handled=true;}; input.KeyboardAccelerators.Add(a); }
    public void Configure(string font, Brush ink, Brush accent, Brush selection) { family=font;foreground=ink;input.FontFamily=new FontFamily(family);input.FontSize=18;Restyle(); }
    public void SetMarkdown(string value) { updating=true; text=value.Replace("\r\n","\n").Replace('\r','\n'); input.Document.SetText(TextSetOptions.None,text); updating=false;Restyle(); }
    public void SetSourceView(bool value) {source=value;Restyle();}
    public void Commit() { }
    public bool Find(string query) {var start=Math.Clamp(input.Document.Selection.EndPosition,0,text.Length);var index=text.IndexOf(query,start,StringComparison.OrdinalIgnoreCase);if(index<0)index=text.IndexOf(query,StringComparison.OrdinalIgnoreCase);if(index<0)return false;input.Focus(FocusState.Programmatic);input.Document.Selection.SetRange(index,index+query.Length);return true;}
    public void FocusEnd() {input.Focus(FocusState.Programmatic);input.Document.Selection.SetRange(text.Length,text.Length);}
    void Replace(int start,int length,string value)
    {
        undo.Push(text);redo.Clear();var next=text[..start]+value+text[(start+length)..];SetMarkdown(next);input.Document.Selection.SetRange(start+value.Length,start+value.Length);input.Focus(FocusState.Programmatic);Changed?.Invoke();
    }
    public void Undo() {if(undo.Count==0)return;redo.Push(text);SetMarkdown(undo.Pop());Changed?.Invoke();}
    public void Redo() {if(redo.Count==0)return;undo.Push(text);SetMarkdown(redo.Pop());Changed?.Invoke();}
    public void InsertMarkdown(string value) {var s=input.Document.Selection;Replace(Math.Clamp(s.StartPosition,0,text.Length),Math.Clamp(s.EndPosition-s.StartPosition,0,text.Length-Math.Clamp(s.StartPosition,0,text.Length)),value);}
    public void InsertLink(string label,string url)=>InsertMarkdown("["+label.Replace("]","\\]")+"]("+url.Replace(" ","%20").Replace(")","%29")+")");
    public void InsertBlock(string type)=>InsertMarkdown("\n\n"+MarkdownBlocks.Template(type)+"\n");
    public void SplitActive()=>InsertMarkdown("\n\n");
    public void MergePrevious() {var p=Math.Clamp(input.Document.Selection.StartPosition,0,text.Length);if(p>=2&&text[..p].EndsWith("\n\n"))Replace(p-2,2,"");}
    public void Format(string command)
    {
        var s=input.Document.Selection;int start=Math.Clamp(s.StartPosition,0,text.Length),end=Math.Clamp(s.EndPosition,start,text.Length);var selected=text[start..end];var wrap=command switch {"bold"=>"**","italic"=>"*","strike"=>"~~","code"=>"`","highlight"=>"==",_=>""};
        if(wrap.Length>0){Replace(start,end-start,wrap+(selected.Length==0?"text":selected)+wrap);return;}
        if(command is "link" or "wikilink"){InsertMarkdown(command=="wikilink"?"[["+(selected.Length==0?"Note title":selected)+"]]":"["+(selected.Length==0?"Link":selected)+"](https://example.com)");return;}
        var lineStart=text.LastIndexOf('\n',Math.Max(0,start-1));lineStart=lineStart<0?0:lineStart+1;
        var prefix=command.StartsWith("heading")?new string('#',int.Parse(command[^1..]))+" ":command switch {"bullet"=>"- ","number"=>"1. ","task"=>"- [ ] ","quote"=>"> ","paragraph"=>"",_=>null};
        if(prefix!=null){Replace(lineStart,0,prefix);return;}InsertBlock(command);
    }
    void Restyle()
    {
        updating=true;
        try {
            input.Foreground=foreground;
            var range=input.Document.GetRange(0,text.Length);var format=range.CharacterFormat;format.Name=family;format.Size=18;format.Bold=FormatEffect.Off;format.Italic=FormatEffect.Off;if(foreground is SolidColorBrush brush)format.ForegroundColor=brush.Color;range.CharacterFormat=format;
            if(!source)foreach(Match h in Regex.Matches(text,@"(?m)^(#{1,6}) .+$")){var r=input.Document.GetRange(h.Index,h.Index+h.Length);var f=r.CharacterFormat;f.Size=32-h.Groups[1].Length*2;f.Bold=FormatEffect.On;r.CharacterFormat=f;}
            if(!source)foreach(Match b in Regex.Matches(text,@"\*\*([^\n]+?)\*\*")){var r=input.Document.GetRange(b.Index,b.Index+b.Length);var f=r.CharacterFormat;f.Bold=FormatEffect.On;r.CharacterFormat=f;}
        } finally {updating=false;}
    }
}
