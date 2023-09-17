using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class MethodDefNode : StatementNode
{
    public string Name { get; }
    public List<ParameterNode> Params { get; }
    public ScopeNode ExecutionBlock { get; }
    public PrivacyType Privacy { get; }
    public TypeRefNode ReturnTypeRef { get; }

    public MethodDefNode(string name, PrivacyType privacyType, TypeRefNode returnTypeRef,
        List<ParameterNode> @params, ScopeNode executionBlock)
    {
        Name = name;
        Privacy = privacyType;
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
    }
}
