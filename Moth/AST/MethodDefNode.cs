using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class MethodDefNode : StatementNode
{
    public string Name { get; }
    public List<ParameterNode> Params { get; }
    public ScopeNode ExecutionBlock { get; }
    public PrivacyType Privacy { get; }
    public DefinitionType ReturnType { get; }
    public ClassRefNode? ReturnObject { get; }

    public MethodDefNode(string name, PrivacyType privacyType, DefinitionType returnType,
        List<ParameterNode> @params, ScopeNode executionBlock, ClassRefNode? returnObject = null)
    {
        Name = name;
        Privacy = privacyType;
        ReturnType = returnType;
        Params = @params;
        ExecutionBlock = executionBlock;
        ReturnObject = returnObject;
    }
}
