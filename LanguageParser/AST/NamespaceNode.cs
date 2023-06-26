﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class NamespaceNode : ASTNode
    {
        public List<string> Namespace { get; }

        public NamespaceNode(List<string> @namespace)
        {
            Namespace = @namespace;
        }
    }
}