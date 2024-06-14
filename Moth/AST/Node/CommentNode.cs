namespace Moth.AST.Node;

public class CommentNode : IStatementNode
{
    public string Text { get; set; }

    public CommentNode(string text)
    {
        Text = text;
    }

    public string GetSource() => $"//{Text}";
}
