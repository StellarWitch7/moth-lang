using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class CompilerContext
{
    public LLVMContextRef Context { get; set; }
    public LLVMBuilderRef Builder { get; set; }
    public LLVMModuleRef Module { get; set; }
    public Dictionary<string, Class> Classes { get; set; } = new Dictionary<string, Class>();
    public Dictionary<string, Function> GlobalFunctions { get; set; } = new Dictionary<string, Function>();
    public Class CurrentClass { get; set; }
    public Function CurrentFunction { get; set; }

    public CompilerContext(string moduleName)
    {
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(moduleName);

        InsertDefaultTypes();
    }

    private void InsertDefaultTypes()
    {
        Classes.Add("void", new Class(LLVMTypeRef.Void, PrivacyType.Public));
        Classes.Add("bool", new Class(LLVMTypeRef.Int1, PrivacyType.Public));
        Classes.Add("i32", new Class(LLVMTypeRef.Int32, PrivacyType.Public));
        Classes.Add("f32", new Class(LLVMTypeRef.Float, PrivacyType.Public));
        Classes.Add("string", new Class(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), PrivacyType.Public));
    }
}
