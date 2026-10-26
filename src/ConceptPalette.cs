using Microsoft.UI.Xaml.Media;
using Windows.UI;
namespace Acrux;
public sealed class ConceptPalette
{
    public readonly SolidColorBrush Window = new(), Panel = new(), Paper = new(), Ink = new(), Body = new(), Muted = new(), Accent = new(), Selection = new(), Line = new(), Gold = new();
    public static Color Color(string hex) { var value = Convert.ToUInt32(hex.TrimStart('#'), 16); return Windows.UI.Color.FromArgb(255, (byte)(value >> 16), (byte)(value >> 8), (byte)value); }
    public void Apply(string name)
    {
        var colors = name switch {
            "Midnight" => new[] { "171724", "1e1d2e", "242233", "e6e0fc", "e8e0d8", "aaa2ba", "aaa0ff", "393252", "373244", "c9aa79" },
            "Sage" => new[] { "f2f3ed", "eff2ea", "fbfcf5", "27443b", "33483f", "7a877a", "618d7b", "dfece1", "dce3d9", "b59a67" },
            "Blossom" => new[] { "f9f0f1", "f8eef1", "fff8f5", "583957", "584554", "97808e", "a07eac", "eee0f0", "e8dce2", "bd9c78" },
            "Monochrome" => new[] { "f1f1ef", "f4f4f1", "fcfcfa", "292933", "3d3d43", "83838a", "656575", "e6e6eb", "dedee1", "99958c" },
            _ => new[] { "f8f7f3", "f8f7f3", "fcfbf8", "22263b", "33384c", "7d7b88", "6b5bd6", "ebe7fa", "e4e1db", "c8a46e" }
        };
        var brushes = new[] { Window, Panel, Paper, Ink, Body, Muted, Accent, Selection, Line, Gold }; for (int i = 0; i < brushes.Length; i++) brushes[i].Color = Color(colors[i]);
    }
}

