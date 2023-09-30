using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ForeignFuncDefNode : FuncDefNode
{
    public ForeignFuncDefNode(string name, PrivacyType privacyType, TypeRefNode returnTypeRef,
        List<ParameterNode> @params, bool isVariadic)
        : base(name, privacyType, returnTypeRef, @params, null, isVariadic)
    {
    }
}
