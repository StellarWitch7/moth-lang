using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class MemberDefNode : DefinitionNode
{
    public List<AttributeNode> Attributes { get; set; }

    public MemberDefNode(List<AttributeNode> attributes)
    {
        Attributes = attributes;
    }
}
