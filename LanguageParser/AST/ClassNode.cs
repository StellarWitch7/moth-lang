using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST;

public class ClassNode : ASTNode
{
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public StatementListNode StatementListNode { get; }

    public ClassNode(string name, PrivacyType privacy, StatementListNode statementListNode)
    {
        Name = name;
        Privacy = privacy;
        StatementListNode = statementListNode;
    }
}