﻿namespace Moth.AST.Node;

public class FuncCallNode : IExpressionNode
{
    public string Name { get; set; }
    public List<IExpressionNode> Arguments { get; set; }
    public IExpressionNode? ToCallOn { get; set; }

    public FuncCallNode(string name, List<IExpressionNode> args, IExpressionNode? callOn)
    {
        Name = name;
        Arguments = args;
        ToCallOn = callOn;
    }

    public string GetSource()
    {
        string call =
            $"{Name}({String.Join(", ", Arguments.ToArray().ExecuteOverAll(e => {
            return e.GetSource();
        }))})";

        if (ToCallOn == null)
            return call;

        return $"{ToCallOn.GetSource()}.{call}";
    }
}
