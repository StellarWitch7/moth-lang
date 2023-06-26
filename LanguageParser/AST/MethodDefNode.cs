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

        public MethodDefNode(string name, ParameterListNode @params, StatementListNode executionStatements)
        {
            Name = name;
            Params = @params;
            ExecutionStatements = executionStatements;
        }
    }
}
