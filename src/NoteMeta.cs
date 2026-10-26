using System.Text.Json;
using System.Text.RegularExpressions;
namespace Acrux;
public static class NoteMeta
{
    public sealed record Source(string Title, string Url, string Detail = "");
    static Match Front(string text) => Regex.Match(text, @"\A---\r?\n(?<data>[\s\S]*?)\r?\n---(?:\r?\n)*");
    public static string Frontmatter(string text) => Front(text).Success ? Front(text).Value : "";
    public static string Body(string text) => text[Frontmatter(text).Length..];
    public static List<string> Tags(string text)
    {
        var match = Regex.Match(Frontmatter(text), @"(?m)^tags:\s*(\[[^\r\n]*\])");
        try { if (match.Success) return JsonSerializer.Deserialize<List<string>>(match.Groups[1].Value) ?? new(); } catch { }
        return match.Success ? match.Groups[1].Value.Trim('[', ']').Split(',').Select(t => t.Trim().Trim('\'', '"')).Where(t => t.Length > 0).ToList() : new();
    }
    public static List<Source> Sources(string text)
    {
        var match = Regex.Match(Frontmatter(text), @"(?m)^source_links:\s*(\[[^\r\n]*\])");
        try { return match.Success ? JsonSerializer.Deserialize<List<Source>>(match.Groups[1].Value) ?? new() : new(); } catch { return new(); }
    }
    public static string Set(string text, string key, object value)
    {
        var front = Front(text); var line = key + ": " + JsonSerializer.Serialize(value);
        if (!front.Success) return "---\n" + line + "\n---\n\n" + text;
        var data = front.Groups["data"].Value; var pattern = @"(?m)^" + Regex.Escape(key) + @":[^\r\n]*";
        data = Regex.IsMatch(data, pattern) ? Regex.Replace(data, pattern, _ => line) : data.TrimEnd() + "\n" + line;
        return "---\n" + data + "\n---\n\n" + text[front.Length..];
    }
    public static string HeaderPrefix(string text, string filename)
    {
        var prefix = Frontmatter(text); var rest = text[prefix.Length..]; var title = Regex.Match(rest, @"\A# ([^\r\n]+)\r?\n(?:\r?\n)?");
        if (title.Success && title.Groups[1].Value.Equals(filename, StringComparison.OrdinalIgnoreCase)) prefix += title.Value;
        var remaining = text[prefix.Length..]; var summary = Regex.Match(remaining, @"\A([^\r\n]+)\r?\n\r?\n");
        if (summary.Success && summary.Groups[1].Length < 150 && !Regex.IsMatch(summary.Groups[1].Value, @"^[#>|`~*\-\[]")) prefix += summary.Value;
        return prefix;
    }
    public static string Subtitle(string text)
    {
        var body = Body(text); body = Regex.Replace(body, @"\A# [^\r\n]+\r?\n(?:\r?\n)?", ""); var first = body.Split('\n')[0].Trim();
        return first.Length is > 0 and < 150 && !Regex.IsMatch(first, @"^[#>|`~*\-\[]") ? first : "A little room for your thoughts.";
    }
    public static string WithSubtitle(string text, string filename, string subtitle)
    {
        var front = Frontmatter(text); var body = text[front.Length..]; var title = Regex.Match(body, @"\A# [^\r\n]+\r?\n(?:\r?\n)?"); var headerLength = front.Length + (title.Success ? title.Length : 0); var prefix = HeaderPrefix(text, filename);
        var bodyStart = Math.Max(headerLength, prefix.Length); return text[..headerLength] + (subtitle.Trim().Length > 0 ? subtitle.Trim() + "\n\n" : "") + text[bodyStart..];
    }
}
