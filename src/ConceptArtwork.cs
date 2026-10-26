using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Markup;
namespace Acrux;
public static class ConceptArtwork
{
    public static UIElement Landscape(int variant = 0, bool portrait = false)
    {
        var asset = portrait ? new[] { "cover-forest.png", "cover-dawn.png", "cover-lake.png", "cover-blossom.png" }[Math.Abs(variant) % 4] : "paper-landscape.png";
        if (System.IO.File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", asset))) return AssetScene(asset, variant, portrait);
        var stage = new Canvas { Width = 500, Height = portrait ? 640 : 190, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 500, portrait ? 640 : 190) } };
        var palettes = new[] { new[] { "253f41", "345b58", "54786c", "c8b989" }, new[] { "e9d4af", "b9956f", "d3b68b", "bc8c4e" }, new[] { "b8c9de", "5f7f9d", "8faac5", "e1e4e8" }, new[] { "e8ccda", "a191b3", "b2a1bf", "f3e1cc" } };
        var colors = palettes[Math.Abs(variant) % palettes.Length];
        if (portrait) stage.Children.Add(new Rectangle { Width = 500, Height = 640, Fill = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(0, 1), GradientStops = { new GradientStop { Color = ConceptPalette.Color(colors[0]), Offset = 0 }, new GradientStop { Color = ConceptPalette.Color(colors[2]), Offset = 1 } } } });
        var moon = new Ellipse { Width = portrait ? 74 : 68, Height = portrait ? 74 : 68, Fill = new SolidColorBrush(ConceptPalette.Color(colors[3])), Opacity = .8 }; Canvas.SetLeft(moon, portrait ? 350 : 120); Canvas.SetTop(moon, portrait ? 75 : 15); stage.Children.Add(moon);
        var arc = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='M20,165 C100,-50 440,-70 480,165' Stroke='#c8b38e' StrokeThickness='0.7' Opacity='0.5'/>"); stage.Children.Add(arc);
        double y = portrait ? 250 : 85;
        AddMountain("M0,110 L70,96 130,50 160,70 225,10 280,50 325,34 390,86 450,63 500,108 500,160 0,160 Z", colors[2], y, .8);
        AddMountain("M0,128 L55,100 130,118 225,45 270,89 315,75 380,130 425,90 500,132 500,190 0,190 Z", colors[1], y + 12, .9);
        if (portrait) { AddMountain("M0,100 L80,48 170,85 260,35 340,85 420,55 500,95 500,350 0,350 Z", colors[0], 380, .92); }
        else AddMountain("M0,22 L100,13 180,26 270,12 330,24 420,10 500,25 500,34 0,34 Z", colors[1], 147, .5);
        var random = new Random(11 + variant); for (int i = 0; i < (portrait ? 48 : 25); i++) { var dot = new Ellipse { Width = i % 7 == 0 ? 3 : 1.2, Height = i % 7 == 0 ? 3 : 1.2, Fill = new SolidColorBrush(ConceptPalette.Color(portrait ? "e4dfc7" : "c2aa7c")), Opacity = .5 }; Canvas.SetLeft(dot, random.Next(15, 485)); Canvas.SetTop(dot, random.Next(5, portrait ? 330 : 160)); stage.Children.Add(dot); }
        foreach (var pos in new[] { (365d, 55d), (430d, 27d), (300d, 85d) }) { var star = Star(portrait ? "dedec0" : "7770bd", 24); Canvas.SetLeft(star, pos.Item1); Canvas.SetTop(star, pos.Item2); stage.Children.Add(star); }
        return new Viewbox { Child = stage, Stretch = Stretch.UniformToFill };
        void AddMountain(string geometry, string color, double top, double opacity) { var path = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + geometry + "' Fill='#" + color + "'/>"); Canvas.SetTop(path, top); path.Opacity = opacity; stage.Children.Add(path); }
    }
    public static UIElement SideLandscape() { var image = new Image { Stretch = Stretch.UniformToFill }; LoadAsset(image, "landing-landscape.png"); return image; }
    static UIElement AssetScene(string asset, int variant, bool portrait)
    {
        var scene = new Grid { Width = 500, Height = portrait ? 640 : 235, Background = portrait ? new SolidColorBrush(ConceptPalette.Color(new[] { "1e3b36", "ead8b9", "527f9d", "d5c4d2" }[Math.Abs(variant) % 4])) : new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        var picture = new Image { Stretch = Stretch.UniformToFill, VerticalAlignment = VerticalAlignment.Top, Height = portrait ? 375 : 235 }; scene.Children.Add(picture); LoadAsset(picture, asset); return new Viewbox { Child = scene, Stretch = Stretch.Uniform };
    }
    static async void LoadAsset(Image image, string name)
    {
        try { var bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", name)); using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0))) { writer.WriteBytes(bytes); await writer.StoreAsync(); } stream.Seek(0); var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(); await bitmap.SetSourceAsync(stream); image.Source = bitmap; } catch { }
    }
    public static UIElement Star(string color, double size) => new Viewbox { Width = size, Height = size, Child = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='M20,0 L24,15 40,20 24,24 20,40 16,24 0,20 16,15 Z M20,7 L18,18 7,20 18,22 20,33 22,22 33,20 22,18 Z' Fill='#" + color + "'/>") };
}




