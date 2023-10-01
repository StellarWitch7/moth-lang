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
    public Logger Logger { get; } = new Logger("moth/compiler");
    public Dictionary<string, Class> Classes { get; set; } = new Dictionary<string, Class>();
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
        Classes.Add(Primitive.Void, new Class(this, Primitive.Void, LLVMTypeRef.Void, PrivacyType.Public));
        Classes.Add(Primitive.String, new Class(this, Primitive.String, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), PrivacyType.Public));
        Classes.Add(Primitive.Float16, new Class(this, Primitive.Float16, LLVMTypeRef.Half, PrivacyType.Public));
        Classes.Add(Primitive.Float32, new Class(this, Primitive.Float32, LLVMTypeRef.Float, PrivacyType.Public));
        Classes.Add(Primitive.Float64, new Class(this, Primitive.Float64, LLVMTypeRef.Double, PrivacyType.Public));
        Classes.Add(Primitive.Bool, new Int(this, Primitive.Bool, LLVMTypeRef.Int1, PrivacyType.Public, false));
        Classes.Add(Primitive.Char, new Int(this, Primitive.Char, LLVMTypeRef.Int8, PrivacyType.Public, false));
        Classes.Add(Primitive.UnsignedInt16, new Int(this, Primitive.UnsignedInt16, LLVMTypeRef.Int16, PrivacyType.Public, false));
        Classes.Add(Primitive.UnsignedInt32, new Int(this, Primitive.UnsignedInt32, LLVMTypeRef.Int32, PrivacyType.Public, false));
        Classes.Add(Primitive.UnsignedInt64, new Int(this, Primitive.UnsignedInt64, LLVMTypeRef.Int64, PrivacyType.Public, false));
        Classes.Add(Primitive.SignedInt16, new Int(this, Primitive.SignedInt16, LLVMTypeRef.Int16, PrivacyType.Public, true));
        Classes.Add(Primitive.SignedInt32, new Int(this, Primitive.SignedInt32, LLVMTypeRef.Int32, PrivacyType.Public, true));
        Classes.Add(Primitive.SignedInt64, new Int(this, Primitive.SignedInt64, LLVMTypeRef.Int64, PrivacyType.Public, true));

        foreach (Class @class in Classes.Values)
        {
            @class.AddBuiltins(this);
        }
    }

    public void Warn(string message)
    {
        Log($"Warning: {message}");
    }

    public void Log(string message)
    {
        Logger.WriteLine(message);
    }
}
