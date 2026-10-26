using System.Text.RegularExpressions;
namespace Acrux;

public static class MarkdownBlocks
{
    // Blank lines inside fenced code are content, not block boundaries.
    public static List<string> Split(string markdown)
    {
        var blocks = new List<string>(); var buffer = new System.Text.StringBuilder(); char fence = '\0'; int fenceSize = 0;
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i]; var match = Regex.Match(line, @"^\s{0,3}(`{3,}|~{3,})");
            if (match.Success)
            {
                var mark = match.Groups[1].Value;
                if (fence == '\0') { fence = mark[0]; fenceSize = mark.Length; }
                else if (mark[0] == fence && mark.Length >= fenceSize) fence = '\0';
            }
            buffer.Append(line); if (i < lines.Length - 1) buffer.Append('\n');
            if (fence == '\0' && buffer.Length >= 2 && buffer[^1] == '\n' && buffer[^2] == '\n') { blocks.Add(buffer.ToString(0, buffer.Length - 2)); buffer.Clear(); }
        }
        blocks.Add(buffer.ToString());
        return blocks;
    }
    public static string Join(IEnumerable<string> blocks) => string.Join("\n\n", blocks);
    public static string Template(string type) => type switch
    {
        "heading1" => "# Heading", "heading2" => "## Heading", "heading3" => "### Heading",
        "bullet" => "- List item", "number" => "1. List item", "task" => "- [ ] To do",
        "quote" => "> A thought worth keeping", "callout" => "> [!note]\n> A little context.",
        "codeblock" => "```text\nYour code here\n```", "divider" => "---",
        "table" => "| Column one | Column two |\n| --- | --- |\n|  |  |", "link" => "[Link text](https://example.com)",
        "wikilink" => "[[Note title]]", "date" => DateTime.Now.ToString("yyyy-MM-dd"), _ => ""
    };
    public static string Prefix(string text, string prefix)
    {
        var clean = Regex.Replace(text, @"^(?:#{1,6}\s+|[-*+]\s+(?:\[[ xX]\]\s+)?|\d+\.\s+|>\s*)", "", RegexOptions.Multiline);
        return string.Join("\n", clean.Split('\n').Select(line => prefix + line));
    }
}

