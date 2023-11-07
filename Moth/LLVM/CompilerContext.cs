using LLVMSharp.Interop;
using Moth.AST.Node;
using Moth.LLVM.Data;

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
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; set; } = new Dictionary<string, GenericClassNode>();
    public GenericDictionary GenericClasses { get; set; } = new GenericDictionary();
    public LlvmFunction? CurrentFunction { get; set; }

    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();

    public CompilerContext(string moduleName)
    {
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(moduleName);
        ModuleName = moduleName;

        InsertDefaultTypes();
    }

    public IntrinsicFunction GetIntrinsic(string name)
    {
        if (_intrinsics.TryGetValue(name, out IntrinsicFunction func))
        {
            return func;
        }
        else
        {
            return CreateIntrinsic(name);
        }
    }

    public Function GetFunction(Signature sig)
    {
        if (GlobalFunctions.TryGetValue(sig, out var func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Function \"{sig}\" does not exist.");
        }
    }

    public Class GetClass(string name)
    {
        if (Classes.TryGetValue(name, out Class @class))
        {
            return @class;
        }
        else
        {
            throw new Exception($"Class \"{name}\" does not exist.");
        }
    }

    private IntrinsicFunction CreateIntrinsic(string name)
    {
        var func = name switch
        {
            "llvm.powi.f32.i32" => new Pow(name, Module, Float.Float32.Type, LLVMTypeRef.Float, LLVMTypeRef.Int32),
            "llvm.powi.f64.i16" => new Pow(name, Module, Float.Float64.Type, LLVMTypeRef.Double, LLVMTypeRef.Int16),
            "llvm.pow.f32" => new Pow(name, Module, Float.Float32.Type, LLVMTypeRef.Float, LLVMTypeRef.Float),
            "llvm.pow.f64" => new Pow(name, Module, Float.Float64.Type, LLVMTypeRef.Double, LLVMTypeRef.Double),
            _ => throw new NotImplementedException(),
        };

        _intrinsics.Add(name, func);
        return func;
    }

    private void InsertDefaultTypes()
    {
        Classes.Add(Reserved.Void,
            new Class(Reserved.Void,
                LLVMTypeRef.Void,
                PrivacyType.Public));
        Classes.Add(Reserved.Float16, Float.Float16);
        Classes.Add(Reserved.Float32, Float.Float32);
        Classes.Add(Reserved.Float64, Float.Float64);
        Classes.Add(Reserved.Bool, UnsignedInt.Bool);
        Classes.Add(Reserved.Char, UnsignedInt.Char);
        Classes.Add(Reserved.UnsignedInt8, UnsignedInt.UInt8);
        Classes.Add(Reserved.UnsignedInt16, UnsignedInt.UInt16);
        Classes.Add(Reserved.UnsignedInt32, UnsignedInt.UInt32);
        Classes.Add(Reserved.UnsignedInt64, UnsignedInt.UInt64);
        Classes.Add(Reserved.SignedInt8, SignedInt.Int8);
        Classes.Add(Reserved.SignedInt16, SignedInt.Int16);
        Classes.Add(Reserved.SignedInt32, SignedInt.Int32);
        Classes.Add(Reserved.SignedInt64, SignedInt.Int64);

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
