using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class LocalFuncDefNode : IExpressionNode
{
    public ITypeRefNode ReturnTypeRef { get; set; }
    public List<ParameterNode> Params { get; set; }
    public ScopeNode ExecutionBlock { get; set; }

    public LocalFuncDefNode(
        ITypeRefNode returnTypeRef,
        List<ParameterNode> @params,
        ScopeNode executionBlock
    )
    {
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
