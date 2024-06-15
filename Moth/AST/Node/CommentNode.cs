namespace Moth.AST.Node;

public class CommentNode : IStatementNode
{
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
