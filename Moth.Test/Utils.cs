using LLVMSharp.Interop;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;

namespace Moth.Unit;

internal class Utils
{
    public static LLVMModuleRef FullCompile(string code)
    {
        List<Token> tokens = Tokenizer.Tokenize(code);
        var context = new ParseContext(tokens);
        ScriptAST ast = ASTGenerator.ProcessScript(context);
        var compiler = new LLVMCompiler("fullcomp", new ScriptAST[] { ast });
        compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        return compiler.Module;
    }

    public static string PrependNamespace(string code) => $"namespace unit.test; {code}";

    public static string WrapInMainFunc(string code) => $"func main() #i32 {{ {code} }}";

    public static string BasicWrap(string code) => PrependNamespace(WrapInMainFunc(code));

    public static object WrapInInit(string code, string type) => $"static init() #{type} {{ {code} }}";
}
