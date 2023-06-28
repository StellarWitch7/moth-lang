using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class MethodDefNode : StatementNode
    {
        public string Name { get; }
        public ParameterListNode Params { get; }
        public StatementListNode ExecutionStatements { get; }
        public PrivacyType PrivacyType { get; }
        public DefinitionType ReturnType { get; }

        public MethodDefNode(string name, PrivacyType privacyType, DefinitionType returnType,
            ParameterListNode @params, StatementListNode executionStatements)
        {
            Name = name;
            PrivacyType = privacyType;
            ReturnType = returnType;
            Params = @params;
            ExecutionStatements = executionStatements;
        }
    }
}
