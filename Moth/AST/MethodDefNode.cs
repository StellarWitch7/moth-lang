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
    public ParameterListNode Params { get; }
    public StatementListNode ExecutionStatements { get; }
    public PrivacyType Privacy { get; }
    public DefinitionType ReturnType { get; }

    public MethodDefNode(string name, PrivacyType privacyType, DefinitionType returnType,
        ParameterListNode @params, StatementListNode executionStatements)
    {
        Name = name;
        Privacy = privacyType;
        ReturnType = returnType;
        Params = @params;
        ExecutionStatements = executionStatements;
    }
}
