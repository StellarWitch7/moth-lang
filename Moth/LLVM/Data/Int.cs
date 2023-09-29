using LLVMSharp.Interop;
using Moth.AST.Node;
using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Class;

public class Int : Data.Class
{
    public bool IsSigned { get; set; }

    public Int(LLVMTypeRef lLVMClass, PrivacyType privacy, bool isSigned) : base(lLVMClass, privacy)
    {
        IsSigned = isSigned;
    }
}
