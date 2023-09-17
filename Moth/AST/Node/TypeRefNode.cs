using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public DefinitionType Type;

    public TypeRefNode(DefinitionType type, string name = null) : base(null)
    {
        Type = type;

        if (type == DefinitionType.ClassObject)
        {
            if (name == null)
            {
                throw new Exception("Unknown type and no reference string given.");
            }

            base.Name = name;
        }
    }
}
