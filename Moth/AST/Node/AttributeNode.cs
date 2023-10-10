using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class AttributeNode : ASTNode
{
    public string Name { get; set; }
    public List<ExpressionNode> Arguments { get; set; }

    public AttributeNode(string name, List<ExpressionNode> arguments)
    {
        Name = name;
        Arguments = arguments;
    }
}
