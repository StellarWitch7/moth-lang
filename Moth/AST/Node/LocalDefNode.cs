using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class LocalDefNode : FieldDefNode
{
    public ExpressionNode DefaultValue { get; set; }

    public LocalDefNode(string name, PrivacyType privacy, TypeRefNode typeRef, ExpressionNode defaultValue)
        : base(name, privacy, typeRef)
    {
        DefaultValue = defaultValue;
    }
}
