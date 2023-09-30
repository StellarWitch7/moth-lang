using LLVMSharp.Interop;
using Moth.AST.Node;
using Moth.LLVM.Data;
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
    public string ModuleName { get; set; }
    public Dictionary<string, Data.Class> Classes { get; set; } = new Dictionary<string, Data.Class>();
    public Dictionary<string, Function> GlobalFunctions { get; set; } = new Dictionary<string, Function>();
    public Function CurrentFunction { get; set; }

    public CompilerContext(string moduleName)
    {
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(moduleName);
        ModuleName = moduleName;

        InsertDefaultTypes();
    }

    private void InsertDefaultTypes()
    {
        Classes.Add("void", new Data.Class(LLVMTypeRef.Void, PrivacyType.Public));
        Classes.Add("bool", new Int(LLVMTypeRef.Int1, PrivacyType.Public, false));
        Classes.Add("char", new Int(LLVMTypeRef.Int8, PrivacyType.Public, false));
        Classes.Add("u16", new Int(LLVMTypeRef.Int16, PrivacyType.Public, false));
        Classes.Add("u32", new Int(LLVMTypeRef.Int32, PrivacyType.Public, false));
        Classes.Add("u64", new Int(LLVMTypeRef.Int64, PrivacyType.Public, false));
        Classes.Add("i16", new Int(LLVMTypeRef.Int16, PrivacyType.Public, true));
        Classes.Add("i32", new Int(LLVMTypeRef.Int32, PrivacyType.Public, true));
        Classes.Add("i64", new Int(LLVMTypeRef.Int64, PrivacyType.Public, true));
        Classes.Add("f16", new Data.Class(LLVMTypeRef.Half, PrivacyType.Public));
        Classes.Add("f32", new Data.Class(LLVMTypeRef.Float, PrivacyType.Public));
        Classes.Add("f64", new Data.Class(LLVMTypeRef.Double, PrivacyType.Public));
        Classes.Add("string", new Data.Class(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), PrivacyType.Public));
    }
}
