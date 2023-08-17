﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class FieldNode : StatementNode
{
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public DefinitionType Type { get; }
    public bool IsConstant { get; }
    public ClassRefNode? TypeObject { get; }

    public FieldNode(string name, PrivacyType privacy, DefinitionType type, bool isConstant, ClassRefNode? typeObject = null)
    {
        Name = name;
        Privacy = privacy;
        Type = type;
        IsConstant = isConstant;
        TypeObject = typeObject;
    }
}

public enum PrivacyType
{
Public,
Private,
Local
}

public enum DefinitionType
{
Bool,
Int32,
Float32,
String,
Matrix,
ClassObject,
Void
}