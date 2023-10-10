using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class FieldDefNode : MemberDefNode
{
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public TypeRefNode TypeRef { get; }

    public FieldDefNode(string name, PrivacyType privacy, TypeRefNode typeRef, List<AttributeNode> attributes)
        : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        TypeRef = typeRef;
    }
}