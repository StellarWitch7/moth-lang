namespace Moth.AST.Node;

public class CommentNode : IStatementNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Text { get; set; }
    public bool IsBlock { get; set; }

    public CommentNode(string text, bool isBlock)
    {
        Text = text;
        IsBlock = isBlock;
    }

    public bool IsMultiLine
    {
        get => Text.Contains('\n');
    }

    public bool IsDocs
    {
        get => Text.StartsWith("!DOCS\n") || Text.StartsWith(" !DOCS\n");
    }

    public string GetSource()
    {
        if (!IsBlock)
            return $"//{Text}";

        return $"/>{Text}</";
    }
}
