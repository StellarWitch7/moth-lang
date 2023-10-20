using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class GenericClass : Class
{
    public Dictionary<string, Type> TypeParams { get; set; } = new Dictionary<string, Type>();

    public GenericClass(string name, Type type, PrivacyType privacy) : base(name, type, privacy)
    {
    }
}
