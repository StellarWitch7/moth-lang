using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ClassNode : ASTNode
{
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public ScopeNode Scope { get; }

    public ClassNode(string name, PrivacyType privacy, ScopeNode scope)
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
    }
}