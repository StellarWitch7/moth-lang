using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class FieldNode : StatementNode
{
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public TypeRefNode TypeRef { get; }
    public bool IsConstant { get; }

    public FieldNode(string name, PrivacyType privacy, TypeRefNode typeRef, bool isConstant)
    {
        Name = name;
        Privacy = privacy;
        TypeRef = typeRef;
        IsConstant = isConstant;
    }
}

public enum PrivacyType
{
    Public,
    Private,
    Local
}
