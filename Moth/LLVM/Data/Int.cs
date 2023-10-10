﻿using LLVMSharp.Interop;
using Moth.AST.Node;
using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Int : Class
{
    public bool IsSigned { get; set; }

    public Int(string name, LLVMTypeRef lLVMClass, PrivacyType privacy, bool isSigned)
        : base(name, lLVMClass, privacy)
    {
        IsSigned = isSigned;
    }
}