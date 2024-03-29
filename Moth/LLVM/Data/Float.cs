﻿using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : PrimitiveType
{
    public Float(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }
}
