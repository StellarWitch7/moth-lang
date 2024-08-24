﻿namespace Moth.AST.Node;

public class FieldDefNode : DefinitionNode, IMemberDeclNode
{
    public ITypeRefNode TypeRef { get; set; }

    public FieldDefNode(
        string name,
        PrivacyType privacy,
        ITypeRefNode typeRef,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        TypeRef = typeRef;
    }

    public override void GetSource(StringBuilder builder)
    {
        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        builder.Append($"{Name} {TypeRef.GetSource()};");
    }
}
