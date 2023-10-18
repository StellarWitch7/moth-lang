using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Signature
{
    public string Name { get; set; }
    public TypeRefNode[] Params { get; set; }
    public bool IsVariadic { get; set; }

    public Signature(string name, TypeRefNode[] @params, bool isVariadic = false)
    {
        Name = name;
        Params = @params;
        IsVariadic = isVariadic;
    }
}
