using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class LocalFuncDefNode : ExpressionNode
{
    public TypeRefNode ReturnTypeRef { get; set; }
    public List<ParameterNode> Params { get; set; }
    public ScopeNode ExecutionBlock { get; set; }

    public LocalFuncDefNode(TypeRefNode returnTypeRef, List<ParameterNode> @params, ScopeNode executionBlock)
    {
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
    }
}
