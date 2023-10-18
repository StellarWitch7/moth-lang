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
    public FuncDictionary GlobalFunctions { get; set; } = new FuncDictionary();
    public Dictionary<string, Constant> GlobalConstants { get; set; } = new Dictionary<string, Constant>();
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
        Classes.Add(Reserved.Void,
            new Class(Reserved.Void,
                new Type(LLVMTypeRef.Void),
                PrivacyType.Public));
        Classes.Add(Reserved.Float16,
            new Class(Reserved.Float16,
                new Type(LLVMTypeRef.Half),
                PrivacyType.Public));
        Classes.Add(Reserved.Float32,
            new Class(Reserved.Float32,
                new Type(LLVMTypeRef.Float),
                PrivacyType.Public));
        Classes.Add(Reserved.Float64,
            new Class(Reserved.Float64,
                new Type(LLVMTypeRef.Double),
                PrivacyType.Public));
        Classes.Add(Reserved.Bool,
            new Int(Reserved.Bool,
                new Type(LLVMTypeRef.Int1),
                PrivacyType.Public,
                false));
        Classes.Add(Reserved.Char,
            new Int(Reserved.Char,
                new Type(LLVMTypeRef.Int8),
                PrivacyType.Public,
                false));
        Classes.Add(Reserved.UnsignedInt16,
                new Int(Reserved.UnsignedInt16,
                new Type(LLVMTypeRef.Int16),
                PrivacyType.Public,
                false));
        Classes.Add(Reserved.UnsignedInt32,
            new Int(Reserved.UnsignedInt32,
                new Type(LLVMTypeRef.Int32),
                PrivacyType.Public,
                false));
        Classes.Add(Reserved.UnsignedInt64,
            new Int(Reserved.UnsignedInt64,
                new Type(LLVMTypeRef.Int64),
                PrivacyType.Public,
                false));
        Classes.Add(Reserved.SignedInt8,
            new Int(Reserved.SignedInt8,
                new Type(LLVMTypeRef.Int8),
                PrivacyType.Public,
                true));
        Classes.Add(Reserved.SignedInt16,
            new Int(Reserved.SignedInt16,
                new Type(LLVMTypeRef.Int16),
                PrivacyType.Public,
                true));
        Classes.Add(Reserved.SignedInt32,
            new Int(Reserved.SignedInt32,
                new Type(LLVMTypeRef.Int32),
                PrivacyType.Public,
                true));
        Classes.Add(Reserved.SignedInt64,
            new Int(Reserved.SignedInt64,
                new Type(LLVMTypeRef.Int64),
                PrivacyType.Public,
                true));

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
