using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class InferredLocalDefNode : FieldNode
{
    public ExpressionNode DefaultValue { get; set; }

    public InferredLocalDefNode(string name, PrivacyType privacy, bool isConstant, ExpressionNode defaultVal)
        : base(name, privacy, string.Empty, isConstant)
    {
        DefaultValue = defaultVal;
    }
}
