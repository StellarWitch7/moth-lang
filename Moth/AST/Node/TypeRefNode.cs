using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public DefinitionType Type;

    public TypeRefNode(DefinitionType type, string name = "") : base(name)
    {
        Type = type;

        if (type == DefinitionType.UnknownObject)
        {
            if (name == "" || name == null)
            {
                throw new Exception("Unknown type and no reference string given.");
            }
        }
    }
}
