﻿namespace Moth.AST.Node;

public class TemplateNode : StructNode
{
    public List<TemplateParameterNode> Params { get; set; }

    public TemplateNode(string name, PrivacyType privacy, List<TemplateParameterNode> @params, ScopeNode scope)
        : base(name, privacy, scope) => Params = @params;
}
