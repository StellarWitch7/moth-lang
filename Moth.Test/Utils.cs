using LLVMSharp.Interop;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;

namespace Moth.Test;

internal class Utils
{
    private static bool hasInit = false;
    
    public static unsafe LLVMExecutionEngineRef InitJIT(LLVMCompiler compiler)
    {
        Console.WriteLine("(unsafe) Initializing JIT...");
        
        if (!hasInit)
        {
            LLVMSharp.Interop.LLVM.LinkInMCJIT();
            LLVMSharp.Interop.LLVM.InitializeX86TargetInfo();
            LLVMSharp.Interop.LLVM.InitializeX86Target();
            LLVMSharp.Interop.LLVM.InitializeX86TargetMC();
            LLVMSharp.Interop.LLVM.InitializeX86AsmParser();
            LLVMSharp.Interop.LLVM.InitializeX86AsmPrinter();
        
            LLVMMCJITCompilerOptions options = new LLVMMCJITCompilerOptions { NoFramePointerElim = 1 };
            LLVMSharp.Interop.LLVM.InitializeMCJITCompilerOptions(&options, (nuint)UIntPtr.Size);

            hasInit = true;
        }
        
        return compiler.Module.CreateExecutionEngine();
    }
    
    public static (LLVMCompiler, LLVMExecutionEngineRef) FullCompile(string code)
    {
        List<Token> tokens = Tokenizer.Tokenize(code);
        var context = new ParseContext(tokens);
        ScriptAST ast = ASTGenerator.ProcessScript(context);
        var compiler = new LLVMCompiler("fullcomp", new ScriptAST[] { ast });
        compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        return (compiler, InitJIT(compiler));
    }

    public static string RunMain(LLVMCompiler compiler, LLVMExecutionEngineRef engine)
    {
        return engine.RunFunctionAsMain(compiler.Module.GetNamedFunction("main"),
                0,
                new string[0] {},
                new string[0] {})
            .ToString();
    }
    
    public static string RunFunction(LLVMCompiler compiler, LLVMExecutionEngineRef engine)
    {
        return engine.RunFunction(compiler.Module.GetNamedFunction("fn"), new LLVMGenericValueRef[0] {}).ToString();
    }

    public static string PrependNamespace(string code) => $"namespace unit.test; {code}";

    public static string WrapInMainFunc(string code) => $"public func main() #i32 {{ {code} }}";

    public static string BasicWrap(string code) => PrependNamespace(WrapInMainFunc(code));

    public static string TypedFuncWrap(string code, string type) => PrependNamespace($"public func fn() #{type} {{ {code} }}");

    public static object WrapInInit(string code, string type) => $"public static init() #{type} {{ {code} }}";
}
