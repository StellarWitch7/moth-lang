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

    public Signature(string name, TypeRefNode[] @params)
    {
        Name = name;
        Params = @params;
    }
}
