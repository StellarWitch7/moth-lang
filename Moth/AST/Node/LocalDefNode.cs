using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class LocalDefNode : StatementNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode TypeRef { get; set; }
    public ExpressionNode DefaultValue { get; set; }


    public LocalDefNode(string name, PrivacyType privacy, TypeRefNode typeRef, ExpressionNode defaultValue = null)
    {
        Name = name;
        Privacy = privacy;
        TypeRef = typeRef;
        DefaultValue = defaultValue;
    }
}
