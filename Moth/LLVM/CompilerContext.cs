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
    public SignedDictionary GlobalFunctions { get; set; } = new SignedDictionary();
    public Dictionary<string, Constant> GlobalConstants { get; set; } = new Dictionary<string, Constant>();
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; set; } = new Dictionary<string, GenericClassNode>();
    public SignedDictionary GenericClasses { get; set; } = new SignedDictionary();
    public Function? CurrentFunction { get; set; }

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
                LLVMTypeRef.Void,
                PrivacyType.Public));
        Classes.Add(Reserved.Float16,
            new Class(Reserved.Float16,
                LLVMTypeRef.Half,
                PrivacyType.Public));
        Classes.Add(Reserved.Float32,
            new Class(Reserved.Float32,
                LLVMTypeRef.Float,
                PrivacyType.Public));
        Classes.Add(Reserved.Float64,
            new Class(Reserved.Float64,
                LLVMTypeRef.Double,
                PrivacyType.Public));
        Classes.Add(Reserved.Bool,
            new Class(Reserved.Bool,
                LLVMTypeRef.Int1,
                PrivacyType.Public));
        Classes.Add(Reserved.Char,
            new Class(Reserved.Char,
                LLVMTypeRef.Int8,
                PrivacyType.Public));
        Classes.Add(Reserved.UnsignedInt16,
            new Class(Reserved.UnsignedInt16,
                LLVMTypeRef.Int16,
                PrivacyType.Public));
        Classes.Add(Reserved.UnsignedInt32,
            new Class(Reserved.UnsignedInt32,
                LLVMTypeRef.Int32,
                PrivacyType.Public));
        Classes.Add(Reserved.UnsignedInt64,
            new Class(Reserved.UnsignedInt64,
                LLVMTypeRef.Int64,
                PrivacyType.Public));
        Classes.Add(Reserved.SignedInt8,
            new Class(Reserved.SignedInt8,
                LLVMTypeRef.Int8,
                PrivacyType.Public));
        Classes.Add(Reserved.SignedInt16,
            new Class(Reserved.SignedInt16,
                LLVMTypeRef.Int16,
                PrivacyType.Public));
        Classes.Add(Reserved.SignedInt32,
            new Class(Reserved.SignedInt32,
                LLVMTypeRef.Int32,
                PrivacyType.Public));
        Classes.Add(Reserved.SignedInt64,
            new Class(Reserved.SignedInt64,
                LLVMTypeRef.Int64,
                PrivacyType.Public));

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
