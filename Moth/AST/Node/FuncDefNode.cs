using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class FuncDefNode : StatementNode
{
    public string Name { get; }
    public List<ParameterNode> Params { get; }
    public ScopeNode ExecutionBlock { get; }
    public PrivacyType Privacy { get; }
    public string ReturnTypeRef { get; }
    public bool IsVariadic { get; }

    public FuncDefNode(string name, PrivacyType privacyType, string returnTypeRef,
        List<ParameterNode> @params, ScopeNode executionBlock, bool isVariadic)
    {
        Name = name;
        Privacy = privacyType;
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
        IsVariadic = isVariadic;
    }
}
