using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class FuncDictionary : Dictionary<Signature, Function>
{
    public FuncDictionary() : base()
    {
    }
}