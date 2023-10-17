using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class GenericClassNode : ClassNode
{
    public List<GenericParameterNode> Params { get; set; }

    public GenericClassNode(string name, PrivacyType privacy, List<GenericParameterNode> @params, ScopeNode scope)
        : base(name, privacy, scope)
    {
        Params = @params;
    }
}
